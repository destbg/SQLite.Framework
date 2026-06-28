using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ReturningRerouteDefaultRows")]
internal sealed class ReturningRerouteDefaultRow
{
    [Key]
    [SQLite.Framework.Attributes.AutoIncrement]
    public int Id { get; set; }

    public required string Title { get; set; }

    [DefaultValue(10)]
    public int Rating { get; set; }
}

public class ReturningRerouteAddOrUpdateDefaultParityTests
{
    [Fact]
    public void ReturningAddReroutedToAddOrUpdateAppliesDatabaseDefault()
    {
        int nonReturningRating;
        using (TestDatabase oracle = new(b => b.OnAction((_, _, a) =>
                   a == SQLiteAction.Add ? SQLiteAction.AddOrUpdate : a)))
        {
            oracle.Table<ReturningRerouteDefaultRow>().Schema.CreateTable();
            ReturningRerouteDefaultRow plain = new() { Title = "x" };
            oracle.Table<ReturningRerouteDefaultRow>().Add(plain);
            nonReturningRating = oracle.Table<ReturningRerouteDefaultRow>().Single().Rating;
        }

        using TestDatabase db = new(b => b.OnAction((_, _, a) =>
            a == SQLiteAction.Add ? SQLiteAction.AddOrUpdate : a));
        db.Table<ReturningRerouteDefaultRow>().Schema.CreateTable();

        ReturningRerouteDefaultRow? returned = db.Table<ReturningRerouteDefaultRow>()
            .Returning()
            .Add(new ReturningRerouteDefaultRow { Title = "x" });

        int storedRating = db.Table<ReturningRerouteDefaultRow>().Single().Rating;

        Assert.Equal(10, nonReturningRating);
        Assert.NotNull(returned);
        Assert.Equal(nonReturningRating, returned!.Rating);
        Assert.Equal(nonReturningRating, storedRating);
    }
}
