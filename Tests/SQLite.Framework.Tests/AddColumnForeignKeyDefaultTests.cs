using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("FkDefaultParent")]
file sealed class FkDefaultParent
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("FkDefaultChild")]
file sealed class FkDefaultChild
{
    [Key]
    public int Id { get; set; }

    [ReferencesTable(typeof(FkDefaultParent))]
    public int ParentId { get; set; }

    public string Note { get; set; } = "";
}

public class AddColumnForeignKeyDefaultTests
{
    [Fact]
    public void AddColumnWithDefaultOnForeignKeyColumnBackfillsLikeRawSqlite()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"FkDefaultParent\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL)");
        db.Execute("CREATE TABLE \"FkDefaultChild\" (\"Id\" INTEGER PRIMARY KEY, \"Note\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"FkDefaultChild\" (\"Id\", \"Note\") VALUES (1, 'a')");

        db.Table<FkDefaultChild>().Schema.AddColumn(c => (object?)c.ParentId, defaultValue: 0);

        int backfilled = db.Query<int>("SELECT \"ParentId\" FROM \"FkDefaultChild\" WHERE \"Id\" = 1").First();
        Assert.Equal(0, backfilled);
    }
}
