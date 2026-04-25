using SQLite.Framework.Enums;

namespace SQLite.Framework.Attributes;

/// <summary>
/// Configures the FTS5 <c>porter</c> stemming tokenizer. The Porter stemmer reduces English words
/// to their stem so that "running" matches "ran" and "runs". It wraps another tokenizer; the wrapped
/// tokenizer does the actual splitting.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class PorterTokenizerAttribute : Attribute
{
    /// <summary>
    /// The base tokenizer that the Porter stemmer wraps. Defaults to <see cref="PorterBaseTokenizer.Unicode61" />.
    /// </summary>
    public PorterBaseTokenizer Base { get; set; } = PorterBaseTokenizer.Unicode61;

    /// <summary>
    /// For a Unicode61 base, controls how diacritics are handled. Ignored for other bases.
    /// </summary>
    public Unicode61Diacritics RemoveDiacritics { get; set; } = Unicode61Diacritics.RemoveAll;

    /// <summary>
    /// For a Unicode61 base, optional Unicode category mask. Ignored for other bases.
    /// </summary>
    public string? Categories { get; set; }

    /// <summary>
    /// Optional list of characters to treat as token separators.
    /// </summary>
    public string? Separators { get; set; }

    /// <summary>
    /// Optional list of characters to keep inside tokens.
    /// </summary>
    public string? TokenChars { get; set; }
}
