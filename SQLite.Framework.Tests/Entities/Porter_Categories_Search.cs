using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;

namespace SQLite.Framework.Tests.Entities;

[FullTextSearch(ContentMode = FtsContentMode.Internal)]
[PorterTokenizer(Categories = "L* N*")]
public class Porter_Categories_Search
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public required string Body { get; set; }
}
