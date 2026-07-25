using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("AllDefaultOrUpdateRow")]
file sealed class AllDefaultOrUpdateRow
{
    [Key]
    [DefaultValue(0)]
    public int Id { get; set; }

    [DefaultValue(10)]
    public int Rating { get; set; }
}

public class AddOrUpdateAllDefaultColumnsTests
{
    [Fact]
    public void AddOrUpdate_AllColumnsAtClrDefault_WithAllDatabaseDefaults_DoesNotProduceSyntaxError()
    {
        using TestDatabase db = new();
        db.Table<AllDefaultOrUpdateRow>().Schema.CreateTable();

        db.Table<AllDefaultOrUpdateRow>().AddOrUpdate(new AllDefaultOrUpdateRow());

        int count = db.Table<AllDefaultOrUpdateRow>().Count();
        Assert.Equal(1, count);
    }

    [Fact]
    public void AddOrUpdateRange_AllColumnsAtClrDefault_WithAllDatabaseDefaults_DoesNotProduceSyntaxError()
    {
        using TestDatabase db = new();
        db.Table<AllDefaultOrUpdateRow>().Schema.CreateTable();

        db.Table<AllDefaultOrUpdateRow>().AddOrUpdateRange(new[]
        {
            new AllDefaultOrUpdateRow(),
            new AllDefaultOrUpdateRow { Id = 2, Rating = 5 },
        });

        int count = db.Table<AllDefaultOrUpdateRow>().Count();
        Assert.Equal(2, count);
    }
}
