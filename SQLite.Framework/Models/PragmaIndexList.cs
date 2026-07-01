using System.ComponentModel.DataAnnotations.Schema;

namespace SQLite.Framework.Models;

/// <summary>
/// One row returned by SQLite's <c>pragma_index_list(name)</c> table-valued function. Each
/// row describes one index attached to the table.
/// </summary>
public class PragmaIndexList
{
    /// <summary>
    /// A sequence number assigned to each index for the table, starting at zero.
    /// </summary>
    [Column("seq")]
    public required long SequenceNumber { get; set; }

    /// <summary>
    /// The index name.
    /// </summary>
    [Column("name")]
    public required string Name { get; set; }

    /// <summary>
    /// <see langword="true" /> when the index enforces uniqueness.
    /// </summary>
    [Column("unique")]
    public required bool IsUnique { get; set; }

    /// <summary>
    /// How the index was created. <c>c</c> means it came from a <c>CREATE INDEX</c>
    /// statement, <c>u</c> from a <c>UNIQUE</c> constraint and <c>pk</c> from a primary key.
    /// </summary>
    [Column("origin")]
    public required string Origin { get; set; }

    /// <summary>
    /// <see langword="true" /> when the index is partial (has a <c>WHERE</c> clause).
    /// </summary>
    [Column("partial")]
    public required bool IsPartial { get; set; }
}
