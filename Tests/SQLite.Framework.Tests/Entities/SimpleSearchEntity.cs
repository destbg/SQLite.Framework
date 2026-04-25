using SQLite.Framework.Attributes;

namespace SQLite.Framework.Tests.Entities;

[FullTextSearch]
public class SimpleSearchEntity
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public required string Body { get; set; }
}
