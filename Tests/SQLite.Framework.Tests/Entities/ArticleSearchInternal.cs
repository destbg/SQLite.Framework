using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;

namespace SQLite.Framework.Tests.Entities;

#if !SQLITECIPHER

[FullTextSearch(ContentMode = FtsContentMode.Internal)]
[TrigramTokenizer(CaseSensitive = false)]
public class ArticleSearchInternal
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public required string Code { get; set; }
}
#endif
