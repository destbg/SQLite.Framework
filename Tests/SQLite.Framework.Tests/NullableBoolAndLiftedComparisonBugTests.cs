using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;
using SQLite.Framework.Tests.Entities;
using System.Globalization;

namespace SQLite.Framework.Tests;

public class NullableBoolAndLiftedComparisonBugTests
{
    [Fact]
    public void NullableBoolAndLiftedComparisonMatchesLinq()
    {
        using TestDatabase db = new();
                db.Table<sqlbinBoolCmpRow>().Schema.CreateTable();
                List<sqlbinBoolCmpRow> rows = new()
                {
                    new sqlbinBoolCmpRow { Id = 1, NFlag = null, NIntA = null },
                    new sqlbinBoolCmpRow { Id = 2, NFlag = true, NIntA = 7 },
                    new sqlbinBoolCmpRow { Id = 3, NFlag = false, NIntA = 2 },
                    new sqlbinBoolCmpRow { Id = 4, NFlag = true, NIntA = null },
                    new sqlbinBoolCmpRow { Id = 5, NFlag = null, NIntA = 5 },
                    new sqlbinBoolCmpRow { Id = 6, NFlag = false, NIntA = null }
                };
                db.Table<sqlbinBoolCmpRow>().AddRange(rows);
                List<bool?> actual = db.Table<sqlbinBoolCmpRow>().OrderBy(x => x.Id).Select(x => x.NFlag & (x.NIntA > 3)).ToList();
                List<bool?> oracle = rows.OrderBy(x => x.Id).Select(x => x.NFlag & (x.NIntA > 3)).ToList();
                Assert.Equal(oracle, actual);
    }
}

public class sqlbinBoolCmpRow
{
    [System.ComponentModel.DataAnnotations.Key] public int Id { get; set; }
    public bool? NFlag { get; set; }
    public int? NIntA { get; set; }
}
