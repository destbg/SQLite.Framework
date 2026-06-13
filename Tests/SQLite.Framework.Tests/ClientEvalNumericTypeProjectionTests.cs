using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class NumericProjectionRow
{
    [Key]
    public int Id { get; set; }

    public int IntA { get; set; }

    public int IntB { get; set; }

    public long LongVal { get; set; }

    public uint UIntVal { get; set; }

    public ulong ULongVal { get; set; }

    public float FloatVal { get; set; }

    public double DoubleVal { get; set; }
}

public class ClientEvalNumericTypeProjectionTests
{
    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<NumericProjectionRow>().Schema.CreateTable();
        return db;
    }

    private static List<TResult> Both<TResult>(TestDatabase db, Func<IQueryable<NumericProjectionRow>, IQueryable<TResult>> query)
    {
        List<TResult> expected = query(db.Table<NumericProjectionRow>().AsEnumerable().AsQueryable()).ToList();
        List<TResult> actual = query(db.Table<NumericProjectionRow>()).ToList();
        Assert.Equal(expected, actual);
        return expected;
    }

    [Fact]
    public void CheckedIntAddAndSubtract()
    {
        using TestDatabase db = Setup();
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 1, IntA = 10 });

        List<int> add = Both(db, q => q.Select(r => checked(ClientEvalTestFunctions.Pass(r.IntA) + 1)));
        Assert.Equal([11], add);

        List<int> sub = Both(db, q => q.Select(r => checked(ClientEvalTestFunctions.Pass(r.IntA) - 1)));
        Assert.Equal([9], sub);
    }

    [Fact]
    public void CheckedLongArithmetic()
    {
        using TestDatabase db = Setup();
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 1, LongVal = 10 });

        Assert.Equal([20L], Both(db, q => q.Select(r => checked(ClientEvalTestFunctions.PassLong(r.LongVal) + r.LongVal))));
        Assert.Equal([7L], Both(db, q => q.Select(r => checked(ClientEvalTestFunctions.PassLong(r.LongVal) - 3L))));
        Assert.Equal([20L], Both(db, q => q.Select(r => checked(ClientEvalTestFunctions.PassLong(r.LongVal) * 2L))));
    }

    [Fact]
    public void CheckedUIntArithmetic()
    {
        using TestDatabase db = Setup();
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 1, UIntVal = 10 });

        Assert.Equal([11u], Both(db, q => q.Select(r => checked(ClientEvalTestFunctions.PassUInt(r.UIntVal) + 1u))));
        Assert.Equal([9u], Both(db, q => q.Select(r => checked(ClientEvalTestFunctions.PassUInt(r.UIntVal) - 1u))));
        Assert.Equal([20u], Both(db, q => q.Select(r => checked(ClientEvalTestFunctions.PassUInt(r.UIntVal) * 2u))));
    }

    [Fact]
    public void CheckedULongArithmetic()
    {
        using TestDatabase db = Setup();
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 1, ULongVal = 10 });

        Assert.Equal([11ul], Both(db, q => q.Select(r => checked(ClientEvalTestFunctions.PassULong(r.ULongVal) + 1ul))));
        Assert.Equal([9ul], Both(db, q => q.Select(r => checked(ClientEvalTestFunctions.PassULong(r.ULongVal) - 1ul))));
        Assert.Equal([20ul], Both(db, q => q.Select(r => checked(ClientEvalTestFunctions.PassULong(r.ULongVal) * 2ul))));
    }

    [Fact]
    public void CheckedFloatArithmeticFallsBackToOperator()
    {
        using TestDatabase db = Setup();
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 1, FloatVal = 10f });

        Assert.Equal([11f], Both(db, q => q.Select(r => checked(ClientEvalTestFunctions.PassFloat(r.FloatVal) + 1f))));
    }

    [Fact]
    public void OnesComplementOverIntegerTypes()
    {
        using TestDatabase db = Setup();
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 1, LongVal = 5, UIntVal = 5, ULongVal = 5 });

        Assert.Equal([-6L], Both(db, q => q.Select(r => ~ClientEvalTestFunctions.PassLong(r.LongVal))));
        Assert.Equal([~5u], Both(db, q => q.Select(r => ~ClientEvalTestFunctions.PassUInt(r.UIntVal))));
        Assert.Equal([~5ul], Both(db, q => q.Select(r => ~ClientEvalTestFunctions.PassULong(r.ULongVal))));
    }

    [Fact]
    public void FloatAndDoubleConversions()
    {
        using TestDatabase db = Setup();
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 1, FloatVal = 10.5f, DoubleVal = 10.9 });

        Assert.Equal([10], Both(db, q => q.Select(r => (int)ClientEvalTestFunctions.PassFloat(r.FloatVal))));
        Assert.Equal([10.5d], Both(db, q => q.Select(r => (double)ClientEvalTestFunctions.PassFloat(r.FloatVal))));
        Assert.Equal([10], Both(db, q => q.Select(r => (int)ClientEvalTestFunctions.PassDouble(r.DoubleVal))));
    }

    [Fact]
    public void NullableGreaterThanOrEqualThreeStates()
    {
        using TestDatabase db = Setup();
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 1, IntA = 2, IntB = 5 });
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 2, IntA = 5, IntB = 2 });
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 3, IntA = 5, IntB = 4 });

        List<bool> result = Both(db, q => q.OrderBy(r => r.Id)
            .Select(r => ClientEvalTestFunctions.ToNullable(r.IntA) >= ClientEvalTestFunctions.ToNullable(r.IntB)));
        Assert.Equal([false, false, true], result);
    }

    [Fact]
    public void NullableLessThanThreeStates()
    {
        using TestDatabase db = Setup();
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 1, IntA = 2, IntB = 5 });
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 2, IntA = 5, IntB = 2 });
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 3, IntA = 4, IntB = 5 });

        List<bool> result = Both(db, q => q.OrderBy(r => r.Id)
            .Select(r => ClientEvalTestFunctions.ToNullable(r.IntA) < ClientEvalTestFunctions.ToNullable(r.IntB)));
        Assert.Equal([false, false, true], result);
    }

    [Fact]
    public void NullableLessThanOrEqualThreeStates()
    {
        using TestDatabase db = Setup();
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 1, IntA = 2, IntB = 5 });
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 2, IntA = 5, IntB = 2 });
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 3, IntA = 4, IntB = 4 });

        List<bool> result = Both(db, q => q.OrderBy(r => r.Id)
            .Select(r => ClientEvalTestFunctions.ToNullable(r.IntA) <= ClientEvalTestFunctions.ToNullable(r.IntB)));
        Assert.Equal([false, false, true], result);
    }

    [Fact]
    public void IntegerArithmeticOperators()
    {
        using TestDatabase db = Setup();
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 1, IntA = 12 });

        Assert.Equal([9], Both(db, q => q.Select(r => ClientEvalTestFunctions.Pass(r.IntA) - 3)));
        Assert.Equal([24], Both(db, q => q.Select(r => ClientEvalTestFunctions.Pass(r.IntA) * 2)));
        Assert.Equal([6], Both(db, q => q.Select(r => ClientEvalTestFunctions.Pass(r.IntA) / 2)));
        Assert.Equal([0], Both(db, q => q.Select(r => ClientEvalTestFunctions.Pass(r.IntA) % 3)));
        Assert.Equal([13], Both(db, q => q.Select(r => ClientEvalTestFunctions.Pass(r.IntA) ^ 1)));
        Assert.Equal([24], Both(db, q => q.Select(r => ClientEvalTestFunctions.Pass(r.IntA) << 1)));
        Assert.Equal([6], Both(db, q => q.Select(r => ClientEvalTestFunctions.Pass(r.IntA) >> 1)));
    }

    [Fact]
    public void NegateInteger()
    {
        using TestDatabase db = Setup();
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 1, IntA = 10 });

        Assert.Equal([-10], Both(db, q => q.Select(r => -ClientEvalTestFunctions.Pass(r.IntA))));
        Assert.Equal([-10], Both(db, q => q.Select(r => checked(-ClientEvalTestFunctions.Pass(r.IntA)))));
    }

    [Fact]
    public void NullableEqualsOnPresentAndNullValues()
    {
        using TestDatabase db = Setup();
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 1, IntA = 5 });
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 2, IntA = 2 });

        List<bool> equals = Both(db, q => q.OrderBy(r => r.Id).Select(r => ClientEvalTestFunctions.ToNullable(r.IntA).Equals(5)));
        Assert.Equal([true, false], equals);
    }

