using System.Collections.Concurrent;

namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Holds per-type reflection data used by the runtime fallback in <see cref="BuildQueryObject" />.
/// Looking up <see cref="PropertyInfo" />, <see cref="Nullable.GetUnderlyingType" />,
/// <see cref="TypeHelpers.IsSimple" />, and so on, only once per type and reusing the result for
/// every row is much faster than calling <see cref="Type.GetProperties()" /> on each row.
/// </summary>
internal static class ReflectionMaterializerCache
{
    private static readonly ConcurrentDictionary<(Type Type, SQLiteOptions Options), MaterializerPlan> planCache = new();

    [UnconditionalSuppressMessage("AOT", "IL2067", Justification = "Type comes from the entity surface; users keep their entities reachable.")]
    [UnconditionalSuppressMessage("AOT", "IL2077", Justification = "Type comes from the entity surface; users keep their entities reachable.")]
    public static MaterializerPlan GetPlan([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type, SQLiteOptions options)
    {
        return planCache.GetOrAdd((type, options), static key => Build(key.Type, key.Options));
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Type comes from the entity surface; users keep their entities reachable.")]
    private static MaterializerPlan Build([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type, SQLiteOptions options)
    {
        PropertyInfo[] all = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        List<PropertySlot> slots = new(all.Length);

        foreach (PropertyInfo prop in all)
        {
            if (!prop.CanWrite)
            {
                continue;
            }

            Type propType = prop.PropertyType;
            Type targetType = Nullable.GetUnderlyingType(propType) ?? propType;
            bool isSimple = TypeHelpers.IsSimple(propType, options);
            bool isEnum = targetType.IsEnum;

            slots.Add(new PropertySlot
            {
                Property = prop,
                Name = prop.Name,
                PropertyType = propType,
                TargetType = targetType,
                IsSimple = isSimple,
                IsEnum = isEnum,
                EnumUnderlyingType = isEnum ? Enum.GetUnderlyingType(targetType) : null,
                Setter = CreateSetter(prop),
                Assigner = CreateAssigner(prop, propType, targetType, options),
            });
        }

        return new MaterializerPlan
        {
            Slots = slots.ToArray(),
            Factory = CreateFactory(type),
        };
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Falls back to Activator.CreateInstance when MakeGenericType is unsupported under NativeAOT.")]
    [UnconditionalSuppressMessage("AOT", "IL2055", Justification = "Falls back to Activator.CreateInstance when MakeGenericType is unsupported under NativeAOT.")]
    private static IInstanceFactory? CreateFactory([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type)
    {
        if (type.IsValueType || !RuntimeFeature.IsDynamicCodeSupported)
        {
            return null;
        }

        if (type.GetConstructor(Type.EmptyTypes) == null)
        {
            return null;
        }

        Type factoryType = typeof(InstanceFactory<>).MakeGenericType(type);
        return (IInstanceFactory)Activator.CreateInstance(factoryType)!;
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Falls back to the boxed setter when MakeGenericMethod is unsupported under NativeAOT.")]
    [UnconditionalSuppressMessage("AOT", "IL2060", Justification = "Falls back to the boxed setter when MakeGenericMethod is unsupported under NativeAOT.")]
    private static Action<sqlite3_stmt, int, object>? CreateAssigner(PropertyInfo prop, Type propType, Type targetType, SQLiteOptions options)
    {
        Type declaringType = prop.DeclaringType!;
        if (declaringType.IsValueType || !RuntimeFeature.IsDynamicCodeSupported)
        {
            return null;
        }

        if (propType != targetType)
        {
            return null;
        }

        if (options.TypeConverters.ContainsKey(targetType))
        {
            return null;
        }

        string? helperName = propType switch
        {
            _ when propType == typeof(int) => nameof(MakeIntAssigner),
            _ when propType == typeof(long) => nameof(MakeLongAssigner),
            _ when propType == typeof(double) => nameof(MakeDoubleAssigner),
            _ when propType == typeof(string) => nameof(MakeStringAssigner),
            _ when propType == typeof(bool) => nameof(MakeBoolAssigner),
            _ => null
        };

        if (helperName == null)
        {
            return null;
        }

        MethodInfo helper = typeof(ReflectionMaterializerCache)
            .GetMethod(helperName, BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(declaringType);
        return (Action<sqlite3_stmt, int, object>)helper.Invoke(null, [prop.SetMethod!])!;
    }

    private static Action<sqlite3_stmt, int, object> MakeIntAssigner<TInstance>(MethodInfo setMethod) where TInstance : class
    {
        Action<TInstance, int> setter = setMethod.CreateDelegate<Action<TInstance, int>>();
        return (stmt, idx, instance) => setter((TInstance)instance, raw.sqlite3_column_int(stmt, idx));
    }

    private static Action<sqlite3_stmt, int, object> MakeLongAssigner<TInstance>(MethodInfo setMethod) where TInstance : class
    {
        Action<TInstance, long> setter = setMethod.CreateDelegate<Action<TInstance, long>>();
        return (stmt, idx, instance) => setter((TInstance)instance, raw.sqlite3_column_int64(stmt, idx));
    }

    private static Action<sqlite3_stmt, int, object> MakeDoubleAssigner<TInstance>(MethodInfo setMethod) where TInstance : class
    {
        Action<TInstance, double> setter = setMethod.CreateDelegate<Action<TInstance, double>>();
        return (stmt, idx, instance) => setter((TInstance)instance, raw.sqlite3_column_double(stmt, idx));
    }

    private static Action<sqlite3_stmt, int, object> MakeBoolAssigner<TInstance>(MethodInfo setMethod) where TInstance : class
    {
        Action<TInstance, bool> setter = setMethod.CreateDelegate<Action<TInstance, bool>>();
        return (stmt, idx, instance) => setter((TInstance)instance, raw.sqlite3_column_int(stmt, idx) != 0);
    }

    private static Action<sqlite3_stmt, int, object> MakeStringAssigner<TInstance>(MethodInfo setMethod) where TInstance : class
    {
        Action<TInstance, string?> setter = setMethod.CreateDelegate<Action<TInstance, string?>>();
        return (stmt, idx, instance) =>
        {
            if (raw.sqlite3_column_type(stmt, idx) == raw.SQLITE_NULL)
            {
                setter((TInstance)instance, null);
            }
            else
            {
                setter((TInstance)instance, raw.sqlite3_column_text(stmt, idx).utf8_to_string());
            }
        };
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Falls back to PropertyInfo.SetValue when MakeGenericType is unsupported under NativeAOT.")]
    [UnconditionalSuppressMessage("AOT", "IL2055", Justification = "Falls back to PropertyInfo.SetValue when MakeGenericType is unsupported under NativeAOT.")]
    private static Action<object, object?> CreateSetter(PropertyInfo prop)
    {
        Type declaringType = prop.DeclaringType!;
        if (declaringType.IsValueType || !RuntimeFeature.IsDynamicCodeSupported)
        {
            return prop.SetValue;
        }

        Type helperType = typeof(SetterHelper<,>).MakeGenericType(declaringType, prop.PropertyType);
        ISetterHelper helper = (ISetterHelper)Activator.CreateInstance(helperType, prop.SetMethod!)!;
        return helper.Set;
    }
}
