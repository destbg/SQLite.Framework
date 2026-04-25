using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;

namespace SQLite.Framework.Tests.Entities;

[FullTextSearch(ContentMode = FtsContentMode.Internal)]
[Unicode61Tokenizer(Categories = "L* N* Co", TokenChars = "_-")]
public class CategorisedSearch
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public required string Body { get; set; }
}
