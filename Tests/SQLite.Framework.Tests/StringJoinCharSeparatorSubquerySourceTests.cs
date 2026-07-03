using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class SeparatorRow
{
    [Key]
    public int Id { get; set; }

    public char Sep { get; set; }
}

public class StringJoinCharSeparatorSubquerySourceTests
{
    [Fact]
    public void JoinsSubqueryValuesWithCharSeparator()
    {
        using TestDatabase db = new();
        db.Table<TwoStringEntity>().Schema.CreateTable();
        db.Table<TwoStringEntity>().AddRange(
        [
            new TwoStringEntity { Id = 1, A = "a", B = "" },
            new TwoStringEntity { Id = 2, A = "b", B = "" },
            new TwoStringEntity { Id = 3, A = "c", B = "" },
        ]);

        TwoStringEntity[] rows =
        [
            new TwoStringEntity { Id = 1, A = "a", B = "" },
            new TwoStringEntity { Id = 2, A = "b", B = "" },
            new TwoStringEntity { Id = 3, A = "c", B = "" },
        ];
        string expected = string.Join('-', rows.OrderBy(x => x.Id).Select(x => x.A));
        Assert.Equal("a-b-c", expected);

        string actual = db.Table<TwoStringEntity>()
            .Where(x => x.Id == 1)
            .Select(x => string.Join('-', db.Table<TwoStringEntity>().OrderBy(y => y.Id).Select(y => y.A)))
            .First();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CharColumnSeparatorThrows()
    {
        using TestDatabase db = new();
        db.Table<TwoStringEntity>().Schema.CreateTable();
        db.Table<SeparatorRow>().Schema.CreateTable();
        db.Table<TwoStringEntity>().Add(new TwoStringEntity { Id = 1, A = "a", B = "" });
        db.Table<SeparatorRow>().Add(new SeparatorRow { Id = 1, Sep = '+' });

        Exception? ex = Record.Exception(() => db.Table<SeparatorRow>()
            .Select(x => string.Join(x.Sep, db.Table<TwoStringEntity>().Select(y => y.A)))
            .First());
        Assert.IsType<NotSupportedException>(ex);
    }
}
