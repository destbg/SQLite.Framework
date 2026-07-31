using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CarryJsonDocs")]
public class CarryJsonDoc
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

[JsonSerializable(typeof(List<int>))]
internal partial class CarryJsonContext : JsonSerializerContext;

public class ProjectedJoinSourceCarryTests
{
    [Fact]
    public void AJsonMemberOfAProjectedJoinSourceKeepsItsValue()
    {
        using TestDatabase db = Setup(nameof(AJsonMemberOfAProjectedJoinSourceKeepsItsValue));

        List<int> actual = db.Table<H26aJoinLeft>()
            .Join(
                db.Table<CarryJsonDoc>().Select(d => new { K = d.Id, First = d.Numbers.First() }),
                l => l.K,
                a => a.K,
                (l, a) => a.First)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(new List<int> { 7, 9 }, actual);
    }

    [Fact]
    public void AWhollyOptionalProjectedJoinSourceKeepsItsRows()
    {
        using TestDatabase db = Setup(nameof(AWhollyOptionalProjectedJoinSourceKeepsItsRows));

        List<int> expected = Lefts()
            .Join(
                Rights()
                    .GroupJoin(Lefts(), r => r.K, l2 => l2.K, (r, ls) => new { r, ls })
                    .SelectMany(t => t.ls.DefaultIfEmpty(), (t, l2) => l2),
                l => l.K,
                x => x!.K,
                (l, x) => x!.Id)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.Table<H26aJoinLeft>()
            .Join(
                db.Table<H26aJoinRight>()
                    .GroupJoin(db.Table<H26aJoinLeft>(), r => r.K, l2 => l2.K, (r, ls) => new { r, ls })
                    .SelectMany(t => t.ls.DefaultIfEmpty(), (t, l2) => l2),
                l => l.K,
                x => x!.K,
                (l, x) => x!.Id)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AnOptionalNestedPathOfAProjectedJoinSourceKeepsItsValues()
    {
        using TestDatabase db = Setup(nameof(AnOptionalNestedPathOfAProjectedJoinSourceKeepsItsValues));

        List<int> expected = Lefts()
            .Join(
                Rights()
                    .GroupJoin(Lefts(), r => r.K, l2 => l2.K, (r, ls) => new { r, ls })
                    .SelectMany(t => t.ls.DefaultIfEmpty(), (t, l2) => new { t.r.K, L = l2 }),
                l => l.K,
                x => x.K,
                (l, x) => x.L!.Id)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.Table<H26aJoinLeft>()
            .Join(
                db.Table<H26aJoinRight>()
                    .GroupJoin(db.Table<H26aJoinLeft>(), r => r.K, l2 => l2.K, (r, ls) => new { r, ls })
                    .SelectMany(t => t.ls.DefaultIfEmpty(), (t, l2) => new { t.r.K, L = l2 }),
                l => l.K,
                x => x.K,
                (l, x) => x.L!.Id)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H26aJoinLeft> Lefts()
    {
        return
        [
            new H26aJoinLeft { Id = 1, K = 1 },
            new H26aJoinLeft { Id = 2, K = 2 }
        ];
    }

    private static List<H26aJoinRight> Rights()
    {
        return
        [
            new H26aJoinRight { Id = 1, K = 1, A = 5 },
            new H26aJoinRight { Id = 2, K = 2, A = 6 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(
            b => b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(CarryJsonContext.Default.ListInt32),
            methodName);
        db.Table<H26aJoinLeft>().Schema.CreateTable();
        db.Table<H26aJoinRight>().Schema.CreateTable();
        db.Table<CarryJsonDoc>().Schema.CreateTable();
        db.Table<H26aJoinLeft>().AddRange(Lefts());
        db.Table<H26aJoinRight>().AddRange(Rights());
        db.Table<CarryJsonDoc>().AddRange(
        [
            new CarryJsonDoc { Id = 1, Numbers = [7, 8] },
            new CarryJsonDoc { Id = 2, Numbers = [9] }
        ]);
        return db;
    }
}
