using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StringLengthSemanticsTests
{
    [Fact]
    public void StringLengthCountsCodePointsNotUtf16Units()
    {
        using TestDatabase db = new();
        db.Table<NullableStringEntity>().Schema.CreateTable();
        db.Table<NullableStringEntity>().Add(new NullableStringEntity { Id = 1, Name = "a\U0001F600b" });

        int actual = db.Table<NullableStringEntity>().Where(x => x.Id == 1).Select(x => x.Name!.Length).First();

        Assert.Equal(3, actual);
    }
}
