using System;
using System.Collections.Generic;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class RareStringSubSelectParityTests
{
    private static readonly string[] Titles =
    [
        "abcabc", "banana", "xyz", "aaa", "Hello World", "a", "mango"
    ];

    private static TestDatabase Seed(Action<SQLiteOptionsBuilder>? configure = null)
    {
        TestDatabase db = configure == null ? new TestDatabase() : new TestDatabase(configure);
        db.Table<Book>().Schema.CreateTable();
        for (int i = 0; i < Titles.Length; i++)
        {
            db.Table<Book>().Add(new Book { Id = i + 1, Title = Titles[i], AuthorId = 1, Price = i + 1 });
        }

        return db;
    }

    [Fact]
    public void LastIndexOf_Char_MatchesDotNet()
    {
        using TestDatabase db = Seed();

        List<int> expected = Titles.Select((t, i) => (t, i)).OrderBy(x => x.i).Select(x => x.t.LastIndexOf('a')).ToList();
        List<int> actual = db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Title.LastIndexOf('a')).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LastIndexOf_String_MatchesDotNet()
    {
        using TestDatabase db = Seed();

        List<int> expected = Titles.Select((t, i) => (t, i)).OrderBy(x => x.i).Select(x => x.t.LastIndexOf("bc", StringComparison.Ordinal)).ToList();
        List<int> actual = db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Title.LastIndexOf("bc")).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LastIndexOf_WithStart_MatchesDotNet()
    {
        using TestDatabase db = Seed();

        List<int> expected = Titles.Select((t, i) => (t, i)).OrderBy(x => x.i).Select(x => x.t.LastIndexOf('a', Math.Min(2, Math.Max(0, x.t.Length - 1)))).ToList();
        List<int> actual = db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Title.LastIndexOf('a', 2)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IndexOf_WithStart_MatchesDotNet()
    {
        using TestDatabase db = Seed();

        List<int> expected = Titles.Select((t, i) => (t, i)).OrderBy(x => x.i).Select(x => x.t.IndexOf("a", 1, StringComparison.Ordinal)).ToList();
        List<int> actual = db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Title.IndexOf("a", 1)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Insert_MatchesDotNet()
    {
        using TestDatabase db = Seed();

        List<string> expected = Titles.Select((t, i) => (t, i)).OrderBy(x => x.i).Select(x => x.t.Insert(1, "XY")).ToList();
        List<string> actual = db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Title.Insert(1, "XY")).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Remove_TwoArgs_MatchesDotNet()
    {
        using TestDatabase db = Seed();

        List<string> expected = Titles.Select((t, i) => (t, i)).OrderBy(x => x.i).Select(x => x.t.Remove(0, 1)).ToList();
        List<string> actual = db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Title.Remove(0, 1)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CompareTo_MatchesDotNet()
    {
        using TestDatabase db = Seed();

        List<int> expected = Titles.Select((t, i) => (t, i)).OrderBy(x => x.i).Select(x => Math.Sign(string.CompareOrdinal(x.t, "mango"))).ToList();
        List<int> actual = db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Title.CompareTo("mango")).ToList().Select(Math.Sign).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Compare_MatchesDotNet()
    {
        using TestDatabase db = Seed();

        List<int> expected = Titles.Select((t, i) => (t, i)).OrderBy(x => x.i).Select(x => Math.Sign(string.CompareOrdinal(x.t, "mango"))).ToList();
        List<int> actual = db.Table<Book>().OrderBy(b => b.Id).Select(b => string.Compare(b.Title, "mango")).ToList().Select(Math.Sign).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PadLeft_MatchesDotNet()
    {
        using TestDatabase db = Seed();

        List<string> expected = Titles.Select((t, i) => (t, i)).OrderBy(x => x.i).Select(x => x.t.PadLeft(8)).ToList();
        List<string> actual = db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Title.PadLeft(8)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PadLeft_WithChar_MatchesDotNet()
    {
        using TestDatabase db = Seed();

        List<string> expected = Titles.Select((t, i) => (t, i)).OrderBy(x => x.i).Select(x => x.t.PadLeft(8, '*')).ToList();
        List<string> actual = db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Title.PadLeft(8, '*')).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PadRight_MatchesDotNet()
    {
        using TestDatabase db = Seed();

        List<string> expected = Titles.Select((t, i) => (t, i)).OrderBy(x => x.i).Select(x => x.t.PadRight(8)).ToList();
        List<string> actual = db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Title.PadRight(8)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PadRight_WithChar_MatchesDotNet()
    {
        using TestDatabase db = Seed();

        List<string> expected = Titles.Select((t, i) => (t, i)).OrderBy(x => x.i).Select(x => x.t.PadRight(8, '*')).ToList();
        List<string> actual = db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Title.PadRight(8, '*')).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CaseSensitiveSearch_MatchesDotNet()
    {
        using TestDatabase db = Seed(b => b.CaseSensitiveStringComparison = true);

        List<int> startsExpected = Titles.Select((t, i) => (t, i)).Where(x => x.t.StartsWith("a", StringComparison.Ordinal)).Select(x => x.i + 1).OrderBy(i => i).ToList();
        List<int> startsActual = db.Table<Book>().Where(b => b.Title.StartsWith("a")).Select(b => b.Id).OrderBy(i => i).ToList();
        Assert.Equal(startsExpected, startsActual);

        List<int> endsExpected = Titles.Select((t, i) => (t, i)).Where(x => x.t.EndsWith("c", StringComparison.Ordinal)).Select(x => x.i + 1).OrderBy(i => i).ToList();
        List<int> endsActual = db.Table<Book>().Where(b => b.Title.EndsWith("c")).Select(b => b.Id).OrderBy(i => i).ToList();
        Assert.Equal(endsExpected, endsActual);

        List<int> containsExpected = Titles.Select((t, i) => (t, i)).Where(x => x.t.Contains("an", StringComparison.Ordinal)).Select(x => x.i + 1).OrderBy(i => i).ToList();
        List<int> containsActual = db.Table<Book>().Where(b => b.Title.Contains("an")).Select(b => b.Id).OrderBy(i => i).ToList();
        Assert.Equal(containsExpected, containsActual);
    }
}
