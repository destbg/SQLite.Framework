using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;
using System.Linq;

namespace SQLite.Framework.Tests;

[Table("WsDefault")]
public sealed class WsDefaultRow
{
    [Key]
    public int Id { get; set; }

    public string? Note { get; set; }
}

public class MigrateInPlaceLiteralWhitespaceDriftTests
{
    [Fact]
    public void InPlaceMigrateReconcilesDefaultThatDiffersOnlyByLiteralWhitespace()
    {
        using ModelTestDatabase db = new(model => model.Entity<WsDefaultRow>().Default(m => m.Note, "x y"));
        db.Execute("CREATE TABLE \"WsDefault\" (\"Id\" INTEGER PRIMARY KEY, \"Note\" TEXT NULL DEFAULT 'xy')");
        db.Execute("INSERT INTO \"WsDefault\" (\"Id\") VALUES (1)");

        db.Table<WsDefaultRow>().Schema.Migrate();

        db.Table<WsDefaultRow>().Add(new WsDefaultRow { Id = 2 });
        string actual = db.ExecuteScalar<string>("SELECT \"Note\" FROM \"WsDefault\" WHERE \"Id\" = 2")!;

        string oracle = "x y";
        Assert.Equal(oracle, actual);
    }
}