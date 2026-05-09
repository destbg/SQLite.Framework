using System.ComponentModel.DataAnnotations.Schema;

namespace SQLite.Framework.Models;

/// <summary>
/// One row returned by SQLite's <c>pragma_foreign_key_list(name)</c> table-valued function.
/// Each row describes one column in one of the table's foreign keys.
/// </summary>
public class PragmaForeignKey
{
    /// <summary>
    /// The foreign key id. Multiple rows may share the same id when the foreign key spans
    /// more than one column.
    /// </summary>
    [Column("id")]
    public required long Id { get; set; }

    /// <summary>
    /// The position of this column inside the foreign key, starting at zero.
    /// </summary>
    [Column("seq")]
    public required long ColumnPosition { get; set; }

    /// <summary>
    /// The name of the table the foreign key points to.
    /// </summary>
    [Column("table")]
    public required string ReferencedTable { get; set; }

    /// <summary>
    /// The column name on this table.
    /// </summary>
    [Column("from")]
    public required string FromColumn { get; set; }

    /// <summary>
    /// The column name on the referenced table.
    /// </summary>
    [Column("to")]
    public required string ToColumn { get; set; }

    /// <summary>
    /// The action to take when the referenced row is updated. One of <c>NO ACTION</c>,
    /// <c>RESTRICT</c>, <c>SET NULL</c>, <c>SET DEFAULT</c>, or <c>CASCADE</c>.
    /// </summary>
    [Column("on_update")]
    public required string OnUpdate { get; set; }

    /// <summary>
    /// The action to take when the referenced row is deleted.
    /// </summary>
    [Column("on_delete")]
    public required string OnDelete { get; set; }

    /// <summary>
    /// The match clause. Always <c>NONE</c> in current SQLite versions.
    /// </summary>
    [Column("match")]
    public required string MatchClause { get; set; }
}
