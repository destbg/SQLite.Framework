using System;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StringIgnoreCaseUnicodeFoldingTests
{
    private const string UpperEAcute = "É";
    private const string LowerEAcute = "é";

    [Fact]
    public void EqualsOrdinalIgnoreCaseFoldsAsciiOnly()
    {
        using TestDatabase db = new();
        db.Table<NullableStringEntity>().Schema.CreateTable();
        db.Table<NullableStringEntity>().Add(new NullableStringEntity { Id = 1, Name = UpperEAcute });

        bool oracle = UpperEAcute.Equals(LowerEAcute, StringComparison.OrdinalIgnoreCase);
        Assert.True(oracle);

        int actual = db.Table<NullableStringEntity>()
            .Count(x => x.Name!.Equals(LowerEAcute, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(0, actual);
    }

    [Fact]
    public void CompareOrdinalIgnoreCaseFoldsAsciiOnly()
    {
        using TestDatabase db = new();
        db.Table<NullableStringEntity>().Schema.CreateTable();
        db.Table<NullableStringEntity>().Add(new NullableStringEntity { Id = 1, Name = UpperEAcute });

        int oracle = string.Compare(UpperEAcute, LowerEAcute, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, oracle);

        int actual = db.Table<NullableStringEntity>()
            .Where(x => x.Id == 1)
            .Select(x => string.Compare(x.Name, LowerEAcute, StringComparison.OrdinalIgnoreCase))
            .First();

        Assert.NotEqual(oracle, actual);
    }
}
