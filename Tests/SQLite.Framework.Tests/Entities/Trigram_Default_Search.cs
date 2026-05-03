using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;

namespace SQLite.Framework.Tests.Entities;

#if !SQLITECIPHER

[FullTextSearch(ContentMode = FtsContentMode.Internal)]
[TrigramTokenizer]
public class Trigram_Default_Search
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public required string Body { get; set; }
}
#endif
