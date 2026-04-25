using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;

namespace SQLite.Framework.Tests.Entities;

[FullTextSearch(
    ContentMode = FtsContentMode.External,
    ContentTable = typeof(Article),
    ContentRowIdColumn = "Id",
    AutoSync = FtsAutoSync.Manual)]
public class Article_ExplicitRowIdColumn_Search
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public required string Title { get; set; }
}
