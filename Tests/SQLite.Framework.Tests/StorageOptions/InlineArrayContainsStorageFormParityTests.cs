using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum H21hInlineLevel
{
    Low = 1,
    Mid = 2,
    High = 3,
}

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<H21hInlineLevel>))]
public partial class H21hInlineLevelContext : JsonSerializerContext;

[Table("H21hInlineRows")]
public class H21hInlineRow
{
    [Key]
    public int Id { get; set; }

    public int Num { get; set; }

    public DateTime Stamp { get; set; }

    public H21hInlineLevel Level { get; set; }
}

public class InlineArrayContainsStorageFormParityTests
{
    private static readonly DateTime BaseStamp = new(2024, 3, 1, 10, 30, 0);

    private static List<H21hInlineRow> Rows()
    {
        return
        [
            new H21hInlineRow { Id = 1, Num = 10, Stamp = BaseStamp, Level = H21hInlineLevel.Low },
            new H21hInlineRow { Id = 2, Num = 20, Stamp = BaseStamp.AddDays(1), Level = H21hInlineLevel.Mid },
            new H21hInlineRow { Id = 3, Num = 30, Stamp = BaseStamp.AddDays(2), Level = H21hInlineLevel.High }
        ];
    }

    private static TestDatabase Setup(Action<SQLiteOptionsBuilder>? configure, string methodName)
    {
        TestDatabase db = new(configure, methodName);
        db.Table<H21hInlineRow>().Schema.CreateTable();
        db.Table<H21hInlineRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void ConstantIntArrayContainsIntColumnMatchesLinq()
    {
        using TestDatabase db = Setup(null, nameof(ConstantIntArrayContainsIntColumnMatchesLinq));

        List<int> expected = Rows()
            .Where(r => new[] { 10, 30 }.Contains(r.Num))
            .Select(r => r.Id).OrderBy(id => id).ToList();
        List<int> actual = db.Table<H21hInlineRow>()
            .Where(r => new[] { 10, 30 }.Contains(r.Num))
            .Select(r => r.Id).OrderBy(id => id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedDateArrayContainsDateColumnMatchesLinq()
    {
        using TestDatabase db = Setup(null, nameof(CapturedDateArrayContainsDateColumnMatchesLinq));

        DateTime first = BaseStamp;
        DateTime third = BaseStamp.AddDays(2);

        List<int> expected = Rows()
            .Where(r => new[] { first, third }.Contains(r.Stamp))
            .Select(r => r.Id).OrderBy(id => id).ToList();
        List<int> actual = db.Table<H21hInlineRow>()
            .Where(r => new[] { first, third }.Contains(r.Stamp))
            .Select(r => r.Id).OrderBy(id => id).ToList();

        Assert.Equal([1, 3], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedDateListContainsDateColumnMatchesLinq()
    {
        using TestDatabase db = Setup(null, nameof(CapturedDateListContainsDateColumnMatchesLinq));

        DateTime first = BaseStamp;
        DateTime third = BaseStamp.AddDays(2);

        List<int> expected = Rows()
            .Where(r => new List<DateTime> { first, third }.Contains(r.Stamp))
            .Select(r => r.Id).OrderBy(id => id).ToList();
        List<int> actual = db.Table<H21hInlineRow>()
            .Where(r => new List<DateTime> { first, third }.Contains(r.Stamp))
            .Select(r => r.Id).OrderBy(id => id).ToList();

        Assert.Equal([1, 3], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConstantEnumArrayContainsEnumColumnTextStorageMatchesLinq()
    {
        using TestDatabase db = Setup(b => b.UseEnumStorage(EnumStorageMode.Text),
            nameof(ConstantEnumArrayContainsEnumColumnTextStorageMatchesLinq));

        List<int> expected = Rows()
            .Where(r => new[] { H21hInlineLevel.Low, H21hInlineLevel.High }.Contains(r.Level))
            .Select(r => r.Id).OrderBy(id => id).ToList();
        List<int> actual = db.Table<H21hInlineRow>()
            .Where(r => new[] { H21hInlineLevel.Low, H21hInlineLevel.High }.Contains(r.Level))
            .Select(r => r.Id).OrderBy(id => id).ToList();

        Assert.Equal([1, 3], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConstantEnumArrayContainsEnumColumnWithJsonContextMatchesLinq()
    {
        using TestDatabase db = Setup(b => b.AddJsonContext(H21hInlineLevelContext.Default),
            nameof(ConstantEnumArrayContainsEnumColumnWithJsonContextMatchesLinq));

        List<int> expected = Rows()
            .Where(r => new[] { H21hInlineLevel.Low, H21hInlineLevel.High }.Contains(r.Level))
            .Select(r => r.Id).OrderBy(id => id).ToList();
        List<int> actual = db.Table<H21hInlineRow>()
            .Where(r => new[] { H21hInlineLevel.Low, H21hInlineLevel.High }.Contains(r.Level))
            .Select(r => r.Id).OrderBy(id => id).ToList();

        Assert.Equal([1, 3], expected);
        Assert.Equal(expected, actual);
    }
}
