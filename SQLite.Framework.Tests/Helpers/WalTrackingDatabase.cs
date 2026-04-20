using System.Runtime.CompilerServices;

namespace SQLite.Framework.Tests.Helpers;

internal class WalTrackingDatabase : TestDatabase
{
    private int activeHolders;
    private int maxConcurrentHolders;

    public int MaxConcurrentLockHolders => maxConcurrentHolders;

    public int LockHoldMilliseconds { get; set; }

    public WalTrackingDatabase([CallerMemberName] string? methodName = null)
        : base(b => b.UseWalMode(), methodName)
    {
    }

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

        if (LockHoldMilliseconds > 0)
        {
            Thread.Sleep(LockHoldMilliseconds);
        }

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
            onDispose();
            inner.Dispose();
        }
    }
}
