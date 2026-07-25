using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class CapturedMemberRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Value { get; set; }
}

public static class CapturedProjectionMath
{
    public static int AddTen(int v) => v + 10;
}

public class CapturedPrivateMemberClientEvalProjectionTests
{
    private readonly int instanceOffset = 32;

    private static int Multiplier => 4;

    [Fact]
    public void PrivateInstanceFieldInClientEvalProjectionIsRead()
    {
        using TestDatabase db = SetupDatabase();

        List<string> expected = db.Table<CapturedMemberRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => r.Name + "|" + (32 + r.Value + 10))
            .ToList();

        Assert.Equal(["a|52", "b|67"], expected);

        List<string> actual = db.Table<CapturedMemberRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Name, F = instanceOffset + CapturedProjectionMath.AddTen(r.Value) })
            .AsEnumerable()
            .Select(x => x.Name + "|" + x.F)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PrivateStaticPropertyInClientEvalProjectionIsRead()
    {
        using TestDatabase db = SetupDatabase();

        List<string> expected = db.Table<CapturedMemberRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => r.Name + "|" + (4 + r.Id + 10))
            .ToList();

        Assert.Equal(["a|15", "b|16"], expected);

        List<string> actual = db.Table<CapturedMemberRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Name, M = CapturedPrivateMemberClientEvalProjectionTests.Multiplier + CapturedProjectionMath.AddTen(r.Id) })
            .AsEnumerable()
            .Select(x => x.Name + "|" + x.M)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PrivateInstanceFieldAndPrivateStaticPropertyTogetherAreRead()
    {
        using TestDatabase db = SetupDatabase();

        List<string> expected = db.Table<CapturedMemberRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => 32 + r.Value + 10 + "|" + (4 + r.Id + 10))
            .ToList();

        Assert.Equal(["52|15", "67|16"], expected);

        List<string> actual = db.Table<CapturedMemberRow>()
            .OrderBy(r => r.Id)
            .Select(r => new
            {
                F = instanceOffset + CapturedProjectionMath.AddTen(r.Value),
                M = CapturedPrivateMemberClientEvalProjectionTests.Multiplier + CapturedProjectionMath.AddTen(r.Id)
            })
            .AsEnumerable()
            .Select(x => x.F + "|" + x.M)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<CapturedMemberRow>().Schema.CreateTable();
        db.Table<CapturedMemberRow>().Add(new CapturedMemberRow { Id = 1, Name = "a", Value = 10 });
        db.Table<CapturedMemberRow>().Add(new CapturedMemberRow { Id = 2, Name = "b", Value = 25 });
        return db;
    }
}
