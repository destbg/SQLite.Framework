using System;
using System.Collections.Generic;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Flags]
public enum AccessFlags
{
    None = 0,
    Read = 1,
    Write = 2,
    Execute = 4
}

public class GuidOrderRow
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }

    public Guid G { get; set; }
}

public class FlagsParseRow
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }

    public string Code { get; set; } = "";
}

public class LinqToObjectsParityTests
{
    [Fact]
    public void CharPlusInt_Arithmetic_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        char[] chars = ['a', '7', 'A', 'z'];
        for (int i = 0; i < chars.Length; i++)
        {
            db.Table<NumericType>().Add(new NumericType { Id = i + 1, CharValue = chars[i] });
        }

        List<int> expected = chars.Select(c => c + 1).ToList();
        List<int> actual = db.Table<NumericType>().OrderBy(n => n.Id).Select(n => n.CharValue + 1).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StringStartsWith_Ordinal_IsCaseSensitive()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "abxyz", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "ABxyz", AuthorId = 1, Price = 2 });

        List<int> expected = new[]
            {
                (Id: 1, Title: "abxyz"),
                (Id: 2, Title: "ABxyz")
            }
            .Where(b => b.Title.StartsWith("AB", StringComparison.Ordinal))
            .Select(b => b.Id)
            .OrderBy(i => i)
            .ToList();

        List<int> actual = db.Table<Book>()
            .Where(b => b.Title.StartsWith("AB", StringComparison.Ordinal))
            .Select(b => b.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StringContains_Ordinal_IsCaseSensitive()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "xabcy", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "xABCy", AuthorId = 1, Price = 2 });

        List<int> expected = new[]
            {
                (Id: 1, Title: "xabcy"),
                (Id: 2, Title: "xABCy")
            }
            .Where(b => b.Title.Contains("abc", StringComparison.Ordinal))
            .Select(b => b.Id)
            .OrderBy(i => i)
            .ToList();

        List<int> actual = db.Table<Book>()
            .Where(b => b.Title.Contains("abc", StringComparison.Ordinal))
            .Select(b => b.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NegatedContains_NullableColumn_KeepsNullRows()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();
        int?[] values = [null, 5, 7, 9];
        for (int i = 0; i < values.Length; i++)
        {
            db.Table<NullableEntity>().Add(new NullableEntity { Id = i + 1, Value = values[i] });
        }

        int?[] filter = [5, 7];

        List<int> expected = Enumerable.Range(1, values.Length)
            .Where(i => !filter.Contains(values[i - 1]))
            .ToList();

        List<int> actual = db.Table<NullableEntity>()
            .Where(x => !filter.Contains(x.Value))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BitwiseComplement_UInt_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        uint[] values = [5, 50, 4294967295];
        for (int i = 0; i < values.Length; i++)
        {
            db.Table<NumericType>().Add(new NumericType { Id = i + 1, UIntValue = values[i] });
        }

        List<int> expected = Enumerable.Range(1, values.Length)
            .Where(i => ~values[i - 1] < 100)
            .ToList();

        List<int> actual = db.Table<NumericType>()
            .Where(n => ~n.UIntValue < 100)
            .Select(n => n.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GuidOrdering_MatchesGuidCompareTo()
    {
        using TestDatabase db = new();
        db.Table<GuidOrderRow>().Schema.CreateTable();
        (int Id, Guid G)[] rows =
        [
            (1, new Guid("ffffffff-0000-0000-0000-000000000000")),
            (2, new Guid("00000001-0000-0000-0000-000000000000")),
            (3, new Guid("7fffffff-0000-0000-0000-000000000000"))
        ];
        foreach ((int id, Guid g) in rows)
        {
            db.Table<GuidOrderRow>().Add(new GuidOrderRow { Id = id, G = g });
        }

        List<int> expected = rows.OrderBy(r => r.G).Select(r => r.Id).ToList();
        List<int> actual = db.Table<GuidOrderRow>().OrderBy(r => r.G).Select(r => r.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EnumParse_FlagsString_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<FlagsParseRow>().Schema.CreateTable();
        string[] codes = ["Read, Write", "Read", "Write"];
        for (int i = 0; i < codes.Length; i++)
        {
            db.Table<FlagsParseRow>().Add(new FlagsParseRow { Id = i + 1, Code = codes[i] });
        }

        List<AccessFlags> expected = codes.Select(Enum.Parse<AccessFlags>).ToList();
        List<AccessFlags> actual = db.Table<FlagsParseRow>()
            .OrderBy(r => r.Id)
            .Select(r => Enum.Parse<AccessFlags>(r.Code))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Distinct_PreservesFirstSeenOrder()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        string[] titles = ["banana", "apple", "cherry", "apple", "banana"];
        for (int i = 0; i < titles.Length; i++)
        {
            db.Table<Book>().Add(new Book { Id = i + 1, Title = titles[i], AuthorId = 1, Price = i + 1 });
        }

        List<string> expected = titles.Distinct().ToList();
        List<string> actual = db.Table<Book>().Select(b => b.Title).Distinct().ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableGetValueOrDefault_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();
        int?[] values = [null, 5];
        for (int i = 0; i < values.Length; i++)
        {
            db.Table<NullableEntity>().Add(new NullableEntity { Id = i + 1, Value = values[i] });
        }

        List<int> expected = values.Select(v => v.GetValueOrDefault(99)).ToList();
        List<int> actual = db.Table<NullableEntity>()
            .OrderBy(x => x.Id)
            .Select(x => x.Value.GetValueOrDefault(99))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SumOfAllNullNullable_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();
        int?[] values = [null, null];
        for (int i = 0; i < values.Length; i++)
        {
            db.Table<NullableEntity>().Add(new NullableEntity { Id = i + 1, Value = values[i] });
        }

        int? expected = values.Sum(v => v);
        int? actual = db.Table<NullableEntity>().Sum(x => x.Value);

        Assert.Equal(expected, actual);
    }
}
