using System.Runtime.CompilerServices;

namespace SQLite.Framework.Tests.Helpers;

/// <summary>
/// A test database that counts how many callers hold the connection lock at the same time.
/// After the test, assert <see cref="MaxConcurrentLockHolders"/> equals 1 to prove serialization.
/// </summary>
internal class ConcurrencyTrackingDatabase : TestDatabase
{
    private int activeHolders;
    private int maxConcurrentHolders;

    public int MaxConcurrentLockHolders => maxConcurrentHolders;

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
