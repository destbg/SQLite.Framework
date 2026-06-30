using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class GuidToStringEntity
{
    [Key]
    public int Id { get; set; }

    public Guid Gid { get; set; }
}

public class GuidToStringInWhereTests
{
    [Fact]
    public void ToStringOnGuidColumnInWhereThrows()
    {
        using TestDatabase db = new();
        db.Table<GuidToStringEntity>().Schema.CreateTable();

        Guid g = new("11111111-1111-1111-1111-111111111111");
        db.Table<GuidToStringEntity>().Add(new GuidToStringEntity { Id = 1, Gid = g });

        string targetText = g.ToString();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<GuidToStringEntity>().Where(x => x.Gid.ToString() == targetText).Select(x => x.Id).ToList());
    }
}
