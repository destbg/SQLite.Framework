using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21mHeadRows")]
public class H21mHeadRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public static class H21mHeadFold
{
    public static string Head(string value)
    {
        return value.Substring(0, 1);
    }
}

public class ClientProjectionContainsParityTests
{
    [Fact]
    public void ContainsOverScalarClientProjectionMatchesLinq()
    {
        using TestDatabase db = Seed();
        List<H21mHeadRow> local = Rows();

        bool expected = local.Select(r => H21mHeadFold.Head(r.Name)).Contains("a");

        bool actual = db.Table<H21mHeadRow>().Select(r => H21mHeadFold.Head(r.Name)).Contains("a");

        Assert.Equal(expected, actual);
    }

    private static List<H21mHeadRow> Rows()
    {
        return
        [
            new H21mHeadRow { Id = 1, Name = "ax" },
            new H21mHeadRow { Id = 2, Name = "by" }
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<H21mHeadRow>().Schema.CreateTable();
        db.Table<H21mHeadRow>().AddRange(Rows());
        return db;
    }
}
