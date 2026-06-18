using System;
using System.Linq;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DateTimeKindRoundTripTests
{
    [Fact]
    public void IntegerStorage_Utc_ReadsBackUnspecified()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        DateTime utc = new DateTime(2024, 6, 17, 10, 0, 0, DateTimeKind.Utc);
        db.Table<Author>().Add(new Author { Id = 1, Name = "a", Email = "e", BirthDate = utc });

        DateTime readBack = db.Table<Author>().First().BirthDate;
        Assert.Equal(DateTimeKind.Unspecified, readBack.Kind);
    }

    [Fact]
    public void IntegerStorage_Local_ReadsBackUnspecified()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        DateTime local = new DateTime(2024, 6, 17, 10, 0, 0, DateTimeKind.Local);
        db.Table<Author>().Add(new Author { Id = 1, Name = "a", Email = "e", BirthDate = local });

        DateTime readBack = db.Table<Author>().First().BirthDate;
        Assert.Equal(DateTimeKind.Unspecified, readBack.Kind);
    }

    [Fact]
    public void TextFormattedRoundtripFormat_Utc_PreservesKindAndValue()
    {
        using TestDatabase db = new(b => b.UseDateTimeStorage(DateTimeStorageMode.TextFormatted, "o"));
        db.Table<Author>().Schema.CreateTable();
        DateTime utc = new DateTime(2024, 6, 17, 10, 30, 0, DateTimeKind.Utc);
        db.Table<Author>().Add(new Author { Id = 1, Name = "a", Email = "e", BirthDate = utc });

        DateTime readBack = db.Table<Author>().First().BirthDate;
        Assert.Equal(DateTimeKind.Utc, readBack.Kind);
        Assert.Equal(utc, readBack);
        Assert.Equal(utc.Ticks, readBack.Ticks);
    }

    [Fact]
    public void TextFormattedRoundtripFormat_Local_PreservesKind()
    {
        using TestDatabase db = new(b => b.UseDateTimeStorage(DateTimeStorageMode.TextFormatted, "o"));
        db.Table<Author>().Schema.CreateTable();
        DateTime local = new DateTime(2024, 6, 17, 10, 30, 0, DateTimeKind.Local);
        db.Table<Author>().Add(new Author { Id = 1, Name = "a", Email = "e", BirthDate = local });

        DateTime readBack = db.Table<Author>().First().BirthDate;
        Assert.Equal(DateTimeKind.Local, readBack.Kind);
        Assert.Equal(local.Ticks, readBack.Ticks);
    }
}
