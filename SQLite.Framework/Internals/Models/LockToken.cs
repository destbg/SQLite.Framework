namespace SQLite.Framework.Internals.Models;

/// <summary>
/// Marks one acquisition of the connection lock. The acquiring flow stores the token in its
/// async-local slot and re-entrancy checks read <see cref="Active" /> through it, so a lease
/// disposed on another thread deactivates the flag for the flow that acquired it too.
/// </summary>
internal sealed class LockToken
{
    private volatile bool active = true;

    /// <summary>
    /// True while the lock acquisition this token marks is still held.
    /// </summary>
    public bool Active => active;

    /// <summary>
    /// Marks the acquisition as released.
    /// </summary>
    public void Deactivate()
    {
        active = false;
    }
}
