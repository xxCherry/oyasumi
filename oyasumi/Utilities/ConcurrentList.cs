using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace oyasumi.Utilities
{
    public class ConcurrentList<T> : IDisposable, IEnumerable
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly List<T> _list = new List<T>();

        public void Add(T item)
        {
            try
            {
                _lock.EnterWriteLock();
                _list.Add(item);
            }
            finally
            {
                if (_lock.IsWriteLockHeld) 
                    _lock.ExitWriteLock();
            }
        }

        public void Clear()
        {
            try
            {
                _lock.EnterWriteLock();
                _list.Clear();
            }
            finally
            {
                if (_lock.IsWriteLockHeld) 
                    _lock.ExitWriteLock();
            }
        }

        public bool Contains(T item)
        {
            try
            {
                _lock.EnterReadLock();
                return _list.Contains(item);
            }
            finally
            {
                if (_lock.IsReadLockHeld) 
                    _lock.ExitReadLock();
            }
        }

        public bool Remove(T item)
        {
            try
            {
                _lock.EnterWriteLock();
                return _list.Remove(item);
            }
            finally
            {
                if (_lock.IsWriteLockHeld) 
                    _lock.ExitWriteLock();
            }
        }

        public int Count
        {
            get
            {
                try
                {
                    _lock.EnterReadLock();
                    return _list.Count;
                }
                finally
                {
                    if (_lock.IsReadLockHeld) 
                        _lock.ExitReadLock();
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public void Dispose()
        {
            if (_lock != null) 
                _lock.Dispose();
        }
    }
}