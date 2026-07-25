using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class NullableCharCastRow
{
    [Key]
    public int Id { get; set; }

    public char Ch { get; set; }

    public char? MaybeCh { get; set; }

    public int Code { get; set; }

    public int? MaybeCode { get; set; }
}

public class NullableCharCastProjectionTests
{
    private static List<NullableCharCastRow> Rows() =>
    [
        new() { Id = 1, Ch = 'A', MaybeCh = 'B', Code = 67, MaybeCode = 68 },
        new() { Id = 2, Ch = 'z', MaybeCh = null, Code = 97, MaybeCode = null },
    ];

    private static TestDatabase Seed(CharStorageMode storage)
    {
        TestDatabase db = new(b => b.CharStorage = storage);
        db.Table<NullableCharCastRow>().Schema.CreateTable();
        db.Table<NullableCharCastRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void CharToNullableIntProjectionTextStorage()
    {
        using TestDatabase db = Seed(CharStorageMode.Text);

        List<int?> expected = Rows().OrderBy(r => r.Id).Select(r => (int?)r.Ch).ToList();
        Assert.Equal([65, 122], expected);

        List<int?> actual = db.Table<NullableCharCastRow>().OrderBy(r => r.Id).Select(r => (int?)r.Ch).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableCharToNullableIntProjectionTextStorage()
    {
        using TestDatabase db = Seed(CharStorageMode.Text);

        List<int?> expected = Rows().OrderBy(r => r.Id).Select(r => (int?)r.MaybeCh).ToList();
        Assert.Equal([66, null], expected);

        List<int?> actual = db.Table<NullableCharCastRow>().OrderBy(r => r.Id).Select(r => (int?)r.MaybeCh).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CharToNullableIntWhereTextStorage()
    {
        using TestDatabase db = Seed(CharStorageMode.Text);

        List<int> expected = Rows().Where(r => (int?)r.Ch == 65).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal([1], expected);

        List<int> actual = db.Table<NullableCharCastRow>().Where(r => (int?)r.Ch == 65).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IntToNullableCharProjectionTextStorage()
    {
        using TestDatabase db = Seed(CharStorageMode.Text);

        List<char?> expected = Rows().OrderBy(r => r.Id).Select(r => (char?)r.Code).ToList();
        Assert.Equal(['C', 'a'], expected);

        List<char?> actual = db.Table<NullableCharCastRow>().OrderBy(r => r.Id).Select(r => (char?)r.Code).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableIntToNullableCharProjectionTextStorage()
    {
        using TestDatabase db = Seed(CharStorageMode.Text);

        List<char?> expected = Rows().OrderBy(r => r.Id).Select(r => (char?)r.MaybeCode).ToList();
        Assert.Equal(['D', null], expected);

        List<char?> actual = db.Table<NullableCharCastRow>().OrderBy(r => r.Id).Select(r => (char?)r.MaybeCode).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LiftedSumToNullableCharProjectionTextStorage()
    {
        using TestDatabase db = Seed(CharStorageMode.Text);

        List<char?> expected = Rows().OrderBy(r => r.Id).Select(r => (char?)(r.MaybeCode + 1)).ToList();
        Assert.Equal(['E', null], expected);

        List<char?> actual = db.Table<NullableCharCastRow>().OrderBy(r => r.Id).Select(r => (char?)(r.MaybeCode + 1)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CharToNullableIntProjectionIntegerStorage()
    {
        using TestDatabase db = Seed(CharStorageMode.Integer);

        List<int?> expected = Rows().OrderBy(r => r.Id).Select(r => (int?)r.Ch).ToList();
        Assert.Equal([65, 122], expected);

        List<int?> actual = db.Table<NullableCharCastRow>().OrderBy(r => r.Id).Select(r => (int?)r.Ch).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IntToNullableCharProjectionIntegerStorage()
    {
        using TestDatabase db = Seed(CharStorageMode.Integer);

        List<char?> expected = Rows().OrderBy(r => r.Id).Select(r => (char?)r.Code).ToList();
        Assert.Equal(['C', 'a'], expected);

        List<char?> actual = db.Table<NullableCharCastRow>().OrderBy(r => r.Id).Select(r => (char?)r.Code).ToList();
        Assert.Equal(expected, actual);
    }
}
