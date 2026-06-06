namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// This class is used to build an object from the provided columns.
/// </summary>
internal static class BuildQueryObject
{
    private const int NotPresentSentinel = -1;

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

    /// <summary>
    /// Builds a row materializer once per query. Hoists every type check and reflection lookup
    /// out of the row loop so the caller only does one delegate invocation per row.
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "The type should be part of the client assemblies.")]
    [UnconditionalSuppressMessage("AOT", "IL2067", Justification = "The type should be part of the client assemblies.")]
    public static Func<SQLiteQueryContext, object?> BuildMaterializer(SQLiteDataReader reader, Dictionary<string, int> columns, SQLQuery? query, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] Type elementType)
    {
        if (query?.CreateObject != null)
        {
            return query.CreateObject;
        }

        SQLiteOptions options = reader.Options;

        bool isDictRow = elementType == typeof(Dictionary<string, object>) || elementType == typeof(Dictionary<string, object?>);

        if (isDictRow && columns.Count > 1)
        {
            (string Name, int Index)[] capturedColumns = columns
                .Select(c => (c.Key, c.Value))
                .OrderBy(c => c.Value)
                .ToArray();
            return ctx =>
            {
                SQLiteDataReader r = ctx.Reader!;
                Dictionary<string, object?> row = new(capturedColumns.Length, StringComparer.Ordinal);
                foreach ((string name, int idx) in capturedColumns)
                {
                    row[name] = r.GetValue(idx, r.GetColumnType(idx), typeof(object));
                }
                return row;
            };
        }

        if (TypeHelpers.IsSimple(elementType, options))
        {
            Type capturedType = elementType;
            return ctx =>
            {
                SQLiteDataReader r = ctx.Reader!;
                return r.GetValue(0, r.GetColumnType(0), capturedType);
            };
        }

        if (isDictRow)
        {
            (string Name, int Index)[] capturedColumns = columns
                .Select(c => (c.Key, c.Value))
                .OrderBy(c => c.Value)
                .ToArray();
            return ctx =>
            {
                SQLiteDataReader r = ctx.Reader!;
                Dictionary<string, object?> row = new(capturedColumns.Length, StringComparer.Ordinal);
                foreach ((string name, int idx) in capturedColumns)
                {
                    row[name] = r.GetValue(idx, r.GetColumnType(idx), typeof(object));
                }
                return row;
            };
        }

        if (elementType.IsInterface)
        {
            Type? converterType = options.GetConverterTypeForInterface(elementType);
            if (converterType != null)
            {
                return ctx =>
                {
                    SQLiteDataReader r = ctx.Reader!;
                    return r.GetValue(0, r.GetColumnType(0), converterType);
                };
            }
        }

        if (options.EntityMaterializers.TryGetValue(elementType, out Func<SQLiteQueryContext, Func<SQLiteQueryContext, object?>>? builderFn))
        {
            SQLiteQueryContext seedContext = new() { Reader = reader, Columns = columns };
            Func<SQLiteQueryContext, object?> generated = builderFn(seedContext);
#if SQLITE_FRAMEWORK_TESTING
            SQLiteDatabase database = reader.Database;
            Func<SQLiteQueryContext, object?> inner = generated;
            return ctx =>
            {
                database.IncrementEntityMaterializerHits();
                return inner(ctx);
            };
#else
            return generated;
#endif
        }

        bool isAnon = IsAnonymousType(elementType);
        bool hasParameterless = HasParameterlessConstructor(elementType);
        ConstructorInfo? positional = !isAnon && !hasParameterless ? FindPositionalConstructor(elementType) : null;

        if (!isAnon && !hasParameterless && positional == null)
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

        return BuildReflective(elementType, prefix: string.Empty, reader, columns, options);
    }

    /// <summary>
    /// Backwards compatible per-row entry point. Equivalent to calling
    /// <see cref="BuildMaterializer" /> once and invoking the result, but keeps the old
    /// signature for callers that materialize a single row.
    /// </summary>
    public static object? CreateInstance(SQLiteQueryContext context, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] Type elementType, SQLQuery? query)
    {
        Func<SQLiteQueryContext, object?> materializer = BuildMaterializer(context.Reader!, context.Columns!, query, elementType);
        return materializer(context);
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
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (ParameterInfo parameter in parameters)
        {
            if (!propertyNames.Contains(parameter.Name!))
            {
                return null;
            }
        }

        return ctor;
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "All types should be part of the client assembly.")]
    [UnconditionalSuppressMessage("AOT", "IL2067", Justification = "All types should be part of the client assembly.")]
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "All types should be part of the client assembly.")]
    private static Func<SQLiteQueryContext, object?> BuildReflective([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, string prefix, SQLiteDataReader reader, Dictionary<string, int> columns, SQLiteOptions options)
    {
        if (TypeHelpers.IsSimple(type, options))
        {
            string columnName = prefix.TrimEnd('.');
            if (!columns.TryGetValue(columnName, out int simpleIndex))
            {
                return _ => null;
            }

            Type capturedType = type;
            int capturedIndex = simpleIndex;
            return ctx =>
            {
                SQLiteDataReader r = ctx.Reader!;
                object? value = r.GetValue(capturedIndex, r.GetColumnType(capturedIndex), capturedType);
                if (value == null)
                {
                    return null;
                }

                Type targetType = Nullable.GetUnderlyingType(capturedType) ?? capturedType;
                return Convert.ChangeType(value, targetType);
            };
        }

        if (IsAnonymousType(type))
        {
            ConstructorInfo ctor = type.GetConstructors().Single();
            ParameterInfo[] parameters = ctor.GetParameters();
            Func<SQLiteQueryContext, object?>[] argBuilders = new Func<SQLiteQueryContext, object?>[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo p = parameters[i];
                string paramPrefix = $"{prefix}{p.Name}.";
                argBuilders[i] = BuildReflective(p.ParameterType, paramPrefix, reader, columns, options);
            }

            return ctx =>
            {
                object?[] args = new object[argBuilders.Length];
                for (int i = 0; i < argBuilders.Length; i++)
                {
                    args[i] = argBuilders[i](ctx);
                }
                return ctor.Invoke(args);
            };
        }

        if (!HasParameterlessConstructor(type))
        {
            ConstructorInfo? positional = FindPositionalConstructor(type);
            if (positional != null)
            {
                ParameterInfo[] parameters = positional.GetParameters();
                PositionalSlot[] positionalSlots = new PositionalSlot[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    ParameterInfo p = parameters[i];
                    string columnName = prefix + p.Name;
                    int columnIndex = columns.TryGetValue(columnName, out int idx) ? idx : NotPresentSentinel;
                    Type targetType = Nullable.GetUnderlyingType(p.ParameterType) ?? p.ParameterType;
                    positionalSlots[i] = new PositionalSlot
                    {
                        ColumnIndex = columnIndex,
                        DeclaredType = p.ParameterType,
                        TargetType = targetType,
                        IsEnum = targetType.IsEnum,
                        EnumUnderlyingType = targetType.IsEnum ? Enum.GetUnderlyingType(targetType) : null,
                    };
                }

                ConstructorInfo capturedCtor = positional;
                return ctx =>
                {
                    SQLiteDataReader r = ctx.Reader!;
                    object?[] args = new object?[positionalSlots.Length];
                    for (int i = 0; i < positionalSlots.Length; i++)
                    {
                        PositionalSlot s = positionalSlots[i];
                        if (s.ColumnIndex == NotPresentSentinel)
                        {
                            continue;
                        }

                        object? value = r.GetValue(s.ColumnIndex, r.GetColumnType(s.ColumnIndex), s.DeclaredType);
                        if (value == null)
                        {
                            continue;
                        }

                        if (s.IsEnum)
                        {
                            object underlyingType = Convert.ChangeType(value, s.EnumUnderlyingType!);
                            args[i] = Enum.ToObject(s.TargetType, underlyingType);
                        }
                        else
                        {
                            args[i] = Convert.ChangeType(value, s.TargetType);
                        }
                    }

                    return capturedCtor.Invoke(args);
                };
            }
        }

        MaterializerPlan plan = ReflectionMaterializerCache.GetPlan(type, options);
        PropertySlot[] slots = plan.Slots;
        SlotPlan[] slotPlans = new SlotPlan[slots.Length];
        for (int s = 0; s < slots.Length; s++)
        {
            PropertySlot slot = slots[s];
            string columnName = prefix.Length == 0 ? slot.Name : prefix + slot.Name;

            if (slot.IsSimple)
            {
                slotPlans[s] = new SlotPlan
                {
                    Slot = slot,
                    ColumnIndex = columns.TryGetValue(columnName, out int idx) ? idx : NotPresentSentinel,
                    NestedMaterializer = null,
                };
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

                slotPlans[s] = new SlotPlan
                {
                    Slot = slot,
                    ColumnIndex = NotPresentSentinel,
                    NestedMaterializer = hasNested
                        ? BuildReflective(slot.PropertyType, nestedPrefix, reader, columns, options)
                        : null,
                };
            }
        }

        IInstanceFactory? factory = plan.Factory;
        Type capturedFallback = type;
        return ctx =>
        {
            SQLiteDataReader r = ctx.Reader!;
            object instance = factory != null
                ? factory.Create()
                : Activator.CreateInstance(capturedFallback, nonPublic: true)!;

            for (int i = 0; i < slotPlans.Length; i++)
            {
                SlotPlan sp = slotPlans[i];
                PropertySlot slot = sp.Slot;
                int columnIndex = sp.ColumnIndex;

                if (slot.IsSimple)
                {
                    if (columnIndex < 0)
                    {
                        continue;
                    }

                    if (slot.Assigner != null)
                    {
                        slot.Assigner(r.Statement, columnIndex, instance);
                    }
                    else
                    {
                        object? val = r.GetValue(columnIndex, r.GetColumnType(columnIndex), slot.PropertyType);
                        if (val != null)
                        {
                            object? convertedValue;

                            if (slot.IsEnum)
                            {
                                object underlyingType = Convert.ChangeType(val, slot.EnumUnderlyingType!);
                                convertedValue = Enum.ToObject(slot.TargetType, underlyingType);
                            }
                            else
                            {
                                convertedValue = Convert.ChangeType(val, slot.TargetType);
                            }

                            slot.Setter(instance, convertedValue);
                        }
                    }
                }
                else if (sp.NestedMaterializer != null)
                {
                    object? nestedObj = sp.NestedMaterializer(ctx);
                    slot.Setter(instance, nestedObj);
                }
            }

            return instance;
        };
    }

    private static bool IsAnonymousType(Type type)
    {
        bool hasAttr = type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Length != 0;
        bool nameOk = type.FullName!.Contains("AnonymousType");
        return hasAttr && nameOk;
    }
}
