/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using Falcon.Modules.Core.SaveLoad.Runtime;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    public class SaveLoadLibUnsentRepository : IUnsentLogRepository
    {
        private const string UNSENT_LOGS_KEY = "F_BIGDATA_UNSENT_LOGS";
        private readonly MyConcurrentQueue<DataWrapper> _queue = new();
        private readonly AtomicRef<bool> _stopFlag = new(false);

        public SaveLoadLibUnsentRepository()
        {
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
            var loaded = SaveLoadHandler.Load(UNSENT_LOGS_KEY, new List<DataWrapper>());
            _queue.EnqueueAll(loaded);
            AnalyticLogger.Instance.Info($"Unsent requests Load success : {loaded.Count} requests");
        }

        private void SaveQueue()
        {
            var unsentData = _queue.DrainAll();
            SaveLoadHandler.Save(UNSENT_LOGS_KEY, unsentData);
            AnalyticLogger.Instance.Info("Unsent requests Save success : " + unsentData.Count + " requests");
        }
    }
    
    
    public class ReserveUnsentRepositoryDisabler : ISingletonShellSourceDisabler
    {
        public int Priority => 0;

        public IEnumerable<Type> DisablingSources
        {
            get
            {
                yield return typeof(UnsentLogRepository);
            }
        }
    }
}