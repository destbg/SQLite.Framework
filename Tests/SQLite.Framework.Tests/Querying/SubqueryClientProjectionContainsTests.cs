using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SubHeadRows")]
public class SubHeadRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public static class SubHeadFold
{
    public static string Head(string value)
    {
        return value.Substring(0, 1);
    }
}

public class SubqueryClientProjectionContainsTests
{
    [Fact]
    public void SubqueryContainsOverClientProjectionWithColumnArgumentThrows()
    {
        using TestDatabase db = Seed();

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => db.Table<SubHeadRow>()
            .Where(r => db.Table<SubHeadRow>().Select(x => SubHeadFold.Head(x.Name)).Contains(r.Name))
            .Select(r => r.Id)
            .ToList());

        Assert.Equal(
            "Contains after a projection that runs in memory is not supported, because SQL cannot compare a " +
            "value the database never computes. Read the projected values into a list first and call Contains on the list.",
            exception.Message);
    }

    [Fact]
    public void SubqueryContainsOverClientProjectionWithConstantArgumentThrows()
    {
        using TestDatabase db = Seed();

        Assert.Throws<NotSupportedException>(() => db.Table<SubHeadRow>()
            .Where(r => db.Table<SubHeadRow>().Select(x => SubHeadFold.Head(x.Name)).Contains("b"))
            .Any());
    }

    [Fact]
    public void ContainsOnAProjectedListMatchesLinq()
    {
        using TestDatabase db = Seed();
        List<SubHeadRow> local = Rows();

        List<int> expected = local
            .Where(r => local.Select(x => SubHeadFold.Head(x.Name)).Contains(r.Name))
            .Select(r => r.Id)
            .ToList();

        List<string> heads = db.Table<SubHeadRow>().Select(x => SubHeadFold.Head(x.Name)).ToList();
        List<int> actual = db.Table<SubHeadRow>()
            .Where(r => heads.Contains(r.Name))
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SubqueryContainsOverTranslatableProjectionMatchesLinq()
    {
        using TestDatabase db = Seed();
        List<SubHeadRow> local = Rows();

        List<int> expected = local
            .Where(r => local.Select(x => x.Name.Substring(0, 1)).Contains(r.Name))
            .Select(r => r.Id)
            .ToList();

        List<int> actual = db.Table<SubHeadRow>()
            .Where(r => db.Table<SubHeadRow>().Select(x => x.Name.Substring(0, 1)).Contains(r.Name))
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<SubHeadRow> Rows()
    {
        return
        [
            new SubHeadRow { Id = 1, Name = "ax" },
            new SubHeadRow { Id = 2, Name = "by" },
            new SubHeadRow { Id = 3, Name = "a" }
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<SubHeadRow>().Schema.CreateTable();
        db.Table<SubHeadRow>().AddRange(Rows());
        return db;
    }
}
