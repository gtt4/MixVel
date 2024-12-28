namespace MixVel.Cache
{
    public class ExecutionStrategy
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private int _isInvalidating = 0;

        // Allows concurrent Add operations unless an Invalidate is running.
        public void ExecuteAdd(Action addAction)
        {
            _lock.EnterReadLock();
            try
            {
                addAction();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        // Executes Invalidate exclusively, blocking new Adds and waiting for ongoing Adds to complete.
        public bool TryExecuteInvalidate(Action invalidateAction)
        {
            if (Interlocked.Exchange(ref _isInvalidating, 1) == 1)
            {
                // Another Invalidate is running; skip execution.
                return false;
            }

            _lock.EnterWriteLock();
            try
            {
                invalidateAction();
                return true;
            }
            finally
            {
                _isInvalidating = 0;
                _lock.ExitWriteLock();
            }
        }
    }
}
