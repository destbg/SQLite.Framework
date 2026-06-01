using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;

namespace SQLite.Framework.Tests.Entities;

#if !SQLITECIPHER

[FullTextSearch(ContentMode = FtsContentMode.Internal)]
[TrigramTokenizer(RemoveDiacritics = true)]
public class Trigram_RemoveDiacritics_Search
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public required string Body { get; set; }
}
#endif
