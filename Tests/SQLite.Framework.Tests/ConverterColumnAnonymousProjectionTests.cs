using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public record ConvAnonCode(string Value);

public class ConvAnonCodeConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Text;

    public object? ToDatabase(object? value)
    {
        return value is ConvAnonCode c ? c.Value : null;
    }

    public object? FromDatabase(object? value)
    {
        return value is string s ? new ConvAnonCode(s) : null;
    }
}

[Table("ConvAnonRows")]
public class ConvAnonRow
{
    [Key]
    public int Id { get; set; }

    public ConvAnonCode Code { get; set; } = new("");

    public int V { get; set; }
}

public class ConverterColumnAnonymousProjectionTests
{
    private static List<ConvAnonRow> Rows()
    {
        return
        [
            new ConvAnonRow { Id = 1, Code = new ConvAnonCode("alpha"), V = 1 },
            new ConvAnonRow { Id = 2, Code = new ConvAnonCode("alpha"), V = 2 },
            new ConvAnonRow { Id = 3, Code = new ConvAnonCode("beta"), V = 3 },
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new(b => b.AddTypeConverter<ConvAnonCode>(new ConvAnonCodeConverter()));
        db.Table<ConvAnonRow>().Schema.CreateTable();
        db.Table<ConvAnonRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void BareGroupByKeyKeepsConverterValue()
    {
        using TestDatabase db = Seed();

        List<ConvAnonCode> expected = Rows().GroupBy(r => r.Code).Select(g => g.Key).OrderBy(k => k.Value, StringComparer.Ordinal).ToList();
        List<ConvAnonCode> actual = db.Table<ConvAnonRow>().GroupBy(r => r.Code).Select(g => g.Key).ToList().OrderBy(k => k == null ? "" : k.Value, StringComparer.Ordinal).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AnonymousProjectionKeepsConverterValue()
    {
        using TestDatabase db = Seed();

        var expected = Rows().OrderBy(r => r.Id).Select(r => new { r.Code, r.Id }).ToList();
        var actual = db.Table<ConvAnonRow>().OrderBy(r => r.Id).Select(r => new { r.Code, r.Id }).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GroupByKeyInAnonymousProjectionKeepsConverterValue()
    {
        using TestDatabase db = Seed();

        var expected = Rows().GroupBy(r => r.Code)
            .Select(g => new { g.Key, N = g.Count() })
            .OrderBy(x => x.Key == null ? "" : x.Key.Value, StringComparer.Ordinal)
            .ToList();
        var actual = db.Table<ConvAnonRow>().GroupBy(r => r.Code)
            .Select(g => new { g.Key, N = g.Count() })
            .ToList()
            .OrderBy(x => x.Key == null ? "" : x.Key.Value, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
