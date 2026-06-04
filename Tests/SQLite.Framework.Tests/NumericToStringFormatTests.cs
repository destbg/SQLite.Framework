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

public class NumericToStringFormatTests
{
    [Fact]
    public void NumericToStringFormat_00()
    {
        using TestDatabase db = new();
                db.Table<NumericType>().Schema.CreateTable();
                db.Table<NumericType>().Add(new NumericType { Id = 1, IntValue = 255 });

                string actual = db.Table<NumericType>().Where(n => n.Id == 1).Select(n => n.IntValue.ToString("X")).First();
                string oracle = new[] { 255 }.Select(v => v.ToString("X")).First();

                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void NumericToStringFormat_01()
    {
        using TestDatabase db = new();
                db.Table<NumericType>().Schema.CreateTable();
                db.Table<NumericType>().Add(new NumericType { Id = 1, LongValue = 42 });

                string actual = db.Table<NumericType>().Where(n => n.Id == 1).Select(n => n.LongValue.ToString("D5")).First();
                string oracle = new[] { 42L }.Select(v => v.ToString("D5")).First();

                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void NumericToStringFormat_02()
    {
        using TestDatabase db = new();
                db.Table<NumericType>().Schema.CreateTable();
                db.Table<NumericType>().Add(new NumericType { Id = 1, DoubleValue = 3.14159 });

                string actual = db.Table<NumericType>().Where(n => n.Id == 1).Select(n => n.DoubleValue.ToString("F2")).First();
                string oracle = new[] { 3.14159 }.Select(v => v.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)).First();

                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void NumericToStringFormat_03()
    {
        using TestDatabase db = new();
                db.Table<NumericType>().Schema.CreateTable();
                db.Table<NumericType>().Add(new NumericType { Id = 1, DecimalValue = 3.1m });

                string actual = db.Table<NumericType>().Where(n => n.Id == 1).Select(n => n.DecimalValue.ToString("F2")).First();
                string oracle = new[] { 3.1m }.Select(v => v.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)).First();

                Assert.Equal(oracle, actual);
    }

}