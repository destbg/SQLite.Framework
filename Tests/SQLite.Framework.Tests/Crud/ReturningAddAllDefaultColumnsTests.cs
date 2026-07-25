using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("AllDefaultRows")]
public class AllDefaultRow
{
    [Key, AutoIncrement]
    public int Id { get; set; }

    [DefaultValue(10)]
    public int Rating { get; set; }
}

public class ReturningAddAllDefaultColumnsTests
{
    [Fact]
    public void ReturningAdd_AllWritableColumnsDefaulted_InsertsWithDatabaseDefaultAndReturnsRow()
    {
        using TestDatabase db = new();
        db.Table<AllDefaultRow>().Schema.CreateTable();

        AllDefaultRow entity = new();

        AllDefaultRow? inserted = db.Table<AllDefaultRow>()
            .Returning()
            .Add(entity);

        Assert.NotNull(inserted);
        Assert.True(inserted!.Id > 0);
        Assert.Equal(10, inserted.Rating);

        int storedRating = db.Query<int>("SELECT \"Rating\" FROM \"AllDefaultRows\" WHERE \"Id\" = " + inserted.Id).First();
        Assert.Equal(10, storedRating);
    }
}