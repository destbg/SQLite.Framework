namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// This class is used to build an object from the provided columns.
/// </summary>
internal static class BuildQueryObject
{
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

        if (options.ReflectionFallbackDisabled)
        {
            throw new InvalidOperationException(
                $"Entity materializer for {elementType.FullName} fell back to runtime reflection but ReflectionFallbackDisabled is set. " +
                "Either install SQLite.Framework.SourceGenerator and call UseGeneratedMaterializers so this type gets a generated materializer, " +
                "or remove the DisableReflectionFallback call.");
        }

        return BuildInternal(elementType, reader, string.Empty, context.Columns!, options);
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "All types should be part of the client assembly.")]
    [UnconditionalSuppressMessage("AOT", "IL2067", Justification = "All types should be part of the client assembly.")]
    private static object? BuildInternal(Type type, SQLiteDataReader reader, string prefix, Dictionary<string, int> columns, SQLiteOptions options)
    {
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
                args[i] = BuildInternal(p.ParameterType, reader, paramPrefix, columns, options);
            }

            return ctor.Invoke(args);
        }

        object? instance = Activator.CreateInstance(type);
        foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite))
        {
            Type propType = prop.PropertyType;
            string columnName = prefix + prop.Name;

            if (TypeHelpers.IsSimple(propType, options))
            {
                if (columns.TryGetValue(columnName, out int columnIndex))
                {
                    object? val = reader.GetValue(columnIndex, reader.GetColumnType(columnIndex), propType);
                    if (val != null)
                    {
                        Type targetType = Nullable.GetUnderlyingType(propType) ?? propType;
                        object? convertedValue;

                        if (targetType.IsEnum)
                        {
                            object underlyingType = Convert.ChangeType(val, Enum.GetUnderlyingType(targetType));
                            convertedValue = Enum.IsDefined(targetType, underlyingType)
                                ? Enum.ToObject(targetType, underlyingType)
                                : null;
                        }
                        else
                        {
                            convertedValue = Convert.ChangeType(val, targetType);
                        }

                        prop.SetValue(instance, convertedValue);
                    }
                }
            }
            else
            {
                string nestedPrefix = columnName + ".";
                bool hasNested = Enumerable.Range(0, reader.FieldCount)
                    .Select(reader.GetName)
                    .Any(n => n.StartsWith(nestedPrefix, StringComparison.Ordinal));

                if (hasNested)
                {
                    object? nestedObj = BuildInternal(propType, reader, nestedPrefix, columns, options);
                    prop.SetValue(instance, nestedObj);
                }
            }
        }

        return instance;
    }

    private static bool IsAnonymousType(Type type)
    {
        bool hasAttr = type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Length != 0;
        bool nameOk = type.FullName!.Contains("AnonymousType");
        return hasAttr && nameOk;
    }
}