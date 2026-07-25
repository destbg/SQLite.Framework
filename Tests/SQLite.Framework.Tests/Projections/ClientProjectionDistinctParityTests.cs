using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21mFoldRows")]
public class H21mFoldRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public static class H21mProjectionFold
{
    public static string Head(string value)
    {
        return value.Substring(0, 1);
    }
}

public class ClientProjectionDistinctParityTests
{
    [Fact]
    public void DistinctOverScalarClientProjectionDedupsProjectedValues()
    {
        using TestDatabase db = Seed();
        List<H21mFoldRow> local = Rows();

        List<string> expected = local
            .Select(r => H21mProjectionFold.Head(r.Name))
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        List<string> actual = db.Table<H21mFoldRow>()
            .Select(r => H21mProjectionFold.Head(r.Name))
            .Distinct()
            .AsEnumerable()
            .OrderBy(x => x)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DistinctOverClientProjectionMemberDedupsProjectedValues()
    {
        using TestDatabase db = Seed();
        List<H21mFoldRow> local = Rows();

        List<string> expected = local
            .Select(r => new { H = H21mProjectionFold.Head(r.Name) })
            .Distinct()
            .Select(x => x.H)
            .OrderBy(x => x)
            .ToList();

        List<string> actual = db.Table<H21mFoldRow>()
            .Select(r => new { H = H21mProjectionFold.Head(r.Name) })
            .Distinct()
            .AsEnumerable()
            .Select(x => x.H)
            .OrderBy(x => x)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H21mFoldRow> Rows()
    {
        return
        [
            new H21mFoldRow { Id = 1, Name = "ax" },
            new H21mFoldRow { Id = 2, Name = "ay" }
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<H21mFoldRow>().Schema.CreateTable();
        db.Table<H21mFoldRow>().AddRange(Rows());
        return db;
    }
}
