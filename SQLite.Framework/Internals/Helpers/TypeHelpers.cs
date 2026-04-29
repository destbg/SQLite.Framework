namespace SQLite.Framework.Internals.Helpers;

internal static class TypeHelpers
{
    public static bool IsSimple(Type type, SQLiteOptions options)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (options.TypeConverters.ContainsKey(type))
        {
            return true;
        }

        return type.IsPrimitive
               || type.IsEnum
               || type == typeof(byte[])
               || type == typeof(string)
               || type == typeof(decimal)
               || type == typeof(DateTime)
               || type == typeof(DateTimeOffset)
               || type == typeof(TimeSpan)
               || type == typeof(Guid)
               || type == typeof(DateOnly)
               || type == typeof(TimeOnly);
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Interface lookup is only used for known collection types with registered converters.")]
    public static Type? GetEnumerableElementType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return type.GetGenericArguments()[0];
        }

        foreach (Type iface in type.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return iface.GetGenericArguments()[0];
            }
        }

        return null;
    }

    public static SQLiteColumnType TypeToSQLiteType(Type type, SQLiteOptions options)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (options.TypeConverters.TryGetValue(type, out ISQLiteTypeConverter? converter))
        {
            return converter.ColumnType;
        }

        return type switch
        {
            _ when type == typeof(string) => SQLiteColumnType.Text,
            _ when type == typeof(byte[]) => SQLiteColumnType.Blob,
            _ when type == typeof(bool) => SQLiteColumnType.Integer,
            _ when type == typeof(char) => SQLiteColumnType.Text,
            _ when type == typeof(DateTime) => SQLiteColumnType.Integer,
            _ when type == typeof(DateTimeOffset) => SQLiteColumnType.Integer,
            _ when type == typeof(DateOnly) && options.DateOnlyStorage == DateOnlyStorageMode.Text => SQLiteColumnType.Text,
            _ when type == typeof(DateOnly) => SQLiteColumnType.Integer,
            _ when type == typeof(TimeOnly) && options.TimeOnlyStorage == TimeOnlyStorageMode.Text => SQLiteColumnType.Text,
            _ when type == typeof(TimeOnly) => SQLiteColumnType.Integer,
            _ when type == typeof(Guid) => SQLiteColumnType.Text,
            _ when type == typeof(TimeSpan) => SQLiteColumnType.Integer,
            _ when type == typeof(decimal) && options.DecimalStorage == DecimalStorageMode.Text => SQLiteColumnType.Text,
            _ when type == typeof(decimal) => SQLiteColumnType.Real,
            _ when type == typeof(double) => SQLiteColumnType.Real,
            _ when type == typeof(float) => SQLiteColumnType.Real,
            _ when type == typeof(byte) => SQLiteColumnType.Integer,
            _ when type == typeof(int) => SQLiteColumnType.Integer,
            _ when type == typeof(long) => SQLiteColumnType.Integer,
            _ when type == typeof(sbyte) => SQLiteColumnType.Integer,
            _ when type == typeof(short) => SQLiteColumnType.Integer,
            _ when type == typeof(uint) => SQLiteColumnType.Integer,
            _ when type == typeof(ulong) => SQLiteColumnType.Integer,
            _ when type == typeof(ushort) => SQLiteColumnType.Integer,
            _ when type.IsEnum => SQLiteColumnType.Integer,
            _ => throw new NotSupportedException($"The type {type} is not supported.")
        };
    }
}
