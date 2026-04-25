using SQLite.Framework.Enums;

namespace SQLite.Framework.Attributes;

/// <summary>
/// Configures the FTS5 <c>unicode61</c> tokenizer for the FTS table. This is the default tokenizer
/// when no tokenizer attribute is present.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class Unicode61TokenizerAttribute : Attribute
{
    /// <summary>
    /// How to handle diacritics. Defaults to <see cref="Unicode61Diacritics.RemoveAll" />.
    /// </summary>
    public Unicode61Diacritics RemoveDiacritics { get; set; } = Unicode61Diacritics.RemoveAll;

    /// <summary>
    /// Optional Unicode category mask, for example <c>"L* N* Co"</c> to keep letters, numbers, and private use.
    /// </summary>
    public string? Categories { get; set; }

    /// <summary>
    /// Optional list of characters to treat as token separators.
    /// </summary>
    public string? Separators { get; set; }

    /// <summary>
    /// Optional list of characters to keep inside tokens that would otherwise be separators.
    /// </summary>
    public string? TokenChars { get; set; }
}
