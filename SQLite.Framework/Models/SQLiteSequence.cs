using System.ComponentModel.DataAnnotations.Schema;

namespace SQLite.Framework.Models;

/// <summary>
/// One row of SQLite's built-in <c>sqlite_sequence</c> table, which tracks the highest
/// <c>AUTOINCREMENT</c> value assigned for each table that uses the
/// <c>INTEGER PRIMARY KEY AUTOINCREMENT</c> declaration. Reachable as a queryable through
/// <see cref="SQLitePragmas.Sequence" />.
/// </summary>
/// <remarks>
/// The <c>sqlite_sequence</c> table only exists when the database has at least one table
/// with an <c>AUTOINCREMENT</c> primary key. Querying it before that point throws.
/// </remarks>
[Table("sqlite_sequence")]
public class SQLiteSequence
{
    /// <summary>
    /// The name of the table the sequence belongs to.
    /// </summary>
    [Column("name")]
    public required string Name { get; set; }

    /// <summary>
    /// The largest <c>AUTOINCREMENT</c> rowid that has ever been assigned in the table.
    /// </summary>
    [Column("seq")]
    public required long LastValue { get; set; }
}
