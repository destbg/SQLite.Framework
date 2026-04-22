using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using SQLite.Framework.Enums;
using SQLite.Framework.Internals.Models;
using SQLite.Framework.Models;

namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// This class is used to build an object from the provided columns.
/// </summary>
internal static class BuildQueryObject
{
    public static object? CreateInstance(SQLiteDataReader reader, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] Type elementType, Dictionary<string, int> columns, SQLQuery? query)
    {
        if (query?.CreateObject != null)
        {
            SQLiteQueryContext context = new()
            {
                Reader = reader,
                Columns = columns,
                ReflectedMethods = query.ReflectedMethods,
                ReflectedMethodInstances = query.ReflectedMethodInstances,
                CapturedValues = query.CapturedValues,
                ReflectedTypes = query.ReflectedTypes,
                ReflectedMembers = query.ReflectedMembers,
            };
            return query.CreateObject(context);
        }

        SQLiteOptions options = reader.Options;
        if (CommonHelpers.IsSimple(elementType, options))
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
            SQLiteQueryContext context = new()
            {
                Reader = reader,
                Columns = columns
            };
            return generated(context);
        }

        if (options.ReflectionFallbackDisabled)
        {
            throw new InvalidOperationException(
                $"Entity materializer for {elementType.FullName} fell back to runtime reflection but ReflectionFallbackDisabled is set. " +
                "Either install SQLite.Framework.SourceGenerator and call UseGeneratedMaterializers so this type gets a generated materializer, " +
                "or remove the DisableReflectionFallback call.");
        }

        return BuildInternal(elementType, reader, string.Empty, columns, options);
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "All types should be part of the client assembly.")]
    [UnconditionalSuppressMessage("AOT", "IL2067", Justification = "All types should be part of the client assembly.")]
    private static object? BuildInternal(Type type, SQLiteDataReader reader, string prefix, Dictionary<string, int> columns, SQLiteOptions options)
    {
        if (CommonHelpers.IsSimple(type, options))
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

            if (CommonHelpers.IsSimple(propType, options))
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
                            if (val is string enumString)
                            {
                                convertedValue = Enum.TryParse(targetType, enumString, out object? parsed) ? parsed : null;
                            }
                            else
                            {
                                object underlyingType = Convert.ChangeType(val, Enum.GetUnderlyingType(targetType));
                                convertedValue = Enum.IsDefined(targetType, underlyingType)
                                    ? Enum.ToObject(targetType, underlyingType)
                                    : null;
                            }
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
        bool nameOk = type.FullName?.Contains("AnonymousType") ?? false;
        return hasAttr && nameOk;
    }
}