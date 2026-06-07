using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class NullableEnumToStringBehaviorTests
{
    [Fact]
    public void ProjectNullableEnumName()
    {
        Run(q => q.Select(r => r.Color.ToString()));
    }

    [Fact]
    public void WhereNullableEnumNameEqualsValue()
    {
        RunWhere(q => q.Where(r => r.Color.ToString() == "Green"));
    }

    [Fact]
    public void WhereNullableEnumNameEqualsEmpty()
    {
        RunWhere(q => q.Where(r => r.Color.ToString() == ""));
    }

    [Fact]
    public void OrderByNullableEnumName()
    {
        using TestDatabase db = new();
        db.Table<NullableEnumRow>().Schema.CreateTable();
        db.Table<NullableEnumRow>().AddRange(Data());

        List<int> oracle = Data().OrderBy(r => r.Color.ToString()).ThenBy(r => r.Id).Select(r => r.Id).ToList();
        List<int> actual = db.Table<NullableEnumRow>().OrderBy(r => r.Color.ToString()).ThenBy(r => r.Id).Select(r => r.Id).ToList();

        Assert.Equal(oracle, actual);
    }

    private static void Run(Func<IQueryable<NullableEnumRow>, IQueryable<string>> project)
    {
        using TestDatabase db = new();
        db.Table<NullableEnumRow>().Schema.CreateTable();
        db.Table<NullableEnumRow>().AddRange(Data());

        List<string> oracle = project(Data().AsQueryable().OrderBy(r => r.Id)).ToList();
        List<string> actual = project(db.Table<NullableEnumRow>().OrderBy(r => r.Id)).ToList();

        Assert.Equal(oracle, actual);
    }

    private static void RunWhere(Func<IQueryable<NullableEnumRow>, IQueryable<NullableEnumRow>> filter)
    {
        using TestDatabase db = new();
        db.Table<NullableEnumRow>().Schema.CreateTable();
        db.Table<NullableEnumRow>().AddRange(Data());

        List<int> oracle = filter(Data().AsQueryable()).Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actual = filter(db.Table<NullableEnumRow>()).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal(oracle, actual);
    }

    private static List<NullableEnumRow> Data()
    {
        return new List<NullableEnumRow>
        {
            new() { Id = 1, Color = EfColorEnum.Green },
            new() { Id = 2, Color = null },
            new() { Id = 3, Color = EfColorEnum.Red },
            new() { Id = 4, Color = (EfColorEnum)99 },
            new() { Id = 5, Color = EfColorEnum.Blue },
        };
    }
}

public class NullableEnumRow
{
    [System.ComponentModel.DataAnnotations.Key] public int Id { get; set; }
    public EfColorEnum? Color { get; set; }
}
