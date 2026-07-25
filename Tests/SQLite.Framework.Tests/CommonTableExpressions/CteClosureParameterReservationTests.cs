using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ClosureParam")]
public class ClosureParamRow
{
    [Key]
    public int Id { get; set; }

    public int Pages { get; set; }
}

public class CteClosureParameterReservationTests
{
    [Fact]
    public void UserParameterInsideAClosureCapturedCteKeepsItsValue()
    {
        using TestDatabase db = new();
        db.Table<ClosureParamRow>().Schema.CreateTable();
        List<ClosureParamRow> rows =
        [
            new ClosureParamRow { Id = 1, Pages = 30 },
            new ClosureParamRow { Id = 2, Pages = 50 },
            new ClosureParamRow { Id = 3, Pages = 10 },
        ];
        db.Table<ClosureParamRow>().AddRange(rows);

        SQLiteCte<ClosureParamRow> cte = db.With(() => db.FromSql<ClosureParamRow>(
            "SELECT * FROM \"ClosureParam\" WHERE \"Id\" = @p0",
            new SQLiteParameter { Name = "@p0", Value = 2 }));
        int min = 25;

        var expected = rows.Where(b => b.Pages > min)
            .Select(b => new { b.Id, Cnt = rows.Where(r => r.Id == 2).Count(c => c.Id == b.Id) })
            .OrderBy(x => x.Id).ToList();
        var actual = db.Table<ClosureParamRow>().Where(b => b.Pages > min)
            .Select(b => new { b.Id, Cnt = cte.Count(c => c.Id == b.Id) })
            .ToList().OrderBy(x => x.Id).ToList();

        Assert.Equal(expected, actual);
    }
}
