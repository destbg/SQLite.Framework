using System.ComponentModel.DataAnnotations;
using System.Numerics;
using System.Runtime.CompilerServices;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class BigIntegerConverterTests
{
    [Fact]
    public void RoundTrip()
    {
        using TestDatabase db = SetupDatabase();
        BigInteger huge = BigInteger.Parse("123456789012345678901234567890");
        db.Table<BigEntity>().Add(new BigEntity { Id = 1, Value = huge });

        BigEntity result = db.Table<BigEntity>().First();

        Assert.Equal(huge, result.Value);
    }

    [Fact]
    public void RoundTrip_NullableNullValue()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<NullableBigEntity>().Add(new NullableBigEntity { Id = 1, Value = null });

        NullableBigEntity result = db.Table<NullableBigEntity>().First();

        Assert.Null(result.Value);
    }

    [Fact]
    public void RoundTrip_NullableNonNullValue()
    {
        using TestDatabase db = SetupDatabase();
        BigInteger value = BigInteger.Parse("99999999999999999999");
        db.Table<NullableBigEntity>().Add(new NullableBigEntity { Id = 1, Value = value });

        NullableBigEntity result = db.Table<NullableBigEntity>().First();

        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void Select_SingleColumn()
    {
        using TestDatabase db = SetupDatabase();
        BigInteger value = BigInteger.Parse("314159265358979323846");
        db.Table<BigEntity>().Add(new BigEntity { Id = 1, Value = value });

        BigInteger result = db.Table<BigEntity>().Select(e => e.Value).First();

        Assert.Equal(value, result);
    }

    [Fact]
    public void Select_AnonymousProjection()
    {
        using TestDatabase db = SetupDatabase();
        BigInteger value = BigInteger.Parse("271828182845904523536");
        db.Table<BigEntity>().Add(new BigEntity { Id = 7, Value = value });

        var result = db.Table<BigEntity>().Select(e => new { e.Id, e.Value }).First();

        Assert.Equal(7, result.Id);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void Where_EqualityFilter()
    {
        using TestDatabase db = SetupDatabase();
        BigInteger a = BigInteger.Parse("100000000000000000000");
        BigInteger b = BigInteger.Parse("200000000000000000000");
        db.Table<BigEntity>().Add(new BigEntity { Id = 1, Value = a });
        db.Table<BigEntity>().Add(new BigEntity { Id = 2, Value = b });

        BigInteger target = a;
        List<BigEntity> results = db.Table<BigEntity>().Where(e => e.Value == target).ToList();

        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    [Fact]
    public void OrderBy_ByConvertedColumn()
    {
        using TestDatabase db = SetupDatabase();
        BigInteger small = BigInteger.Parse("100000000000000000000");
        BigInteger mid = BigInteger.Parse("200000000000000000000");
        BigInteger big = BigInteger.Parse("300000000000000000000");
        db.Table<BigEntity>().Add(new BigEntity { Id = 1, Value = mid });
        db.Table<BigEntity>().Add(new BigEntity { Id = 2, Value = big });
        db.Table<BigEntity>().Add(new BigEntity { Id = 3, Value = small });

        List<BigEntity> results = db.Table<BigEntity>().OrderBy(e => e.Value).ToList();

        Assert.Equal(small, results[0].Value);
        Assert.Equal(mid, results[1].Value);
        Assert.Equal(big, results[2].Value);
    }

    [Fact]
    public void Select_ToString_ClientSide()
    {
        using TestDatabase db = SetupDatabase();
        BigInteger value = BigInteger.Parse("999999999999999999999999");
        db.Table<BigEntity>().Add(new BigEntity { Id = 1, Value = value });

        string result = db.Table<BigEntity>().Select(e => e.Value.ToString()).First();

        Assert.Equal("999999999999999999999999", result);
    }

    [Fact]
    public void Select_BinaryOperatorPlus_ClientSide()
    {
        using TestDatabase db = SetupDatabase();
        BigInteger value = BigInteger.Parse("1000000000000000000000");
        db.Table<BigEntity>().Add(new BigEntity { Id = 1, Value = value });

        BigInteger bonus = BigInteger.Parse("500000000000000000000");
        var result = db.Table<BigEntity>().Select(e => new { Total = e.Value + bonus }).First();

        Assert.Equal(BigInteger.Parse("1500000000000000000000"), result.Total);
    }

    [Fact]
    public void Select_StaticMethod_ClientSide()
    {
        using TestDatabase db = SetupDatabase();
        BigInteger a = BigInteger.Parse("123456789012345");
        BigInteger b = BigInteger.Parse("987654321098765");
        db.Table<BigEntity>().Add(new BigEntity { Id = 1, Value = a });

        BigInteger captured = b;
        var result = db.Table<BigEntity>().Select(e => new { Gcd = BigInteger.GreatestCommonDivisor(e.Value, captured) }).First();

        Assert.Equal(BigInteger.GreatestCommonDivisor(a, b), result.Gcd);
    }

    [Fact]
    public void Select_InstanceProperty_ClientSide()
    {
        using TestDatabase db = SetupDatabase();
        BigInteger even = BigInteger.Parse("200000000000000000000");
        BigInteger odd = BigInteger.Parse("100000000000000000001");
        db.Table<BigEntity>().Add(new BigEntity { Id = 1, Value = even });
        db.Table<BigEntity>().Add(new BigEntity { Id = 2, Value = odd });

        List<bool> evenFlags = db.Table<BigEntity>().OrderBy(e => e.Id).Select(e => e.Value.IsEven).ToList();

        Assert.Equal(new[] { true, false }, evenFlags);
    }

    private static TestDatabase SetupDatabase(Action<SQLiteOptionsBuilder>? configure = null, [CallerMemberName] string? methodName = null)
    {
        TestDatabase db = new(b =>
        {
            b.AddTypeConverter<BigInteger>(new BigIntegerConverter());
            configure?.Invoke(b);
        }, methodName);
        db.Table<BigEntity>().Schema.CreateTable();
        db.Table<NullableBigEntity>().Schema.CreateTable();
        return db;
    }

    public class BigEntity
    {
        [Key]
        public required int Id { get; set; }

        public required BigInteger Value { get; set; }
    }

    public class NullableBigEntity
    {
        [Key]
        public required int Id { get; set; }

        public BigInteger? Value { get; set; }
    }

    public class BigIntegerConverter : ISQLiteTypeConverter
    {
        public SQLiteColumnType ColumnType => SQLiteColumnType.Text;

        public object? ToDatabase(object? value)
        {
            return value is BigInteger bi ? PadForSort(bi) : null;
        }

        public object? FromDatabase(object? value)
        {
            return value is string s ? BigInteger.Parse(UnpadForSort(s)) : null;
        }

        private static string PadForSort(BigInteger value)
        {
            bool negative = value.Sign < 0;
            BigInteger abs = BigInteger.Abs(value);
            string digits = abs.ToString();
            string padded = digits.PadLeft(40, '0');
            return negative ? "-" + Invert(padded) : "+" + padded;
        }

        private static string UnpadForSort(string stored)
        {
            if (stored.Length == 0)
            {
                return "0";
            }
            char sign = stored[0];
            string body = stored.Substring(1);
            string digits = sign == '-' ? Invert(body) : body;
            string trimmed = digits.TrimStart('0');
            string magnitude = trimmed.Length == 0 ? "0" : trimmed;
            return sign == '-' ? "-" + magnitude : magnitude;
        }

        private static string Invert(string digits)
        {
            char[] result = new char[digits.Length];
            for (int i = 0; i < digits.Length; i++)
            {
                result[i] = (char)('0' + ('9' - digits[i]));
            }
            return new string(result);
        }
    }
}
