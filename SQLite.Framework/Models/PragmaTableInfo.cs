using System.ComponentModel.DataAnnotations.Schema;

namespace SQLite.Framework.Models;

/// <summary>
/// One row returned by SQLite's <c>pragma_table_info(name)</c> table-valued function. Each
/// row describes one column of the table.
/// </summary>
public class PragmaTableInfo
{
    /// <summary>
    /// The column index inside the table, starting at zero.
    /// </summary>
    [Column("cid")]
    public required long ColumnId { get; set; }

    /// <summary>
    /// The column name.
    /// </summary>
    [Column("name")]
    public required string Name { get; set; }

    /// <summary>
    /// The declared column type, for example <c>INTEGER</c> or <c>TEXT</c>.
    /// </summary>
    [Column("type")]
    public required string Type { get; set; }

    /// <summary>
    /// <see langword="true" /> when the column is declared <c>NOT NULL</c>.
    /// </summary>
    [Column("notnull")]
    public required bool IsNotNull { get; set; }

    /// <summary>
    /// The default value expression for the column or <see langword="null" /> when none was
    /// declared.
    /// </summary>
    [Column("dflt_value")]
    public string? DefaultValue { get; set; }

    /// <summary>
    /// The position of the column in the primary key (starting at <c>1</c>) or <c>0</c>
    /// when the column is not part of the primary key.
    /// </summary>
    [Column("pk")]
    public required long PrimaryKeyOrder { get; set; }
}
