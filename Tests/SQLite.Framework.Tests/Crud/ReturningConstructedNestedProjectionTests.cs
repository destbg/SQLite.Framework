using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24nNestedReturningRows")]
public class H24nNestedReturningRow
{
    [Key]
    public int Id { get; set; }

    public string? City { get; set; }

    public string? Zip { get; set; }

    public string Note { get; set; } = "";
}

public class H24nNestedReturningPart
{
    public string? City { get; set; }

    public string? Zip { get; set; }
}

public class H24nNestedReturningView
{
    public int Id { get; set; }

    public H24nNestedReturningPart? Address { get; set; }
}

public class ReturningConstructedNestedProjectionTests
{
    [Fact]
    public void ReturningUpdateKeepsTheConstructedNestedObjectWhenEveryMemberIsNull()
    {
        using TestDatabase db = new(nameof(ReturningUpdateKeepsTheConstructedNestedObjectWhenEveryMemberIsNull));
        db.Table<H24nNestedReturningRow>().Schema.CreateTable();

        H24nNestedReturningRow row = new() { Id = 1, City = null, Zip = null, Note = "before" };
        db.Table<H24nNestedReturningRow>().Add(row);
        row.Note = "after";

        H24nNestedReturningView? actual = db.Table<H24nNestedReturningRow>()
            .Returning(r => new H24nNestedReturningView
            {
                Id = r.Id,
                Address = new H24nNestedReturningPart { City = r.City, Zip = r.Zip },
            })
            .Update(row);

        H24nNestedReturningView expected = new List<H24nNestedReturningRow> { row }
            .Select(r => new H24nNestedReturningView
            {
                Id = r.Id,
                Address = new H24nNestedReturningPart { City = r.City, Zip = r.Zip },
            })
            .Single();

        Assert.NotNull(actual);
        Assert.Equal(expected.Id, actual!.Id);
        Assert.NotNull(actual.Address);
        Assert.Equal(expected.Address!.City, actual.Address!.City);
        Assert.Equal(expected.Address.Zip, actual.Address.Zip);
    }

    [Fact]
    public void ReturningAddKeepsTheConstructedNestedObjectWhenEveryMemberIsNull()
    {
        using TestDatabase db = new(nameof(ReturningAddKeepsTheConstructedNestedObjectWhenEveryMemberIsNull));
        db.Table<H24nNestedReturningRow>().Schema.CreateTable();

        H24nNestedReturningRow row = new() { Id = 2, City = null, Zip = null, Note = "new" };

        H24nNestedReturningView? actual = db.Table<H24nNestedReturningRow>()
            .Returning(r => new H24nNestedReturningView
            {
                Id = r.Id,
                Address = new H24nNestedReturningPart { City = r.City, Zip = r.Zip },
            })
            .Add(row);

        Assert.NotNull(actual);
        Assert.Equal(2, actual!.Id);
        Assert.NotNull(actual.Address);
        Assert.Null(actual.Address!.City);
        Assert.Null(actual.Address.Zip);
    }
}
