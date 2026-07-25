using System.Globalization;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum ParityColor
{
    Red = 0,
    Green = 1,
    Blue = 2
}

public class ParityRow
{
    [Key]
    public int Id { get; set; }

    public int IntVal { get; set; }

    public long LongVal { get; set; }

    public double DblVal { get; set; }

    public string Text { get; set; } = "";

    public int? NullableInt { get; set; }

    public long? NullableLong { get; set; }

    public double? NullableDouble { get; set; }

    public uint UIntVal { get; set; }

    public ulong ULongVal { get; set; }

    public ParityColor Color { get; set; }
}

public record ParityPosRecord(int Number, string Label);

public class SelectProjectionParityTests
{
    private static readonly ParityRow[] Data =
    [
        new ParityRow
        {
            Id = 1,
            IntVal = 70000,
            LongVal = 5_000_000_000L,
            DblVal = 1.5,
            Text = "hello",
            NullableInt = 42,
            NullableLong = 9_000_000_000L,
            NullableDouble = 2.75,
            UIntVal = 4_000_000_000U,
            ULongVal = 9_000_000_000UL,
            Color = ParityColor.Green
        },
        new ParityRow
        {
            Id = 2,
            IntVal = 9,
            LongVal = 2L,
            DblVal = -2.5,
            Text = "world",
            NullableInt = null,
            NullableLong = null,
            NullableDouble = null,
            UIntVal = 7U,
            ULongVal = 11UL,
            Color = ParityColor.Blue
        },
    ];

    private static TestDatabase Db()
    {
        TestDatabase db = new();
        db.Table<ParityRow>().Schema.CreateTable();
        db.Table<ParityRow>().AddRange(Data);
        return db;
    }

