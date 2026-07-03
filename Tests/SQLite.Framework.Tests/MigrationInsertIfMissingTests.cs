using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("MissingCategory")]
public class MissingCategoryRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public string? Name { get; set; }
}

[Table("MissingStatus")]
public class MissingStatusRow
{
    [Key]
    public int Id { get; set; }

    public MissingState State { get; set; }
}

public enum MissingState
{
    Draft = 0,
    Active = 1,
}

public class MigrationInsertIfMissingTests
{
    [Fact]
    public void InsertsAllRowsIntoAnEmptyTable()
    {
        using TestDatabase db = new(useFile: true);

        db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<MissingCategoryRow>()
                .InsertIfMissing(x => x.Name, new MissingCategoryRow { Name = "fiction" }, new MissingCategoryRow { Name = "science" }))
            .Migrate();

        Assert.Equal(["fiction", "science"], db.Table<MissingCategoryRow>().OrderBy(r => r.Id).Select(r => r.Name).ToList());
        Assert.Equal(1, db.Pragmas.UserVersion);
    }

    [Fact]
    public void SkipsRowsWhoseKeyIsAlreadyInTheTable()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<MissingCategoryRow>()
                .Insert(new MissingCategoryRow { Name = "fiction" }, new MissingCategoryRow { Name = "science" }))
            .Migrate();

        List<string?> existing = db.Table<MissingCategoryRow>().OrderBy(r => r.Id).Select(r => r.Name).ToList();
        List<string?> candidates = ["science", "history"];
        List<string?> expected = existing.Concat(candidates.Where(c => !existing.Contains(c))).ToList();

        db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<MissingCategoryRow>()
                .Insert(new MissingCategoryRow { Name = "fiction" }, new MissingCategoryRow { Name = "science" }))
            .Version(2, m => m.InsertIfMissing(x => x.Name, new MissingCategoryRow { Name = "science" }, new MissingCategoryRow { Name = "history" }))
            .Migrate();

        Assert.Equal(expected, db.Table<MissingCategoryRow>().OrderBy(r => r.Id).Select(r => r.Name).ToList());
        Assert.Equal(2, db.Pragmas.UserVersion);
    }

    [Fact]
    public void InsertsNothingWhenEveryKeyExists()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<MissingCategoryRow>()
                .Insert(new MissingCategoryRow { Name = "fiction" }, new MissingCategoryRow { Name = "science" }))
            .Migrate();

        List<string?> before = db.Table<MissingCategoryRow>().OrderBy(r => r.Id).Select(r => r.Name).ToList();

        int count = db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<MissingCategoryRow>()
                .Insert(new MissingCategoryRow { Name = "fiction" }, new MissingCategoryRow { Name = "science" }))
            .Version(2, m => m.InsertIfMissing(x => x.Name, new MissingCategoryRow { Name = "science" }, new MissingCategoryRow { Name = "fiction" }))
            .Migrate();

        Assert.Equal(0, count);
        Assert.Equal(before, db.Table<MissingCategoryRow>().OrderBy(r => r.Id).Select(r => r.Name).ToList());
        Assert.Equal(2, db.Pragmas.UserVersion);
    }

    [Fact]
    public void MatchesRowsOnNullKeys()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<MissingCategoryRow>().Insert(new MissingCategoryRow { Name = null }))
            .Migrate();

        List<string?> existing = db.Table<MissingCategoryRow>().OrderBy(r => r.Id).Select(r => r.Name).ToList();
        List<string?> candidates = [null, "science"];
        List<string?> expected = existing.Concat(candidates.Where(c => !existing.Contains(c))).ToList();

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<MissingCategoryRow>().Insert(new MissingCategoryRow { Name = null }))
            .Version(2, m => m.InsertIfMissing(x => x.Name, new MissingCategoryRow { Name = null }, new MissingCategoryRow { Name = "science" }))
            .Migrate();

        Assert.Equal(expected, db.Table<MissingCategoryRow>().OrderBy(r => r.Id).Select(r => r.Name).ToList());
    }

    [Fact]
    public void ComparesKeysThroughEnumTextStorage()
    {
        using TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text), useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<MissingStatusRow>().Insert(new MissingStatusRow { Id = 1, State = MissingState.Active }))
            .Migrate();

        List<MissingState> existing = db.Table<MissingStatusRow>().OrderBy(r => r.Id).Select(r => r.State).ToList();
        List<MissingState> candidates = [MissingState.Active, MissingState.Draft];
        List<MissingState> expected = existing.Concat(candidates.Where(c => !existing.Contains(c))).ToList();

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<MissingStatusRow>().Insert(new MissingStatusRow { Id = 1, State = MissingState.Active }))
            .Version(2, m => m.InsertIfMissing(x => x.State, new MissingStatusRow { Id = 2, State = MissingState.Active }, new MissingStatusRow { Id = 3, State = MissingState.Draft }))
            .Migrate();

        Assert.Equal(expected, db.Table<MissingStatusRow>().OrderBy(r => r.Id).Select(r => r.State).ToList());
        Assert.Equal("Draft", db.ExecuteScalar<string>("SELECT \"State\" FROM \"MissingStatus\" WHERE \"Id\" = 3"));
    }

    [Fact]
    public void WritesBackKeysOnlyForInsertedRows()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<MissingCategoryRow>().Insert(new MissingCategoryRow { Name = "fiction" }))
            .Migrate();

        MissingCategoryRow skipped = new() { Name = "fiction" };
        MissingCategoryRow inserted = new() { Name = "history" };

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<MissingCategoryRow>().Insert(new MissingCategoryRow { Name = "fiction" }))
            .Version(2, m => m.InsertIfMissing(x => x.Name, skipped, inserted))
            .Migrate();

        Assert.Equal(0, skipped.Id);
        Assert.Equal(2, inserted.Id);
    }

    [Fact]
    public void AcceptsACastKeySelector()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<MissingStatusRow>().Insert(new MissingStatusRow { Id = 1, State = MissingState.Active }))
            .Migrate();

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<MissingStatusRow>().Insert(new MissingStatusRow { Id = 1, State = MissingState.Active }))
            .Version(2, m => m.InsertIfMissing(x => (long)x.Id, new MissingStatusRow { Id = 1, State = MissingState.Draft }, new MissingStatusRow { Id = 2, State = MissingState.Draft }))
            .Migrate();

        List<MissingStatusRow> rows = db.Table<MissingStatusRow>().OrderBy(r => r.Id).ToList();
        Assert.Equal(2, rows.Count);
        Assert.Equal(MissingState.Active, rows[0].State);
        Assert.Equal(MissingState.Draft, rows[1].State);
    }

    [Fact]
    public void ChecksRowsAgainstTheTableNotAgainstEachOther()
    {
        using TestDatabase db = new(useFile: true);

        db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<MissingCategoryRow>()
                .InsertIfMissing(x => x.Name, new MissingCategoryRow { Name = "fiction" }, new MissingCategoryRow { Name = "fiction" }))
            .Migrate();

        Assert.Equal(["fiction", "fiction"], db.Table<MissingCategoryRow>().OrderBy(r => r.Id).Select(r => r.Name).ToList());
    }

    [Fact]
    public async Task InsertsThroughMigrateAsync()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<MissingCategoryRow>().Insert(new MissingCategoryRow { Name = "fiction" }))
            .Migrate();

        await db.Schema.Migrations()
            .Version(1, m => m.CreateTable<MissingCategoryRow>().Insert(new MissingCategoryRow { Name = "fiction" }))
            .Version(2, m => m.InsertIfMissing(x => x.Name, new MissingCategoryRow { Name = "fiction" }, new MissingCategoryRow { Name = "history" }))
            .MigrateAsync();

        Assert.Equal(["fiction", "history"], db.Table<MissingCategoryRow>().OrderBy(r => r.Id).Select(r => r.Name).ToList());
    }

    [Fact]
    public void PlanReportsTheStepWithoutInserting()
    {
        using TestDatabase db = new(useFile: true);

        SQLiteMigrationPlan plan = db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<MissingCategoryRow>()
                .InsertIfMissing(x => x.Name, new MissingCategoryRow { Name = "fiction" }, new MissingCategoryRow { Name = "science" }))
            .Plan();

        Assert.False(plan.IsUpToDate);
        Assert.Equal(["create \"MissingCategory\"", "insert up to 2 row(s) into \"MissingCategory\" missing by \"Name\""], plan.Operations);
        Assert.False(db.Schema.TableExists<MissingCategoryRow>());
    }

    [Fact]
    public void NullKeyThrows()
    {
        using TestDatabase db = new(useFile: true);
        SQLiteMigrationRunner runner = db.Schema.Migrations()
            .Version(1, m => m.InsertIfMissing((Expression<Func<MissingCategoryRow, string?>>)null!, new MissingCategoryRow { Name = "fiction" }));

        Assert.Throws<ArgumentNullException>(() => runner.Migrate());
    }

    [Fact]
    public void NullRowsThrows()
    {
        using TestDatabase db = new(useFile: true);
        SQLiteMigrationRunner runner = db.Schema.Migrations()
            .Version(1, m => m.InsertIfMissing(x => x.Name, (MissingCategoryRow[])null!));

        Assert.Throws<ArgumentNullException>(() => runner.Migrate());
    }

    [Fact]
    public void WithoutRowsThrows()
    {
        using TestDatabase db = new(useFile: true);
        SQLiteMigrationRunner runner = db.Schema.Migrations()
            .Version(1, m => m.InsertIfMissing<MissingCategoryRow, string?>(x => x.Name));

        ArgumentException ex = Assert.Throws<ArgumentException>(() => runner.Migrate());
        Assert.StartsWith("InsertIfMissing requires at least one row.", ex.Message);
    }

    [Fact]
    public void KeyThatIsNotAPropertyAccessThrows()
    {
        using TestDatabase db = new(useFile: true);
        SQLiteMigrationRunner runner = db.Schema.Migrations()
            .Version(1, m => m.InsertIfMissing(x => x.Id + 1, new MissingCategoryRow { Name = "fiction" }));

        ArgumentException ex = Assert.Throws<ArgumentException>(() => runner.Migrate());
        Assert.StartsWith("The key must be a mapped property on the entity, like x => x.Name.", ex.Message);
    }

    [Fact]
    public void KeyThatIsNotAMappedColumnThrows()
    {
        using TestDatabase db = new(useFile: true);
        SQLiteMigrationRunner runner = db.Schema.Migrations()
            .Version(1, m => m.InsertIfMissing(x => x.Name!.Length, new MissingCategoryRow { Name = "fiction" }));

        Assert.Throws<ArgumentException>(() => runner.Migrate());
    }
}
