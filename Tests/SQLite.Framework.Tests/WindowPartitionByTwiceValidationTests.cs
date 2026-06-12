using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class DoublePartitionRow
{
    [Key]
    public int Id { get; set; }

    public int GroupA { get; set; }

    public int GroupB { get; set; }

    public int Value { get; set; }
}

public class WindowPartitionByTwiceValidationTests
{
    [Fact]
    public void SecondPartitionByWithoutThenThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<DoublePartitionRow>().Schema.CreateTable();
        db.Table<DoublePartitionRow>().Add(new DoublePartitionRow { Id = 1, GroupA = 1, GroupB = 1, Value = 10 });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<DoublePartitionRow>()
                .Select(r => SQLiteWindowFunctions.Sum(r.Value).Over().PartitionBy(r.GroupA).PartitionBy(r.GroupB).AsValue())
                .ToList());
    }
}
