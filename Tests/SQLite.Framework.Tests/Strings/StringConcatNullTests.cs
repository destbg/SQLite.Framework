using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StringConcatNullTests
{
    private static TestDatabase SeedNames(params (int id, string? name)[] rows)
    {
        TestDatabase db = new();
        db.Table<NullableStringEntity>().Schema.CreateTable();
        foreach ((int id, string? name) in rows)
        {
            db.Table<NullableStringEntity>().Add(new NullableStringEntity { Id = id, Name = name });
        }

        return db;
    }

    [Fact]
    public void ConcatNullableColumnWithLiteralMatchesDotNet()
    {
        (int id, string? name)[] rows = { (1, null), (2, "ab") };
        using TestDatabase db = SeedNames(rows);

        List<string> actual = db.Table<NullableStringEntity>().OrderBy(x => x.Id).Select(x => x.Name + "!").ToList();
        List<string> oracle = rows.OrderBy(r => r.id).Select(r => r.name + "!").ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ConcatLiteralWithNullableColumnMatchesDotNet()
    {
        (int id, string? name)[] rows = { (1, null), (2, "ab") };
        using TestDatabase db = SeedNames(rows);

        List<string> actual = db.Table<NullableStringEntity>().OrderBy(x => x.Id).Select(x => "p" + x.Name).ToList();
        List<string> oracle = rows.OrderBy(r => r.id).Select(r => "p" + r.name).ToList();

        Assert.Equal(oracle, actual);
    }
}
