namespace SQLite.Framework.Attributes;

/// <summary>
/// Configures the FTS5 <c>ascii</c> tokenizer. Faster than <c>unicode61</c> but only handles ASCII text.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class AsciiTokenizerAttribute : Attribute
{
    /// <summary>
    /// Optional list of characters to treat as token separators.
    /// </summary>
    public string? Separators { get; set; }

    /// <summary>
    /// Optional list of characters to keep inside tokens.
    /// </summary>
    public string? TokenChars { get; set; }
}
