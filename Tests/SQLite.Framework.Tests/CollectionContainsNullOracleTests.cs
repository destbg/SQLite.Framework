using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CollectionContainsNullOracleTests
{
    private static readonly (int id, int? value)[] Rows =
    {
        (1, null),
        (2, 5),
        (3, 7),
    };

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();
        foreach ((int id, int? value) in Rows)
        {
            db.Table<NullableEntity>().Add(new NullableEntity { Id = id, Value = value });
        }

        return db;
    }

    private static List<NullableEntity> InMemory()
    {
        return Rows.Select(r => new NullableEntity { Id = r.id, Value = r.value }).ToList();
    }

    [Fact]
    public void ContainsWithNullAndNonNullElementsMatchesDotNet()
    {
        using TestDatabase db = Seed();
        int?[] arr = { 5, null };

        List<int> actual = db.Table<NullableEntity>().Where(x => arr.Contains(x.Value)).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> oracle = InMemory().Where(x => arr.Contains(x.Value)).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ContainsWithOnlyNullElementMatchesDotNet()
    {
        using TestDatabase db = Seed();
        int?[] arr = { null };

        List<int> actual = db.Table<NullableEntity>().Where(x => arr.Contains(x.Value)).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> oracle = InMemory().Where(x => arr.Contains(x.Value)).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ContainsWithNoNullElementsMatchesDotNet()
    {
        using TestDatabase db = Seed();
        int?[] arr = { 5, 7 };

        List<int> actual = db.Table<NullableEntity>().Where(x => arr.Contains(x.Value)).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> oracle = InMemory().Where(x => arr.Contains(x.Value)).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ContainsEmptyListMatchesDotNet()
    {
        using TestDatabase db = Seed();
        int?[] arr = Array.Empty<int?>();

        List<int> actual = db.Table<NullableEntity>().Where(x => arr.Contains(x.Value)).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> oracle = InMemory().Where(x => arr.Contains(x.Value)).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ContainsWithComputedProbeAndNullElementMatchesDotNet()
    {
        using TestDatabase db = Seed();
        int offset = 0;
        int?[] arr = { 5, null };

        List<int> actual = db.Table<NullableEntity>().Where(x => arr.Contains(x.Value + offset)).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> oracle = InMemory().Where(x => arr.Contains(x.Value + offset)).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ContainsNullableStringWithNullElementMatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NullableStringEntity>().Schema.CreateTable();
        db.CreateCommand("INSERT INTO NullableStringEntity (\"Id\",\"Name\") VALUES (1,NULL),(2,'a'),(3,'b')", []).ExecuteNonQuery();
        string?[] arr = { "a", null };

        List<int> actual = db.Table<NullableStringEntity>().Where(x => arr.Contains(x.Name)).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> oracle = new NullableStringEntity[]
            {
                new() { Id = 1, Name = null },
                new() { Id = 2, Name = "a" },
                new() { Id = 3, Name = "b" }
            }
            .Where(x => arr.Contains(x.Name)).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal(oracle, actual);
    }
}
