using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class UnsignedIntMultiplyRow
{
    [Key]
    public int Id { get; set; }

    public uint Value { get; set; }
}

public class UnsignedIntMultiplyOverflowComparisonTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<UnsignedIntMultiplyRow>().Schema.CreateTable();
        db.Table<UnsignedIntMultiplyRow>().Add(new UnsignedIntMultiplyRow { Id = 1, Value = 65536 });
        db.Table<UnsignedIntMultiplyRow>().Add(new UnsignedIntMultiplyRow { Id = 2, Value = 3 });
        return db;
    }

    [Fact]
    public void MultiplyKeepsFull64BitProductInComparison()
    {
        using TestDatabase db = SetupDatabase();

        List<int> linqWrap = db.Table<UnsignedIntMultiplyRow>().AsEnumerable()
            .Where(r => r.Value * r.Value == 0u)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([1], linqWrap);

        List<int> actual = db.Table<UnsignedIntMultiplyRow>()
            .Where(r => r.Value * r.Value == 0u)
            .Select(r => r.Id)
            .ToList();

        Assert.Empty(actual);
    }
}
