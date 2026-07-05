using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("TrimmedBook")]
public class TrimmedBookRow
{
    [Key]
    public int Id { get; set; }

    public string? Title { get; set; }
}

public class MigrationDropColumnScopeTests
{
    [Fact]
    public void DropColumnWithAnIndexedTargetKeepsOtherLiveColumns()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"TrimmedBook\" (\"Id\" INTEGER PRIMARY KEY, \"Title\" TEXT, \"LegacyA\" TEXT, \"LegacyB\" TEXT)");
        db.Execute("CREATE INDEX \"IX_TrimmedBook_LegacyA\" ON \"TrimmedBook\" (\"LegacyA\")");
        db.Execute("INSERT INTO \"TrimmedBook\" (\"Id\", \"Title\", \"LegacyA\", \"LegacyB\") VALUES (1, 't', 'a', 'keep')");

        db.Schema.Migrations()
            .Version(1, m => m.DropColumn<TrimmedBookRow>("LegacyA"))
            .Migrate();

        List<string> columns = db.Pragmas.TableInfo("TrimmedBook").Select(c => c.Name).ToList();
        Assert.Equal(["Id", "Title", "LegacyB"], columns);
        Assert.Equal("keep", db.ExecuteScalar<string>("SELECT \"LegacyB\" FROM \"TrimmedBook\" WHERE \"Id\" = 1"));
    }

    [Fact]
    public void DropColumnWithAnUnquotedIndexReferenceStillDropsTheColumn()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"TrimmedBook\" (\"Id\" INTEGER PRIMARY KEY, \"Title\" TEXT, \"LegacyA\" TEXT)");
        db.Execute("CREATE INDEX IX_TrimmedBook_Plain ON TrimmedBook(LegacyA)");
        db.Execute("INSERT INTO \"TrimmedBook\" (\"Id\", \"Title\", \"LegacyA\") VALUES (1, 't', 'a')");

        db.Schema.Migrations()
            .Version(1, m => m.DropColumn<TrimmedBookRow>("LegacyA"))
            .Migrate();

        List<string> columns = db.Pragmas.TableInfo("TrimmedBook").Select(c => c.Name).ToList();
        Assert.Equal(["Id", "Title"], columns);
    }
}
