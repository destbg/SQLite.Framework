using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SelectClientEvalInstanceReceiverArgOrderTests
{
    [Fact]
    public void ClientEvalIndexOfWithColumnValue_ReceiverAndArgumentNotSwapped()
    {
        using TestDatabase db = new();
        db.Table<TwoStringEntity>().Schema.CreateTable();
        db.Table<TwoStringEntity>().Add(new TwoStringEntity { Id = 1, A = "xabcd", B = "abc" });
        db.Table<TwoStringEntity>().Add(new TwoStringEntity { Id = 2, A = "zz", B = "z" });

        TwoStringEntity[] rows =
        [
            new TwoStringEntity { Id = 1, A = "xabcd", B = "abc" },
            new TwoStringEntity { Id = 2, A = "zz", B = "z" },
        ];

        List<int> oracle = rows
            .OrderBy(s => s.Id)
            .Select(s => s.A.IndexOf(s.B, StringComparison.Ordinal))
            .ToList();

        Assert.Equal(new List<int> { 1, 0 }, oracle);

        List<int> actual = db.Table<TwoStringEntity>()
            .OrderBy(s => s.Id)
            .Select(s => s.A.IndexOf(s.B, StringComparison.Ordinal))
            .ToList();

        Assert.Equal(oracle, actual);
    }
}