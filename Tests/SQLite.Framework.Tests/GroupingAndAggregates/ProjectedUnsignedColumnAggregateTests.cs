using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25cCounters")]
public class H25cCounter
{
    [Key]
    public int Id { get; set; }

    public ulong Big { get; set; }
}

public class ProjectedUnsignedColumnAggregateTests
{
    [Fact]
    public void MaxOverAProjectedUnsignedColumnMatchesLinqToObjects()
    {
        using TestDatabase db = Setup(nameof(MaxOverAProjectedUnsignedColumnMatchesLinqToObjects));

        ulong expected = Rows().Select(r => r.Big).Max();
        ulong actual = db.Table<H25cCounter>().Select(r => r.Big).Max();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MinOverAProjectedUnsignedColumnMatchesLinqToObjects()
    {
        using TestDatabase db = Setup(nameof(MinOverAProjectedUnsignedColumnMatchesLinqToObjects));

        ulong expected = Rows().Select(r => r.Big).Min();
        ulong actual = db.Table<H25cCounter>().Select(r => r.Big).Min();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MaxOverAFilteredProjectedUnsignedColumnMatchesLinqToObjects()
    {
        using TestDatabase db = Setup(nameof(MaxOverAFilteredProjectedUnsignedColumnMatchesLinqToObjects));

        ulong expected = Rows().Where(r => r.Id > 0).Select(r => r.Big).Max();
        ulong actual = db.Table<H25cCounter>().Where(r => r.Id > 0).Select(r => r.Big).Max();

        Assert.Equal(expected, actual);
    }

    private static List<H25cCounter> Rows()
    {
        return
        [
            new H25cCounter { Id = 1, Big = 1 },
            new H25cCounter { Id = 2, Big = ulong.MaxValue },
            new H25cCounter { Id = 3, Big = 100 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H25cCounter>().Schema.CreateTable();
        db.Table<H25cCounter>().AddRange(Rows());
        return db;
    }
}
