using SQLite.Framework.Attributes;

namespace SQLite.Framework.Tests.Entities;

[FullTextSearch]
[Unicode61Tokenizer(Categories = "L*", Separators = ",", TokenChars = "-")]
public class Unicode61_AllSettings_Search
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public required string Body { get; set; }
}
