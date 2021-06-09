﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace BrightWire.Cuda.Helper
{
    /// <summary>
    /// A hash set that can be accessed by more than one thread at the same time
    /// </summary>
    /// <typeparam name="T">The wrapped type</typeparam>
    class ThreadSafeHashSet<T> : IDisposable
    {
        readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        readonly HashSet<T> _hashSet = new HashSet<T>();

        ~ThreadSafeHashSet()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
	            _lock?.Dispose();
            }
        }

        public bool Add(T item)
        {
            _lock.EnterWriteLock();
            try {
                return _hashSet.Add(item);
            }
            finally {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }

        public void Clear()
        {
            _lock.EnterWriteLock();
            try {
                _hashSet.Clear();
            }
            finally {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }

        public bool Contains(T item)
        {
            _lock.EnterReadLock();
            try {
                return _hashSet.Contains(item);
            }
            finally {
                if (_lock.IsReadLockHeld) _lock.ExitReadLock();
            }
        }

        public bool Remove(T item)
        {
            _lock.EnterWriteLock();
            try {
                return _hashSet.Remove(item);
            }
            finally {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }

        public int Count
        {
            get
            {
                _lock.EnterReadLock();
                try {
                    return _hashSet.Count;
                }
                finally {
                    if (_lock.IsReadLockHeld) _lock.ExitReadLock();
                }
            }
        }

        public void ForEach(Action<T> callback)
        {
            _lock.EnterReadLock();
            try {
                foreach (var item in _hashSet)
                    callback(item);
            }
            finally {
                if (_lock.IsReadLockHeld) _lock.ExitReadLock();
            }
        }

        public bool TryPop(out T ret)
        {
            _lock.EnterWriteLock();
            try {
                if (_hashSet.Any()) {
                    ret = _hashSet.First();
                    return _hashSet.Remove(ret);
                }
                ret = default(T);
                return false;
            }
            finally {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }
    }
}
