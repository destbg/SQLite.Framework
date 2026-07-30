using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25aConcatRows")]
public class H25aConcatRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

public class H25aConcatInner
{
    public int V { get; set; }

    public override bool Equals(object? obj)
    {
        return obj is H25aConcatInner other && other.V == V;
    }

    public override int GetHashCode()
    {
        return V;
    }
}

public class ConcatWithConstructedProjectionOperandTests
{
    [Fact]
    public void DistinctOnTheFirstConcatOperandKeepsTheSecondOperandDuplicates()
    {
        using TestDatabase db = Setup(nameof(DistinctOnTheFirstConcatOperandKeepsTheSecondOperandDuplicates));

        List<int> expected = Rows().Where(r => r.Id <= 2)
            .Select(r => new { Inner = new H25aConcatInner { V = r.A } })
            .Distinct()
            .Concat(Rows().Where(r => r.Id >= 3)
                .Select(r => new { Inner = new H25aConcatInner { V = r.A } }))
            .Select(x => x.Inner.V)
            .OrderBy(v => v)
            .ToList();

        AssertValuesOrRefusal(expected, () => db.Table<H25aConcatRow>().Where(r => r.Id <= 2)
            .Select(r => new { Inner = new H25aConcatInner { V = r.A } })
            .Distinct()
            .Concat(db.Table<H25aConcatRow>().Where(r => r.Id >= 3)
                .Select(r => new { Inner = new H25aConcatInner { V = r.A } }))
            .ToList()
            .Select(x => x.Inner.V)
            .OrderBy(v => v)
            .ToList());
    }

    [Fact]
    public void TakeOnTheSecondConcatOperandLimitsThatOperand()
    {
        using TestDatabase db = Setup(nameof(TakeOnTheSecondConcatOperandLimitsThatOperand));

        int expected = Rows().Where(r => r.Id == 1)
            .Select(r => new { Inner = new H25aConcatInner { V = r.A } })
            .Concat(Rows()
                .Select(r => new { Inner = new H25aConcatInner { V = r.A } })
                .Distinct()
                .Take(2))
            .Count();

        AssertValueOrRefusal(expected, () => db.Table<H25aConcatRow>().Where(r => r.Id == 1)
            .Select(r => new { Inner = new H25aConcatInner { V = r.A } })
            .Concat(db.Table<H25aConcatRow>()
                .Select(r => new { Inner = new H25aConcatInner { V = r.A } })
                .Distinct()
                .Take(2))
            .ToList()
            .Count);
    }

    private static void AssertValuesOrRefusal<T>(List<T> expected, Func<List<T>> run)
    {
        List<T> actual;
        try
        {
            actual = run();
        }
        catch (NotSupportedException)
        {
            return;
        }

        Assert.Equal(expected, actual);
    }

    private static void AssertValueOrRefusal<T>(T expected, Func<T> run)
    {
        T actual;
        try
        {
            actual = run();
        }
        catch (NotSupportedException)
        {
            return;
        }

        Assert.Equal(expected, actual);
    }

    private static List<H25aConcatRow> Rows()
    {
        return
        [
            new H25aConcatRow { Id = 1, A = 10 },
            new H25aConcatRow { Id = 2, A = 20 },
            new H25aConcatRow { Id = 3, A = 10 },
            new H25aConcatRow { Id = 4, A = 20 },
            new H25aConcatRow { Id = 5, A = 30 },
            new H25aConcatRow { Id = 6, A = 40 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H25aConcatRow>().Schema.CreateTable();
        db.Table<H25aConcatRow>().AddRange(Rows());
        return db;
    }
}
