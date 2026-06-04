using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DateTimeAddTickPrecisionBugTests
{
    [Fact]
    public void DateTimeAddTickPrecision_00()
    {
        using TestDatabase db = new();
                db.Table<Author>().Schema.CreateTable();
                DateTime birth = new DateTime(2000, 2, 3, 4, 5, 6).AddTicks(8);
                db.Table<Author>().Add(new Author { Id = 1, Name = "x", Email = "e", BirthDate = birth });
                long actual = db.Table<Author>().Where(a => a.Id == 1).Select(a => a.BirthDate.AddSeconds(45)).First().Ticks;
                long oracle = new[] { birth }.Select(b => b.AddSeconds(45)).First().Ticks;
                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DateTimeAddTickPrecision_01()
    {
        using TestDatabase db = new();
                db.Table<Author>().Schema.CreateTable();
                DateTime birth = new DateTime(2000, 2, 3, 4, 5, 6).AddTicks(8);
                db.Table<Author>().Add(new Author { Id = 1, Name = "x", Email = "e", BirthDate = birth });
                long actual = db.Table<Author>().Where(a => a.Id == 1).Select(a => a.BirthDate.AddMinutes(30)).First().Ticks;
                long oracle = new[] { birth }.Select(b => b.AddMinutes(30)).First().Ticks;
                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DateTimeAddTickPrecision_02()
    {
        using TestDatabase db = new();
                db.Table<Author>().Schema.CreateTable();
                DateTime birth = new DateTime(2000, 2, 3, 4, 5, 6).AddTicks(8);
                db.Table<Author>().Add(new Author { Id = 1, Name = "x", Email = "e", BirthDate = birth });
                long actual = db.Table<Author>().Where(a => a.Id == 1).Select(a => a.BirthDate.AddHours(5)).First().Ticks;
                long oracle = new[] { birth }.Select(b => b.AddHours(5)).First().Ticks;
                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DateTimeAddTickPrecision_03()
    {
        using TestDatabase db = new();
                db.Table<Author>().Schema.CreateTable();
                DateTime birth = new DateTime(2000, 2, 3, 4, 5, 6, 7).AddTicks(8);
                db.Table<Author>().Add(new Author { Id = 1, Name = "x", Email = "e", BirthDate = birth });
                long actual = db.Table<Author>().Where(a => a.Id == 1).Select(a => a.BirthDate.AddMilliseconds(2)).First().Ticks;
                long oracle = new[] { birth }.Select(b => b.AddMilliseconds(2)).First().Ticks;
                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DateTimeAddTickPrecision_04()
    {
        using TestDatabase db = new();
                db.Table<Author>().Schema.CreateTable();
                DateTime birth = new DateTime(2000, 2, 3, 4, 5, 6).AddTicks(8);
                db.Table<Author>().Add(new Author { Id = 1, Name = "x", Email = "e", BirthDate = birth });
                long actual = db.Table<Author>().Where(a => a.Id == 1).Select(a => a.BirthDate.AddDays(0.123456789)).First().Ticks;
                long oracle = new[] { birth }.Select(b => b.AddDays(0.123456789)).First().Ticks;
                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DateTimeAddTickPrecision_05()
    {
        using TestDatabase db = new();
                db.Table<DateTimeOffsetEntity>().Schema.CreateTable();
                DateTimeOffset birth = new DateTimeOffset(2021, 3, 15, 10, 20, 30, TimeSpan.Zero).AddTicks(1234567);
                db.Table<DateTimeOffsetEntity>().Add(new DateTimeOffsetEntity { Id = 1, Value = birth });
                long actual = db.Table<DateTimeOffsetEntity>().Where(a => a.Id == 1).Select(a => a.Value.AddSeconds(45)).First().UtcDateTime.Ticks;
                long oracle = new[] { birth }.Select(b => b.AddSeconds(45)).First().UtcDateTime.Ticks;
                Assert.Equal(oracle, actual);
    }

}
public class DateTimeOffsetEntity
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }

    public DateTimeOffset Value { get; set; }
}
