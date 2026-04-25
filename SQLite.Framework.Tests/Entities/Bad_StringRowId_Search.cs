using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;

namespace SQLite.Framework.Tests.Entities;

[FullTextSearch(ContentMode = FtsContentMode.Internal)]
public class Bad_StringRowId_Search
{
    [FullTextRowId]
    public required string Id { get; set; }

    [FullTextIndexed]
    public required string Body { get; set; }
}
