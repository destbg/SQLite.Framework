using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;

namespace SQLite.Framework.Tests.Entities;

#if !SQLITECIPHER

[FullTextSearch(ContentMode = FtsContentMode.Internal)]
[Unicode61Tokenizer]
[TrigramTokenizer]
public class Bad_TwoTokenizers_Search
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public required string Body { get; set; }
}
#endif
