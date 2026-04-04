using System.Diagnostics.CodeAnalysis;

namespace SQLite.Framework.JsonB;

/// <summary>
/// Marker methods for SQLite JSON functions. These methods throw at runtime and are only
/// valid inside a LINQ query where they are translated to their SQL equivalents.
/// Register translations by calling <see cref="SQLiteStorageOptionsJsonExtensions.AddJson" />
/// on your <see cref="SQLiteStorageOptions" />.
/// </summary>
[ExcludeFromCodeCoverage]
public static class SQLiteJsonFunctions
{
    /// <summary>
    /// Extracts a value from a JSON document at the given path. Translates to <c>json_extract(json, path)</c>.
    /// </summary>
    public static T Extract<T>(string json, string path)
    {
        throw new InvalidOperationException("JsonFunctions.Extract can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Sets a value at the given path, creating intermediate nodes if needed. Translates to <c>json_set(json, path, value)</c>
    /// .
    /// </summary>
    public static string Set(string json, string path, object? value)
    {
        throw new InvalidOperationException("JsonFunctions.Set can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Inserts a value at the given path only if it does not already exist. Translates to
    /// <c>json_insert(json, path, value)</c>.
    /// </summary>
    public static string Insert(string json, string path, object? value)
    {
        throw new InvalidOperationException("JsonFunctions.Insert can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Replaces an existing value at the given path. Translates to <c>json_replace(json, path, value)</c>.
    /// </summary>
    public static string Replace(string json, string path, object? value)
    {
        throw new InvalidOperationException("JsonFunctions.Replace can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Removes the value at the given path. Translates to <c>json_remove(json, path)</c>.
    /// </summary>
    public static string Remove(string json, string path)
    {
        throw new InvalidOperationException("JsonFunctions.Remove can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the JSON type of the value at the given path as a string. Translates to <c>json_type(json, path)</c>.
    /// </summary>
    public static string Type(string json, string path)
    {
        throw new InvalidOperationException("JsonFunctions.Type can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns 1 if the argument is well-formed JSON, or 0 otherwise. Translates to <c>json_valid(json)</c>.
    /// </summary>
    public static bool Valid(string json)
    {
        throw new InvalidOperationException("JsonFunctions.Valid can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Applies a JSON Merge Patch to a JSON document. Translates to <c>json_patch(json, patch)</c>.
    /// </summary>
    public static string Patch(string json, string patch)
    {
        throw new InvalidOperationException("JsonFunctions.Patch can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the number of top-level elements in a JSON array or object. Translates to <c>json_array_length(json)</c>.
    /// </summary>
    public static int ArrayLength(string json)
    {
        throw new InvalidOperationException("JsonFunctions.ArrayLength can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the number of elements at the given path. Translates to <c>json_array_length(json, path)</c>.
    /// </summary>
    public static int ArrayLength(string json, string path)
    {
        throw new InvalidOperationException("JsonFunctions.ArrayLength can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns a minified JSON string. Translates to <c>json(json)</c>.
    /// </summary>
    public static string Minify(string json)
    {
        throw new InvalidOperationException("JsonFunctions.Minify can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Converts a JSON text value to a JSONB binary blob. Translates to <c>jsonb(json)</c>.
    /// </summary>
    public static byte[] ToJsonb(string json)
    {
        throw new InvalidOperationException("JsonFunctions.ToJsonb can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Extracts a value from a JSONB binary document at the given path. Translates to <c>jsonb_extract(json, path)</c>.
    /// </summary>
    public static T ExtractJsonb<T>(byte[] json, string path)
    {
        throw new InvalidOperationException("JsonFunctions.ExtractJsonb can only be used inside a LINQ query.");
    }
}