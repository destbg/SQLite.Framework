using System;
using System.Collections.Generic;
using System.Linq;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class NullableTextRow
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }

    public string? Text { get; set; }
}

public class SingleEvaluationParityTests
{
    private static readonly char[] SampleChars =
    [
        '0', '5', '9', 'a', 'm', 'z', 'A', 'M', 'Z', '@', '[', '`', '{', ' ', '!', '~'
    ];

    private static readonly int[] SampleInts = [-7, -1, 0, 1, 2, 5, 8, 9, 20];

    private static TestDatabase IntDb(int[] values)
    {
        TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        for (int i = 0; i < values.Length; i++)
        {
            db.Table<NumericType>().Add(new NumericType { Id = i + 1, IntValue = values[i] });
        }

        return db;
    }

    private static TestDatabase CharDb(char[] chars)
    {
        TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        for (int i = 0; i < chars.Length; i++)
        {
            db.Table<NumericType>().Add(new NumericType { Id = i + 1, CharValue = chars[i] });
        }

        return db;
    }

    [Fact]
    public void MathMin_MatchesDotNet()
    {
        using TestDatabase db = IntDb(SampleInts);

        List<int> expected = SampleInts.OrderBy(v => v).Select(v => Math.Min(v, 5)).ToList();
        List<int> actual = db.Table<NumericType>().OrderBy(n => n.IntValue).Select(n => Math.Min(n.IntValue, 5)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MathMax_MatchesDotNet()
    {
        using TestDatabase db = IntDb(SampleInts);

        List<int> expected = SampleInts.OrderBy(v => v).Select(v => Math.Max(v, 5)).ToList();
        List<int> actual = db.Table<NumericType>().OrderBy(n => n.IntValue).Select(n => Math.Max(n.IntValue, 5)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MathMin_TwoColumns_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, IntValue = 3, LongValue = 8 });
        db.Table<NumericType>().Add(new NumericType { Id = 2, IntValue = 9, LongValue = 4 });

        List<long> expected = new List<long> { Math.Min(3L, 8L), Math.Min(9L, 4L) };
        List<long> actual = db.Table<NumericType>().OrderBy(n => n.Id).Select(n => Math.Min(n.IntValue, n.LongValue)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MathClamp_MatchesDotNet()
    {
        using TestDatabase db = IntDb(SampleInts);

        List<int> expected = SampleInts.OrderBy(v => v).Select(v => Math.Clamp(v, 2, 8)).ToList();
        List<int> actual = db.Table<NumericType>().OrderBy(n => n.IntValue).Select(n => Math.Clamp(n.IntValue, 2, 8)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MathClamp_NegativeRange_MatchesDotNet()
    {
        using TestDatabase db = IntDb(SampleInts);

        List<int> expected = SampleInts.OrderBy(v => v).Select(v => Math.Clamp(v, -5, 5)).ToList();
        List<int> actual = db.Table<NumericType>().OrderBy(n => n.IntValue).Select(n => Math.Clamp(n.IntValue, -5, 5)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CharIsAsciiDigit_MatchesDotNet()
    {
        using TestDatabase db = CharDb(SampleChars);

        List<char> expected = SampleChars.Where(c => char.IsAsciiDigit(c)).OrderBy(c => c).ToList();
        List<char> actual = db.Table<NumericType>().Where(n => char.IsAsciiDigit(n.CharValue)).Select(n => n.CharValue).ToList().OrderBy(c => c).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CharIsAsciiLetter_MatchesDotNet()
    {
        using TestDatabase db = CharDb(SampleChars);

        List<char> expected = SampleChars.Where(c => char.IsAsciiLetter(c)).OrderBy(c => c).ToList();
        List<char> actual = db.Table<NumericType>().Where(n => char.IsAsciiLetter(n.CharValue)).Select(n => n.CharValue).ToList().OrderBy(c => c).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CharIsAsciiLetterLower_MatchesDotNet()
    {
        using TestDatabase db = CharDb(SampleChars);

        List<char> expected = SampleChars.Where(c => char.IsAsciiLetterLower(c)).OrderBy(c => c).ToList();
        List<char> actual = db.Table<NumericType>().Where(n => char.IsAsciiLetterLower(n.CharValue)).Select(n => n.CharValue).ToList().OrderBy(c => c).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CharIsAsciiLetterUpper_MatchesDotNet()
    {
        using TestDatabase db = CharDb(SampleChars);

        List<char> expected = SampleChars.Where(c => char.IsAsciiLetterUpper(c)).OrderBy(c => c).ToList();
        List<char> actual = db.Table<NumericType>().Where(n => char.IsAsciiLetterUpper(n.CharValue)).Select(n => n.CharValue).ToList().OrderBy(c => c).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CharIsAsciiLetterOrDigit_MatchesDotNet()
    {
        using TestDatabase db = CharDb(SampleChars);

        List<char> expected = SampleChars.Where(c => char.IsAsciiLetterOrDigit(c)).OrderBy(c => c).ToList();
        List<char> actual = db.Table<NumericType>().Where(n => char.IsAsciiLetterOrDigit(n.CharValue)).Select(n => n.CharValue).ToList().OrderBy(c => c).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StringIsNullOrEmpty_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NullableTextRow>().Schema.CreateTable();
        string?[] texts = [null, "", " ", "x", " x "];
        for (int i = 0; i < texts.Length; i++)
        {
            db.Table<NullableTextRow>().Add(new NullableTextRow { Id = i + 1, Text = texts[i] });
        }

        List<int> expected = Enumerable.Range(1, texts.Length).Where(i => string.IsNullOrEmpty(texts[i - 1])).ToList();
        List<int> actual = db.Table<NullableTextRow>().Where(r => string.IsNullOrEmpty(r.Text)).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StringIsNullOrWhiteSpace_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NullableTextRow>().Schema.CreateTable();
        string?[] texts = [null, "", " ", "\t", "x", " x "];
        for (int i = 0; i < texts.Length; i++)
        {
            db.Table<NullableTextRow>().Add(new NullableTextRow { Id = i + 1, Text = texts[i] });
        }

        List<int> expected = Enumerable.Range(1, texts.Length).Where(i => string.IsNullOrWhiteSpace(texts[i - 1])).ToList();
        List<int> actual = db.Table<NullableTextRow>().Where(r => string.IsNullOrWhiteSpace(r.Text)).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NotNullableComparison_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();
        int?[] values = [null, 3, 5, 7];
        for (int i = 0; i < values.Length; i++)
        {
            db.Table<NullableEntity>().Add(new NullableEntity { Id = i + 1, Value = values[i] });
        }

        List<int> expected = Enumerable.Range(1, values.Length).Where(i => !(values[i - 1] > 5)).ToList();
        List<int> actual = db.Table<NullableEntity>().Where(x => !(x.Value > 5)).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }
}
