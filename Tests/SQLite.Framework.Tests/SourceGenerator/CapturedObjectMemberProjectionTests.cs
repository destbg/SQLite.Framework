using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23mCapturedMemberRows")]
public class H23mCapturedMemberRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class H23mCapturedInner
{
    public string Suffix { get; set; } = "";
}

public class H23mCapturedBox
{
    public string Marker = "";

    public string Label { get; set; } = "";

    public H23mCapturedInner Inner { get; set; } = new();
}

public static class H23mCapturedMemberFunctions
{
    public static string Combine(string name, string decoration)
    {
        return decoration + "|" + name;
    }
}

public class CapturedObjectMemberProjectionTests
{
    [Fact]
    public void CapturedObjectPropertyBesideARowMemberMatchesLinq()
    {
        using TestDatabase db = Setup();
        H23mCapturedBox box = Box();

        List<string> expected = Rows()
            .ConvertAll(r => H23mCapturedMemberFunctions.Combine(r.Name, box.Label));

        List<string> actual = db.Table<H23mCapturedMemberRow>()
            .OrderBy(r => r.Id)
            .Select(r => H23mCapturedMemberFunctions.Combine(r.Name, box.Label))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedObjectFieldBesideARowMemberMatchesLinq()
    {
        using TestDatabase db = Setup();
        H23mCapturedBox box = Box();

        List<string> expected = Rows()
            .ConvertAll(r => H23mCapturedMemberFunctions.Combine(r.Name, box.Marker));

        List<string> actual = db.Table<H23mCapturedMemberRow>()
            .OrderBy(r => r.Id)
            .Select(r => H23mCapturedMemberFunctions.Combine(r.Name, box.Marker))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedObjectNestedPropertyBesideARowMemberMatchesLinq()
    {
        using TestDatabase db = Setup();
        H23mCapturedBox box = Box();

        List<string> expected = Rows()
            .ConvertAll(r => H23mCapturedMemberFunctions.Combine(r.Name, box.Inner.Suffix));

        List<string> actual = db.Table<H23mCapturedMemberRow>()
            .OrderBy(r => r.Id)
            .Select(r => H23mCapturedMemberFunctions.Combine(r.Name, box.Inner.Suffix))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static H23mCapturedBox Box()
    {
        return new H23mCapturedBox
        {
            Marker = "M",
            Label = "L",
            Inner = new H23mCapturedInner { Suffix = "S" }
        };
    }

    private static List<H23mCapturedMemberRow> Rows()
    {
        return
        [
            new H23mCapturedMemberRow { Id = 1, Name = "a" },
            new H23mCapturedMemberRow { Id = 2, Name = "b" }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H23mCapturedMemberRow>().Schema.CreateTable();
        db.Table<H23mCapturedMemberRow>().AddRange(Rows());
        return db;
    }
}
