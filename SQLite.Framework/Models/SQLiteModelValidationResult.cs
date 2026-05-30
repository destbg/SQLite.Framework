namespace SQLite.Framework.Models;

/// <summary>
/// The outcome of validating an entity model against the live database schema. Returned by
/// <see cref="SQLiteSchema.ValidateModel{T}()" />. When <see cref="IsValid" /> is
/// <see langword="false" />, <see cref="Issues" /> lists each drift found, one message per problem.
/// </summary>
public sealed class SQLiteModelValidationResult
{
    internal SQLiteModelValidationResult(IReadOnlyList<string> issues)
    {
        Issues = issues;
    }

    /// <summary>
    /// One human-readable message per piece of drift between the model and the database. Empty when
    /// the model matches.
    /// </summary>
    public IReadOnlyList<string> Issues { get; }

    /// <summary>
    /// <see langword="true" /> when no drift was found.
    /// </summary>
    public bool IsValid => Issues.Count == 0;
}
