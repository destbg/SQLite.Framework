using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StringIndexerCharIntegerStorageParityTests
{
    private static TestDatabase SeedWithString(string title)
    {
        TestDatabase db = new(o => o.CharStorage = SQLite.Framework.Enums.CharStorageMode.Integer);
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = title, AuthorId = 1, Price = 1 });
        return db;
    }

    [Fact]
    public void StringIndexer_ComparedToCharConstant_IntegerStorage_MatchesDotNet()
    {
        using TestDatabase db = SeedWithString("hello");

        char target = 'l';
        bool expected = "hello"[2] == target;

        bool actual = db.Table<Book>()
            .Where(b => b.Id == 1)
            .Select(b => b.Title[2] == target)
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StringIndexer_WhereClause_ComparedToCharConstant_IntegerStorage_MatchesDotNet()
    {
        using TestDatabase db = SeedWithString("hello");

        char target = 'h';
        List<int> expected = new[] { "hello" }
            .Where(s => s[0] == target)
            .Select((_, i) => i + 1)
            .ToList();

        List<int> actual = db.Table<Book>()
            .Where(b => b.Title[0] == target)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StringIndexer_AsStringMethodArg_DifferentColumn_IntegerStorage_MatchesDotNet()
    {
        TestDatabase db = new(o => o.CharStorage = SQLite.Framework.Enums.CharStorageMode.Integer);
        db.Table<TwoStringEntity>().Schema.CreateTable();
        db.Table<TwoStringEntity>().Add(new TwoStringEntity { Id = 1, A = "xyz", B = "abc" });

        using (db)
        {
            bool expected = "xyz".Contains("abc"[0]);

            bool actual = db.Table<TwoStringEntity>()
                .Where(r => r.Id == 1)
                .Select(r => r.A.Contains(r.B[0]))
                .First();

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void StringIndexer_CharMethod_IntegerStorage_MatchesDotNet()
    {
        using TestDatabase db = SeedWithString("5abc");

        bool expected = char.IsAsciiDigit("5abc"[0]);

        bool actual = db.Table<Book>()
            .Where(b => b.Id == 1)
            .Select(b => char.IsAsciiDigit(b.Title[0]))
            .First();

        Assert.Equal(expected, actual);
    }
}

file sealed class CharColumnEntity
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }

    public char CharValue { get; set; }

    public char? NullableCharValue { get; set; }
}

public class CharColumnToStringIntegerStorageParityTests
{
    private static TestDatabase SeedChar(char value)
    {
        TestDatabase db = new(o => o.CharStorage = SQLite.Framework.Enums.CharStorageMode.Integer);
        db.Table<CharColumnEntity>().Schema.CreateTable();
        db.Table<CharColumnEntity>().Add(new CharColumnEntity { Id = 1, CharValue = value });
        return db;
    }

    [Fact]
    public void CharColumnToString_IntegerStorage_MatchesDotNet()
    {
        using TestDatabase db = SeedChar('l');

        string expected = 'l'.ToString();

        string actual = db.Table<CharColumnEntity>()
            .Where(r => r.Id == 1)
            .Select(r => r.CharValue.ToString())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CharColumnConcatWithString_IntegerStorage_MatchesDotNet()
    {
        using TestDatabase db = SeedChar('h');

        string expected = "say " + 'h';

        string actual = db.Table<CharColumnEntity>()
            .Where(r => r.Id == 1)
            .Select(r => "say " + r.CharValue)
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableCharColumnConcatWithString_IntegerStorage_MatchesDotNet()
    {
        TestDatabase db = new(o => o.CharStorage = SQLite.Framework.Enums.CharStorageMode.Integer);
        db.Table<CharColumnEntity>().Schema.CreateTable();
        db.Table<CharColumnEntity>().Add(new CharColumnEntity { Id = 1, CharValue = 'a', NullableCharValue = 'h' });
        db.Table<CharColumnEntity>().Add(new CharColumnEntity { Id = 2, CharValue = 'a', NullableCharValue = null });

        using (db)
        {
            char?[] source = ['h', null];
            List<string> expected = source.Select(c => "say " + c).ToList();

            List<string> actual = db.Table<CharColumnEntity>()
                .OrderBy(r => r.Id)
                .Select(r => "say " + r.NullableCharValue)
                .ToList();

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void CharColumnCastToNullableConcat_IntegerStorage_MatchesDotNet()
    {
        using TestDatabase db = SeedChar('h');

        string expected = "say " + (char?)'h';

        string actual = db.Table<CharColumnEntity>()
            .Where(r => r.Id == 1)
            .Select(r => "say " + (char?)r.CharValue)
            .First();

        Assert.Equal(expected, actual);
    }
}

public class CharColumnToStringTextStorageParityTests
{
    private static TestDatabase SeedChar(char value)
    {
        TestDatabase db = new();
        db.Table<CharColumnEntity>().Schema.CreateTable();
        db.Table<CharColumnEntity>().Add(new CharColumnEntity { Id = 1, CharValue = value });
        return db;
    }

    [Fact]
    public void CharColumnToString_TextStorage_MatchesDotNet()
    {
        using TestDatabase db = SeedChar('l');

        string expected = 'l'.ToString();

        string actual = db.Table<CharColumnEntity>()
            .Where(r => r.Id == 1)
            .Select(r => r.CharValue.ToString())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CharColumnConcatWithString_TextStorage_MatchesDotNet()
    {
        using TestDatabase db = SeedChar('h');

        string expected = "say " + 'h';

        string actual = db.Table<CharColumnEntity>()
            .Where(r => r.Id == 1)
            .Select(r => "say " + r.CharValue)
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CharColumnToStringWithCulture_InWhere_Throws()
    {
        using TestDatabase db = SeedChar('a');

        Assert.Throws<NotSupportedException>(() => db.Table<CharColumnEntity>()
            .Where(r => r.CharValue.ToString(CultureInfo.InvariantCulture) == "a")
            .ToList());
    }

    [Fact]
    public void CharColumnCompareTo_InWhere_Throws()
    {
        using TestDatabase db = SeedChar('b');

        Assert.Throws<NotSupportedException>(() => db.Table<CharColumnEntity>()
            .Where(r => r.CharValue.CompareTo('a') > 0)
            .ToList());
    }

    [Fact]
    public void StringIndexerToString_ClientProjection_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "hello", AuthorId = 1, Price = 1 });

        string expected = "hello".Normalize()[0].ToString();

        string actual = db.Table<Book>()
            .Where(b => b.Id == 1)
            .Select(b => b.Title.Normalize()[0].ToString())
            .First();

        Assert.Equal(expected, actual);
    }
}
