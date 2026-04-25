namespace SQLite.Framework.Enums;

/// <summary>
/// The base tokenizer that the FTS5 <c>porter</c> stemmer wraps.
/// </summary>
public enum PorterBaseTokenizer
{
    /// <summary>
    /// Wraps the <c>unicode61</c> tokenizer. This is the default.
    /// </summary>
    Unicode61,

    /// <summary>
    /// Wraps the <c>ascii</c> tokenizer.
    /// </summary>
    Ascii,
}
