using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

public class StringSemanticsBugTests
{
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
