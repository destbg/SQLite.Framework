using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class JsonDistinctSingleTests
{
    private static TestDatabase IntDb()
    {
        return new TestDatabase(b => b.TypeConverters[typeof(List<int>)] =
            new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
    }

    [Fact]
    public void Distinct_Single_DuplicatesReduceToOneValue_ReturnsTheValue()
    {
        using TestDatabase db = IntDb();
        db.Table<QqIntRow>().Schema.CreateTable();
        db.Table<QqIntRow>().Add(new QqIntRow { Id = 1, Numbers = [5, 5] });

        int oracle = new List<int> { 5, 5 }.Distinct().Single();

        int actual = db.Table<QqIntRow>()
            .Select(r => r.Numbers.Distinct().Single())
            .First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void Distinct_Single_ThreeIdenticalValues_ReturnsTheValue()
    {
        using TestDatabase db = IntDb();
        db.Table<QqIntRow>().Schema.CreateTable();
        db.Table<QqIntRow>().Add(new QqIntRow { Id = 1, Numbers = [7, 7, 7] });

        int oracle = new List<int> { 7, 7, 7 }.Distinct().Single();

        int actual = db.Table<QqIntRow>()
            .Select(r => r.Numbers.Distinct().Single())
            .First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void Distinct_Single_TwoDifferentValues_ReturnsNullDefault()
    {
        using TestDatabase db = IntDb();
        db.Table<QqIntRow>().Schema.CreateTable();
        db.Table<QqIntRow>().Add(new QqIntRow { Id = 1, Numbers = [1, 2] });

        int actual = db.Table<QqIntRow>()
            .Select(r => r.Numbers.Distinct().Single())
            .First();

        Assert.Equal(0, actual);
    }

    [Fact]
    public void Distinct_Single_AlreadyUniqueValue_ReturnsTheValue()
    {
        using TestDatabase db = IntDb();
        db.Table<QqIntRow>().Schema.CreateTable();
        db.Table<QqIntRow>().Add(new QqIntRow { Id = 1, Numbers = [42] });

        int oracle = new List<int> { 42 }.Distinct().Single();

        int actual = db.Table<QqIntRow>()
            .Select(r => r.Numbers.Distinct().Single())
            .First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void Distinct_SingleOrDefault_DuplicatesReduceToOneValue_ReturnsTheValue()
    {
        using TestDatabase db = IntDb();
        db.Table<QqIntRow>().Schema.CreateTable();
        db.Table<QqIntRow>().Add(new QqIntRow { Id = 1, Numbers = [9, 9, 9] });

        int oracle = new List<int> { 9, 9, 9 }.Distinct().SingleOrDefault();

        int actual = db.Table<QqIntRow>()
            .Select(r => r.Numbers.Distinct().SingleOrDefault())
            .First();

        Assert.Equal(oracle, actual);
    }
}
