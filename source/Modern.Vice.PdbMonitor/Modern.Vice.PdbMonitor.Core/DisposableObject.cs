using System;

namespace Modern.Vice.PdbMonitor.Core
{
    /// <summary>
    /// Base class for disposing.
    /// </summary>
    public abstract class DisposableObject : IDisposable
    {
        protected bool disposed;

        protected virtual void Dispose(bool disposing)
        {
            disposed = true;
        }

        public bool IsDisposed
        {
            get { return disposed; }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
