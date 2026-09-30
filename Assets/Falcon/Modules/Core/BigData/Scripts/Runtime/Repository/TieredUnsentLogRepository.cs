/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-30
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Falcon.Helpers.Devkit;
using Falcon.Modules.Core.SaveLoad.Runtime;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Queue log chưa gửi 2 tầng, FIFO xuyên tầng: RAM giữ tối đa <see cref="RAM_CAP"/> message đầu queue
    /// (phần được flush gửi đi), phần dư spill xuống segment file mã hoá AES trong persistentDataPath.
    /// <br/>- Bình thường (queue nhỏ vì flush 15s rút liên tục): mọi thứ nằm RAM, không có IO file nào.
    /// <br/>- Offline / server outage kéo dài: RAM đầy → dồn message thành segment (ghi 1 lần duy nhất khi đủ
    ///   <see cref="SEGMENT_SIZE"/> message, không có file dở dang). Trần <see cref="MAX_SEGMENTS"/> segment,
    ///   vượt thì bỏ segment cũ nhất (log warning).
    /// <br/>- App pause: RAM snapshot xuống head file + overflow dở dang chốt thành segment → swipe-kill không mất log.
    ///   Head file bị xoá khi app resume (process còn sống thì RAM là nguồn chân lý) để tránh gửi trùng;
    ///   crash giữa session chỉ mất tối đa phần RAM, mọi thứ đã spill vẫn còn.
    /// <br/>- Remove/RemoveAll chỉ tìm trong RAM + overflow (không quét segment trên đĩa).
    /// </summary>
    public class TieredUnsentLogRepository : IUnsentLogRepository
    {
        private const int RAM_CAP = 500;
        private const int SEGMENT_SIZE = 200;
        private const int MAX_SEGMENTS = 500;
        private const string DEFAULT_FOLDER = "f_bigdata_unsent";
        private const string HEAD_FILE_NAME = "head.json";
        private const string SEGMENT_PREFIX = "segment_";
        private const string LEGACY_SAVE_LOAD_KEY = "F_BIGDATA_UNSENT_LOGS";

        private readonly FLocalFileRepository _fileRepository;
        private readonly string _folder;
        private readonly object _lock = new();
        private readonly LinkedList<DataWrapper> _ram = new();
        private readonly List<DataWrapper> _overflow = new();
        private readonly SortedSet<long> _segmentIndexes = new();
        private long _nextSegmentIndex;
        private bool _stopping;

        [SingletonConstructor]
        public TieredUnsentLogRepository(FLocalFileRepository fileRepository)
            : this(fileRepository, DEFAULT_FOLDER, true)
        {
        }

        /// <param name="folder">Folder chứa head/segment file — tham số hoá để test không đụng data thật.</param>
        /// <param name="migrateLegacy">Import queue cũ từ SaveLoadHandler (bản SaveLoadLibUnsentRepository) nếu có.</param>
        public TieredUnsentLogRepository(FLocalFileRepository fileRepository, string folder, bool migrateLegacy)
        {
            _fileRepository = fileRepository;
            _folder = folder;
            lock (_lock)
            {
                ScanSegments();
                LoadHead();
                if (migrateLegacy) MigrateFromSaveLoadLib();
            }
        }

        public void OnPostStop()
        {
            lock (_lock)
            {
                if (_stopping) return;
                _stopping = true;
                PersistNoLock();
            }
        }

        public void OnPreContinue()
        {
            lock (_lock)
            {
                if (!_stopping) return;
                _stopping = false;
                // Process còn sống, RAM là nguồn chân lý — xoá head snapshot để lần chạy sau
                // không load lại những message có thể đã được gửi sau khi resume (tránh gửi trùng).
                DeleteHeadNoLock();
            }
        }

        public void Enqueue(DataWrapper unsentData)
        {
            lock (_lock)
            {
                EnqueueNoLock(unsentData);
                if (_stopping) PersistNoLock();
            }
        }

        public void EnqueueAll(IEnumerable<DataWrapper> unsentData)
        {
            lock (_lock)
            {
                foreach (var wrapper in unsentData) EnqueueNoLock(wrapper);
                if (_stopping) PersistNoLock();
            }
        }

        public bool Remove(DataWrapper unsentData)
        {
            lock (_lock)
            {
                var removed = _ram.Remove(unsentData) || _overflow.Remove(unsentData);
                if (removed && _stopping) PersistNoLock();
                return removed;
            }
        }

        public bool RemoveAll(IEnumerable<DataWrapper> unsentData)
        {
            lock (_lock)
            {
                var removed = false;
                foreach (var wrapper in unsentData)
                    removed |= _ram.Remove(wrapper) || _overflow.Remove(wrapper);
                if (removed && _stopping) PersistNoLock();
                return removed;
            }
        }

        public List<DataWrapper> DrainAll()
        {
            lock (_lock)
            {
                var result = new List<DataWrapper>();
                RefillFromDiskNoLock();
                while (_ram.Count > 0)
                {
                    result.AddRange(_ram);
                    _ram.Clear();
                    RefillFromDiskNoLock();
                }
                if (_stopping) PersistNoLock();
                return result;
            }
        }

        public List<DataWrapper> Drain(int size)
        {
            lock (_lock)
            {
                var result = new List<DataWrapper>(Math.Min(size, _ram.Count));
                while (result.Count < size && _ram.Count > 0)
                {
                    result.Add(_ram.First.Value);
                    _ram.RemoveFirst();
                }
                RefillFromDiskNoLock();
                if (_stopping) PersistNoLock();
                return result;
            }
        }

        public List<DataWrapper> PeekAll()
        {
            lock (_lock)
            {
                RefillFromDiskNoLock();
                return new List<DataWrapper>(_ram);
            }
        }

        private void EnqueueNoLock(DataWrapper wrapper)
        {
            // Có bất kỳ data nào phía sau RAM (segment/overflow) thì message mới phải xếp sau đó để giữ FIFO.
            if (_segmentIndexes.Count == 0 && _overflow.Count == 0 && _ram.Count < RAM_CAP)
            {
                _ram.AddLast(wrapper);
                return;
            }

            _overflow.Add(wrapper);
            if (_overflow.Count >= SEGMENT_SIZE) FlushOverflowToSegmentNoLock();
        }

        private void RefillFromDiskNoLock()
        {
            while (_ram.Count + SEGMENT_SIZE <= RAM_CAP && _segmentIndexes.Count > 0)
            {
                var index = _segmentIndexes.Min;
                try
                {
                    var file = SegmentFile(index);
                    var loaded = file.LoadJson<DataWrapper[]>();
                    foreach (var wrapper in loaded) _ram.AddLast(wrapper);
                    file.Delete();
                    AnalyticLogger.Instance.Info($"Unsent segment {index} refilled : {loaded.Length} requests");
                }
                catch (Exception e)
                {
                    // Segment hỏng/không đọc được: bỏ đi để không kẹt queue mãi ở 1 file lỗi.
                    AnalyticLogger.Instance.Error(e);
                    try
                    {
                        SegmentFile(index).Delete();
                    }
                    catch (Exception deleteError)
                    {
                        AnalyticLogger.Instance.Error(deleteError);
                    }
                }

                _segmentIndexes.Remove(index);
            }

            if (_ram.Count == 0 && _segmentIndexes.Count == 0 && _overflow.Count > 0)
            {
                foreach (var wrapper in _overflow) _ram.AddLast(wrapper);
                _overflow.Clear();
            }
        }

        private void FlushOverflowToSegmentNoLock()
        {
            if (_overflow.Count == 0) return;
            try
            {
                var index = _nextSegmentIndex;
                SegmentFile(index).Save(_overflow);
                _nextSegmentIndex++;
                _segmentIndexes.Add(index);
                _overflow.Clear();
                EnforceSegmentCapNoLock();
            }
            catch (Exception e)
            {
                // Ghi hỏng thì giữ overflow trong RAM, lần flush sau thử lại.
                AnalyticLogger.Instance.Error(e);
            }
        }

        private void EnforceSegmentCapNoLock()
        {
            while (_segmentIndexes.Count > MAX_SEGMENTS)
            {
                var oldest = _segmentIndexes.Min;
                try
                {
                    SegmentFile(oldest).Delete();
                }
                catch (Exception e)
                {
                    AnalyticLogger.Instance.Error(e);
                }

                _segmentIndexes.Remove(oldest);
                AnalyticLogger.Instance.Warning(
                    $"Unsent queue hit cap of {MAX_SEGMENTS} segments, dropped oldest segment {oldest} (~{SEGMENT_SIZE} requests)");
            }
        }

        private void PersistNoLock()
        {
            SaveHeadNoLock();
            FlushOverflowToSegmentNoLock();
        }

        private void SaveHeadNoLock()
        {
            try
            {
                HeadFile().Save(_ram.ToList());
                AnalyticLogger.Instance.Info("Unsent requests Save success : " + _ram.Count + " requests");
            }
            catch (Exception e)
            {
                AnalyticLogger.Instance.Error(e);
            }
        }

        private void LoadHead()
        {
            try
            {
                var head = HeadFile();
                if (!head.Exists()) return;
                var loaded = head.LoadJson<DataWrapper[]>();
                foreach (var wrapper in loaded) _ram.AddLast(wrapper);
                head.Delete();
                AnalyticLogger.Instance.Info($"Unsent requests Load success : {loaded.Length} requests");
            }
            catch (Exception e)
            {
                AnalyticLogger.Instance.Error(e);
            }
        }

        private void DeleteHeadNoLock()
        {
            try
            {
                var head = HeadFile();
                if (head.Exists()) head.Delete();
            }
            catch (Exception e)
            {
                AnalyticLogger.Instance.Error(e);
            }
        }

        private void ScanSegments()
        {
            foreach (var file in _fileRepository.ListFilesAtFolderExact(_folder))
            {
                var name = Path.GetFileName(file.FilePath);
                if (!name.StartsWith(SEGMENT_PREFIX, StringComparison.Ordinal)) continue;
                var numberPart = Path.GetFileNameWithoutExtension(name).Substring(SEGMENT_PREFIX.Length);
                if (long.TryParse(numberPart, NumberStyles.None, CultureInfo.InvariantCulture, out var index))
                    _segmentIndexes.Add(index);
            }

            _nextSegmentIndex = _segmentIndexes.Count > 0 ? _segmentIndexes.Max + 1 : 0;
        }

        /// <summary>
        /// Import queue còn sót của bản cũ (SaveLoadLibUnsentRepository lưu qua SaveLoadHandler)
        /// để user update app không mất log đang chờ gửi. Best effort: SaveLoadHandler có thể chưa
        /// sẵn sàng nếu constructor chạy quá sớm — khi đó giữ nguyên key cũ, lần chạy sau thử lại.
        /// </summary>
        private void MigrateFromSaveLoadLib()
        {
            try
            {
                var legacy = SaveLoadHandler.Load(LEGACY_SAVE_LOAD_KEY, new List<DataWrapper>());
                if (legacy.Count == 0) return;
                foreach (var wrapper in legacy) EnqueueNoLock(wrapper);
                SaveLoadHandler.Save(LEGACY_SAVE_LOAD_KEY, new List<DataWrapper>());
                AnalyticLogger.Instance.Info($"Migrated {legacy.Count} unsent requests from SaveLoad lib storage");
            }
            catch (Exception e)
            {
                AnalyticLogger.Instance.Error(e);
            }
        }

        private IFFile HeadFile()
        {
            return _fileRepository.GetFile(_folder + "/" + HEAD_FILE_NAME).AesSimpleEncrypt();
        }

        private IFFile SegmentFile(long index)
        {
            return _fileRepository
                .GetFile(_folder + "/" + SEGMENT_PREFIX + index.ToString("D10", CultureInfo.InvariantCulture) + ".json")
                .AesSimpleEncrypt();
        }
    }

    public class SaveLoadLibUnsentRepositoryDisabler : ISingletonShellSourceDisabler
    {
        public int Priority => 0;

        public IEnumerable<Type> DisablingSources
        {
            get
            {
                yield return typeof(SaveLoadLibUnsentRepository);
            }
        }
    }
}
