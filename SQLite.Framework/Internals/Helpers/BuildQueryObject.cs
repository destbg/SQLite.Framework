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
            ConstructedPaths = query?.ConstructedPaths,
            SelectValueTypes = query?.SelectValueTypes,
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
            SQLiteQueryContext seedContext = BuildContext(reader, columns, query);
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

        if (IsCollectionResult(elementType))
        {
            throw new NotSupportedException(
                $"Cannot read a query result into the collection type '{elementType.FullName}'.");
        }

        bool isAnon = IsAnonymousType(elementType);
        bool hasParameterless = HasParameterlessConstructor(elementType);
        bool hasPositional = !isAnon && !hasParameterless && FindPositionalConstructors(elementType).Count > 0;

        if (!isAnon && !hasParameterless && !hasPositional)
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

        return BuildReflective(elementType, prefix: string.Empty, reader, columns, options, query?.ConstructedPaths, query?.SelectValueTypes);
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

    private static bool IsCollectionResult(Type type)
    {
        return type.IsArray
            || (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type));
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Type comes from the entity surface.")]
    private static bool HasParameterlessConstructor(Type type)
    {
        return type.GetConstructor(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null) != null;
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Type comes from the entity surface.")]
    private static bool HasReadOnlyDataProperty(Type type)
    {
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length == 0
                && !property.CanWrite
                && property.GetMethod!.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
            {
                return true;
            }
        }

        return false;
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Type comes from the entity surface.")]
    private static List<ConstructorInfo> FindPositionalConstructors(Type type)
    {
        Dictionary<string, Type> memberTypes = new(StringComparer.OrdinalIgnoreCase);
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length == 0)
            {
                memberTypes[property.Name] = property.PropertyType;
            }
        }

        foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            memberTypes[field.Name] = field.FieldType;
        }

        List<ConstructorInfo> eligible = new();
        foreach (ConstructorInfo ctor in type.GetConstructors())
        {
            ParameterInfo[] parameters = ctor.GetParameters();
            if (parameters.Length == 0)
            {
                continue;
            }

            bool allMatch = true;
            foreach (ParameterInfo parameter in parameters)
            {
                if (!memberTypes.TryGetValue(parameter.Name!, out Type? memberType)
                    || (Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType)
                    != (Nullable.GetUnderlyingType(memberType) ?? memberType))
                {
                    allMatch = false;
                    break;
                }
            }

            if (allMatch)
            {
                eligible.Add(ctor);
            }
        }

        eligible.Sort((a, b) => b.GetParameters().Length.CompareTo(a.GetParameters().Length));
        return eligible;
    }

    private static ConstructorInfo ChooseCoveredConstructor(List<ConstructorInfo> eligible, Dictionary<string, int> columns, string prefix)
    {
        foreach (ConstructorInfo ctor in eligible)
        {
            bool covered = true;
            foreach (ParameterInfo parameter in ctor.GetParameters())
            {
                if (FindColumnIndex(columns, prefix + parameter.Name) == NotPresentSentinel
                    && !HasColumnWithPrefix(columns, prefix + parameter.Name + "."))
                {
                    covered = false;
                    break;
                }
            }

            if (covered)
            {
                return ctor;
            }
        }

        return eligible[0];
    }

    private static bool HasColumnWithPrefix(Dictionary<string, int> columns, string prefix)
    {
        foreach (KeyValuePair<string, int> column in columns)
        {
            if (column.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Backing fields may be trimmed and are skipped when missing.")]
    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "Backing fields may be trimmed and are skipped when missing.")]
    private static ReadOnlyFieldSlot[] BuildReadOnlyFieldSlots(Type type, ParameterInfo[] constructorParameters, string prefix, Dictionary<string, int> columns)
    {
        List<ReadOnlyFieldSlot> slots = new();
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.CanWrite || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            bool coveredByConstructor = false;
            foreach (ParameterInfo parameter in constructorParameters)
            {
                if (string.Equals(parameter.Name, property.Name, StringComparison.OrdinalIgnoreCase))
                {
                    coveredByConstructor = true;
                    break;
                }
            }

            if (coveredByConstructor)
            {
                continue;
            }

            FieldInfo? backingField = property.DeclaringType!.GetField($"<{property.Name}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            if (backingField == null)
            {
                continue;
            }

            int columnIndex = FindColumnIndex(columns, prefix + property.Name);
            if (columnIndex == NotPresentSentinel)
            {
                continue;
            }

            Type targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            slots.Add(new ReadOnlyFieldSlot
            {
                Field = backingField,
                ColumnIndex = columnIndex,
                DeclaredType = property.PropertyType,
                TargetType = targetType,
                IsEnum = targetType.IsEnum,
                EnumUnderlyingType = targetType.IsEnum ? Enum.GetUnderlyingType(targetType) : null,
            });
        }

        return slots.ToArray();
    }

    private static bool ApplyReadOnlyFieldSlots(ReadOnlyFieldSlot[] fieldSlots, SQLiteDataReader r, object instance)
    {
        bool anyNonNull = false;
        for (int i = 0; i < fieldSlots.Length; i++)
        {
            ReadOnlyFieldSlot slot = fieldSlots[i];
            object? value = r.GetValue(slot.ColumnIndex, r.GetColumnType(slot.ColumnIndex), slot.DeclaredType);
            if (value != null)
            {
                anyNonNull = true;
                value = slot.IsEnum
                    ? Enum.ToObject(slot.TargetType, Convert.ChangeType(value, slot.EnumUnderlyingType!))
                    : Convert.ChangeType(value, slot.TargetType);
            }

            slot.Field.SetValue(instance, value);
        }

        return anyNonNull;
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "All types should be part of the client assembly.")]
    [UnconditionalSuppressMessage("AOT", "IL2067", Justification = "All types should be part of the client assembly.")]
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "All types should be part of the client assembly.")]
    private static Func<SQLiteQueryContext, object?> BuildReflective([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, string prefix, SQLiteDataReader reader, Dictionary<string, int> columns, SQLiteOptions options, IReadOnlyCollection<string>? constructedPaths, IReadOnlyDictionary<string, Type>? selectValueTypes)
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
                argBuilders[i] = BuildReflective(p.ParameterType, paramPrefix, reader, columns, options, constructedPaths, selectValueTypes);
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

        if (!HasParameterlessConstructor(type) || HasReadOnlyDataProperty(type))
        {
            List<ConstructorInfo> positionals = FindPositionalConstructors(type);
            if (positionals.Count > 0)
            {
                ConstructorInfo positional = ChooseCoveredConstructor(positionals, columns, prefix);
                MaterializerPlan positionalPlan = ReflectionMaterializerCache.GetPlan(type, options);
                ParameterInfo[] parameters = positional.GetParameters();
                PositionalSlot[] positionalSlots = new PositionalSlot[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    ParameterInfo p = parameters[i];
                    int columnIndex = FindColumnIndex(columns, prefix + p.Name);
                    Type targetType = Nullable.GetUnderlyingType(p.ParameterType) ?? p.ParameterType;
                    Func<SQLiteQueryContext, object?>? nestedArg = null;
                    if (columnIndex == NotPresentSentinel
                        && !TypeHelpers.IsSimple(p.ParameterType, options)
                        && HasColumnWithPrefix(columns, prefix + p.Name + "."))
                    {
                        nestedArg = BuildReflective(targetType, prefix + p.Name + ".", reader, columns, options, constructedPaths, selectValueTypes);
                    }

                    positionalSlots[i] = new PositionalSlot
                    {
                        ColumnIndex = columnIndex,
                        DeclaredType = p.ParameterType,
                        TargetType = targetType,
                        IsEnum = targetType.IsEnum,
                        EnumUnderlyingType = targetType.IsEnum ? Enum.GetUnderlyingType(targetType) : null,
                        NestedMaterializer = nestedArg,
                    };
                }

                List<SlotPlan> extraSlotPlans = new();
                foreach (PropertySlot slot in positionalPlan.Slots)
                {
                    string columnName = prefix.Length == 0 ? slot.Name : prefix + slot.Name;
                    extraSlotPlans.Add(BuildSlotPlan(slot, columnName, reader, columns, options, constructedPaths, selectValueTypes));
                }

                ConstructorInfo capturedCtor = positional;
                SlotPlan[] extras = extraSlotPlans.ToArray();
                ReadOnlyFieldSlot[] fieldSlots = BuildReadOnlyFieldSlots(type, parameters, prefix, columns);
                bool trackPositionalNulls = prefix.Length > 0 && !IsConstructedPrefix(constructedPaths, prefix);
                return ctx =>
                {
                    SQLiteDataReader r = ctx.Reader!;
                    object?[] args = new object?[positionalSlots.Length];
                    bool anyNonNull = false;
                    for (int i = 0; i < positionalSlots.Length; i++)
                    {
                        PositionalSlot s = positionalSlots[i];
                        if (s.ColumnIndex == NotPresentSentinel)
                        {
                            if (s.NestedMaterializer != null)
                            {
                                args[i] = s.NestedMaterializer(ctx);
                                anyNonNull |= args[i] != null;
                            }

                            continue;
                        }

                        object? value = r.GetValue(s.ColumnIndex, r.GetColumnType(s.ColumnIndex), s.DeclaredType);
                        if (value == null)
                        {
                            continue;
                        }

                        anyNonNull = true;
                        args[i] = s.IsEnum
                            ? Enum.ToObject(s.TargetType, Convert.ChangeType(value, s.EnumUnderlyingType!))
                            : Convert.ChangeType(value, s.TargetType);
                    }

                    object instance = capturedCtor.Invoke(args);
                    for (int i = 0; i < extras.Length; i++)
                    {
                        anyNonNull |= ApplySlotPlan(in extras[i], ctx, r, instance, trackPositionalNulls);
                    }

                    anyNonNull |= ApplyReadOnlyFieldSlots(fieldSlots, r, instance);
                    return trackPositionalNulls && !anyNonNull ? null : instance;
                };
            }
        }

        MaterializerPlan plan = ReflectionMaterializerCache.GetPlan(type, options);
        PropertySlot[] slots = plan.Slots;
        SlotPlan[] slotPlans = new SlotPlan[slots.Length];
        for (int s = 0; s < slots.Length; s++)
        {
            string columnName = prefix.Length == 0 ? slots[s].Name : prefix + slots[s].Name;
            slotPlans[s] = BuildSlotPlan(slots[s], columnName, reader, columns, options, constructedPaths, selectValueTypes);
        }

        IInstanceFactory? factory = plan.Factory;
        Type capturedFallback = type;
        ReadOnlyFieldSlot[] planFieldSlots = BuildReadOnlyFieldSlots(type, [], prefix, columns);
        bool trackNulls = prefix.Length > 0 && !IsConstructedPrefix(constructedPaths, prefix);
        return ctx =>
        {
            SQLiteDataReader r = ctx.Reader!;
            object instance = factory != null
                ? factory.Create()
                : Activator.CreateInstance(capturedFallback, nonPublic: true)!;

            bool anyNonNull = false;
            for (int i = 0; i < slotPlans.Length; i++)
            {
                anyNonNull |= ApplySlotPlan(in slotPlans[i], ctx, r, instance, trackNulls);
            }

            anyNonNull |= ApplyReadOnlyFieldSlots(planFieldSlots, r, instance);
            return trackNulls && !anyNonNull ? null : instance;
        };
    }

    private static bool IsConstructedPrefix(IReadOnlyCollection<string>? constructedPaths, string prefix)
    {
        if (constructedPaths == null)
        {
            return false;
        }

        string path = prefix[..^1];
        foreach (string constructed in constructedPaths)
        {
            if (string.Equals(constructed, path, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static int FindColumnIndex(Dictionary<string, int> columns, string columnName)
    {
        if (columns.TryGetValue(columnName, out int idx))
        {
            return idx;
        }

        foreach (KeyValuePair<string, int> column in columns)
        {
            if (string.Equals(column.Key, columnName, StringComparison.OrdinalIgnoreCase))
            {
                return column.Value;
            }
        }

        return NotPresentSentinel;
    }

    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "Nested entity type comes from the entity surface.")]
    private static SlotPlan BuildSlotPlan(PropertySlot slot, string columnName, SQLiteDataReader reader, Dictionary<string, int> columns, SQLiteOptions options, IReadOnlyCollection<string>? constructedPaths, IReadOnlyDictionary<string, Type>? selectValueTypes)
    {
        if (slot.IsSimple)
        {
            Type? projectedType = null;
            if (slot.PropertyType == typeof(object)
                && selectValueTypes != null
                && selectValueTypes.TryGetValue(columnName, out Type? projected)
                && projected != typeof(object))
            {
                projectedType = projected;
            }

            return new SlotPlan
            {
                Slot = slot,
                ColumnIndex = FindColumnIndex(columns, columnName),
                NestedMaterializer = null,
                ProjectedReadType = projectedType,
            };
        }

        if (slot.PropertyType.IsInterface)
        {
            int directIndex = FindColumnIndex(columns, columnName);
            if (directIndex != NotPresentSentinel)
            {
                Type readType = selectValueTypes != null && selectValueTypes.TryGetValue(columnName, out Type? projected)
                    ? projected
                    : typeof(object);
                return new SlotPlan
                {
                    Slot = slot,
                    ColumnIndex = directIndex,
                    NestedMaterializer = null,
                    ProjectedReadType = readType,
                };
            }
        }

        string nestedPrefix = columnName + ".";
        bool hasNested = false;
        int fieldCount = reader.FieldCount;
        for (int j = 0; j < fieldCount; j++)
        {
            if (reader.GetName(j).StartsWith(nestedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                hasNested = true;
                break;
            }
        }

        return new SlotPlan
        {
            Slot = slot,
            ColumnIndex = NotPresentSentinel,
            NestedMaterializer = hasNested
                ? BuildReflective(slot.PropertyType, nestedPrefix, reader, columns, options, constructedPaths, selectValueTypes)
                : null,
        };
    }

    private static bool ApplySlotPlan(in SlotPlan sp, SQLiteQueryContext ctx, SQLiteDataReader r, object instance, bool trackNonNull)
    {
        PropertySlot slot = sp.Slot;
        int columnIndex = sp.ColumnIndex;

        if (slot.IsSimple)
        {
            if (columnIndex < 0)
            {
                return false;
            }

            if (slot.Assigner != null)
            {
                bool nonNull = trackNonNull && r.GetColumnType(columnIndex) != SQLiteColumnType.Null;
                slot.Assigner(r.Statement, columnIndex, instance);
                return nonNull;
            }

            if (sp.ProjectedReadType != null)
            {
                object? projected = r.GetValue(columnIndex, r.GetColumnType(columnIndex), sp.ProjectedReadType);
                slot.Setter(instance, projected);
                return projected != null;
            }

            object? val = r.GetValue(columnIndex, r.GetColumnType(columnIndex), slot.PropertyType);
            if (val == null)
            {
                slot.Setter(instance, !slot.PropertyType.IsValueType || Nullable.GetUnderlyingType(slot.PropertyType) != null
                    ? null
                    : slot.BoxedDefault);

                return false;
            }

            object? convertedValue = slot.IsEnum
                ? Enum.ToObject(slot.TargetType, Convert.ChangeType(val, slot.EnumUnderlyingType!))
                : Convert.ChangeType(val, slot.TargetType);
            slot.Setter(instance, convertedValue);
            return true;
        }

        if (sp.ProjectedReadType != null)
        {
            object? projectedValue = r.GetValue(columnIndex, r.GetColumnType(columnIndex), sp.ProjectedReadType);
            slot.Setter(instance, projectedValue);
            return projectedValue != null;
        }

        if (sp.NestedMaterializer != null)
        {
            object? nestedObj = sp.NestedMaterializer(ctx);
            if (nestedObj != null)
            {
                slot.Setter(instance, nestedObj);
                return true;
            }
        }

        return false;
    }

    private static bool IsAnonymousType(Type type)
    {
        bool hasAttr = type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Length != 0;
        bool nameOk = type.FullName!.Contains("AnonymousType");
        return hasAttr && nameOk;
    }
}
