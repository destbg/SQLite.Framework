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

    public int ReadHoldMilliseconds { get; set; }

    public ConcurrencyTrackingDatabase([CallerMemberName] string? methodName = null)
        : base(methodName) { }

    public override IDisposable Lock()
    {
        IDisposable inner = base.Lock();

        int current = Interlocked.Increment(ref activeHolders);

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

        if (ReadHoldMilliseconds > 0)
        {
            Thread.Sleep(ReadHoldMilliseconds);
        }

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
            onDispose();
            inner.Dispose();
        }
    }
}
