using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22wRenameLefts")]
public class H22wRenameLeft
{
    [Key]
    public int Id { get; set; }

    public int K { get; set; }
}

[Table("H22wRenameRights")]
public class H22wRenameRight
{
    [Key]
    public int Id { get; set; }

    public int K { get; set; }
}

[Table("H22wRenameExtras")]
public class H22wRenameExtra
{
    [Key]
    public int Id { get; set; }

    public int K { get; set; }
}

public class H22wRenameBox
{
    public H22wRenameLeft Left { get; set; } = null!;

    public IEnumerable<H22wRenameRight> Carried { get; set; } = null!;
}

public class GroupJoinMemberInitRenameShapeTests
{
    [Fact]
    public void MemberInitRenamedGroupFlattensLikeLinq()
    {
        using TestDatabase db = new();
        (List<H22wRenameLeft> ls, List<H22wRenameRight> rs) = Seed(db);

        var expected = ls
            .GroupJoin(rs, l => l.K, r => r.K, (l, g) => new H22wRenameBox { Left = l, Carried = g })
            .SelectMany(b => b.Carried.DefaultIfEmpty(), (b, r) => new
            {
                LId = b.Left.Id,
                RId = r == null ? -1 : r.Id
            })
            .OrderBy(t => t.LId)
            .ThenBy(t => t.RId)
            .ToList();

        var actual = db.Table<H22wRenameLeft>()
            .GroupJoin(db.Table<H22wRenameRight>(), l => l.K, r => r.K, (l, g) => new H22wRenameBox { Left = l, Carried = g })
            .SelectMany(b => b.Carried.DefaultIfEmpty(), (b, r) => new
            {
                LId = b.Left.Id,
                RId = r == null ? -1 : r.Id
            })
            .ToList()
            .OrderBy(t => t.LId)
            .ThenBy(t => t.RId)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WholeResultCarriedGroupFlattensLikeLinq()
    {
        using TestDatabase db = new();
        (List<H22wRenameLeft> ls, List<H22wRenameRight> rs, List<H22wRenameExtra> es) = SeedWithExtras(db);

        var expected = ls
            .GroupJoin(rs, l => l.K, r => r.K, (l, g) => new { l, g })
            .GroupJoin(es, x => x.l.K, e => e.K, (x, g2) => new { x, g2 })
            .SelectMany(y => y.g2.DefaultIfEmpty(), (y, e) => new { Parent = y.x, e })
            .SelectMany(z => z.Parent.g.DefaultIfEmpty(), (z, r) => new
            {
                LId = z.Parent.l.Id,
                EId = z.e == null ? -1 : z.e.Id,
                RId = r == null ? -1 : r.Id
            })
            .OrderBy(t => t.LId)
            .ThenBy(t => t.EId)
            .ThenBy(t => t.RId)
            .ToList();

        var actual = db.Table<H22wRenameLeft>()
            .GroupJoin(db.Table<H22wRenameRight>(), l => l.K, r => r.K, (l, g) => new { l, g })
            .GroupJoin(db.Table<H22wRenameExtra>(), x => x.l.K, e => e.K, (x, g2) => new { x, g2 })
            .SelectMany(y => y.g2.DefaultIfEmpty(), (y, e) => new { Parent = y.x, e })
            .SelectMany(z => z.Parent.g.DefaultIfEmpty(), (z, r) => new
            {
                LId = z.Parent.l.Id,
                EId = z.e == null ? -1 : z.e.Id,
                RId = r == null ? -1 : r.Id
            })
            .ToList()
            .OrderBy(t => t.LId)
            .ThenBy(t => t.EId)
            .ThenBy(t => t.RId)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static (List<H22wRenameLeft> Ls, List<H22wRenameRight> Rs) Seed(TestDatabase db)
    {
        db.Table<H22wRenameLeft>().Schema.CreateTable();
        db.Table<H22wRenameRight>().Schema.CreateTable();

        List<H22wRenameLeft> ls =
        [
            new H22wRenameLeft { Id = 1, K = 10 },
            new H22wRenameLeft { Id = 2, K = 20 },
            new H22wRenameLeft { Id = 3, K = 30 }
        ];
        List<H22wRenameRight> rs =
        [
            new H22wRenameRight { Id = 1, K = 10 },
            new H22wRenameRight { Id = 2, K = 10 },
            new H22wRenameRight { Id = 3, K = 30 }
        ];

        db.Table<H22wRenameLeft>().AddRange(ls);
        db.Table<H22wRenameRight>().AddRange(rs);
        return (ls, rs);
    }

    private static (List<H22wRenameLeft> Ls, List<H22wRenameRight> Rs, List<H22wRenameExtra> Es) SeedWithExtras(TestDatabase db)
    {
        (List<H22wRenameLeft> ls, List<H22wRenameRight> rs) = Seed(db);
        db.Table<H22wRenameExtra>().Schema.CreateTable();

        List<H22wRenameExtra> es =
        [
            new H22wRenameExtra { Id = 1, K = 10 },
            new H22wRenameExtra { Id = 2, K = 20 }
        ];

        db.Table<H22wRenameExtra>().AddRange(es);
        return (ls, rs, es);
    }
}
