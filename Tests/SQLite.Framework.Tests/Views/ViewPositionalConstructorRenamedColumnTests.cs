using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("PcvBooks")]
public class PcvBook
{
    [Key]
    public int Id { get; set; }

    public string Title { get; set; } = "";
}

[Table("vPcvSummaries")]
public class PcvSummary
{
    [Column("SummaryId")]
    public int Id { get; set; }

    [Column("SummaryTitle")]
    public string Title { get; set; } = "";

    public PcvSummary(int id, string title)
    {
        Id = id;
        Title = title;
    }
}

public class ViewPositionalConstructorRenamedColumnTests
{
    private static TestDatabase Create(out List<PcvBook> books)
    {
        TestDatabase db = new();
        db.Table<PcvBook>().Schema.CreateTable();
        books =
        [
            new PcvBook { Id = 1, Title = "alpha" },
            new PcvBook { Id = 2, Title = "beta" },
        ];
        db.Table<PcvBook>().AddRange(books);
        db.Schema.CreateView<PcvSummary>(() =>
            from b in db.Table<PcvBook>()
            select new PcvSummary(b.Id, b.Title));
        return db;
    }

    [Fact]
    public void ReadsRowsThroughRenamedColumns()
    {
        using TestDatabase db = Create(out List<PcvBook> books);

        List<PcvSummary> rows = db.ReadOnlyTable<PcvSummary>().OrderBy(s => s.Id).ToList();

        List<PcvSummary> expected = books.Select(b => new PcvSummary(b.Id, b.Title)).OrderBy(s => s.Id).ToList();
        Assert.Equal(expected.Count, rows.Count);
        for (int i = 0; i < rows.Count; i++)
        {
            Assert.Equal(expected[i].Id, rows[i].Id);
            Assert.Equal(expected[i].Title, rows[i].Title);
        }
    }

    [Fact]
    public void FiltersOnRenamedColumn()
    {
        using TestDatabase db = Create(out List<PcvBook> books);

        List<int> ids = db.ReadOnlyTable<PcvSummary>().Where(s => s.Title == "beta").Select(s => s.Id).ToList();

        Assert.Equal(books.Where(b => b.Title == "beta").Select(b => b.Id).ToList(), ids);
    }
}
