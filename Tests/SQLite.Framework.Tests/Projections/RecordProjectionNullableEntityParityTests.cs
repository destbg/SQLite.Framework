using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("rpn_team")]
public class RpnTeam
{
    [Key]
    public int Id { get; set; }

    public string Title { get; set; } = "";
}

[Table("rpn_coach")]
public class RpnCoach
{
    [Key]
    public int Id { get; set; }

    public int TeamId { get; set; }

    public string Alias { get; set; } = "";
}

public record RpnPair(string Title)
{
    public RpnCoach? Coach { get; set; }
}

public class RecordProjectionNullableEntityParityTests
{
    private static List<RpnTeam> Teams()
    {
        return
        [
            new RpnTeam { Id = 1, Title = "Alpha" },
            new RpnTeam { Id = 2, Title = "Beta" },
        ];
    }

    private static List<RpnCoach> Coaches()
    {
        return
        [
            new RpnCoach { Id = 5, TeamId = 1, Alias = "Sam" },
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<RpnTeam>().Schema.CreateTable();
        db.Table<RpnTeam>().AddRange(Teams());
        db.Table<RpnCoach>().Schema.CreateTable();
        db.Table<RpnCoach>().AddRange(Coaches());
        return db;
    }

    [Fact]
    public void RecordConstructorWithNullableEntityMember()
    {
        using TestDatabase db = Seed();
        List<RpnTeam> ts = Teams();
        List<RpnCoach> cs = Coaches();

        List<(string, bool, string)> expected = (from t in ts
                join c in cs on t.Id equals c.TeamId into gc
                from c in gc.DefaultIfEmpty()
                select new RpnPair(t.Title) { Coach = c })
            .OrderBy(x => x.Title)
            .Select(x => (x.Title, x.Coach == null, x.Coach == null ? "" : x.Coach.Alias))
            .ToList();

        List<(string, bool, string)> actual = (from t in db.Table<RpnTeam>()
                join c in db.Table<RpnCoach>() on t.Id equals c.TeamId into gc
                from c in gc.DefaultIfEmpty()
                select new RpnPair(t.Title) { Coach = c })
            .AsEnumerable()
            .OrderBy(x => x.Title)
            .Select(x => (x.Title, x.Coach == null, x.Coach == null ? "" : x.Coach.Alias))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
