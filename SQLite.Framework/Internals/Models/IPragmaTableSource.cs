namespace SQLite.Framework.Internals.Models;

/// <summary>
/// Non-generic view of <see cref="SQLitePragmaTable{T}" /> used by the translator. Lets the
/// constant-visitor read the pragma name and arguments without knowing the row type at
/// compile time.
/// </summary>
internal interface IPragmaTableSource
{
    string PragmaName { get; }
    IReadOnlyList<object?> Arguments { get; }
    Type ElementType { get; }
}
