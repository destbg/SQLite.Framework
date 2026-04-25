using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;

namespace SQLite.Framework.Tests.Entities;

[FullTextSearch(ContentMode = FtsContentMode.External)]
public class Bad_ExternalNoContentTable_Search
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public required string Body { get; set; }
}
