using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23oSetOperationRows")]
public class H23oSetOperationRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public static class H23oSetOperationFns
{
    public static string Decorate(string value)
    {
        return "[" + value + "]";
    }
}

public class SetOperationOverInMemoryProjectionTests
{
    [Fact]
    public void ConcatWithAnInMemoryFirstOperandReportsItCannotRun()
    {
        using TestDatabase db = Setup(nameof(ConcatWithAnInMemoryFirstOperandReportsItCannotRun));

        NotSupportedException error = Assert.Throws<NotSupportedException>(() => db.Table<H23oSetOperationRow>()
            .Select(r => H23oSetOperationFns.Decorate(r.Name))
            .Concat(db.Table<H23oSetOperationRow>().Select(r => r.Name))
            .ToList());

        Assert.Contains("projection that runs in memory", error.Message);
    }

    [Fact]
    public void ConcatWithAnInMemorySecondOperandReportsItCannotRun()
    {
        using TestDatabase db = Setup(nameof(ConcatWithAnInMemorySecondOperandReportsItCannotRun));

        NotSupportedException error = Assert.Throws<NotSupportedException>(() => db.Table<H23oSetOperationRow>()
            .Select(r => r.Name)
            .Concat(db.Table<H23oSetOperationRow>().Select(r => H23oSetOperationFns.Decorate(r.Name)))
            .ToList());

        Assert.Contains("projection runs in memory", error.Message);
    }

    private static List<H23oSetOperationRow> Rows()
    {
        return
        [
            new H23oSetOperationRow { Id = 1, Name = "alpha" },
            new H23oSetOperationRow { Id = 2, Name = "beta" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H23oSetOperationRow>().Schema.CreateTable();
        db.Table<H23oSetOperationRow>().AddRange(Rows());
        return db;
    }
}
