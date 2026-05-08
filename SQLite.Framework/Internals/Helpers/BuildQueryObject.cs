namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// This class is used to build an object from the provided columns.
/// </summary>
internal static class BuildQueryObject
{
    private const int NotPresentSentinel = -1;
    private const int NestedSentinel = -2;

    public static SQLiteQueryContext BuildContext(SQLiteDataReader reader, Dictionary<string, int> columns, SQLQuery? query)
    {
        return new()
        {
            Reader = reader,
            Columns = columns,
            ReflectedMethods = query?.ReflectedMethods,
            ReflectedMethodInstances = query?.ReflectedMethodInstances,
            CapturedValues = query?.CapturedValues,
            ReflectedTypes = query?.ReflectedTypes,
            ReflectedMembers = query?.ReflectedMembers,
            ReflectedConstructors = query?.ReflectedConstructors,
        };
    }

    public static object? CreateInstance(SQLiteQueryContext context, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] Type elementType, SQLQuery? query)
    {
        SQLiteDataReader reader = context.Reader!;

        if (query?.CreateObject != null)
        {
            return query.CreateObject(context);
        }

        SQLiteOptions options = reader.Options;
        if (TypeHelpers.IsSimple(elementType, options))
        {
            SQLiteColumnType columnType = reader.GetColumnType(0);
            return reader.GetValue(0, columnType, elementType);
        }

        if (elementType.IsInterface || elementType.IsAbstract)
        {
            Type? converterType = options.GetConverterTypeForInterface(elementType);
            if (converterType != null)
            {
                SQLiteColumnType columnType = reader.GetColumnType(0);
                return reader.GetValue(0, columnType, converterType);
            }
        }

        if (options.EntityMaterializers.TryGetValue(elementType, out Func<SQLiteQueryContext, object?>? generated))
        {
#if SQLITE_FRAMEWORK_TESTING
            reader.Database.IncrementEntityMaterializerHits();
#endif
            return generated(context);
        }

        if (!IsAnonymousType(elementType)
            && !HasParameterlessConstructor(elementType)
            && FindPositionalConstructor(elementType) == null)
        {
            throw new InvalidOperationException(
                $"Entity type '{elementType.FullName}' has no parameterless constructor and no usable positional constructor. " +
                "The framework needs either a parameterless constructor (init-only properties are fine) " +
                $"or a single public constructor whose parameter names match the entity's property names (as positional records have).");
        }

        if (options.ReflectionFallbackDisabled)
        {
            throw new InvalidOperationException(
                $"Entity materializer for {elementType.FullName} fell back to runtime reflection but ReflectionFallbackDisabled is set. " +
                "Either install SQLite.Framework.SourceGenerator and call UseGeneratedMaterializers so this type gets a generated materializer, " +
                "or remove the DisableReflectionFallback call.");
        }

        return BuildInternal(elementType, reader, string.Empty, context, options);
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Type comes from the entity surface; users keep their entities reachable.")]
    private static bool HasParameterlessConstructor(Type type)
    {
        return type.GetConstructor(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null) != null;
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Type comes from the entity surface; users keep their entities reachable.")]
    private static ConstructorInfo? FindPositionalConstructor(Type type)
    {
        ConstructorInfo[] ctors = type.GetConstructors();
        if (ctors.Length != 1)
        {
            return null;
        }

        ConstructorInfo ctor = ctors[0];
        ParameterInfo[] parameters = ctor.GetParameters();

        HashSet<string> propertyNames = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (ParameterInfo parameter in parameters)
        {
            if (parameter.Name == null || !propertyNames.Contains(parameter.Name))
            {
                return null;
            }
        }

        return ctor;
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "All types should be part of the client assembly.")]
    [UnconditionalSuppressMessage("AOT", "IL2067", Justification = "All types should be part of the client assembly.")]
    private static object? BuildInternal(Type type, SQLiteDataReader reader, string prefix, SQLiteQueryContext context, SQLiteOptions options)
    {
        Dictionary<string, int> columns = context.Columns!;

        if (TypeHelpers.IsSimple(type, options))
        {
            string columnName = prefix.TrimEnd('.');
            if (columns.TryGetValue(columnName, out int columnIndex))
            {
                object? value = reader.GetValue(columnIndex, reader.GetColumnType(columnIndex), type);

                if (value == null)
                {
                    return null;
                }

                Type targetType = Nullable.GetUnderlyingType(type) ?? type;
                return Convert.ChangeType(value, targetType);
            }

            return null;
        }

        if (IsAnonymousType(type))
        {
            ConstructorInfo ctor = type.GetConstructors().Single();
            ParameterInfo[] parameters = ctor.GetParameters();
            object?[] args = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo p = parameters[i];
                string paramPrefix = $"{prefix}{p.Name}.";
                args[i] = BuildInternal(p.ParameterType, reader, paramPrefix, context, options);
            }

            return ctor.Invoke(args);
        }

        if (!HasParameterlessConstructor(type))
        {
            ConstructorInfo? positional = FindPositionalConstructor(type);
            if (positional != null)
            {
                ParameterInfo[] parameters = positional.GetParameters();
                object?[] args = new object[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    ParameterInfo p = parameters[i];
                    string columnName = prefix + p.Name;
                    if (columns.TryGetValue(columnName, out int columnIndex))
                    {
                        object? value = reader.GetValue(columnIndex, reader.GetColumnType(columnIndex), p.ParameterType);
                        if (value != null)
                        {
                            Type targetType = Nullable.GetUnderlyingType(p.ParameterType) ?? p.ParameterType;
                            if (targetType.IsEnum)
                            {
                                object underlyingType = Convert.ChangeType(value, Enum.GetUnderlyingType(targetType));
                                args[i] = Enum.IsDefined(targetType, underlyingType)
                                    ? Enum.ToObject(targetType, underlyingType)
                                    : null;
                            }
                            else
                            {
                                args[i] = Convert.ChangeType(value, targetType);
                            }
                        }
                    }
                }

                return positional.Invoke(args);
            }
        }

        MaterializerPlan plan = ReflectionMaterializerCache.GetPlan(type, options);
        PropertySlot[] slots = plan.Slots;
        object? instance = plan.Factory != null
            ? plan.Factory.Create()
            : Activator.CreateInstance(type, nonPublic: true);
        int[] indices = GetOrBuildIndices(type, prefix, slots, context, reader);

        for (int s = 0; s < slots.Length; s++)
        {
            PropertySlot slot = slots[s];
            int columnIndex = indices[s];

            if (slot.IsSimple)
            {
                if (columnIndex >= 0)
                {
                    if (slot.Assigner != null)
                    {
                        slot.Assigner(reader.Statement, columnIndex, instance!);
                    }
                    else
                    {
                        object? val = reader.GetValue(columnIndex, reader.GetColumnType(columnIndex), slot.PropertyType);
                        if (val != null)
                        {
                            object? convertedValue;

                            if (slot.IsEnum)
                            {
                                object underlyingType = Convert.ChangeType(val, slot.EnumUnderlyingType!);
                                convertedValue = Enum.IsDefined(slot.TargetType, underlyingType)
                                    ? Enum.ToObject(slot.TargetType, underlyingType)
                                    : null;
                            }
                            else
                            {
                                convertedValue = Convert.ChangeType(val, slot.TargetType);
                            }

                            slot.Setter(instance!, convertedValue);
                        }
                    }
                }
            }
            else if (columnIndex == NestedSentinel)
            {
                string nestedPrefix = (prefix.Length == 0 ? slot.Name : prefix + slot.Name) + ".";
                object? nestedObj = BuildInternal(slot.PropertyType, reader, nestedPrefix, context, options);
                slot.Setter(instance!, nestedObj);
            }
        }

        return instance;
    }

    private static int[] GetOrBuildIndices(Type type, string prefix, PropertySlot[] slots, SQLiteQueryContext context, SQLiteDataReader reader)
    {
        Dictionary<(Type Type, string Prefix), int[]> cache = context.SlotIndexCache ??= new Dictionary<(Type, string), int[]>();
        if (cache.TryGetValue((type, prefix), out int[]? cached))
        {
            return cached;
        }

        Dictionary<string, int> columns = context.Columns!;
        int[] indices = new int[slots.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            PropertySlot slot = slots[i];
            string columnName = prefix.Length == 0 ? slot.Name : prefix + slot.Name;

            if (slot.IsSimple)
            {
                indices[i] = columns.TryGetValue(columnName, out int idx) ? idx : NotPresentSentinel;
            }
            else
            {
                string nestedPrefix = columnName + ".";
                bool hasNested = false;
                int fieldCount = reader.FieldCount;
                for (int j = 0; j < fieldCount; j++)
                {
                    if (reader.GetName(j).StartsWith(nestedPrefix, StringComparison.Ordinal))
                    {
                        hasNested = true;
                        break;
                    }
                }
                indices[i] = hasNested ? NestedSentinel : NotPresentSentinel;
            }
        }

        cache[(type, prefix)] = indices;
        return indices;
    }

    private static bool IsAnonymousType(Type type)
    {
        bool hasAttr = type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Length != 0;
        bool nameOk = type.FullName!.Contains("AnonymousType");
        return hasAttr && nameOk;
    }
}
