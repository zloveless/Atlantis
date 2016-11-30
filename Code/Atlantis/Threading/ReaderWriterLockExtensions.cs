// -----------------------------------------------------------------------------
//  <copyright file="RWLockExtensions.cs" author="Zack Loveless">
//      Copyright (c) Zack Loveless All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Threading
{
    using System;
    using System.Threading;

    /// <summary>
    /// 
    /// </summary>
    public static class ReaderWriterLockExtensions
    {
        // Credit: http://theburningmonk.com/2010/02/threading-using-readerwriterlockslim/

        #region Nested type: ReadLockToken

        private sealed class ReadLockToken : IDisposable
        {
            private ReaderWriterLockSlim _sync;

            public ReadLockToken(ReaderWriterLockSlim sync)
            {
                _sync = sync;
                sync.EnterReadLock();
            }

            #region Implementation of IDisposable

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                if (_sync != null)
                {
                    _sync.ExitReadLock();
                    _sync = null;
                }
            }

            #endregion
        }

        #endregion

        #region Nested type: WriteLockToken

        private sealed class WriteLockToken : IDisposable
        {
            private ReaderWriterLockSlim _sync;

            public WriteLockToken(ReaderWriterLockSlim sync)
            {
                _sync = sync;
                sync.EnterWriteLock();
            }

            #region Implementation of IDisposable

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                if (_sync != null)
                {
                    _sync.ExitWriteLock();
                    _sync = null;
                }
            }

            #endregion
        }

        #endregion
        
        /// <summary>
        /// Opens a read-lock using the specified ReaderWriterLockSlim.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IDisposable Read(this ReaderWriterLockSlim source)
        {
            return new ReadLockToken(source);
        }
        
        /// <summary>
        /// Opens a write-lock using the specified ReaderWriterLockSlim.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IDisposable Write(this ReaderWriterLockSlim source)
        {
            return new WriteLockToken(source);
        }
    }
}
