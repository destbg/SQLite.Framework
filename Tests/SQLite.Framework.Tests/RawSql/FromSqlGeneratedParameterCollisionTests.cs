using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("RawParamBook")]
public class RawParamBookRow
{
    [Key]
    public int Id { get; set; }

    public string Title { get; set; } = "";

    public int Pages { get; set; }
}

public class FromSqlGeneratedParameterCollisionTests
{
    private static (TestDatabase db, List<RawParamBookRow> mem) Seed()
    {
        TestDatabase db = new();
        db.Table<RawParamBookRow>().Schema.CreateTable();
        List<RawParamBookRow> mem =
        [
            new() { Id = 1, Title = "a", Pages = 10 },
            new() { Id = 2, Title = "b", Pages = 20 },
            new() { Id = 3, Title = "c", Pages = 30 },
        ];
        foreach (RawParamBookRow row in mem)
        {
            db.Table<RawParamBookRow>().Add(row);
        }

        return (db, mem);
    }

    [Fact]
    public void ConcatOperandUserParameterKeepsOuterWhereValue()
    {
        (TestDatabase db, List<RawParamBookRow> mem) = Seed();
        using (db)
        {
            List<int> expected = mem.Where(b => b.Pages > 25).Select(b => b.Id)
                .Concat(mem.Where(b => b.Id == 1).Select(b => b.Id))
                .OrderBy(i => i)
                .ToList();

            List<int> actual = db.Table<RawParamBookRow>().Where(b => b.Pages > 25).Select(b => b.Id)
                .Concat(db.FromSql<RawParamBookRow>(
                    "SELECT * FROM \"RawParamBook\" WHERE \"Id\" = @p0",
                    new SQLiteParameter { Name = "@p0", Value = 1 }).Select(b => b.Id))
                .OrderBy(i => i)
                .ToList();

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void JoinInnerUserParameterKeepsOuterWhereValue()
    {
        (TestDatabase db, List<RawParamBookRow> mem) = Seed();
        using (db)
        {
            List<int> expected = mem.Where(b => b.Pages < 25)
                .Join(mem.Where(a => a.Id == 2), b => b.Id, a => a.Id, (b, a) => b.Id)
                .OrderBy(i => i)
                .ToList();

            List<int> actual = db.Table<RawParamBookRow>().Where(b => b.Pages < 25)
                .Join(db.FromSql<RawParamBookRow>(
                        "SELECT * FROM \"RawParamBook\" WHERE \"Id\" = @p0",
                        new SQLiteParameter { Name = "@p0", Value = 2 }),
                    b => b.Id, a => a.Id, (b, a) => b.Id)
                .ToList()
                .OrderBy(i => i)
                .ToList();

            Assert.Equal(expected, actual);
        }
    }
}
