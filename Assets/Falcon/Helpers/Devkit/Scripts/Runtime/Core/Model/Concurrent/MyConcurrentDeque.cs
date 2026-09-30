/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public class MyConcurrentDeque<T> : IReadOnlyCollection<T>, ICollection
    {
        private readonly LinkedList<T> _baseList;
        private readonly MyRwLock _lock = new();

        public MyConcurrentDeque()
        {
            _baseList = new LinkedList<T>();
        }

        public MyConcurrentDeque(IEnumerable<T> collection)
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

        public void CopyTo(T[] array, int arrayIndex)
        {
            _lock.LockRead(() => _baseList.CopyTo(array, arrayIndex));
        }

        public T DequeueFirst()
        {
            return _lock.LockWrite(() =>
            {
                var result = _baseList.First.Value;
                _baseList.RemoveFirst();
                return result;
            });
        }

        public T DequeueLast()
        {
            return _lock.LockWrite(() =>
            {
                var result = _baseList.Last.Value;
                _baseList.RemoveLast();
                return result;
            });
        }

        public void EnqueueLast(T item)
        {
            _lock.LockWrite(() =>
            {
                _baseList.AddLast(item);
            });
        }

        public void EnqueueFirst(T item)
        {
            _lock.LockWrite(() =>
            {
                _baseList.AddFirst(item);
            });
        }

        public T PeekFirst()
        {
            return _lock.LockRead(() => _baseList.First.Value);
        }

        public T PeekLast()
        {
            return _lock.LockRead(() => _baseList.Last.Value);
        }

        public bool TryDequeueFirst(out T result)
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

        public bool TryPeekFirst(out T result)
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

        public bool TryDequeueLast(out T result)
        {
            T temp = default;
            var invoked = _lock.LockWrite(() =>
            {
                if (_baseList.Count <= 0) return false;
                temp = _baseList.Last.Value;
                _baseList.RemoveFirst();
                return true;
            });
            result = temp;
            return invoked;
        }

        public bool TryPeekLast(out T result)
        {
            T temp = default;
            var invoked = _lock.LockRead(() =>
            {
                if (_baseList.Count <= 0) return false;
                temp = _baseList.Last.Value;
                return true;
            });
            result = temp;
            return invoked;
        }

        public bool Remove(T item)
        {
            return _lock.LockWrite(() => _baseList.Remove(item));
        }

        public IEnumerable<T> DrainAll()
        {
            return _lock.LockWrite(() =>
            {
                IEnumerable<T> result = _baseList.ToList();
                _baseList.Clear();
                return result;
            });
        }
    }
}