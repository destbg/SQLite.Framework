namespace SQLite.Framework.Internals;

internal static class TypeMetadata
{
    public static bool HasTextOrBlobConverter(this SQLiteOptions options, Type type)
    {
        Type stripped = Nullable.GetUnderlyingType(type) ?? type;
        return options.TypeConverters.TryGetValue(stripped, out ISQLiteTypeConverter? converter)
            && (converter.ColumnType == SQLiteColumnType.Text || converter.ColumnType == SQLiteColumnType.Blob);
    }

    public static bool HasJsonConverter(this SQLiteOptions options, Type? type)
    {
        if (type == null)
        {
            return false;
        }

        Type stripped = Nullable.GetUnderlyingType(type) ?? type;
        if (!options.TypeConverters.TryGetValue(stripped, out ISQLiteTypeConverter? converter))
        {
            return false;
        }

        Type ct = converter.GetType();
        if (!ct.IsGenericType)
        {
            return false;
        }

        Type def = ct.GetGenericTypeDefinition();
#if SQLITECIPHER
        return def == typeof(SQLiteJsonConverter<>);
#else
        return def == typeof(SQLiteJsonConverter<>) || def == typeof(SQLiteJsonbConverter<>);
#endif
    }

    public static Type CoercedResultType(this SQLiteOptions options, Type declaredType, Type sourceType)
    {
        if (!options.TypeConverters.ContainsKey(sourceType))
        {
            return declaredType;
        }

        if (declaredType.IsAssignableFrom(sourceType))
        {
            return sourceType;
        }

        if (TypeHelpers.GetEnumerableElementType(declaredType) is Type declaredElem &&
            TypeHelpers.GetEnumerableElementType(sourceType) is Type sourceElem &&
            declaredElem == sourceElem)
        {
            return sourceType;
        }

        return declaredType;
    }

}
