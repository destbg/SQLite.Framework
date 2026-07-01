using System.ComponentModel.DataAnnotations.Schema;

namespace SQLite.Framework.Models;

/// <summary>
/// One row of SQLite's built-in <c>sqlite_master</c> (also known as <c>sqlite_schema</c>)
/// table. Each row describes a table, index, view or trigger in the database. Reachable
/// as a queryable through <see cref="SQLitePragmas.Master" />.
/// </summary>
[Table("sqlite_master")]
public class SQLiteMaster
{
    /// <summary>
    /// The kind of object the row describes: <c>table</c>, <c>index</c>, <c>view</c> or
    /// <c>trigger</c>.
    /// </summary>
    [Column("type")]
    public required string Type { get; set; }

    /// <summary>
    /// The name of the object (table, index, view or trigger).
    /// </summary>
    [Column("name")]
    public required string Name { get; set; }

    /// <summary>
    /// For tables and views, the same as <see cref="Name" />. For indexes and triggers,
    /// the name of the table the object is attached to.
    /// </summary>
    [Column("tbl_name")]
    public required string TableName { get; set; }

    /// <summary>
    /// The root B-tree page for the object or <c>0</c> for views, triggers and virtual
    /// tables.
    /// </summary>
    [Column("rootpage")]
    public required long RootPage { get; set; }

    /// <summary>
    /// The DDL statement that created the object. <see langword="null" /> for some
    /// auto-created entries (for example, the implicit <c>autoindex</c> indexes).
    /// </summary>
    [Column("sql")]
    public string? Sql { get; set; }
}
