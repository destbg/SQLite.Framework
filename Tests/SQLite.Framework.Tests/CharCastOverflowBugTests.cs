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

public class CharCastOverflowBugTests
{
    [Fact]
    public void CharCastOverflow()
    {
        using TestDatabase db = new();
                db.Table<NumericType>().Schema.CreateTable();
                db.Table<NumericType>().Add(new NumericType { Id = 1, IntValue = 70000 });

                char actual = db.Table<NumericType>().Where(n => n.Id == 1).Select(n => (char)n.IntValue).First();
                int src = 70000;
                char oracle = new[] { src }.Select(v => unchecked((char)v)).First();

                Assert.Equal(oracle, actual);
    }

}