using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SupersededOpsLog")]
public class SupersededOpsLogRow
{
    [Key]
    public int Id { get; set; }

    public string Note { get; set; } = "";
}

[Table("SupersededBystander")]
public class SupersededBystanderRow
{
    [Key]
    public int Id { get; set; }

    public string Note { get; set; } = "";
}

[Table("SupersededScriptLog")]
public class SupersededScriptLogRow
{
    [Key]
    public int Id { get; set; }

    public string Note { get; set; } = "";
}

[Table("SupersededFtsItems")]
public class SupersededFtsItemRow
{
    [Key]
    public int Id { get; set; }

    public string Body { get; set; } = "";
}

[FullTextSearch(ContentMode = FtsContentMode.External, ContentTable = typeof(SupersededFtsItemRow))]
[Table("SupersededFtsSearch")]
public class SupersededFtsSearchRow
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public string Body { get; set; } = "";
}

public class MigrationSupersededDropDiscardsEarlierWorkTests
{
    [Fact]
    public void FreshRunKeepsOnlyWorkDeclaredAfterTheRecreate()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<SupersededOpsLogRow>()
                .CreateTable<SupersededBystanderRow>()
                .Insert(new SupersededOpsLogRow { Id = 1, Note = "seed" })
                .Insert(new SupersededBystanderRow { Id = 1, Note = "other" })
                .Update<SupersededOpsLogRow>(s => s.Set(x => x.Note, "updated"))
                .Delete<SupersededOpsLogRow>(x => x.Id > 100)
                .TableChanged<SupersededOpsLogRow>(s => s.Set(x => x.Note, "filled"))
                .DropColumn<SupersededOpsLogRow>("Ghost"))
            .Version(2, m => m.DropTable<SupersededOpsLogRow>())
            .Version(3, m => m
                .CreateTable<SupersededOpsLogRow>()
                .Insert(new SupersededOpsLogRow { Id = 2, Note = "kept" }))
            .Migrate();

        SupersededOpsLogRow survivor = db.Table<SupersededOpsLogRow>().Single();
        Assert.Equal(2, survivor.Id);
        Assert.Equal("kept", survivor.Note);
        Assert.Equal("other", db.Table<SupersededBystanderRow>().Single().Note);
    }

    [Fact]
    public void TwoSupersededDropsOfTheSameTableLeaveItEmpty()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<SupersededOpsLogRow>().Insert(new SupersededOpsLogRow { Id = 1, Note = "first" }))
            .Version(2, m => m.DropTable<SupersededOpsLogRow>())
            .Version(3, m => m.CreateTable<SupersededOpsLogRow>().Insert(new SupersededOpsLogRow { Id = 3, Note = "second" }))
            .Version(4, m => m.DropTable<SupersededOpsLogRow>())
            .Version(5, m => m.CreateTable<SupersededOpsLogRow>())
            .Migrate();

        Assert.Equal(0, db.Table<SupersededOpsLogRow>().Count());
    }

    [Fact]
    public void FullTextSearchRebuildBeforeASupersededDropDoesNotRefill()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<SupersededFtsItemRow>()
                .Insert(new SupersededFtsItemRow { Id = 1, Body = "hello world" })
                .CreateTable<SupersededFtsSearchRow>()
                .RebuildFullTextSearch<SupersededFtsSearchRow>())
            .Version(2, m => m.DropTable<SupersededFtsSearchRow>())
            .Version(3, m => m.CreateTable<SupersededFtsSearchRow>())
            .Migrate();

        Assert.Equal(0L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"SupersededFtsSearch\" WHERE \"SupersededFtsSearch\" MATCH 'hello'"));
    }

    [Fact]
    public void ScriptOmitsWorkSupersededByARecreate()
    {
        using TestDatabase db = new(useFile: true);
        IReadOnlyList<string> statements = db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<SupersededScriptLogRow>()
                .Insert(new SupersededScriptLogRow { Id = 1, Note = "seed" })
                .TableChanged<SupersededScriptLogRow>(s => s.Set(x => x.Note, "filled")))
            .Version(2, m => m.DropTable<SupersededScriptLogRow>())
            .Version(3, m => m.CreateTable<SupersededScriptLogRow>())
            .Script();

        Assert.DoesNotContain(statements, s => s.Contains("INSERT INTO \"SupersededScriptLog\"", StringComparison.Ordinal));
        Assert.DoesNotContain(statements, s => s.Contains("UPDATE \"SupersededScriptLog\"", StringComparison.Ordinal));
    }
}
