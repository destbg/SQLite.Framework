namespace SQLite.Framework.Attributes;

/// <summary>
/// Configures the FTS5 <c>trigram</c> tokenizer. Trigram indexing makes substring search work
/// (so a search for <c>"sqli"</c> matches <c>"sqlite"</c>) at the cost of a larger index.
/// Trigram requires SQLite 3.34 or newer.
/// </summary>
#if SQLITECIPHER
[Obsolete("The trigram tokenizer is not available in SQLCipher's bundled SQLite. Use SQLite.Framework or SQLite.Framework.Bundled if you need trigram tokenization.", error: true)]
#elif SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
[UnsupportedOSPlatform("android")]
[SupportedOSPlatform("android33.0")]
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
    /// When <see langword="true" />, diacritics are removed before indexing. Defaults to <see langword="true" />.
    /// </summary>
    public bool RemoveDiacritics { get; set; } = true;
}
