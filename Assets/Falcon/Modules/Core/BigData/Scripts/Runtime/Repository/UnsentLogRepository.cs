/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    public class UnsentLogRepository : IUnsentLogRepository
    {
        private const string kUnsetLogsFile = "UnsentLogs.txt";
        private readonly MyConcurrentQueue<DataWrapper> _queue = new();
        private readonly FLocalFile _saveFile;
        private readonly AtomicRef<bool> _stopFlag = new(false);

        public UnsentLogRepository(FLocalFileRepository fileRepository)
        {
            _saveFile = fileRepository.GetFile(kUnsetLogsFile);
            LoadQueue();
        }

        public void OnPreContinue()
        {
            _stopFlag.Compute(stopping =>
            {
                if (!stopping) return false;
                LoadQueue();
                return false;
            });
        }

        public void OnPostStop()
        {
            _stopFlag.Compute(stopping =>
            {
                if (stopping) return true;
                SaveQueue();
                return true;
            });
        }

        public void Enqueue(DataWrapper unsentData)
        {
            _stopFlag.Compute(stopping =>
            {
                if (stopping) LoadQueue();
                _queue.Enqueue(unsentData);
                if (stopping) SaveQueue();
                return stopping;
            });
        }

        public void EnqueueAll(IEnumerable<DataWrapper> unsentData)
        {
            _stopFlag.Compute(stopping =>
            {
                if (stopping) LoadQueue();
                _queue.EnqueueAll(unsentData);
                if (stopping) SaveQueue();
                return stopping;
            });
        }

        public bool Remove(DataWrapper unsentData)
        {
            bool result = false;
            _stopFlag.Compute(stopping =>
            {
                if (stopping) LoadQueue();
                result = _queue.Remove(unsentData);
                if (stopping) SaveQueue();
                return stopping;
            });

            return result;
        }
        
        public bool RemoveAll(IEnumerable<DataWrapper> unsentData)
        {
            bool result = false;
            
            _stopFlag.Compute(stopping =>
            {
                if (stopping) LoadQueue();
                result = _queue.RemoveAll(unsentData);
                if (stopping) SaveQueue();
                return stopping;
            });
            return result;
        }

        public List<DataWrapper> DrainAll()
        {
            List<DataWrapper> result = new();
            _stopFlag.Compute(stopping =>
            {
                if (stopping) LoadQueue();
                result.AddRange(_queue.DrainAll());
                if (stopping) SaveQueue();
                return stopping;
            });
            return result;
        }

        public List<DataWrapper> Drain(int size)
        {
            List<DataWrapper> result = new();
            _stopFlag.Compute(stopping =>
            {
                if (stopping) LoadQueue();
                result.AddRange(_queue.Drain(size));
                if (stopping) SaveQueue();
                return stopping;
            });
            return result;
        }

        public List<DataWrapper> PeekAll()
        {
            List<DataWrapper> result = new();
            _stopFlag.Compute(stopping =>
            {
                if (stopping) LoadQueue();
                result.AddRange(_queue);
                if (stopping) SaveQueue();
                return stopping;
            });
            return result;
        }

        private void LoadQueue()
        {
            try
            {
                if (!_saveFile.Exists()) return;
                var loaded = _saveFile.LoadJson<DataWrapper[]>();
                _queue.EnqueueAll(loaded);
                AnalyticLogger.Instance.Info($"Unsent requests Load success : {loaded.Length} requests");
            }
            catch ( Exception e )
            {
                AnalyticLogger.Instance.Error(e);
            }
        }

        private void SaveQueue()
        {
            try
            {
                var unsentData = _queue.DrainAll();
                _saveFile.Save(unsentData);
                AnalyticLogger.Instance.Info("Unsent requests Save success : " + unsentData.Count + " requests");
            }
            catch ( Exception e )
            {
                AnalyticLogger.Instance.Error(e);
            }
        }
    }
}