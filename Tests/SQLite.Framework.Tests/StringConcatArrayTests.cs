using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StringConcatArrayTests
{
    [Fact]
    public void ConcatOverStringArrayMatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<TwoStringEntity>().Schema.CreateTable();
        db.Table<TwoStringEntity>().Add(new TwoStringEntity { Id = 1, A = "Hello", B = "World" });
        db.Table<TwoStringEntity>().Add(new TwoStringEntity { Id = 2, A = "Foo", B = "Bar" });

        List<TwoStringEntity> seed =
        [
            new TwoStringEntity { Id = 1, A = "Hello", B = "World" },
            new TwoStringEntity { Id = 2, A = "Foo", B = "Bar" },
        ];

        List<string> oracle = seed.OrderBy(s => s.Id).Select(s => string.Concat(new[] { s.A, s.B, "!" })).ToList();
        List<string> actual = db.Table<TwoStringEntity>().OrderBy(s => s.Id).Select(s => string.Concat(new[] { s.A, s.B, "!" })).ToList();

        Assert.Equal(["HelloWorld!", "FooBar!"], oracle);
        Assert.Equal(oracle, actual);
    }
}
