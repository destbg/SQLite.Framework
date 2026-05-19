using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;

namespace SQLite.Framework.Tests.Entities;

[FullTextSearch]
[PorterTokenizer(Base = PorterBaseTokenizer.Unicode61, Categories = "", Separators = "", TokenChars = "")]
public class Porter_EmptySettings_Search
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public required string Body { get; set; }
}
