namespace SQLite.Framework.Internals.Helpers;

internal static class JsonCollectionMaterializer
{
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The collection type is rooted by the user query.")]
    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "The collection type is rooted by the user query.")]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "The collection type is rooted by the user query.")]
    public static Func<string, object?>? TryBuild(Type collectionType, SQLiteOptions options)
    {
        Type? elementType = TypeHelpers.GetEnumerableElementType(collectionType);
        if (elementType == null)
        {
            return null;
        }

        if (options.JsonCollectionMaterializers.TryGetValue(
            collectionType,
            out Func<string, SQLiteOptions, object?>? generated))
        {
            return json => generated(json, options);
        }

        if (options.ReflectionFallbackDisabled)
        {
            return _ => throw new InvalidOperationException(
                $"Collection materializer for {collectionType.FullName} fell back to runtime reflection but ReflectionFallbackDisabled is set. " +
                "The source generator did not cover this collection projection. " +
                "Install SQLite.Framework.SourceGenerator and call UseGeneratedMaterializers, " +
                "change the Select to a supported collection shape or remove the DisableReflectionFallback call.");
        }

        JsonTypeInfo? elementInfo = options.ResolveJsonTypeInfo(elementType);
        if (collectionType.IsArray && collectionType.GetArrayRank() == 1)
        {
            return json => MaterializeArray(json, elementType, elementInfo);
        }

        if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(List<>))
        {
            return json => MaterializeList(json, collectionType, elementType, elementInfo);
        }

        if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(HashSet<>))
        {
            return json => MaterializeHashSet(json, collectionType, elementType, elementInfo);
        }

        return null;
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The array type is rooted by the user query.")]
    private static Array MaterializeArray(string json, Type elementType, JsonTypeInfo? elementInfo)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement.ArrayEnumerator items = document.RootElement.EnumerateArray();
        Array result = Array.CreateInstance(elementType, document.RootElement.GetArrayLength());
        int index = 0;
        foreach (JsonElement item in items)
        {
            result.SetValue(ReadElement(item, elementType, elementInfo), index++);
        }

        return result;
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The collection type is rooted by the user query.")]
    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "The collection type is rooted by the user query.")]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "The collection type is rooted by the user query.")]
    private static object MaterializeHashSet(string json, Type collectionType, Type elementType, JsonTypeInfo? elementInfo)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        object result = Activator.CreateInstance(collectionType)!;
        MethodInfo add = collectionType.GetMethod(nameof(HashSet<>.Add), [elementType])!;
        foreach (JsonElement item in document.RootElement.EnumerateArray())
        {
            add.Invoke(result, [ReadElement(item, elementType, elementInfo)]);
        }

        return result;
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The collection type is rooted by the user query.")]
    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "The collection type is rooted by the user query.")]
    private static object MaterializeList(string json, Type collectionType, Type elementType, JsonTypeInfo? elementInfo)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        IList result = (IList)Activator.CreateInstance(collectionType)!;
        foreach (JsonElement item in document.RootElement.EnumerateArray())
        {
            result.Add(ReadElement(item, elementType, elementInfo));
        }

        return result;
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Runtime JSON deserialization is allowed only with reflection fallback enabled.")]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Runtime JSON deserialization is allowed only with reflection fallback enabled.")]
    private static object? ReadElement(JsonElement item, Type elementType, JsonTypeInfo? elementInfo)
    {
        if (elementInfo != null)
        {
            return JsonSerializer.Deserialize(item.GetRawText(), elementInfo);
        }

        return JsonSerializer.Deserialize(item.GetRawText(), elementType);
    }
}
