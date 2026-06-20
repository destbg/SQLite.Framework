using System;
using System.Collections.Generic;
using System.Linq;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ConditionalLiftedComparisonParityTests
{
    private static readonly ProjEdgeRow[] Data =
    [
        new ProjEdgeRow { Id = 1, Flag = true, NullableInt = null, Text = "" },
        new ProjEdgeRow { Id = 2, Flag = true, NullableInt = 9, Text = "" },
        new ProjEdgeRow { Id = 3, Flag = false, NullableInt = null, Text = "" },
        new ProjEdgeRow { Id = 4, Flag = true, NullableInt = 3, Text = "" },
    ];

    private static void Same<T>(Func<IEnumerable<ProjEdgeRow>, IEnumerable<T>> enumerable, Func<IQueryable<ProjEdgeRow>, IQueryable<T>> queryable)
    {
        using TestDatabase db = new();
        db.Table<ProjEdgeRow>().Schema.CreateTable();
        db.Table<ProjEdgeRow>().AddRange(Data);

        List<T> expected = enumerable(Data.OrderBy(x => x.Id)).ToList();
        List<T> actual = queryable(db.Table<ProjEdgeRow>().OrderBy(x => x.Id)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConditionalWithLiftedComparisonBranchNegatedInWhere()
        => Same(
            q => q.Select(x => new { x.Id, B = x.Flag ? x.NullableInt > 5 : false }).Where(a => !a.B).Select(a => a.Id),
            q => q.Select(x => new { x.Id, B = x.Flag ? x.NullableInt > 5 : false }).Where(a => !a.B).Select(a => a.Id));

    [Fact]
    public void ConditionalWithLiftedComparisonBranchComparedFalseInWhere()
        => Same(
            q => q.Select(x => new { x.Id, B = x.Flag ? x.NullableInt > 5 : false }).Where(a => a.B == false).Select(a => a.Id),
            q => q.Select(x => new { x.Id, B = x.Flag ? x.NullableInt > 5 : false }).Where(a => a.B == false).Select(a => a.Id));
}
