using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class ReverseJoinParentRow
{
    [Key]
    public int Id { get; set; }
}

internal sealed class ReverseJoinChildRow
{
    [Key]
    public int Id { get; set; }

    public int ParentId { get; set; }

    public string Tag { get; set; } = "";

    public int Rank { get; set; }
}

public class CorrelatedSubqueryReverseOrderingTests
{
    [Fact]
    public void ReverseInsideStringJoinSubqueryReversesTheOrder()
    {
        using TestDatabase db = new();
        db.Table<ReverseJoinParentRow>().Schema.CreateTable();
        db.Table<ReverseJoinChildRow>().Schema.CreateTable();
        db.Table<ReverseJoinParentRow>().Add(new ReverseJoinParentRow { Id = 1 });
        db.Table<ReverseJoinChildRow>().Add(new ReverseJoinChildRow { Id = 1, ParentId = 1, Tag = "a", Rank = 2 });
        db.Table<ReverseJoinChildRow>().Add(new ReverseJoinChildRow { Id = 2, ParentId = 1, Tag = "b", Rank = 1 });
        db.Table<ReverseJoinChildRow>().Add(new ReverseJoinChildRow { Id = 3, ParentId = 1, Tag = "c", Rank = 3 });

        List<ReverseJoinChildRow> children = db.Table<ReverseJoinChildRow>().AsEnumerable().ToList();

        string expected = string.Join(",", children
            .Where(c => c.ParentId == 1)
            .OrderBy(c => c.Rank)
            .Select(c => c.Tag)
            .Reverse());

        Assert.Equal("c,a,b", expected);

        Assert.Throws<NotSupportedException>(() => db.Table<ReverseJoinParentRow>()
            .Select(p => string.Join(",", db.Table<ReverseJoinChildRow>()
                .Where(c => c.ParentId == p.Id)
                .OrderBy(c => c.Rank)
                .Select(c => c.Tag)
                .Reverse()))
            .First());
    }
}
