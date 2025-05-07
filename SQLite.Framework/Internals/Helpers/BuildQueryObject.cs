using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SQLite.Framework.Internals.Helpers;

internal static class BuildQueryObject
{
    public static object? CreateInstance(IDataRecord reader, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type elementType)
    {
        if (CommonHelpers.IsSimple(elementType))
        {
            object? v = reader[0];
            if (v is DBNull)
            {
                v = null;
            }

            return v == null ? null : Convert.ChangeType(v, elementType);
        }

        return BuildInternal(elementType, reader, string.Empty);
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "All types should be part of the client assembly.")]
    [UnconditionalSuppressMessage("AOT", "IL2067", Justification = "All types should be part of the client assembly.")]
    private static object? BuildInternal(Type type, IDataRecord reader, string prefix)
    {
        if (CommonHelpers.IsSimple(type))
        {
            string columnName = prefix.TrimEnd('.');
            if (reader.HasColumn(columnName))
            {
                object value = reader[columnName];
                if (value == DBNull.Value)
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
                args[i] = BuildInternal(p.ParameterType, reader, paramPrefix);
            }

            return ctor.Invoke(args);
        }

        object? instance = Activator.CreateInstance(type);
        foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite))
        {
            Type propType = prop.PropertyType;
            string columnName = $"{prefix}{prop.Name}";

            if (CommonHelpers.IsSimple(propType))
            {
                if (reader.HasColumn(columnName))
                {
                    object val = reader[columnName];
                    if (val != DBNull.Value)
                    {
                        Type targetType = Nullable.GetUnderlyingType(propType) ?? propType;
                        object? convertedValue;

                        if (targetType.IsEnum)
                        {
                            object underlyingType = Convert.ChangeType(val, Enum.GetUnderlyingType(targetType));

                            if (Enum.IsDefined(targetType, underlyingType))
                            {
                                convertedValue = Enum.ToObject(targetType, underlyingType);
                            }
                            else
                            {
                                convertedValue = null;
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
                    object? nestedObj = BuildInternal(propType, reader, nestedPrefix);
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

    private static bool HasColumn(this IDataRecord reader, string columnName)
    {
        for (int i = 0; i < reader.FieldCount; i++)
        {
            if (reader.GetName(i).Equals(columnName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}