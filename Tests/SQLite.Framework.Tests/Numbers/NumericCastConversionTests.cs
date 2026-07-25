using System.Collections.Generic;
using System.Linq;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class NumericCastConversionTests
{
    [Fact]
    public void ULongColumnToDouble_AboveSignBit_KeepsSign()
    {
        ulong stored = 9223372036854775808UL;
    
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, ULongValue = stored });
    
        double oracle = new[] { stored }.Select(v => (double)v).First();
        Assert.Equal(9223372036854775808.0, oracle);
    
        double actual = db.Table<NumericType>().Where(n => n.Id == 1).Select(n => (double)n.ULongValue).First();
    
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void NegativeIntColumnToUInt_InPredicate_Matches()
    {
        int stored = -1;
        uint target = 4294967295U;
    
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, IntValue = stored });
    
        List<int> oracle = new[] { (id: 1, val: stored) }
            .Where(r => (uint)r.val == target)
            .Select(r => r.id)
            .ToList();
        Assert.Equal(new List<int> { 1 }, oracle);
    
        List<int> actual = db.Table<NumericType>()
            .Where(n => (uint)n.IntValue == target)
            .Select(n => n.Id)
            .ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ULongColumnToFloat_AboveSignBit_KeepsSign()
    {
        ulong stored = 9223372036854775808UL;

        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, ULongValue = stored });

        float oracle = new[] { stored }.Select(v => (float)v).First();
        Assert.Equal(9.223372E18f, oracle);

        float actual = db.Table<NumericType>().Where(n => n.Id == 1).Select(n => (float)n.ULongValue).First();

        Assert.Equal(oracle, actual);
    }
}
