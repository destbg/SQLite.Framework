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

public class TimeSpanTimeOnlyComponentBugTests
{
    [Fact]
    public void TimeSpanTimeOnlyComponent_00()
    {
        using TestDatabase db = new();
                db.Table<TimeSpanRow>().Schema.CreateTable();
                db.Table<TimeSpanRow>().Add(new TimeSpanRow { Id = 1, Span = new TimeSpan(2, 3, 4, 5, 6, 7) });

                double actual = db.Table<TimeSpanRow>()
                    .Where(x => x.Id == 1)
                    .Select(x => x.Span.TotalMicroseconds)
                    .First();

                double oracle = new TimeSpan(2, 3, 4, 5, 6, 7).TotalMicroseconds;

                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void TimeSpanTimeOnlyComponent_01()
    {
        using TestDatabase db = new();
                db.Table<TimeSpanRow>().Schema.CreateTable();
                db.Table<TimeSpanRow>().Add(new TimeSpanRow { Id = 1, Span = new TimeSpan(2, 3, 4, 5, 6, 7) });

                double actual = db.Table<TimeSpanRow>()
                    .Where(x => x.Id == 1)
                    .Select(x => x.Span.TotalNanoseconds)
                    .First();

                double oracle = new TimeSpan(2, 3, 4, 5, 6, 7).TotalNanoseconds;

                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void TimeSpanTimeOnlyComponent_02()
    {
        using TestDatabase db = new();
                db.Table<TimeSpanRow>().Schema.CreateTable();
                db.Table<TimeSpanRow>().Add(new TimeSpanRow { Id = 1, Span = new TimeSpan(2, 3, 4, 5, 6, 7) });

                int actual = db.Table<TimeSpanRow>()
                    .Where(x => x.Id == 1)
                    .Select(x => x.Span.Microseconds)
                    .First();

                int oracle = new TimeSpan(2, 3, 4, 5, 6, 7).Microseconds;

                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void TimeSpanTimeOnlyComponent_03()
    {
        using TestDatabase db = new();
                db.Table<TimeSpanRow>().Schema.CreateTable();
                db.Table<TimeSpanRow>().Add(new TimeSpanRow { Id = 1, Span = new TimeSpan(2, 3, 4, 5, 6, 7) });

                int actual = db.Table<TimeSpanRow>()
                    .Where(x => x.Id == 1)
                    .Select(x => x.Span.Nanoseconds)
                    .First();

                int oracle = new TimeSpan(2, 3, 4, 5, 6, 7).Nanoseconds;

                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void TimeSpanTimeOnlyComponent_04()
    {
        using TestDatabase db = new();
                db.Table<DateOnlyMethodEntity>().Schema.CreateTable();
                db.Table<DateOnlyMethodEntity>().Add(new DateOnlyMethodEntity { Id = 1, Date = new DateOnly(2000, 2, 3) });

                int actual = db.Table<DateOnlyMethodEntity>()
                    .Where(x => x.Id == 1)
                    .Select(x => x.Date.DayNumber)
                    .First();

                int oracle = new DateOnly(2000, 2, 3).DayNumber;

                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void TimeSpanTimeOnlyComponent_05()
    {
        using TestDatabase db = new();
                db.Table<TimeOnlyMethodEntity>().Schema.CreateTable();
                db.Table<TimeOnlyMethodEntity>().Add(new TimeOnlyMethodEntity { Id = 1, Time = new TimeOnly(3, 4, 5, 6, 7) });

                int actual = db.Table<TimeOnlyMethodEntity>()
                    .Where(x => x.Id == 1)
                    .Select(x => x.Time.Millisecond)
                    .First();

                int oracle = new TimeOnly(3, 4, 5, 6, 7).Millisecond;

                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void TimeSpanTimeOnlyComponent_06()
    {
        using TestDatabase db = new();
                db.Table<TimeOnlyMethodEntity>().Schema.CreateTable();
                db.Table<TimeOnlyMethodEntity>().Add(new TimeOnlyMethodEntity { Id = 1, Time = new TimeOnly(3, 4, 5, 6, 7) });

                int actual = db.Table<TimeOnlyMethodEntity>()
                    .Where(x => x.Id == 1)
                    .Select(x => x.Time.Microsecond)
                    .First();

                int oracle = new TimeOnly(3, 4, 5, 6, 7).Microsecond;

                Assert.Equal(oracle, actual);
    }

}