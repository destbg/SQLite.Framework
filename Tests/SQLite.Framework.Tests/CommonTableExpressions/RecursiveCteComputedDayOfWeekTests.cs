using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21nDowWalks")]
public class H21nDowWalk
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }
}

public class H21nDowWalkStep
{
    public int Id { get; set; }

    public DateTime When { get; set; }

    public DayOfWeek Dow { get; set; }
}

public class RecursiveCteComputedDayOfWeekTests
{
    private static List<H21nDowWalk> Rows()
    {
        return
        [
            new H21nDowWalk { Id = 1, When = new DateTime(2024, 1, 1) },
            new H21nDowWalk { Id = 2, When = new DateTime(2024, 1, 2) },
            new H21nDowWalk { Id = 3, When = new DateTime(2024, 1, 7) },
            new H21nDowWalk { Id = 4, When = new DateTime(2024, 1, 8) },
        ];
    }

    private static TestDatabase Setup(EnumStorageMode mode)
    {
        TestDatabase db = new(b => b.UseEnumStorage(mode));
        db.Table<H21nDowWalk>().Schema.CreateTable();
        db.Table<H21nDowWalk>().AddRange(Rows());
        return db;
    }

    private static List<int> ExpectedIds(List<H21nDowWalk> rows)
    {
        List<H21nDowWalkStep> reached = [];
        List<H21nDowWalkStep> frontier = rows
            .Select(w => new H21nDowWalkStep { Id = w.Id, When = w.When, Dow = w.When.DayOfWeek })
            .ToList();

        while (frontier.Count > 0)
        {
            reached.AddRange(frontier);
            frontier = frontier
                .Where(s => s.Dow != DayOfWeek.Sunday && s.Id < 100)
                .Select(s => new H21nDowWalkStep { Id = s.Id + 100, When = s.When, Dow = s.When.DayOfWeek })
                .ToList();
        }

        return reached.Select(s => s.Id).OrderBy(i => i).ToList();
    }

    [Fact]
    public void RecursiveArmDayOfWeekFilterTextStorageMatchesLinq()
    {
        using TestDatabase db = Setup(EnumStorageMode.Text);

        List<int> expected = ExpectedIds(Rows());

        SQLiteCte<H21nDowWalkStep> cte = db.WithRecursive<H21nDowWalkStep>(self =>
            db.Table<H21nDowWalk>()
                .Select(w => new H21nDowWalkStep { Id = w.Id, When = w.When, Dow = w.When.DayOfWeek })
                .Concat(from s in self
                        where s.Dow != DayOfWeek.Sunday && s.Id < 100
                        select new H21nDowWalkStep { Id = s.Id + 100, When = s.When, Dow = s.When.DayOfWeek }));

        List<int> actual = cte.Select(s => s.Id).ToList().OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RecursiveArmDayOfWeekFilterIntegerStorageMatchesLinq()
    {
        using TestDatabase db = Setup(EnumStorageMode.Integer);

        List<int> expected = ExpectedIds(Rows());

        SQLiteCte<H21nDowWalkStep> cte = db.WithRecursive<H21nDowWalkStep>(self =>
            db.Table<H21nDowWalk>()
                .Select(w => new H21nDowWalkStep { Id = w.Id, When = w.When, Dow = w.When.DayOfWeek })
                .Concat(from s in self
                        where s.Dow != DayOfWeek.Sunday && s.Id < 100
                        select new H21nDowWalkStep { Id = s.Id + 100, When = s.When, Dow = s.When.DayOfWeek }));

        List<int> actual = cte.Select(s => s.Id).ToList().OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RecursiveArmDayOfWeekEqualityTextStorageMatchesLinq()
    {
        using TestDatabase db = Setup(EnumStorageMode.Text);

        List<H21nDowWalk> local = Rows();
        List<H21nDowWalkStep> reached = [];
        List<H21nDowWalkStep> frontier = local
            .Select(w => new H21nDowWalkStep { Id = w.Id, When = w.When, Dow = w.When.DayOfWeek })
            .ToList();

        while (frontier.Count > 0)
        {
            reached.AddRange(frontier);
            frontier = frontier
                .Where(s => s.Dow == DayOfWeek.Sunday && s.Id < 100)
                .Select(s => new H21nDowWalkStep { Id = s.Id + 100, When = s.When, Dow = s.When.DayOfWeek })
                .ToList();
        }

        List<int> expected = reached.Select(s => s.Id).OrderBy(i => i).ToList();

        SQLiteCte<H21nDowWalkStep> cte = db.WithRecursive<H21nDowWalkStep>(self =>
            db.Table<H21nDowWalk>()
                .Select(w => new H21nDowWalkStep { Id = w.Id, When = w.When, Dow = w.When.DayOfWeek })
                .Concat(from s in self
                        where s.Dow == DayOfWeek.Sunday && s.Id < 100
                        select new H21nDowWalkStep { Id = s.Id + 100, When = s.When, Dow = s.When.DayOfWeek }));

        List<int> actual = cte.Select(s => s.Id).ToList().OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }
}
