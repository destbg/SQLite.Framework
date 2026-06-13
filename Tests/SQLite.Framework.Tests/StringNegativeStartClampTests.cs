using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class ClampFruitRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class StringNegativeStartClampTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<ClampFruitRow>().Schema.CreateTable();
        db.Table<ClampFruitRow>().Add(new ClampFruitRow { Id = 1, Name = "banana" });
        return db;
    }

    [Fact]
    public void SubstringNegativeStartClampsToZero()
    {
        using TestDatabase db = SetupDatabase();
        int start = -2;

        string actual = db.Table<ClampFruitRow>().Select(r => r.Name.Substring(start)).First();

        Assert.Equal("banana", actual);
    }

    [Fact]
    public void IndexOfNegativeStartClampsToZero()
    {
        using TestDatabase db = SetupDatabase();
        int start = -1;

        int actual = db.Table<ClampFruitRow>().Select(r => r.Name.IndexOf("n", start)).First();

        Assert.Equal(2, actual);
    }
}
