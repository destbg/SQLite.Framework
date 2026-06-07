using System.Collections.Generic;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class JsonSingleEvalParityTests
{
    private static TestDatabase IntDb()
    {
        return new TestDatabase(b => b.TypeConverters[typeof(List<int>)] =
            new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
    }

    private static TestDatabase StrDb()
    {
        return new TestDatabase(b => b.TypeConverters[typeof(List<string>)] =
            new SQLiteJsonConverter<List<string>>(TestJsonContext.Default.ListString));
    }

    [Fact]
    public void Last_Int_ReturnsPositionalLast()
    {
        using TestDatabase db = IntDb();
        db.Table<QqIntRow>().Schema.CreateTable();
        db.Table<QqIntRow>().Add(new QqIntRow { Id = 1, Numbers = [3, 1, 2] });

        int expected = new List<int> { 3, 1, 2 }.Last();
        int actual = db.Table<QqIntRow>().Select(r => r.Numbers.Last()).First();

        Assert.Equal(expected, actual);
        Assert.Equal(2, actual);
    }

    [Fact]
    public void Last_String_ReturnsPositionalLast()
    {
        using TestDatabase db = StrDb();
        db.Table<QqStrRow>().Schema.CreateTable();
        db.Table<QqStrRow>().Add(new QqStrRow { Id = 1, Tags = ["c", "a", "b"] });

        string expected = new List<string> { "c", "a", "b" }.Last();
        string actual = db.Table<QqStrRow>().Select(r => r.Tags.Last()).First();

        Assert.Equal(expected, actual);
        Assert.Equal("b", actual);
    }

    [Fact]
    public void LastOrDefault_NonEmpty_ReturnsPositionalLast()
    {
        using TestDatabase db = StrDb();
        db.Table<QqStrRow>().Schema.CreateTable();
        db.Table<QqStrRow>().Add(new QqStrRow { Id = 1, Tags = ["c", "a", "b"] });

        string? expected = new List<string> { "c", "a", "b" }.LastOrDefault();
        string? actual = db.Table<QqStrRow>().Select(r => r.Tags.LastOrDefault()).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LastOrDefault_Empty_ReturnsNull()
    {
        using TestDatabase db = StrDb();
        db.Table<QqStrRow>().Schema.CreateTable();
        db.Table<QqStrRow>().Add(new QqStrRow { Id = 1, Tags = [] });

        string? actual = db.Table<QqStrRow>().Select(r => r.Tags.LastOrDefault()).First();

        Assert.Null(actual);
    }

    [Fact]
    public void Single_ExactlyOne_ReturnsElement()
    {
        using TestDatabase db = IntDb();
        db.Table<QqIntRow>().Schema.CreateTable();
        db.Table<QqIntRow>().Add(new QqIntRow { Id = 1, Numbers = [7] });

        int expected = new List<int> { 7 }.Single();
        int actual = db.Table<QqIntRow>().Select(r => r.Numbers.Single()).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Single_String_ExactlyOne_ReturnsElement()
    {
        using TestDatabase db = StrDb();
        db.Table<QqStrRow>().Schema.CreateTable();
        db.Table<QqStrRow>().Add(new QqStrRow { Id = 1, Tags = ["only"] });

        string expected = new List<string> { "only" }.Single();
        string actual = db.Table<QqStrRow>().Select(r => r.Tags.Single()).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Single_Multiple_ReturnsNull()
    {
        using TestDatabase db = StrDb();
        db.Table<QqStrRow>().Schema.CreateTable();
        db.Table<QqStrRow>().Add(new QqStrRow { Id = 1, Tags = ["a", "b"] });

        string? actual = db.Table<QqStrRow>().Select(r => r.Tags.Single()).First();

        Assert.Null(actual);
    }

    [Fact]
    public void Single_Empty_ReturnsNull()
    {
        using TestDatabase db = StrDb();
        db.Table<QqStrRow>().Schema.CreateTable();
        db.Table<QqStrRow>().Add(new QqStrRow { Id = 1, Tags = [] });

        string? actual = db.Table<QqStrRow>().Select(r => r.Tags.Single()).First();

        Assert.Null(actual);
    }

    [Fact]
    public void SingleOrDefault_ExactlyOne_ReturnsElement()
    {
        using TestDatabase db = StrDb();
        db.Table<QqStrRow>().Schema.CreateTable();
        db.Table<QqStrRow>().Add(new QqStrRow { Id = 1, Tags = ["only"] });

        string? expected = new List<string> { "only" }.SingleOrDefault();
        string? actual = db.Table<QqStrRow>().Select(r => r.Tags.SingleOrDefault()).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Last_AcrossMultipleRows_MatchesDotNet()
    {
        using TestDatabase db = IntDb();
        db.Table<QqIntRow>().Schema.CreateTable();
        QqIntRow[] rows =
        [
            new QqIntRow { Id = 1, Numbers = [10, 20, 30] },
            new QqIntRow { Id = 2, Numbers = [5] },
            new QqIntRow { Id = 3, Numbers = [9, 8] },
        ];
        db.Table<QqIntRow>().AddRange(rows);

        List<int> expected = rows.OrderBy(r => r.Id).Select(r => r.Numbers.Last()).ToList();
        List<int> actual = db.Table<QqIntRow>().OrderBy(r => r.Id).Select(r => r.Numbers.Last()).ToList();

        Assert.Equal(expected, actual);
    }
}
