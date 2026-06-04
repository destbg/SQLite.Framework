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

public class TimeOnlySubtractionWrapBugTests
{
    [Fact]
    public void TimeOnlySubtractionWrap()
    {
        using TestDatabase db = new();
                db.Table<TimeOnlyMethodEntity>().Schema.CreateTable();
                db.Table<TimeOnlyMethodEntity>().Add(new TimeOnlyMethodEntity { Id = 1, Time = new TimeOnly(1, 0, 0) });

                TimeSpan actual = db.Table<TimeOnlyMethodEntity>()
                    .Where(x => x.Id == 1)
                    .Select(x => x.Time - new TimeOnly(3, 0, 0))
                    .First();

                TimeSpan oracle = new TimeOnly(1, 0, 0) - new TimeOnly(3, 0, 0);

                Assert.Equal(oracle, actual);
    }

}