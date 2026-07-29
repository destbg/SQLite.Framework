namespace SQLite.Framework.Internals.Models;

/// <summary>
/// Describes how a bare row parameter binds its columns in a DDL-style translation: the SQL prefix
/// placed before each column name and whether a converter's read wrap applies to column reads.
/// </summary>
internal readonly struct RowColumnBinding
{
    public RowColumnBinding(string? prefix, bool wrapConverterReads)
    {
        Prefix = prefix;
        WrapConverterReads = wrapConverterReads;
    }

    public string? Prefix { get; }

    public bool WrapConverterReads { get; }
}
