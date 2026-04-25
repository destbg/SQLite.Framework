using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;

namespace SQLite.Framework.Tests.Entities;

[FullTextSearch(ContentMode = FtsContentMode.External, ContentTable = typeof(Article), AutoSync = FtsAutoSync.Triggers)]
public class ArticleSearch
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed(Weight = 10.0)]
    public required string Title { get; set; }

    [FullTextIndexed]
    public required string Body { get; set; }
}
