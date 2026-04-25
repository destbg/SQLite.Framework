namespace SQLite.Framework.Models;

/// <summary>
/// One column entry returned by <see cref="SQLiteSchema.ListColumns{T}" />. The values come
/// straight from <c>PRAGMA table_info(...)</c>, so they reflect what SQLite actually stored,
/// not what your model expects.
/// </summary>
public class SchemaColumnInfo
{
    /// <summary>
    /// The column name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The declared column type as SQLite stored it (for example <c>INTEGER</c>).
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// <see langword="true" /> when the column allows nulls.
    /// </summary>
    public required bool IsNullable { get; init; }

    /// <summary>
    /// <see langword="true" /> when the column is part of the primary key.
    /// </summary>
    public required bool IsPrimaryKey { get; init; }

    /// <summary>
    /// The default value expression for the column, or <see langword="null" /> when none was declared.
    /// </summary>
    public required string? DefaultValue { get; init; }
}
