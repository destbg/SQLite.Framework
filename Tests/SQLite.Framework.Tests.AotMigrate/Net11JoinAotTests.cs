#if NET11_0_OR_GREATER
using SQLite.Framework.Extensions;
using SQLite.Framework.Generated;

namespace SQLite.Framework.Tests.AotMigrate;

public class Net11JoinAotTests
{
    [Fact]
    public void FullJoinTupleAndSelectorUseGeneratedMaterializers()
    {
        SQLiteOptionsBuilder builder = new(":memory:");
        builder.UseGeneratedMaterializers();
        builder.DisableReflectionFallback();
        using SQLiteDatabase db = new(builder.Build());
        db.Table<AotMessageRow>().Schema.CreateTable();
        db.Table<AotMessageRow>().AddRange([
            new() { Id = 1, SessionId = "match" }, new() { Id = 2, SessionId = "left" },
            new() { Id = 3, SessionId = "match" }, new() { Id = 4, SessionId = "right" }
        ]);
        var left = db.Table<AotMessageRow>().Where(x => x.Id <= 2);
        var right = db.Table<AotMessageRow>().Where(x => x.Id > 2);
        var pairs = left.FullJoin(right, l => l.SessionId, r => r.SessionId).ToList();
        Assert.Equal([(0, 4), (1, 3), (2, 0)], pairs.Select(x => (x.Outer?.Id ?? 0, x.Inner?.Id ?? 0)).Order());
        var projection = left.FullJoin(right, l => l.SessionId, r => r.SessionId,
            (l, r) => new { Left = l == null ? "missing" : Decorate(l.SessionId), Right = r == null ? "missing" : Decorate(r.SessionId) }, comparer: null)
            .ToList();
        Assert.Equal(["[left]/missing", "[match]/[match]", "missing/[right]"], projection.Select(x => x.Left + "/" + x.Right).Order());
        var groups = left.GroupJoin(right, l => l.SessionId, r => r.SessionId)
            .SelectMany(g => g.DefaultIfEmpty(), (g, r) => new { g.Key.Id, Match = r == null ? 0 : r.Id }).ToList();
        Assert.Equal([(1, 3), (2, 0)], groups.Select(x => (x.Id, x.Match)).Order());
        Assert.Equal([3, 4], db.Table<AotMessageRow>().Where(x => new[] { x.Id, 2 }.Max() > 2).Select(x => x.Id).OrderBy(x => x).ToList());
        Assert.Equal(0, db.SelectCompilerFallbacks);
    }

    private static string Decorate(string value) => "[" + value + "]";
}
#endif
