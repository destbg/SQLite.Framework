namespace SQLite.Framework.Enums;

/// <summary>
/// Controls how the FTS5 <c>unicode61</c> tokenizer handles diacritics (accents).
/// </summary>
public enum Unicode61Diacritics
{
    /// <summary>
    /// Keep diacritics. Searching for "cafe" will not match "café".
    /// </summary>
    Keep = 0,

    /// <summary>
    /// Remove diacritics that map to ASCII letters only. Older FTS5 default.
    /// </summary>
    RemoveAscii = 1,

    /// <summary>
    /// Remove all diacritics. Searching for "cafe" matches "café".
    /// </summary>
    RemoveAll = 2,
}
