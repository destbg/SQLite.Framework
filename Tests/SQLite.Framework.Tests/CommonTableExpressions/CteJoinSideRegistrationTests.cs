using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CteJoinNode")]
public class CteJoinNodeRow
{
    [Key]
    public int Id { get; set; }

    public int ParentId { get; set; }

    public string Name { get; set; } = "";
}

public class CteJoinSideRegistrationTests
{
    private static (TestDatabase db, List<CteJoinNodeRow> mem) Seed()
    {
        TestDatabase db = new();
        db.Table<CteJoinNodeRow>().Schema.CreateTable();
        List<CteJoinNodeRow> mem =
        [
            new() { Id = 1, ParentId = 0, Name = "root" },
            new() { Id = 2, ParentId = 1, Name = "child" },
            new() { Id = 3, ParentId = 2, Name = "leaf" },
        ];
        foreach (CteJoinNodeRow row in mem)
        {
            db.Table<CteJoinNodeRow>().Add(row);
        }

        return (db, mem);
    }

    [Fact]
    public void ScalarCteAsJoinSideMatchesLinqToObjects()
    {
        (TestDatabase db, List<CteJoinNodeRow> mem) = Seed();
        using (db)
        {
            SQLiteCte<int> ids = db.With(() => db.Table<CteJoinNodeRow>().Select(n => n.Id));

            List<string> expected = mem
                .Join(mem.Select(n => n.Id), n => n.Id, v => v, (n, v) => n.Name)
                .OrderBy(x => x)
                .ToList();
            List<string> actual = (
                from n in db.Table<CteJoinNodeRow>()
                join v in ids on n.Id equals v
                select n.Name).ToList().OrderBy(x => x).ToList();

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void RecursiveScalarCteAsJoinSideMatchesLinqToObjects()
    {
        (TestDatabase db, List<CteJoinNodeRow> mem) = Seed();
        using (db)
        {
            SQLiteCte<int> depths = db.WithRecursive<int>(self =>
                db.Values(1).Concat(
                    from v in self
                    where v < 3
                    select v + 1));

            List<string> expected = mem
                .Join(new[] { 1, 2, 3 }, n => n.Id, v => v, (n, v) => n.Name)
                .OrderBy(x => x)
                .ToList();
            List<string> actual = (
                from n in db.Table<CteJoinNodeRow>()
                join v in depths on n.Id equals v
                select n.Name).ToList().OrderBy(x => x).ToList();

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void RecursiveEntityCteAsJoinSideMatchesLinqToObjects()
    {
        (TestDatabase db, List<CteJoinNodeRow> mem) = Seed();
        using (db)
        {
            SQLiteCte<CteJoinNodeRow> walk = db.WithRecursive<CteJoinNodeRow>(self =>
                db.Table<CteJoinNodeRow>().Where(n => n.ParentId == 0).Concat(
                    from n in db.Table<CteJoinNodeRow>()
                    join s in self on n.ParentId equals s.Id
                    select n));

            List<CteJoinNodeRow> memWalk = [];
            List<CteJoinNodeRow> frontier = mem.Where(n => n.ParentId == 0).ToList();
            while (frontier.Count > 0)
            {
                memWalk.AddRange(frontier);
                List<CteJoinNodeRow> parents = frontier;
                frontier = mem.Where(n => parents.Any(p => p.Id == n.ParentId)).ToList();
            }

            List<string> expected = mem
                .Join(memWalk, n => n.Id, w => w.Id, (n, w) => n.Name + w.ParentId)
                .OrderBy(x => x)
                .ToList();
            List<string> actual = (
                from n in db.Table<CteJoinNodeRow>()
                join w in walk on n.Id equals w.Id
                select n.Name + w.ParentId).ToList().OrderBy(x => x).ToList();

            Assert.Equal(expected, actual);
        }
    }
}
