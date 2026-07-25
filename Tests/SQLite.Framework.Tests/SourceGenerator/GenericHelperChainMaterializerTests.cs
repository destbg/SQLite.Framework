using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21kStoredRows")]
public class H21kStoredRow
{
    [Key]
    public int Id { get; set; }

    public int Value { get; set; }
}

[Table("H21kChainRows")]
public class H21kChainRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class H21kChainViewBase
{
    public int Id { get; set; }

    public string Label { get; set; } = "";
}

public class H21kChainView : H21kChainViewBase
{
}

public class H21kStoreBase<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>
{
    private readonly TestDatabase database;

    public H21kStoreBase(TestDatabase database)
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

public class H21kStore<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T> : H21kStoreBase<T>
{
    public H21kStore(TestDatabase database)
        : base(database)
    {
    }
}

public class GenericHelperChainMaterializerTests
{
    private static List<H21kStoredRow> StoredRows()
    {
        return
        [
            new H21kStoredRow { Id = 1, Value = 10 },
            new H21kStoredRow { Id = 2, Value = 20 }
        ];
    }

    private static List<H21kChainRow> ChainRows()
    {
        return
        [
            new H21kChainRow { Id = 1, Name = "a" },
            new H21kChainRow { Id = 2, Name = "b" }
        ];
    }

    private static List<H21kChainView> ProjectAll(TestDatabase db)
    {
        return ProjectVia<H21kChainView>(db.Table<H21kChainRow>());
    }

    private static List<TResult> ProjectVia<TResult>(IQueryable<H21kChainRow> query)
        where TResult : H21kChainViewBase, new()
    {
        return ProjectCore<TResult>(query);
    }

    private static List<TResult> ProjectCore<TResult>(IQueryable<H21kChainRow> query)
        where TResult : H21kChainViewBase, new()
    {
        return query.Select(r => new TResult { Id = r.Id, Label = r.Name }).ToList();
    }

    [Fact]
    public void EntityReachedThroughGenericBaseStoreMatchesLinq()
    {
        using TestDatabase db = new();
        H21kStore<H21kStoredRow> store = new(db);
        store.Create();
        store.Insert(StoredRows());

        List<(int Id, int Value)> expected = StoredRows()
            .OrderBy(r => r.Id)
            .Select(r => (r.Id, r.Value))
            .ToList();

        List<(int Id, int Value)> actual = store.Load()
            .OrderBy(r => r.Id)
            .Select(r => (r.Id, r.Value))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProjectionThroughForwardedGenericHelperMatchesLinq()
    {
        using TestDatabase db = new();
        db.Table<H21kChainRow>().Schema.CreateTable();
        db.Table<H21kChainRow>().AddRange(ChainRows());

        List<(int Id, string Label)> expected = ChainRows()
            .OrderBy(r => r.Id)
            .Select(r => (r.Id, Label: r.Name))
            .ToList();

        List<(int Id, string Label)> actual = ProjectAll(db)
            .OrderBy(v => v.Id)
            .Select(v => (v.Id, v.Label))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
