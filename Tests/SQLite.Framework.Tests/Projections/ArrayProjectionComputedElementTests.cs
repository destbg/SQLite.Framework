using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class ArrayProjectionItem
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Value { get; set; }
}

public static class ArrayProjectionMath
{
    public static int AddTen(int v) => v + 10;
}

public class ArrayProjectionComputedElementTests
{
    [Fact]
    public void ClientEvalElementWithRawElementComputesBothValues()
    {
        using TestDatabase db = SetupDatabase();

        List<int[]> expected = db.Table<ArrayProjectionItem>().AsEnumerable()
            .OrderBy(i => i.Id)
            .Select(i => new[] { ArrayProjectionMath.AddTen(i.Value), i.Value })
            .ToList();

        Assert.Equal([[20, 10], [35, 25]], expected);

        List<int[]> actual = db.Table<ArrayProjectionItem>()
            .OrderBy(i => i.Id)
            .Select(i => new[] { ArrayProjectionMath.AddTen(i.Value), i.Value })
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SqlComputedElementWithRawElementComputesBothValues()
    {
        using TestDatabase db = SetupDatabase();

        List<int[]> expected = db.Table<ArrayProjectionItem>().AsEnumerable()
            .OrderBy(i => i.Id)
            .Select(i => new[] { i.Value * 2, i.Value })
            .ToList();

        Assert.Equal([[20, 10], [50, 25]], expected);

        List<int[]> actual = db.Table<ArrayProjectionItem>()
            .OrderBy(i => i.Id)
            .Select(i => new[] { i.Value * 2, i.Value })
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AllRawElementsKeepColumnValues()
    {
        using TestDatabase db = SetupDatabase();

        List<int[]> expected = db.Table<ArrayProjectionItem>().AsEnumerable()
            .OrderBy(i => i.Id)
            .Select(i => new[] { i.Value, i.Id })
            .ToList();

        Assert.Equal([[10, 1], [25, 2]], expected);

        List<int[]> actual = db.Table<ArrayProjectionItem>()
            .OrderBy(i => i.Id)
            .Select(i => new[] { i.Value, i.Id })
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SqlComputedAndClientEvalAndRawElementsComputeAllValues()
    {
        using TestDatabase db = SetupDatabase();

        List<int[]> expected = db.Table<ArrayProjectionItem>().AsEnumerable()
            .OrderBy(i => i.Id)
            .Select(i => new[] { i.Value * 2, ArrayProjectionMath.AddTen(i.Value), i.Value })
            .ToList();

        Assert.Equal([[20, 20, 10], [50, 35, 25]], expected);

        List<int[]> actual = db.Table<ArrayProjectionItem>()
            .OrderBy(i => i.Id)
            .Select(i => new[] { i.Value * 2, ArrayProjectionMath.AddTen(i.Value), i.Value })
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ClientEvalElementInsideBinaryWithRawOperandComputesValue()
    {
        using TestDatabase db = SetupDatabase();

        List<int[]> expected = db.Table<ArrayProjectionItem>().AsEnumerable()
            .OrderBy(i => i.Id)
            .Select(i => new[] { ArrayProjectionMath.AddTen(i.Value) + i.Id, i.Value })
            .ToList();

        Assert.Equal([[21, 10], [37, 25]], expected);

        List<int[]> actual = db.Table<ArrayProjectionItem>()
            .OrderBy(i => i.Id)
            .Select(i => new[] { ArrayProjectionMath.AddTen(i.Value) + i.Id, i.Value })
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ClientEvalStringElementWithRawElementComputesValue()
    {
        using TestDatabase db = SetupDatabase();

        List<string[]> expected = db.Table<ArrayProjectionItem>().AsEnumerable()
            .OrderBy(i => i.Id)
            .Select(i => new[] { string.Concat("[", i.Name, "]"), i.Name })
            .ToList();

        Assert.Equal([["[ten]", "ten"], ["[two]", "two"]], expected);

        List<string[]> actual = db.Table<ArrayProjectionItem>()
            .OrderBy(i => i.Id)
            .Select(i => new[] { Tag(i.Name), i.Name })
            .ToList();

        Assert.Equal(expected, actual);
    }

    public static string Tag(string s) => "[" + s + "]";

    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<ArrayProjectionItem>().Schema.CreateTable();
        db.Table<ArrayProjectionItem>().Add(new ArrayProjectionItem { Id = 1, Name = "ten", Value = 10 });
        db.Table<ArrayProjectionItem>().Add(new ArrayProjectionItem { Id = 2, Name = "two", Value = 25 });
        return db;
    }
}
