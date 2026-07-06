namespace SQLite.Framework.Internals.JSON;

/// <summary>
/// Exposes the <see cref="JsonTypeInfo" /> a JSON converter serializes with, so translation can
/// resolve the names and forms the serializer writes into the stored document.
/// </summary>
internal interface IJsonTypeInfoSource
{
    /// <summary>
    /// The type info the converter passes to the serializer.
    /// </summary>
    JsonTypeInfo TypeInfo { get; }
}
