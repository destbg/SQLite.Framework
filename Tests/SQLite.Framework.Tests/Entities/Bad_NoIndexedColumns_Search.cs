using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;

namespace SQLite.Framework.Tests.Entities;

[FullTextSearch(ContentMode = FtsContentMode.Internal)]
public class Bad_NoIndexedColumns_Search
{
    [FullTextRowId]
    public int Id { get; set; }

    public required string Body { get; set; }
}
