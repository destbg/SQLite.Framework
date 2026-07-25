using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class TaggedItemBase
{
    public int Tag { get; set; }
}

[Table("TaggedItem")]
public class TaggedItemRow : TaggedItemBase
{
    [Key]
    public int Id { get; set; }

    public new string Tag { get; set; } = "";
}

public class RetypedShadowedPropertyColumnMappingTests
{
    [Fact]
    public void EntityWithRetypedNewShadowedPropertyRoundTrips()
    {
        List<TaggedItemRow> memory = [new TaggedItemRow { Id = 1, Tag = "derived" }];
        string expected = memory.Single(r => r.Id == 1).Tag;
        Assert.Equal("derived", expected);

        using TestDatabase db = new();
        db.Schema.CreateTable<TaggedItemRow>();
        db.Table<TaggedItemRow>().Add(new TaggedItemRow { Id = 1, Tag = "derived" });
        string actual = db.Table<TaggedItemRow>().Single(r => r.Id == 1).Tag;

        Assert.Equal(expected, actual);
    }
}