    private static void Same<T>(Func<IEnumerable<ParityRow>, IEnumerable<T>> enumerable, Func<IQueryable<ParityRow>, IQueryable<T>> queryable)
    {
        using TestDatabase db = Db();
        List<T> expected = enumerable(Data.OrderBy(x => x.Id)).ToList();
        List<T> actual = queryable(db.Table<ParityRow>().OrderBy(x => x.Id)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableHasValue_InClientProjection()
        => Same(q => q.Select(x => x.NullableInt.HasValue ? x.Text.Normalize() : "none"),
                q => q.Select(x => x.NullableInt.HasValue ? x.Text.Normalize() : "none"));

    [Fact]
    public void NullableCoalesce_InClientProjection()
        => Same(q => q.Select(x => (x.NullableInt ?? 0) + x.Text.Normalize().Length),
                q => q.Select(x => (x.NullableInt ?? 0) + x.Text.Normalize().Length));

    [Fact]
    public void NullableEqualsNull_InClientProjection()
        => Same(q => q.Select(x => x.NullableInt == null ? x.Text.Normalize() : "has"),
                q => q.Select(x => x.NullableInt == null ? x.Text.Normalize() : "has"));

    [Fact]
    public void NullableGetValueOrDefault_InClientProjection()
        => Same(q => q.Select(x => x.NullableInt.GetValueOrDefault().ToString(CultureInfo.InvariantCulture) + x.Text.Normalize()),
                q => q.Select(x => x.NullableInt.GetValueOrDefault().ToString(CultureInfo.InvariantCulture) + x.Text.Normalize()));

    [Fact]
    public void NullableGetValueOrDefaultArg_InClientProjection()
        => Same(q => q.Select(x => x.NullableInt.GetValueOrDefault(99) + x.Text.Normalize().Length),
                q => q.Select(x => x.NullableInt.GetValueOrDefault(99) + x.Text.Normalize().Length));

    [Fact]
    public void NullableToString_InClientProjection()
        => Same(q => q.Select(x => x.NullableInt.ToString() + x.Text.Normalize()),
                q => q.Select(x => x.NullableInt.ToString() + x.Text.Normalize()));

    [Fact]
    public void NullableLongCoalesce_InClientProjection()
        => Same(q => q.Select(x => (x.NullableLong ?? 0L) + x.Text.Normalize().Length),
                q => q.Select(x => (x.NullableLong ?? 0L) + x.Text.Normalize().Length));

    [Fact]
    public void NullableDoubleToString_InClientProjection()
        => Same(q => q.Select(x => x.NullableDouble.ToString() + x.Text.Normalize()),
                q => q.Select(x => x.NullableDouble.ToString() + x.Text.Normalize()));

    [Fact]
    public void NarrowLongToInt()
        => Same(q => q.Select(x => (int)x.LongVal),
                q => q.Select(x => (int)x.LongVal));

    [Fact]
    public void NarrowIntToByte()
        => Same(q => q.Select(x => (byte)x.IntVal),
                q => q.Select(x => (byte)x.IntVal));

    [Fact]
    public void NarrowIntToShort()
        => Same(q => q.Select(x => (short)x.IntVal),
                q => q.Select(x => (short)x.IntVal));

    [Fact]
    public void NarrowIntToSByte()
        => Same(q => q.Select(x => (sbyte)x.IntVal),
                q => q.Select(x => (sbyte)x.IntVal));

    [Fact]
    public void NarrowLongToUInt()
        => Same(q => q.Select(x => (uint)x.LongVal),
                q => q.Select(x => (uint)x.LongVal));

    [Fact]
    public void NarrowLongToShort()
        => Same(q => q.Select(x => (short)x.LongVal),
                q => q.Select(x => (short)x.LongVal));

    [Fact]
    public void NarrowIntToChar()
        => Same(q => q.Select(x => (char)x.IntVal),
                q => q.Select(x => (char)x.IntVal));

    [Fact]
    public void NarrowUIntToByte()
        => Same(q => q.Select(x => (byte)x.UIntVal),
                q => q.Select(x => (byte)x.UIntVal));

    [Fact]
    public void NarrowULongToInt()
        => Same(q => q.Select(x => (int)x.ULongVal),
                q => q.Select(x => (int)x.ULongVal));

    [Fact]
    public void DoubleToInt_Truncates()
        => Same(q => q.Select(x => (int)x.DblVal + x.Text.Normalize().Length),
                q => q.Select(x => (int)x.DblVal + x.Text.Normalize().Length));

    [Fact]
    public void DoubleToIntPureScalar_Truncates()
        => Same(q => q.Select(x => (int)x.DblVal),
                q => q.Select(x => (int)x.DblVal));

    [Fact]
    public void NegativeDoubleToInt_TruncatesTowardZero()
        => Same(q => q.Select(x => (int)x.DblVal + x.Text.Normalize().Length - x.Text.Normalize().Length),
                q => q.Select(x => (int)x.DblVal + x.Text.Normalize().Length - x.Text.Normalize().Length));

    [Fact]
    public void DoubleToLong_InClientProjection()
        => Same(q => q.Select(x => (long)x.DblVal + x.Text.Normalize().Length),
                q => q.Select(x => (long)x.DblVal + x.Text.Normalize().Length));

    [Fact]
    public void IntToLong_InClientProjection()
        => Same(q => q.Select(x => (long)x.IntVal + x.Text.Normalize().Length),
                q => q.Select(x => (long)x.IntVal + x.Text.Normalize().Length));

    [Fact]
    public void DoubleToDecimal_InClientProjection()
        => Same(q => q.Select(x => ((decimal)x.DblVal).ToString(CultureInfo.InvariantCulture) + x.Text.Normalize()),
                q => q.Select(x => ((decimal)x.DblVal).ToString(CultureInfo.InvariantCulture) + x.Text.Normalize()));

    [Fact]
    public void TupleProjection()
        => Same(q => q.Select(x => new Tuple<int, string>(x.Id, x.Text)),
                q => q.Select(x => new Tuple<int, string>(x.Id, x.Text)));

    [Fact]
    public void KeyValuePairProjection()
        => Same(q => q.Select(x => new KeyValuePair<int, string>(x.Id, x.Text)),
                q => q.Select(x => new KeyValuePair<int, string>(x.Id, x.Text)));

    [Fact]
    public void ArrayElementProjection()
        => Same(q => q.Select(x => new[] { x.Id, x.IntVal }),
                q => q.Select(x => new[] { x.Id, x.IntVal }));

    [Fact]
    public void ToCharArrayProjection()
        => Same(q => q.Select(x => x.Text.ToCharArray()),
                q => q.Select(x => x.Text.ToCharArray()));

    [Fact]
    public void StringArrayProjection()
        => Same(q => q.Select(x => new[] { x.Text, x.Color.ToString() }),
                q => q.Select(x => new[] { x.Text, x.Color.ToString() }));

    [Fact]
    public void ValueTupleProjection()
        => Same(q => q.Select(x => ValueTuple.Create(x.Id, x.Text)),
                q => q.Select(x => ValueTuple.Create(x.Id, x.Text)));

    [Fact]
    public void ListInitProjection()
        => Same(q => q.Select(x => new List<int> { x.Id, x.IntVal }),
                q => q.Select(x => new List<int> { x.Id, x.IntVal }));

    [Fact]
    public void PositionalRecordProjection()
        => Same(q => q.Select(x => new ParityPosRecord(x.IntVal, x.Text)),
                q => q.Select(x => new ParityPosRecord(x.IntVal, x.Text)));
}
