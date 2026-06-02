using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal enum ParseUlongEnum : ulong
{
    A = 1,
}

internal sealed class ParseUlongRow
{
    [Key]
    public int Id { get; set; }

    public string Code { get; set; } = "";
}

public class ParseCastSemanticsTests
{
    [Fact]
    public void IntAndDoubleParseOverColumn_MatchSqliteCast()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "12abc", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "abc", AuthorId = 1, Price = 2 });
        db.Table<Book>().Add(new Book { Id = 3, Title = "3.14xyz", AuthorId = 1, Price = 3 });

        int framework1 = db.Table<Book>().Where(b => b.Id == 1).Select(b => int.Parse(b.Title)).First();
        int framework2 = db.Table<Book>().Where(b => b.Id == 2).Select(b => int.Parse(b.Title)).First();
        double framework3 = db.Table<Book>().Where(b => b.Id == 3).Select(b => double.Parse(b.Title)).First();

        int? oracle1 = db.ExecuteScalar<int>("SELECT CAST(\"BookTitle\" AS INTEGER) FROM \"Books\" WHERE \"BookId\" = 1");
        int? oracle2 = db.ExecuteScalar<int>("SELECT CAST(\"BookTitle\" AS INTEGER) FROM \"Books\" WHERE \"BookId\" = 2");
        double? oracle3 = db.ExecuteScalar<double>("SELECT CAST(\"BookTitle\" AS REAL) FROM \"Books\" WHERE \"BookId\" = 3");

        Assert.Equal(oracle1, framework1);
        Assert.Equal(oracle2, framework2);
        Assert.Equal(oracle3, framework3);
    }

    [Fact]
    public void EnumParseUlongFromColumn_MatchesSqliteCastSaturation()
    {
        const string s = "9999999999999999999";

        using TestDatabase db = new();
        db.Table<ParseUlongRow>().Schema.CreateTable();
        db.Table<ParseUlongRow>().Add(new ParseUlongRow { Id = 1, Code = s });

        ulong framework = db.Table<ParseUlongRow>().Select(r => (ulong)Enum.Parse<ParseUlongEnum>(r.Code)).First();
        long? oracleSigned = db.ExecuteScalar<long>("SELECT CAST(\"Code\" AS INTEGER) FROM \"ParseUlongRow\" WHERE \"Id\" = 1");

        Assert.Equal((ulong)oracleSigned!.Value, framework);
    }
}
