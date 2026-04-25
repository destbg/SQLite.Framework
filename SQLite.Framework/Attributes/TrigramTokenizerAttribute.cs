namespace SQLite.Framework.Attributes;

/// <summary>
/// Configures the FTS5 <c>trigram</c> tokenizer. Trigram indexing makes substring search work
/// (so a search for <c>"sqli"</c> matches <c>"sqlite"</c>) at the cost of a larger index.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class TrigramTokenizerAttribute : Attribute
{
    /// <summary>
    /// When <see langword="true" />, search is case-sensitive. Defaults to <see langword="false" />.
    /// </summary>
    public bool CaseSensitive { get; set; }

    /// <summary>
    /// When <see langword="true" />, diacritics are removed before indexing. Defaults to <see langword="true" />.
    /// </summary>
    public bool RemoveDiacritics { get; set; } = true;
}
