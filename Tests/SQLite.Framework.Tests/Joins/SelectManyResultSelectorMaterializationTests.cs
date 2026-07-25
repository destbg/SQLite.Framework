using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SelectManyResultSelectorMaterializationTests
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
    public void AnonymousProjectionFromUncorrelatedSelectMany()
    {
        using TestDatabase db = Seed();

        List<(string, string)> oracle = new();
        foreach (RosterTeam team in Teams())
        {
            foreach (RosterPlayer player in Players())
            {
                oracle.Add((team.Name, player.Alias));
            }
        }
        oracle.Sort();

        var rows = db.Table<RosterTeam>()
            .SelectMany(t => db.Table<RosterPlayer>(), (t, p) => new { t.Name, p.Alias })
            .ToList();

        List<(string, string)> actual = rows.Select(r => (r.Name, r.Alias)).ToList();
        actual.Sort();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ClientEvalProjectionFromUncorrelatedSelectMany()
    {
        using TestDatabase db = Seed();

        List<(string, int)> oracle = new();
        foreach (RosterTeam team in Teams())
        {
            foreach (RosterPlayer player in Players())
            {
                oracle.Add((team.Name, -1));
            }
        }
        oracle.Sort();

#if SQLITE_FRAMEWORK_SOURCE_GENERATOR && SQLITE_FRAMEWORK_TESTING
        long hitsBefore = db.SelectMaterializerHits;
        long fallbacksBefore = db.SelectCompilerFallbacks;
#endif

        var rows = db.Table<RosterTeam>()
            .SelectMany(t => db.Table<RosterPlayer>(), (t, p) => new { t.Name, Mark = CommonHelpers.ConvertString(p.Alias) })
            .ToList();

        List<(string, int)> actual = rows.Select(r => (r.Name, r.Mark)).ToList();
        actual.Sort();

        Assert.Equal(oracle, actual);
#if SQLITE_FRAMEWORK_SOURCE_GENERATOR && SQLITE_FRAMEWORK_TESTING
        Assert.True(db.SelectMaterializerHits > hitsBefore,
            "Expected a generated select materializer to handle the SelectMany result selector.");
        Assert.Equal(fallbacksBefore, db.SelectCompilerFallbacks);
#endif
    }

    [Fact]
    public void ClientEvalProjectionFromQuerySyntaxCrossJoin()
    {
        using TestDatabase db = Seed();

        List<(string, int)> oracle = new();
        foreach (RosterTeam team in Teams())
        {
            foreach (RosterPlayer player in Players())
            {
                oracle.Add((team.Name, -1));
            }
        }
        oracle.Sort();

#if SQLITE_FRAMEWORK_SOURCE_GENERATOR && SQLITE_FRAMEWORK_TESTING
        long hitsBefore = db.SelectMaterializerHits;
        long fallbacksBefore = db.SelectCompilerFallbacks;
#endif

        var rows = (
            from t in db.Table<RosterTeam>()
            from p in db.Table<RosterPlayer>()
            select new { t.Name, Mark = CommonHelpers.ConvertString(p.Alias) }
        ).ToList();

        List<(string, int)> actual = rows.Select(r => (r.Name, r.Mark)).ToList();
        actual.Sort();

        Assert.Equal(oracle, actual);
#if SQLITE_FRAMEWORK_SOURCE_GENERATOR && SQLITE_FRAMEWORK_TESTING
        Assert.True(db.SelectMaterializerHits > hitsBefore,
            "Expected a generated select materializer to handle the fused query syntax projection.");
        Assert.Equal(fallbacksBefore, db.SelectCompilerFallbacks);
#endif
    }

    [Fact]
    public void ClientEvalProjectionFromQuerySyntaxLeftJoin()
    {
        using TestDatabase db = Seed();

        List<(int, string?)> oracle = new();
        foreach (RosterTeam team in Teams())
        {
            bool any = false;
            foreach (RosterPlayer player in Players())
            {
                if (player.TeamId == team.Id)
                {
                    any = true;
                    oracle.Add((-1, player.Alias));
                }
            }
            if (!any)
            {
                oracle.Add((-1, null));
            }
        }
        oracle.Sort();

#if SQLITE_FRAMEWORK_SOURCE_GENERATOR && SQLITE_FRAMEWORK_TESTING
        long hitsBefore = db.SelectMaterializerHits;
        long fallbacksBefore = db.SelectCompilerFallbacks;
#endif

        var rows = (
            from t in db.Table<RosterTeam>()
            join p in db.Table<RosterPlayer>() on t.Id equals p.TeamId into g
            from p in g.DefaultIfEmpty()
            select new { Mark = CommonHelpers.ConvertString(t.Name), p.Alias }
        ).ToList();

        List<(int, string?)> actual = rows.Select(r => (r.Mark, (string?)r.Alias)).ToList();
        actual.Sort();

        Assert.Equal(oracle, actual);
#if SQLITE_FRAMEWORK_SOURCE_GENERATOR && SQLITE_FRAMEWORK_TESTING
        Assert.True(db.SelectMaterializerHits > hitsBefore,
            "Expected a generated select materializer to handle the flattened left join projection.");
        Assert.Equal(fallbacksBefore, db.SelectCompilerFallbacks);
#endif
    }
}
