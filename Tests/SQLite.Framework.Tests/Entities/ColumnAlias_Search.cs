using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;

namespace SQLite.Framework.Tests.Entities;

[FullTextSearch(ContentMode = FtsContentMode.Internal)]
public class ColumnAlias_Search
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    [Column("body_text")]
    public required string Body { get; set; }
}
