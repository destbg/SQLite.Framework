namespace SQLite.Framework.Attributes;

/// <summary>
/// Configures the FTS5 <c>trigram</c> tokenizer. Trigram indexing makes substring search work
/// (so a search for <c>"sqli"</c> matches <c>"sqlite"</c>) at the cost of a larger index.
/// Trigram requires SQLite 3.34 or newer.
/// </summary>
#if SQLITECIPHER
[Obsolete("The trigram tokenizer is not available in SQLCipher's bundled SQLite. Use SQLite.Framework or SQLite.Framework.Bundled if you need trigram tokenization.", error: true)]
#elif SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
[UnsupportedOSPlatform("ios")]
[SupportedOSPlatform("ios15.0")]
#endif
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class TrigramTokenizerAttribute : Attribute
{
    /// <summary>
    /// When <see langword="true" />, search is case-sensitive. Defaults to <see langword="false" />.
    /// </summary>
    public bool CaseSensitive { get; set; }

    /// <summary>
    /// When <see langword="true" />, diacritics are removed before indexing. Defaults to
    /// <see langword="false" />, which is SQLite's own trigram default. The trigram tokenizer
    /// only learned this option in SQLite 3.45.0, so setting it to <see langword="true" />
    /// requires SQLite 3.45.0 or newer. Leaving it at the default keeps the table portable to
    /// SQLite 3.34 and newer.
    /// </summary>
    public bool RemoveDiacritics { get; set; }
}
