using System.Collections.Concurrent;

namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Answers whether a subclass overrides a virtual method, so a wrapper can dispatch through the
/// override instead of the base fast path. Results are cached per type, declaring type, method
/// name and parameter count. When NativeAOT trims the reflection metadata the lookup returns
/// null and the method is treated as overridden, which is safe because calling the virtual
/// member on a subclass that does not override it still runs the base implementation.
/// </summary>
internal static class MethodOverrideCache
{
    private static readonly ConcurrentDictionary<(Type Type, Type DeclaringType, string Name, int ParameterCount), bool> overrides = new();

    public static bool IsOverridden(Type type, Type declaringType, string name, params Type[] parameterTypes)
    {
        return IsOverridden(type, declaringType, name, BindingFlags.Instance | BindingFlags.Public, parameterTypes);
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "When NativeAOT trims the metadata, GetMethod returns null and the method is treated as overridden, which is safe.")]
    public static bool IsOverridden(Type type, Type declaringType, string name, BindingFlags flags, params Type[] parameterTypes)
    {
        (Type, Type, string, int) key = (type, declaringType, name, parameterTypes.Length);
        if (overrides.TryGetValue(key, out bool overridden))
        {
            return overridden;
        }

        MethodInfo? method = type.GetMethod(name, flags, parameterTypes);
        overridden = method is null || method.DeclaringType != declaringType;
        overrides[key] = overridden;
        return overridden;
    }
}
