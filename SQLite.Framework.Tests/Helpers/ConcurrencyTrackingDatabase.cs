using System.Runtime.CompilerServices;

namespace SQLite.Framework.Tests.Helpers;

/// <summary>
/// A test database that counts how many callers hold the connection lock at the same time.
/// After the test, assert <see cref="MaxConcurrentLockHolders"/> equals 1 to prove write serialization,
/// or assert <see cref="MaxConcurrentReadHolders"/> is greater than 1 to prove reads can run in parallel.
/// </summary>
internal class ConcurrencyTrackingDatabase : TestDatabase
{
    private int activeHolders;
    private int maxConcurrentHolders;
    private int activeReadHolders;
    private int maxConcurrentReadHolders;

    public int MaxConcurrentLockHolders => maxConcurrentHolders;
    public int MaxConcurrentReadHolders => maxConcurrentReadHolders;

    public ConcurrencyTrackingDatabase([CallerMemberName] string? methodName = null)
        : base(methodName) { }

    public override IDisposable Lock()
    {
        IDisposable inner = base.Lock();

        int current = Interlocked.Increment(ref activeHolders);

        // Atomically update the running maximum.
        int snapshot;
        do
        {
            snapshot = maxConcurrentHolders;
            if (current <= snapshot)
            {
                break;
            }
        }
        while (Interlocked.CompareExchange(ref maxConcurrentHolders, current, snapshot) != snapshot);

        // Decrement BEFORE releasing the inner lock so that a thread unblocked by the
        // release cannot increment the counter before we decrement, which would produce
        // a false max-of-2 reading even when serialization is correct.
        return new TrackingLock(inner, () => Interlocked.Decrement(ref activeHolders));
    }

    public override IDisposable ReadLock()
    {
        IDisposable inner = base.ReadLock();

        int current = Interlocked.Increment(ref activeReadHolders);

        int snapshot;
        do
        {
            snapshot = maxConcurrentReadHolders;
            if (current <= snapshot)
            {
                break;
            }
        }
        while (Interlocked.CompareExchange(ref maxConcurrentReadHolders, current, snapshot) != snapshot);

        return new TrackingLock(inner, () => Interlocked.Decrement(ref activeReadHolders));
    }

    private sealed class TrackingLock : IDisposable
    {
        private readonly IDisposable inner;
        private readonly Action onDispose;

        public TrackingLock(IDisposable inner, Action onDispose)
        {
            this.inner = inner;
            this.onDispose = onDispose;
        }

        public void Dispose()
        {
            onDispose(); // decrement while still holding the lock
            inner.Dispose(); // then release the lock
        }
    }
}
