using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class FluentJoinResultSelectorMaterializationTests
{
    private static List<RosterTeam> Teams()
    {
        return new List<RosterTeam>
        {
            new RosterTeam { Id = 1, Name = "red" },
            new RosterTeam { Id = 2, Name = "blue" },
            new RosterTeam { Id = 3, Name = "gold" },
        };
    }

    private static List<RosterPlayer> Players()
    {
        return new List<RosterPlayer>
        {
            new RosterPlayer { Id = 1, TeamId = 1, Alias = "ace" },
            new RosterPlayer { Id = 2, TeamId = 1, Alias = "bolt" },
            new RosterPlayer { Id = 3, TeamId = 2, Alias = "core" },
        };
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<RosterTeam>().Schema.CreateTable();
        db.Table<RosterPlayer>().Schema.CreateTable();
        db.Table<RosterTeam>().AddRange(Teams());
        db.Table<RosterPlayer>().AddRange(Players());
        return db;
    }

    [Fact]
    public void AnonymousProjectionFromJoin()
    {
        using TestDatabase db = Seed();

        List<(string, string)> oracle = new();
        foreach (RosterTeam team in Teams())
        {
            foreach (RosterPlayer player in Players())
            {
                if (player.TeamId == team.Id)
                {
                    oracle.Add((team.Name, player.Alias));
                }
            }
        }
        oracle.Sort();

        var rows = db.Table<RosterTeam>()
            .Join(db.Table<RosterPlayer>(), t => t.Id, p => p.TeamId, (t, p) => new { t.Name, p.Alias })
            .ToList();

        List<(string, string)> actual = rows.Select(r => (r.Name, r.Alias)).ToList();
        actual.Sort();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void MemberInitProjectionFromJoin()
    {
        using TestDatabase db = Seed();

        List<(string, string)> oracle = new();
        foreach (RosterTeam team in Teams())
        {
            foreach (RosterPlayer player in Players())
            {
                if (player.TeamId == team.Id)
                {
                    oracle.Add((team.Name, player.Alias));
                }
            }
        }
        oracle.Sort();

        List<RosterPairView> rows = db.Table<RosterTeam>()
            .Join(db.Table<RosterPlayer>(), t => t.Id, p => p.TeamId, (t, p) => new RosterPairView { TeamName = t.Name, PlayerAlias = p.Alias })
            .ToList();

        List<(string, string)> actual = rows.Select(r => (r.TeamName, r.PlayerAlias)).ToList();
        actual.Sort();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void AnonymousProjectionFromLeftJoin()
    {
        using TestDatabase db = Seed();

        List<(string, string?)> oracle = new();
        foreach (RosterTeam team in Teams())
        {
            bool any = false;
            foreach (RosterPlayer player in Players())
            {
                if (player.TeamId == team.Id)
                {
                    any = true;
                    oracle.Add((team.Name, player.Alias));
                }
            }
            if (!any)
            {
                oracle.Add((team.Name, null));
            }
        }
        oracle.Sort();

        var rows = db.Table<RosterTeam>()
            .LeftJoin(db.Table<RosterPlayer>(), t => t.Id, p => p.TeamId, (t, p) => new { t.Name, Alias = (string?)p.Alias })
            .ToList();

        List<(string, string?)> actual = rows.Select(r => (r.Name, r.Alias)).ToList();
        actual.Sort();

        Assert.Equal(oracle, actual);
    }
}

public class RosterTeam
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class RosterPlayer
{
    public int Id { get; set; }

    public int TeamId { get; set; }

    public string Alias { get; set; } = string.Empty;
}

public class RosterPairView
{
    public string TeamName { get; set; } = string.Empty;

    public string PlayerAlias { get; set; } = string.Empty;
}
