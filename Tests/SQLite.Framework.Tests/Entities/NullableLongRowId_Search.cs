using SQLite.Framework.Attributes;

namespace SQLite.Framework.Tests.Entities;

[FullTextSearch]
public class NullableLongRowId_Search
{
    [FullTextRowId]
    public long? Id { get; set; }

    [FullTextIndexed]
    public required string Body { get; set; }
}
