using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

public class StringSemanticsBugTests
{
    [Fact]
    public void CompareWithNullLeftOperand()
    {
        using TestDatabase db = new();
        db.Table<NullableStringEntity>().Schema.CreateTable();
        db.Table<NullableStringEntity>().Add(new NullableStringEntity { Id = 1, Name = null });
        db.Table<NullableStringEntity>().Add(new NullableStringEntity { Id = 2, Name = "z" });

        List<NullableStringEntity> rows =
        [
            new NullableStringEntity { Id = 1, Name = null },
            new NullableStringEntity { Id = 2, Name = "z" }
        ];

        List<int> expected = rows
            .OrderBy(r => r.Id)
            .Select(r => Math.Sign(string.Compare(r.Name, "m")))
            .ToList();

        List<int> actual = db.Table<NullableStringEntity>()
            .OrderBy(r => r.Id)
            .Select(r => string.Compare(r.Name, "m"))
            .ToList()
            .Select(Math.Sign)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToUpperNonAscii()
    {
        using TestDatabase db = new();
        db.Table<NullableStringEntity>().Schema.CreateTable();
        db.Table<NullableStringEntity>().Add(new NullableStringEntity { Id = 1, Name = "café" });

        string expected = "café".ToUpper();

        string actual = db.Table<NullableStringEntity>()
            .Where(r => r.Id == 1)
            .Select(r => r.Name!.ToUpper())
            .First();

        Assert.Equal(expected, actual);
    }
}
