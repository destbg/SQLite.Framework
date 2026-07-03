using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class LongCodeRow
{
    [Key]
    public int Id { get; set; }

    public long Code { get; set; }

    public double RealCode { get; set; }
}

public class CharCastFromLongColumnTests
{
    private static List<LongCodeRow> Rows() =>
    [
        new() { Id = 1, Code = 65, RealCode = 66.0 },
        new() { Id = 2, Code = 65 + 65536, RealCode = 67.0 },
    ];

    private static TestDatabase Seed(CharStorageMode storage)
    {
        TestDatabase db = new(b => b.CharStorage = storage);
        db.Table<LongCodeRow>().Schema.CreateTable();
        db.Table<LongCodeRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void LongToCharProjectionTextStorage()
    {
        using TestDatabase db = Seed(CharStorageMode.Text);

        List<char> expected = Rows().OrderBy(r => r.Id).Select(r => (char)r.Code).ToList();
        Assert.Equal(['A', 'A'], expected);

        List<char> actual = db.Table<LongCodeRow>().OrderBy(r => r.Id).Select(r => (char)r.Code).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LongToCharProjectionIntegerStorage()
    {
        using TestDatabase db = Seed(CharStorageMode.Integer);

        List<char> expected = Rows().OrderBy(r => r.Id).Select(r => (char)r.Code).ToList();
        Assert.Equal(['A', 'A'], expected);

        List<char> actual = db.Table<LongCodeRow>().OrderBy(r => r.Id).Select(r => (char)r.Code).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DoubleToCharProjectionIntegerStorage()
    {
        using TestDatabase db = Seed(CharStorageMode.Integer);

        List<char> expected = Rows().OrderBy(r => r.Id).Select(r => (char)r.RealCode).ToList();
        Assert.Equal(['B', 'C'], expected);

        List<char> actual = db.Table<LongCodeRow>().OrderBy(r => r.Id).Select(r => (char)r.RealCode).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LongToCharWhereTextStorage()
    {
        using TestDatabase db = Seed(CharStorageMode.Text);

        List<int> expected = Rows().Where(r => (char)r.Code == 'A').Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal([1, 2], expected);

        List<int> actual = db.Table<LongCodeRow>().Where(r => (char)r.Code == 'A').Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }
}
