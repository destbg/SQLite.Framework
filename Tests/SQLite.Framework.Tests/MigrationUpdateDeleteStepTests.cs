using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("UpdDelRows")]
public class UpdateDeleteStepRow
{
    [Key]
    public int Id { get; set; }

    public string Status { get; set; } = "";

    public bool Archived { get; set; }
}

public class MigrationUpdateDeleteStepTests
{
    [Fact]
    public void UpdateStepUpdatesEveryRow()
    {
        using TestDatabase db = new(useFile: true);
        Seed(db);

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<UpdateDeleteStepRow>().Update<UpdateDeleteStepRow>(s => s.Set(x => x.Status, "done")))
            .Migrate();

        Assert.Equal(["done", "done", "done"], db.Table<UpdateDeleteStepRow>().OrderBy(x => x.Id).Select(x => x.Status).ToList());
    }

    [Fact]
    public void UpdateStepWithPredicateUpdatesMatchingRows()
    {
        using TestDatabase db = new(useFile: true);
        Seed(db);

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<UpdateDeleteStepRow>().Update<UpdateDeleteStepRow>(x => x.Id > 1, s => s.Set(x => x.Status, "done")))
            .Migrate();

        Assert.Equal(["new", "done", "done"], db.Table<UpdateDeleteStepRow>().OrderBy(x => x.Id).Select(x => x.Status).ToList());
    }

    [Fact]
    public void UpdateStepIgnoresQueryFilters()
    {
        using TestDatabase db = new(b => b.AddQueryFilter<UpdateDeleteStepRow>(x => !x.Archived), useFile: true);
        Seed(db);

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<UpdateDeleteStepRow>().Update<UpdateDeleteStepRow>(s => s.Set(x => x.Status, "done")))
            .Migrate();

        Assert.Equal(["done", "done", "done"], db.Table<UpdateDeleteStepRow>().IgnoreQueryFilters().OrderBy(x => x.Id).Select(x => x.Status).ToList());
    }

    [Fact]
    public void DeleteStepDeletesEveryRow()
    {
        using TestDatabase db = new(useFile: true);
        Seed(db);

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<UpdateDeleteStepRow>().Delete<UpdateDeleteStepRow>())
            .Migrate();

        Assert.Empty(db.Table<UpdateDeleteStepRow>().ToList());
    }

    [Fact]
    public void DeleteStepWithPredicateDeletesMatchingRows()
    {
        using TestDatabase db = new(useFile: true);
        Seed(db);

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<UpdateDeleteStepRow>().Delete<UpdateDeleteStepRow>(x => x.Id > 1))
            .Migrate();

        Assert.Equal([1], db.Table<UpdateDeleteStepRow>().Select(x => x.Id).ToList());
    }

    [Fact]
    public void DeleteStepIgnoresQueryFilters()
    {
        using TestDatabase db = new(b => b.AddQueryFilter<UpdateDeleteStepRow>(x => !x.Archived), useFile: true);
        Seed(db);

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<UpdateDeleteStepRow>().Delete<UpdateDeleteStepRow>())
            .Migrate();

        Assert.Empty(db.Table<UpdateDeleteStepRow>().IgnoreQueryFilters().ToList());
    }

    [Fact]
    public void UpdateStepRunsThroughMigrateAsync()
    {
        using TestDatabase db = new(useFile: true);
        Seed(db);

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<UpdateDeleteStepRow>().Update<UpdateDeleteStepRow>(s => s.Set(x => x.Status, "async")))
            .MigrateAsync().GetAwaiter().GetResult();

        Assert.Equal(["async", "async", "async"], db.Table<UpdateDeleteStepRow>().OrderBy(x => x.Id).Select(x => x.Status).ToList());
    }

    [Fact]
    public void NullSettersThrows()
    {
        using TestDatabase db = new(useFile: true);

        Assert.Throws<ArgumentNullException>(() => db.Schema.Migrations()
            .Version(1, m => m.Update<UpdateDeleteStepRow>(null!))
            .Migrate());
        Assert.Throws<ArgumentNullException>(() => db.Schema.Migrations()
            .Version(1, m => m.Update<UpdateDeleteStepRow>(x => x.Id > 1, null!))
            .Migrate());
    }

    [Fact]
    public void NullPredicateThrows()
    {
        using TestDatabase db = new(useFile: true);

        Assert.Throws<ArgumentNullException>(() => db.Schema.Migrations()
            .Version(1, m => m.Update<UpdateDeleteStepRow>(null!, s => s.Set(x => x.Status, "x")))
            .Migrate());
        Assert.Throws<ArgumentNullException>(() => db.Schema.Migrations()
            .Version(1, m => m.Delete<UpdateDeleteStepRow>(null!))
            .Migrate());
    }

    [Fact]
    public void PlanDescribesUpdateAndDelete()
    {
        using TestDatabase db = new(useFile: true);

        SQLiteMigrationPlan plan = db.Schema.Migrations()
            .Version(1, m => m
                .Update<UpdateDeleteStepRow>(s => s.Set(x => x.Status, "done"))
                .Delete<UpdateDeleteStepRow>(x => x.Archived))
            .Plan();

        Assert.Equal(["update \"UpdDelRows\"", "delete from \"UpdDelRows\""], plan.Operations);
    }

    private static void Seed(TestDatabase db)
    {
        db.Table<UpdateDeleteStepRow>().Schema.CreateTable();
        db.Table<UpdateDeleteStepRow>().AddRange(new[]
        {
            new UpdateDeleteStepRow { Id = 1, Status = "new", Archived = false },
            new UpdateDeleteStepRow { Id = 2, Status = "new", Archived = false },
            new UpdateDeleteStepRow { Id = 3, Status = "new", Archived = true },
        });
    }
}
