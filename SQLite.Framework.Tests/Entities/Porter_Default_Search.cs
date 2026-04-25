using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;

namespace SQLite.Framework.Tests.Entities;

[FullTextSearch(ContentMode = FtsContentMode.Internal)]
[PorterTokenizer]
public class Porter_Default_Search
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public required string Body { get; set; }
}
