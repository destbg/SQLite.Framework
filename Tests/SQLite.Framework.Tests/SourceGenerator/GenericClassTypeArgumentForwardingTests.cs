using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22nForwardStoreRows")]
public class H22nForwardStoreRow
{
    [Key]
    public int Id { get; set; }

    public int Value { get; set; }
}

[Table("H22nForwardProjectRows")]
public class H22nForwardProjectRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class H22nForwardViewBase
{
    public int Id { get; set; }

    public string Label { get; set; } = "";
}

public class H22nForwardView : H22nForwardViewBase
{
}

public class H22nForwardStore<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>
{
    private readonly TestDatabase database;

    public H22nForwardStore(TestDatabase database)
    {
        this.database = database;
    }

    public void Create()
    {
        database.Table<T>().Schema.CreateTable();
    }

    public void Insert(IEnumerable<T> items)
    {
        database.Table<T>().AddRange(items);
    }

    public List<T> Load()
    {
        return database.Table<T>().ToList();
    }
}

public class H22nForwardProjector<TView>
    where TView : H22nForwardViewBase, new()
{
    public List<TView> Project(IQueryable<H22nForwardProjectRow> query)
    {
        return query.Select(r => new TView { Id = r.Id, Label = r.Name }).ToList();
    }
}

public class GenericClassTypeArgumentForwardingTests
{
    [Fact]
    public void EntityReachedThroughAForwardedGenericStoreMatchesLinq()
    {
        using TestDatabase db = new();
        CreateVia<H22nForwardStoreRow>(db);
        InsertVia(db, StoreRows());

        List<(int Id, int Value)> expected = StoreRows()
            .OrderBy(r => r.Id)
            .Select(r => (r.Id, r.Value))
            .ToList();

        List<(int Id, int Value)> actual = LoadVia<H22nForwardStoreRow>(db)
            .OrderBy(r => r.Id)
            .Select(r => (r.Id, r.Value))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProjectionThroughAForwardedGenericProjectorMatchesLinq()
    {
        using TestDatabase db = new();
        db.Table<H22nForwardProjectRow>().Schema.CreateTable();
        db.Table<H22nForwardProjectRow>().AddRange(ProjectRows());

        List<(int Id, string Label)> expected = ProjectRows()
            .OrderBy(r => r.Id)
            .Select(r => (r.Id, Label: r.Name))
            .ToList();

        List<(int Id, string Label)> actual = ProjectVia<H22nForwardView>(db.Table<H22nForwardProjectRow>())
            .OrderBy(v => v.Id)
            .Select(v => (v.Id, v.Label))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22nForwardStoreRow> StoreRows()
    {
        return
        [
            new H22nForwardStoreRow { Id = 1, Value = 10 },
            new H22nForwardStoreRow { Id = 2, Value = 20 }
        ];
    }

    private static List<H22nForwardProjectRow> ProjectRows()
    {
        return
        [
            new H22nForwardProjectRow { Id = 1, Name = "a" },
            new H22nForwardProjectRow { Id = 2, Name = "b" }
        ];
    }

    private static void CreateVia<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(TestDatabase db)
    {
        new H22nForwardStore<T>(db).Create();
    }

    private static void InsertVia<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(TestDatabase db, IEnumerable<T> items)
    {
        new H22nForwardStore<T>(db).Insert(items);
    }

    private static List<T> LoadVia<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(TestDatabase db)
    {
        return new H22nForwardStore<T>(db).Load();
    }

    private static List<TView> ProjectVia<TView>(IQueryable<H22nForwardProjectRow> query)
        where TView : H22nForwardViewBase, new()
    {
        return new H22nForwardProjector<TView>().Project(query);
    }
}
