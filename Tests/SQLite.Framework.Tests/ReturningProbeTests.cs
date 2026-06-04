using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("DefaultRatingRows")]
file sealed class DefaultRatingRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public required string Title { get; set; }

    [DefaultValue(10)]
    public int Rating { get; set; }
}

public class ReturningProbeTests
{
    [Fact]
    public void ReturningAddAppliesDatabaseDefault()
    {
        using TestDatabase db = new();
        db.Table<DefaultRatingRow>().Schema.CreateTable();

        DefaultRatingRow? r = db.Table<DefaultRatingRow>().Returning().Add(new DefaultRatingRow { Title = "Hello" });

        Assert.NotNull(r);
        Assert.Equal(10, r!.Rating);
    }
}
