using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21jSrcArticle")]
public class H21jSrcArticle
{
    [Key]
    public int Id { get; set; }

    public required string Title { get; set; }

    public required string Content { get; set; }
}

[FullTextSearch(ContentMode = FtsContentMode.External, ContentTable = typeof(H21jSrcArticle), AutoSync = FtsAutoSync.Triggers)]
[Table("H21jSrcArticleSearch")]
public class H21jSrcArticleSearch
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public required string Title { get; set; }

    [FullTextIndexed]
    public required string Body { get; set; }
}

[Table("H21jSrcHiddenArticle")]
public class H21jSrcHiddenArticle
{
    [Key]
    public int Id { get; set; }

    public required string Title { get; set; }

    [NotMapped]
    public string Body { get; set; } = "";
}

[FullTextSearch(ContentMode = FtsContentMode.External, ContentTable = typeof(H21jSrcHiddenArticle), AutoSync = FtsAutoSync.Triggers)]
[Table("H21jSrcHiddenArticleSearch")]
public class H21jSrcHiddenArticleSearch
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public required string Body { get; set; }
}

public class FtsIndexedColumnSourcePropertyTests
{
    [Fact]
    public void IndexedColumnWithNoContentTablePropertyReportsAModelError()
    {
        using TestDatabase db = new();
        db.Table<H21jSrcArticle>().Schema.CreateTable();

        Assert.Throws<InvalidOperationException>(() => db.Table<H21jSrcArticleSearch>().Schema.CreateTable());
    }

    [Fact]
    public void IndexedColumnMatchingAnUnmappedContentPropertyReportsAModelError()
    {
        using TestDatabase db = new();
        db.Table<H21jSrcHiddenArticle>().Schema.CreateTable();

        Assert.Throws<InvalidOperationException>(() => db.Table<H21jSrcHiddenArticleSearch>().Schema.CreateTable());
    }
}
