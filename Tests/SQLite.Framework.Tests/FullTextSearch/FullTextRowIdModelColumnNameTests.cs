using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26kNamedRowIdDocs")]
[FullTextSearch]
public class H26kNamedRowIdDoc
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public string Body { get; set; } = "";
}

[Table("H26kAttributeRowIdDocs")]
[FullTextSearch]
public class H26kAttributeRowIdDoc
{
    [FullTextRowId]
    [Column("docid")]
    public int Id { get; set; }

    [FullTextIndexed]
    public string Body { get; set; } = "";
}

public class FullTextRowIdModelColumnNameTests
{
    [Fact]
    public void AFullTextRowIdRenamedThroughTheModelReadsBackTheSameWayAsTheAttribute()
    {
        List<int> attributeIds;
        using (TestDatabase attributeDb = new(null, nameof(AFullTextRowIdRenamedThroughTheModelReadsBackTheSameWayAsTheAttribute)))
        {
            attributeDb.Schema.CreateTable<H26kAttributeRowIdDoc>();
            attributeDb.Table<H26kAttributeRowIdDoc>().Add(new H26kAttributeRowIdDoc { Id = 7, Body = "apple" });
            attributeIds = attributeDb.Table<H26kAttributeRowIdDoc>().OrderBy(d => d.Id).Select(d => d.Id).ToList();
        }

        using ModelTestDatabase modelDb = new(model => model.Entity<H26kNamedRowIdDoc>().HasColumnName(d => d.Id, "docid"));
        modelDb.Schema.CreateTable<H26kNamedRowIdDoc>();
        modelDb.Table<H26kNamedRowIdDoc>().Add(new H26kNamedRowIdDoc { Id = 7, Body = "apple" });

        List<int> modelIds = modelDb.Table<H26kNamedRowIdDoc>().OrderBy(d => d.Id).Select(d => d.Id).ToList();

        Assert.Equal(attributeIds, modelIds);
    }
}
