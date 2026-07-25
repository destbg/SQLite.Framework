using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ViewStepRows")]
public class ViewStepRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Value { get; set; }
}

[Table("ViewStepSummaries")]
public class ViewStepSummary
{
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class MigrationViewStepTests
{
    [Fact]
    public void CreateViewStepCreatesQueryableView()
    {
        using TestDatabase db = new(useFile: true);

        db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<ViewStepRow>()
                .Insert(new ViewStepRow { Id = 1, Name = "big", Value = 10 }, new ViewStepRow { Id = 2, Name = "small", Value = 1 })
                .CreateView<ViewStepSummary>(() =>
                    from r in db.Table<ViewStepRow>()
                    where r.Value > 5
                    select new ViewStepSummary { Id = r.Id, Name = r.Name }))
            .Migrate();

        Assert.True(db.Schema.ViewExists("ViewStepSummaries"));
        Assert.Equal("big", db.ReadOnlyTable<ViewStepSummary>().Single().Name);
    }

    [Fact]
    public void RedeclaredViewInLaterVersionReplacesBody()
    {
        using TestDatabase db = new(useFile: true);

        db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<ViewStepRow>()
                .Insert(new ViewStepRow { Id = 1, Name = "big", Value = 10 }, new ViewStepRow { Id = 2, Name = "small", Value = 1 })
                .CreateView<ViewStepSummary>(() =>
                    from r in db.Table<ViewStepRow>()
                    where r.Value > 5
                    select new ViewStepSummary { Id = r.Id, Name = r.Name }))
            .Version(2, m => m
                .CreateView<ViewStepSummary>(() =>
                    from r in db.Table<ViewStepRow>()
                    where r.Value <= 5
                    select new ViewStepSummary { Id = r.Id, Name = r.Name }))
            .Migrate();

        Assert.Equal("small", db.ReadOnlyTable<ViewStepSummary>().Single().Name);
    }

    [Fact]
    public void DropViewStepRemovesView()
    {
        using TestDatabase db = new(useFile: true);
        db.Table<ViewStepRow>().Schema.CreateTable();
        db.Schema.CreateView<ViewStepSummary>(() =>
            from r in db.Table<ViewStepRow>()
            select new ViewStepSummary { Id = r.Id, Name = r.Name });

        db.Schema.Migrations()
            .Version(1, m => m.DropView<ViewStepSummary>())
            .Migrate();

        Assert.False(db.Schema.ViewExists("ViewStepSummaries"));
    }

    [Fact]
    public void DropViewByNameIsTolerantWhenAbsent()
    {
        using TestDatabase db = new(useFile: true);

        db.Schema.Migrations()
            .Version(1, m => m.DropView("NoSuchView"))
            .Migrate();

        Assert.Equal(1, db.Pragmas.UserVersion);
    }

    [Fact]
    public void NullQueryThrows()
    {
        using TestDatabase db = new(useFile: true);

        Assert.Throws<ArgumentNullException>(() => db.Schema.Migrations()
            .Version(1, m => m.CreateView<ViewStepSummary>(null!))
            .Migrate());
    }

    [Fact]
    public void EmptyViewNameThrows()
    {
        using TestDatabase db = new(useFile: true);

        Assert.Throws<ArgumentException>(() => db.Schema.Migrations()
            .Version(1, m => m.DropView(""))
            .Migrate());
    }

    [Fact]
    public void PlanDescribesViewSteps()
    {
        using TestDatabase db = new(useFile: true);

        SQLiteMigrationPlan plan = db.Schema.Migrations()
            .Version(1, m => m
                .CreateView<ViewStepSummary>(() =>
                    from r in db.Table<ViewStepRow>()
                    select new ViewStepSummary { Id = r.Id, Name = r.Name })
                .DropView("OldView"))
            .Plan();

        Assert.Equal(["create view \"ViewStepSummaries\"", "drop view \"OldView\""], plan.Operations);
    }
}
