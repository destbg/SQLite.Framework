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

public class MathClampInvalidRangeTests
{
    [Fact]
    public void MathClampInvalidRange()
    {
        using TestDatabase db = new();
                db.Table<NumericType>().Schema.CreateTable();
                db.Table<NumericType>().Add(new NumericType { Id = 1, DoubleValue = 5.0 });

                string actual;
                try
                {
                    double v = db.Table<NumericType>()
                        .Select(n => Math.Clamp(n.DoubleValue, 10.0, 0.0))
                        .First();
                    actual = "value:" + v;
                }
                catch (ArgumentException)
                {
                    actual = "ArgumentException";
                }

                string oracle;
                try
                {
                    double v = new[] { 5.0 }
                        .Select(x => Math.Clamp(x, 10.0, 0.0))
                        .First();
                    oracle = "value:" + v;
                }
                catch (ArgumentException)
                {
                    oracle = "ArgumentException";
                }

                Assert.Equal(oracle, actual);
    }

}