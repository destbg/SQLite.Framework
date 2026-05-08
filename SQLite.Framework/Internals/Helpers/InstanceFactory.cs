namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Builds a new instance of <typeparamref name="T" /> with <c>new T()</c>. When <typeparamref name="T" />
/// has a public parameterless constructor, the JIT turns this into a direct allocation, which is
/// faster than calling <see cref="Activator.CreateInstance(Type)" />.
/// </summary>
internal sealed class InstanceFactory<T> : IInstanceFactory
    where T : class, new()
{
    public object Create()
    {
        return new T();
    }
}
