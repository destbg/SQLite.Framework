using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class CastComplementRow
{
    [Key]
    public int Id { get; set; }

    public int Amount { get; set; }
}

internal enum PaintColor
{
    Red = 1,
    Blue = 2
}

public class ClientEvalCastAndComplementTests
{
    private static TestDatabase SetupDatabase(params int[] amounts)
    {
        TestDatabase db = new();
        db.Table<CastComplementRow>().Schema.CreateTable();
        int id = 1;
        foreach (int amount in amounts)
        {
            db.Table<CastComplementRow>().Add(new CastComplementRow { Id = id++, Amount = amount });
        }
        return db;
    }

    [Fact]
    public void OnesComplementOfClientIntComputesComplement()
    {
        using TestDatabase db = SetupDatabase(5, 300);

        List<int> expected = db.Table<CastComplementRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => ~ClientEvalTestFunctions.Pass(r.Amount))
            .ToList();

        Assert.Equal([-6, -301], expected);

        List<int> actual = db.Table<CastComplementRow>()
            .OrderBy(r => r.Id)
            .Select(r => ~ClientEvalTestFunctions.Pass(r.Amount))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CheckedMultiplyOverflowThrows()
    {
        using TestDatabase db = SetupDatabase(1_000_000_000);

        Assert.Throws<OverflowException>(() => db.Table<CastComplementRow>().AsEnumerable()
            .Select(r => checked(ClientEvalTestFunctions.Pass(r.Amount) * r.Amount))
            .ToList());

        Assert.Throws<OverflowException>(() => db.Table<CastComplementRow>()
            .Select(r => checked(ClientEvalTestFunctions.Pass(r.Amount) * r.Amount))
            .ToList());
    }

    [Fact]
    public void UncheckedNarrowingCastWraps()
    {
        using TestDatabase db = SetupDatabase(300, 44);

        List<byte> expected = db.Table<CastComplementRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => unchecked((byte)ClientEvalTestFunctions.Pass(r.Amount)))
            .ToList();

        Assert.Equal([44, 44], expected);

        List<byte> actual = db.Table<CastComplementRow>()
            .OrderBy(r => r.Id)
            .Select(r => unchecked((byte)ClientEvalTestFunctions.Pass(r.Amount)))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CastToEnumConverts()
    {
        using TestDatabase db = SetupDatabase(1, 2);

        List<PaintColor> expected = db.Table<CastComplementRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => (PaintColor)ClientEvalTestFunctions.Pass(r.Amount))
            .ToList();

        Assert.Equal([PaintColor.Red, PaintColor.Blue], expected);

        List<PaintColor> actual = db.Table<CastComplementRow>()
            .OrderBy(r => r.Id)
            .Select(r => (PaintColor)ClientEvalTestFunctions.Pass(r.Amount))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableReceiverGetHashCodeEvaluates()
    {
        using TestDatabase db = SetupDatabase(5, 2);

        List<int> expected = db.Table<CastComplementRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => ClientEvalTestFunctions.ToNullable(r.Amount).GetHashCode())
            .ToList();

        Assert.Equal([5, 0], expected);

        List<int> actual = db.Table<CastComplementRow>()
            .OrderBy(r => r.Id)
            .Select(r => ClientEvalTestFunctions.ToNullable(r.Amount).GetHashCode())
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableReceiverGetValueOrDefaultEvaluates()
    {
        using TestDatabase db = SetupDatabase(5, 2);

        List<int> expected = db.Table<CastComplementRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => ClientEvalTestFunctions.ToNullable(r.Amount).GetValueOrDefault(42))
            .ToList();

        Assert.Equal([5, 42], expected);

        List<int> actual = db.Table<CastComplementRow>()
            .OrderBy(r => r.Id)
            .Select(r => ClientEvalTestFunctions.ToNullable(r.Amount).GetValueOrDefault(42))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
