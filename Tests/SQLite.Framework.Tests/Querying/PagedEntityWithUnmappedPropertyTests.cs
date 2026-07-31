using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26uNoteRows")]
public class H26uNoteRow
{
    [Key]
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    [NotMapped]
    public string Slug { get; set; } = "default";
}

public class PagedEntityWithUnmappedPropertyTests
{
    [Fact]
    public void AFilterAfterTakeReadsAnEntityThatHasAnUnmappedProperty()
    {
        using TestDatabase db = Setup(nameof(AFilterAfterTakeReadsAnEntityThatHasAnUnmappedProperty));

        List<string> expected = Rows().Take(2).Where(r => r.Id > 0).Select(r => r.Title).ToList();

        List<string> actual = db.Table<H26uNoteRow>()
            .Take(2)
            .Where(r => r.Id > 0)
            .AsEnumerable()
            .Select(r => r.Title)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DistinctAfterTakeReadsAnEntityThatHasAnUnmappedProperty()
    {
        using TestDatabase db = Setup(nameof(DistinctAfterTakeReadsAnEntityThatHasAnUnmappedProperty));

        List<string> expected = Rows().Take(2).Select(r => r.Title).Distinct().ToList();

        List<string> actual = db.Table<H26uNoteRow>()
            .Take(2)
            .Distinct()
            .AsEnumerable()
            .Select(r => r.Title)
            .Distinct()
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H26uNoteRow> Rows()
    {
        return
        [
            new H26uNoteRow { Id = 1, Title = "first" },
            new H26uNoteRow { Id = 2, Title = "second" },
            new H26uNoteRow { Id = 3, Title = "third" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H26uNoteRow>().Schema.CreateTable();
        db.Table<H26uNoteRow>().AddRange(Rows());
        return db;
    }
}
