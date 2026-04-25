using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;

namespace SQLite.Framework.Tests.Entities;

[FullTextSearch(ContentMode = FtsContentMode.Internal)]
public class Bad_TwoRowIds_Search
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextRowId]
    public long OtherId { get; set; }

    [FullTextIndexed]
    public required string Body { get; set; }
}
