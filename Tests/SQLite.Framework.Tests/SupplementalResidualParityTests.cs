using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ResidualRow")]
public class ResidualRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int IntCode { get; set; }
}

[Table("ResidualMoney")]
public class ResidualMoneyRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public decimal Amount { get; set; }
}

public class SupplementalResidualParityTests
{
    private static (TestDatabase db, List<ResidualRow> mem) Seed()
    {
        TestDatabase db = new();
        db.Table<ResidualRow>().Schema.CreateTable();
        List<ResidualRow> mem =
        [
            new() { Name = "a", IntCode = 3 },
            new() { Name = "b", IntCode = 7 },
            new() { Name = "a", IntCode = 11 },
            new() { Name = "c", IntCode = 2 },
        ];
        foreach (ResidualRow row in mem)
        {
            db.Table<ResidualRow>().Add(row);
        }

        return (db, mem);
    }

    [Fact]
    public void CapturedDelegateProjectionUsesEachRowsValues()
    {
        (TestDatabase db, List<ResidualRow> mem) = Seed();
        using (db)
        {
            Func<int, int, int> combine = (a, b) => a * 100 + b;
            List<int> expected = mem.OrderBy(r => r.Id).Select(r => combine(r.IntCode, r.Id)).ToList();
            List<int> actual = db.Table<ResidualRow>().OrderBy(r => r.Id).Select(r => combine(r.IntCode, r.Id)).ToList();

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void CapturedDelegateInWhereThrows()
    {
        (TestDatabase db, List<ResidualRow> _) = Seed();
        using (db)
        {
            Func<int, int, int> combine = (a, b) => a * 100 + b;

            Assert.Throws<NotSupportedException>(() =>
                db.Table<ResidualRow>().Where(r => combine(r.IntCode, r.Id) == 301).ToList());
        }
    }

    [Fact]
    public void FirstOrDefaultConstantDefaultMatchesLinqToObjects()
    {
        (TestDatabase db, List<ResidualRow> mem) = Seed();
        using (db)
        {
            string expected = mem.Where(r => r.IntCode > 999).Select(r => r.Name).FirstOrDefault("none")!;
            string actual = db.Table<ResidualRow>().Where(r => r.IntCode > 999).Select(r => r.Name).FirstOrDefault("none")!;

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void CorrelatedFirstOrDefaultConstantDefaultReadsTypeDefault()
    {
        (TestDatabase db, List<ResidualRow> mem) = Seed();
        using (db)
        {
            List<string> memory = mem.Select(r => mem.Where(x => x.IntCode > 999).Select(x => x.Name).FirstOrDefault("none")!).ToList();
            Assert.Equal(["none", "none", "none", "none"], memory);

            List<string?> actual = db.Table<ResidualRow>().Select(r => db.Table<ResidualRow>().Where(x => x.IntCode > 999).Select(x => x.Name).FirstOrDefault("none")).ToList();

            Assert.Equal([null, null, null, null], actual);
        }
    }

    [Fact]
    public void StringConcatRowArgumentMatchesLinqToObjects()
    {
        (TestDatabase db, List<ResidualRow> mem) = Seed();
        using (db)
        {
            List<string> expected = mem.OrderBy(r => r.Id).Select(r => string.Concat(r, "!")).ToList();
            List<string> actual = db.Table<ResidualRow>().OrderBy(r => r.Id).Select(r => string.Concat(r, "!")).ToList();

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void CorrelatedFirstOrDefaultRowDefaultThrows()
    {
        (TestDatabase db, List<ResidualRow> _) = Seed();
        using (db)
        {
            Assert.Throws<NotSupportedException>(() =>
                db.Table<ResidualRow>().Select(r => db.Table<ResidualRow>().Where(x => x.IntCode > 999).Select(x => x.Name).FirstOrDefault(r.Name)).ToList());
        }
    }

    [Fact]
    public void TextDecimalSumMatchesLinqToObjects()
    {
        using TestDatabase db = new(o => o.UseDecimalStorage(DecimalStorageMode.Text));
        db.Table<ResidualMoneyRow>().Schema.CreateTable();
        List<ResidualMoneyRow> mem =
        [
            new() { Amount = 0.1m },
            new() { Amount = 0.2m },
            new() { Amount = 0.3m },
        ];
        foreach (ResidualMoneyRow row in mem)
        {
            db.Table<ResidualMoneyRow>().Add(row);
        }

        decimal expected = mem.Sum(r => r.Amount);
        decimal actual = db.Table<ResidualMoneyRow>().Sum(r => r.Amount);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TextDecimalAverageRoundsThroughDouble()
    {
        using TestDatabase db = new(o => o.UseDecimalStorage(DecimalStorageMode.Text));
        db.Table<ResidualMoneyRow>().Schema.CreateTable();
        List<ResidualMoneyRow> mem =
        [
            new() { Amount = 0.1m },
            new() { Amount = 0.2m },
            new() { Amount = 0.4m },
        ];
        foreach (ResidualMoneyRow row in mem)
        {
            db.Table<ResidualMoneyRow>().Add(row);
        }

        decimal exact = mem.Average(r => r.Amount);
        decimal expected = (decimal)mem.Average(r => (double)r.Amount);
        Assert.NotEqual(exact, expected);

        decimal actual = db.Table<ResidualMoneyRow>().Average(r => r.Amount);

        Assert.Equal(expected, actual);
    }
}
