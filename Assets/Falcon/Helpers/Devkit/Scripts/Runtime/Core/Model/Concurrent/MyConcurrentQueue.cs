/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public class MyConcurrentQueue<T> : IReadOnlyCollection<T>, ICollection
    {
        private readonly LinkedList<T> _baseList;
        private readonly MyRwLock _lock = new();

        public MyConcurrentQueue()
        {
            _baseList = new LinkedList<T>();
        }

        public MyConcurrentQueue(IEnumerable<T> collection)
        {
            _baseList = new LinkedList<T>(collection);
        }

        public bool IsEmpty => Count == 0;

        bool ICollection.IsSynchronized => true;
        object ICollection.SyncRoot => _lock;

        void ICollection.CopyTo(Array array, int index)
        {
            CopyTo((T[])array, index);
        }

        public int Count => _lock.LockRead(() => _baseList.Count);

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        public IEnumerator<T> GetEnumerator()
        {
            return _lock.LockRead(() => _baseList.ToList().GetEnumerator());
        }

        public void Clear()
        {
            _lock.LockWrite(() => _baseList.Clear());
        }

        public bool Contains(T item)
        {
            return _lock.LockRead(() => _baseList.Contains(item));
        }

        public bool ContainsAll(Collection<T> items)
        {
            return _lock.LockRead(() => items.All(Contains));
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _lock.LockRead(() => _baseList.CopyTo(array, arrayIndex));
        }

        public T Dequeue()
        {
            return _lock.LockWrite(() =>
            {
                var result = _baseList.First.Value;
                _baseList.RemoveFirst();
                return result;
            });
        }

        public void Enqueue(T item)
        {
            _lock.LockWrite(() => _baseList.AddLast(item));
        }

        public void EnqueueAll(IEnumerable<T> items)
        {
            
            _lock.LockWrite(() =>
            {
                foreach (var item in items)
                {
                    _baseList.AddLast(item);
                }
            });
        }

        public T Peek()
        {
            return _lock.LockRead(() => _baseList.First.Value);
        }

        public bool TryDequeue(out T result)
        {
            T temp = default;
            var invoked = _lock.LockWrite(() =>
            {
                if (_baseList.Count <= 0) return false;
                temp = _baseList.First.Value;
                _baseList.RemoveFirst();
                return true;
            });
            result = temp;
            return invoked;
        }

        public bool TryPeek(out T result)
        {
            T temp = default;
            var invoked = _lock.LockRead(() =>
            {
                if (_baseList.Count <= 0) return false;
                temp = _baseList.First.Value;
                return true;
            });
            result = temp;
            return invoked;
        }

        public bool Remove(T item)
        {
            return _lock.LockWrite(() => _baseList.Remove(item));
        }
        
        public bool RemoveAll(IEnumerable<T> items)
        {
            return _lock.LockWrite(() =>
            {
                bool result = false;
                foreach (var item in items)
                {
                    result = _baseList.Remove(item) || result;
                }
                return result;
            });
        }

        public List<T> DrainAll()
        {
            return _lock.LockWrite(() =>
            {
                var result = _baseList.ToList();
                _baseList.Clear();
                return result;
            });
        }
        
        public List<T> Drain(int size)
        {
            return _lock.LockWrite(() =>
            {
                var result = new List<T>();
                for (var i = 0; i < size; i++)
                {
                    if (_baseList.Count == 0) break;
                    result.Add(_baseList.First.Value);
                    _baseList.RemoveFirst();
                }
                return result;
            });
        }
    }
}