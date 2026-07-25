using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("GroupConcatItem")]
public class GroupConcatItemRow
{
    [Key]
    public int Id { get; set; }

    public string Kind { get; set; } = "";

    public string Name { get; set; } = "";
}

public class GroupProjectionConcatTests
{
    private static string Shift(string value)
    {
        return value + "!";
    }

    private static (TestDatabase db, List<GroupConcatItemRow> mem) Seed()
    {
        TestDatabase db = new();
        db.Table<GroupConcatItemRow>().Schema.CreateTable();
        List<GroupConcatItemRow> mem =
        [
            new() { Id = 1, Kind = "a", Name = "x" },
            new() { Id = 2, Kind = "a", Name = "y" },
            new() { Id = 3, Kind = "b", Name = "z" },
        ];
        foreach (GroupConcatItemRow row in mem)
        {
            db.Table<GroupConcatItemRow>().Add(row);
        }

        return (db, mem);
    }

    [Fact]
    public void InterpolatedStringConcatOverGroupMatchesLinqToObjects()
    {
        (TestDatabase db, List<GroupConcatItemRow> mem) = Seed();
        using (db)
        {
            List<string> expected = mem.GroupBy(r => r.Kind).Select(g => $"{g.Key}:{string.Concat(g.Select(x => x.Name))}").OrderBy(x => x).ToList();
            List<string> actual = db.Table<GroupConcatItemRow>().GroupBy(r => r.Kind).Select(g => $"{g.Key}:{string.Concat(g.Select(x => x.Name))}").ToList().OrderBy(x => x).ToList();

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void StringJoinOverGroupMatchesLinqToObjects()
    {
        (TestDatabase db, List<GroupConcatItemRow> mem) = Seed();
        using (db)
        {
            List<string> expected = mem.GroupBy(r => r.Kind).Select(g => string.Join(",", g.Select(x => x.Name))).OrderBy(x => x).ToList();
            List<string> actual = db.Table<GroupConcatItemRow>().GroupBy(r => r.Kind).Select(g => string.Join(",", g.Select(x => x.Name))).ToList().OrderBy(x => x).ToList();

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void StringJoinCharSeparatorOverGroupMatchesLinqToObjects()
    {
        (TestDatabase db, List<GroupConcatItemRow> mem) = Seed();
        using (db)
        {
            List<string> expected = mem.GroupBy(r => r.Kind).Select(g => string.Join(';', g.Select(x => x.Name))).OrderBy(x => x).ToList();
            List<string> actual = db.Table<GroupConcatItemRow>().GroupBy(r => r.Kind).Select(g => string.Join(';', g.Select(x => x.Name))).ToList().OrderBy(x => x).ToList();

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void StringJoinOverFilteredGroupMatchesLinqToObjects()
    {
        (TestDatabase db, List<GroupConcatItemRow> mem) = Seed();
        using (db)
        {
            List<string> expected = mem.GroupBy(r => r.Kind).Select(g => string.Join(",", g.Where(x => x.Id > 1).Select(x => x.Name))).OrderBy(x => x).ToList();
            List<string> actual = db.Table<GroupConcatItemRow>().GroupBy(r => r.Kind).Select(g => string.Join(",", g.Where(x => x.Id > 1).Select(x => x.Name))).ToList().OrderBy(x => x).ToList();

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void StringConcatOverElementGroupMatchesLinqToObjects()
    {
        (TestDatabase db, List<GroupConcatItemRow> mem) = Seed();
        using (db)
        {
            List<string> expected = mem.GroupBy(r => r.Kind, r => r.Name).Select(g => string.Concat(g)).OrderBy(x => x).ToList();
            List<string> actual = db.Table<GroupConcatItemRow>().GroupBy(r => r.Kind, r => r.Name).Select(g => string.Concat(g)).ToList().OrderBy(x => x).ToList();

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void StringJoinWhereOnlyOverElementGroupMatchesLinqToObjects()
    {
        (TestDatabase db, List<GroupConcatItemRow> mem) = Seed();
        using (db)
        {
            List<string> expected = mem.GroupBy(r => r.Kind, r => r.Name).Select(g => string.Join(",", g.Where(x => x != "x"))).OrderBy(x => x).ToList();
            List<string> actual = db.Table<GroupConcatItemRow>().GroupBy(r => r.Kind, r => r.Name).Select(g => string.Join(",", g.Where(x => x != "x"))).ToList().OrderBy(x => x).ToList();

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void StringJoinKeySeparatorMatchesLinqToObjects()
    {
        (TestDatabase db, List<GroupConcatItemRow> mem) = Seed();
        using (db)
        {
            List<string> expected = mem.GroupBy(r => r.Kind).Select(g => string.Join(g.Key, g.Select(x => x.Name))).OrderBy(x => x).ToList();
            List<string> actual = db.Table<GroupConcatItemRow>().GroupBy(r => r.Kind).Select(g => string.Join(g.Key, g.Select(x => x.Name))).ToList().OrderBy(x => x).ToList();

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void StringConcatTwoValuesMatchesLinqToObjects()
    {
        (TestDatabase db, List<GroupConcatItemRow> mem) = Seed();
        using (db)
        {
            List<string> expected = mem.GroupBy(r => r.Kind).Select(g => string.Concat(g.Key, "!")).OrderBy(x => x).ToList();
            List<string> actual = db.Table<GroupConcatItemRow>().GroupBy(r => r.Kind).Select(g => string.Concat(g.Key, "!")).ToList().OrderBy(x => x).ToList();

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void StringJoinOverOrderedGroupThrows()
    {
        (TestDatabase db, List<GroupConcatItemRow> _) = Seed();
        using (db)
        {
            Assert.Throws<NotSupportedException>(() =>
                db.Table<GroupConcatItemRow>().GroupBy(r => r.Kind).Select(g => string.Join(",", g.OrderBy(x => x.Id).Select(x => x.Name))).ToList());
        }
    }

    [Fact]
    public void StringJoinIndexedSelectorThrows()
    {
        (TestDatabase db, List<GroupConcatItemRow> _) = Seed();
        using (db)
        {
            Assert.Throws<NotSupportedException>(() =>
                db.Table<GroupConcatItemRow>().GroupBy(r => r.Kind).Select(g => string.Join(",", g.Select((x, i) => x.Name))).ToList());
        }
    }

    [Fact]
    public void StringJoinClientSelectorThrows()
    {
        (TestDatabase db, List<GroupConcatItemRow> _) = Seed();
        using (db)
        {
            Assert.Throws<NotSupportedException>(() =>
                db.Table<GroupConcatItemRow>().GroupBy(r => r.Kind).Select(g => string.Join(",", g.Select(x => Shift(x.Name)))).ToList());
        }
    }

    [Fact]
    public void StringJoinClientFilterThrows()
    {
        (TestDatabase db, List<GroupConcatItemRow> _) = Seed();
        using (db)
        {
            Assert.Throws<NotSupportedException>(() =>
                db.Table<GroupConcatItemRow>().GroupBy(r => r.Kind).Select(g => string.Join(",", g.Where(x => Shift(x.Name) == "q").Select(x => x.Name))).ToList());
        }
    }

    [Fact]
    public void StringConcatOverRowGroupThrows()
    {
        (TestDatabase db, List<GroupConcatItemRow> _) = Seed();
        using (db)
        {
            Assert.Throws<NotSupportedException>(() =>
                db.Table<GroupConcatItemRow>().GroupBy(r => r.Kind).Select(g => string.Concat(g)).ToList());
        }
    }

    [Fact]
    public void StringJoinNonConstantCharSeparatorThrows()
    {
        (TestDatabase db, List<GroupConcatItemRow> _) = Seed();
        using (db)
        {
            Assert.Throws<NotSupportedException>(() =>
                db.Table<GroupConcatItemRow>().GroupBy(r => r.Kind).Select(g => string.Join(g.Key[0], g.Select(x => x.Name))).ToList());
        }
    }

    [Fact]
    public void StringJoinClientSeparatorThrows()
    {
        (TestDatabase db, List<GroupConcatItemRow> _) = Seed();
        using (db)
        {
            Assert.Throws<NotSupportedException>(() =>
                db.Table<GroupConcatItemRow>().GroupBy(r => r.Kind).Select(g => string.Join(Shift(g.Key), g.Select(x => x.Name))).ToList());
        }
    }

    [Fact]
    public void StringJoinRangeOverloadRunsInMemory()
    {
        (TestDatabase db, List<GroupConcatItemRow> mem) = Seed();
        using (db)
        {
            string[] parts = ["a", "b", "c"];
            List<string> expected = mem.GroupBy(r => r.Kind).Select(g => string.Join(",", parts, 0, 2)).OrderBy(x => x).ToList();
            List<string> actual = db.Table<GroupConcatItemRow>().GroupBy(r => r.Kind).Select(g => string.Join(",", parts, 0, 2)).ToList().OrderBy(x => x).ToList();

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void InterpolatedStringJoinOverCapturedListMatchesLinqToObjects()
    {
        (TestDatabase db, List<GroupConcatItemRow> mem) = Seed();
        using (db)
        {
            string[] tags = ["t1", "t2"];
            List<string> expected = mem.GroupBy(r => r.Kind).Select(g => $"{g.Count()}:{string.Join("|", tags)}").OrderBy(x => x).ToList();
            List<string> actual = db.Table<GroupConcatItemRow>().GroupBy(r => r.Kind).Select(g => $"{g.Count()}:{string.Join("|", tags)}").ToList().OrderBy(x => x).ToList();

            Assert.Equal(expected, actual);
        }
    }
}
