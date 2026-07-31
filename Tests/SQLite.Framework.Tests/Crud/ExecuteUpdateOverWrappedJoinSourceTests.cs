using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26uInvoiceRows")]
public class H26uInvoiceRow
{
    [Key]
    public int Id { get; set; }

    public string Label { get; set; } = string.Empty;

    public int OwnerId { get; set; }

    public int Amount { get; set; }
}

[Table("H26uOwnerRows")]
public class H26uOwnerRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class ExecuteUpdateOverWrappedJoinSourceTests
{
    [Fact]
    public void ExecuteUpdateCopiesTheJoinedValueWhenAFilterComesBeforeTheJoin()
    {
        using TestDatabase db = Setup(null, nameof(ExecuteUpdateCopiesTheJoinedValueWhenAFilterComesBeforeTheJoin));

        List<string> expected = ExpectedLabels();

        db.Table<H26uInvoiceRow>()
            .Where(i => i.Amount > 0)
            .Join(db.Table<H26uOwnerRow>(), i => i.OwnerId, o => o.Id, (i, o) => new { i, o })
            .ExecuteUpdate(s => s.Set(x => x.i.Label, x => x.o.Name));

        List<string> actual = db.Table<H26uInvoiceRow>().OrderBy(i => i.Id).Select(i => i.Label).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ExecuteUpdateCopiesTheJoinedValueWhenAQueryFilterIsRegistered()
    {
        using TestDatabase db = Setup(
            b => b.AddQueryFilter<H26uInvoiceRow>(i => i.Amount > 0),
            nameof(ExecuteUpdateCopiesTheJoinedValueWhenAQueryFilterIsRegistered));

        List<string> expected = ExpectedLabels();

        db.Table<H26uInvoiceRow>()
            .Join(db.Table<H26uOwnerRow>(), i => i.OwnerId, o => o.Id, (i, o) => new { i, o })
            .ExecuteUpdate(s => s.Set(x => x.i.Label, x => x.o.Name));

        List<string> actual = db.Table<H26uInvoiceRow>().OrderBy(i => i.Id).Select(i => i.Label).ToList();

        Assert.Equal(expected, actual);
    }

    private static List<string> ExpectedLabels()
    {
        List<H26uOwnerRow> owners = Owners();
        return Invoices()
            .Where(i => i.Amount > 0)
            .OrderBy(i => i.Id)
            .Select(i => owners.First(o => o.Id == i.OwnerId).Name)
            .ToList();
    }

    private static List<H26uInvoiceRow> Invoices()
    {
        return
        [
            new H26uInvoiceRow { Id = 1, Label = "old one", OwnerId = 1, Amount = 5 },
            new H26uInvoiceRow { Id = 2, Label = "old two", OwnerId = 2, Amount = 7 }
        ];
    }

    private static List<H26uOwnerRow> Owners()
    {
        return
        [
            new H26uOwnerRow { Id = 1, Name = "Ann" },
            new H26uOwnerRow { Id = 2, Name = "Bob" }
        ];
    }

    private static TestDatabase Setup(Action<SQLiteOptionsBuilder>? configure, string methodName)
    {
        TestDatabase db = new(configure, methodName);
        db.Table<H26uInvoiceRow>().Schema.CreateTable();
        db.Table<H26uOwnerRow>().Schema.CreateTable();
        db.Table<H26uInvoiceRow>().AddRange(Invoices());
        db.Table<H26uOwnerRow>().AddRange(Owners());
        return db;
    }
}
