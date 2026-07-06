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

        type = Nullable.GetUnderlyingType(type) ?? type;

        if (!options.TypeConverters.TryGetValue(type, out ISQLiteTypeConverter? converter))
        {
            return false;
        }

        if (converter is SQLiteJsonObjectConverter)
        {
            return true;
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

    public static JsonTypeInfo? ResolveJsonTypeInfo(this SQLiteOptions options, Type type)
    {
        Type stripped = Nullable.GetUnderlyingType(type) ?? type;
        if (options.TypeConverters.TryGetValue(stripped, out ISQLiteTypeConverter? registered)
            && registered is IJsonTypeInfoSource direct)
        {
            return direct.TypeInfo;
        }

        foreach (ISQLiteTypeConverter converter in options.TypeConverters.Values)
        {
            if (converter is IJsonTypeInfoSource source
                && source.TypeInfo.Options.TryGetTypeInfo(stripped, out JsonTypeInfo? resolved))
            {
                return resolved;
            }
        }

        return null;
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