#pragma warning disable CS8629
    [Fact]
    public void NullableGetTypeOnPresentValue()
    {
        using TestDatabase db = Setup();
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 1, IntA = 5 });

        List<string> result = Both(db, q => q.Select(r => ClientEvalTestFunctions.ToNullable(r.IntA).GetType().Name));
        Assert.Equal(["Int32"], result);
    }
#pragma warning restore CS8629

    [Fact]
    public void ConvertEnumValueToAnotherEnum()
    {
        using TestDatabase db = Setup();
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 1, IntA = 1 });
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 2, IntA = 2 });

        List<ProjColorB> result = Both(db, q => q.OrderBy(r => r.Id)
            .Select(r => (ProjColorB)ClientEvalTestFunctions.PassColorA((ProjColorA)r.IntA)));
        Assert.Equal([ProjColorB.Dark, ProjColorB.Light], result);
    }

    [Fact]
    public void LiftedBitwiseAndWithNullOperand()
    {
        using TestDatabase db = Setup();
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 1, UIntVal = 6 });
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 2, UIntVal = 2 });

        List<uint?> result = Both(db, q => q.OrderBy(r => r.Id)
            .Select(r => ClientEvalTestFunctions.ToNullableUInt(r.UIntVal) & ClientEvalTestFunctions.ToNullableUInt(1u)));
        Assert.Equal([0u, null], result);
    }

    [Fact]
    public void LiftedBitwiseOrOnNullableBool()
    {
        using TestDatabase db = Setup();
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 1, IntA = 5, IntB = 5 });
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 2, IntA = 5, IntB = 2 });
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 3, IntA = 2, IntB = 5 });

        List<bool?> result = Both(db, q => q.OrderBy(r => r.Id)
            .Select(r => ClientEvalTestFunctions.ToNullableFalseBool(r.IntA) | ClientEvalTestFunctions.ToNullableFalseBool(r.IntB)));
        Assert.Equal([false, null, null], result);
    }

    [Fact]
    public void NullableGetValueOrDefaultWithoutArgument()
    {
        using TestDatabase db = Setup();
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 1, IntA = 5 });
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 2, IntA = 2 });

        List<int> result = Both(db, q => q.OrderBy(r => r.Id)
            .Select(r => ClientEvalTestFunctions.ToNullable(r.IntA).GetValueOrDefault()));
        Assert.Equal([5, 0], result);
    }

    [Fact]
    public void CheckedNegateOfNullDecimal()
    {
        using TestDatabase db = Setup();
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 1, IntA = 5 });
        db.Table<NumericProjectionRow>().Add(new NumericProjectionRow { Id = 2, IntA = 2 });

        List<decimal?> result = Both(db, q => q.OrderBy(r => r.Id)
            .Select(r => checked(-ClientEvalTestFunctions.ToNullableDecimal(r.IntA))));
        Assert.Equal([-5m, null], result);
    }
}
