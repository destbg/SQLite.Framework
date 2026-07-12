using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public readonly struct CwHole : IComparable<CwHole>
{
    public CwHole(int n)
    {
        N = n;
    }

    public int N { get; }

    public int CompareTo(CwHole other) => N.CompareTo(other.N);

    public static bool operator ==(CwHole a, CwHole b) => a.N == b.N;

    public static bool operator !=(CwHole a, CwHole b) => a.N != b.N;

    public override bool Equals(object? obj) => obj is CwHole h && h.N == N;

    public override int GetHashCode() => N;
}

public sealed class CwHoleConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Integer;

    public object? ToDatabase(object? value) => value is CwHole h ? h.N == 5 ? null : (long)h.N : null;

    public object? FromDatabase(object? value) => value is long l ? new CwHole((int)l) : new CwHole(5);
}

[Table("CwHoleRows")]
public class CwHoleRow
{
    [Key]
    public int Id { get; set; }

    public CwHole H { get; set; }
}

public class ConverterNullMappedOrderingTests
{
    private static ModelTestDatabase Seed(out List<CwHoleRow> rows, string methodName)
    {
        ModelTestDatabase db = new(
            model => model.Entity<CwHoleRow>().IsRequired(r => r.H, false),
            b => b.AddTypeConverter<CwHole>(new CwHoleConverter()),
            methodName);
        db.Table<CwHoleRow>().Schema.CreateTable();
        rows =
        [
            new CwHoleRow { Id = 1, H = new CwHole(1) },
            new CwHoleRow { Id = 2, H = new CwHole(5) },
            new CwHoleRow { Id = 3, H = new CwHole(9) },
        ];
        db.Table<CwHoleRow>().AddRange(rows);
        return db;
    }

    [Fact]
    public void OrderByNullMappedConverterValueSortsStoredNullFirst()
    {
        using ModelTestDatabase db = Seed(out List<CwHoleRow> rows, nameof(OrderByNullMappedConverterValueSortsStoredNullFirst));

        List<int> expected = rows
            .OrderBy(r => r.H.N == 5 ? 0 : 1)
            .ThenBy(r => r.H.N)
            .Select(r => r.Id)
            .ToList();
        List<int> actual = db.Table<CwHoleRow>().OrderBy(r => r.H).Select(r => r.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByDescendingNullMappedConverterValueSortsStoredNullLast()
    {
        using ModelTestDatabase db = Seed(out List<CwHoleRow> rows, nameof(OrderByDescendingNullMappedConverterValueSortsStoredNullLast));

        List<int> expected = rows
            .OrderBy(r => r.H.N == 5 ? 1 : 0)
            .ThenByDescending(r => r.H.N)
            .Select(r => r.Id)
            .ToList();
        List<int> actual = db.Table<CwHoleRow>().OrderByDescending(r => r.H).Select(r => r.Id).ToList();

        Assert.Equal(expected, actual);
    }

}
