using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum H26pShipmentState
{
    Draft = 0,
    Active = 1,
    Closed = 2
}

[Table("H26pShipmentComputedRows")]
public class H26pShipmentComputedRow
{
    [Key]
    public int Id { get; set; }

    public H26pShipmentState State { get; set; }

    public string StateName { get; set; } = "";
}

[Table("H26pShipmentCheckRows")]
public class H26pShipmentCheckRow
{
    [Key]
    public int Id { get; set; }

    public H26pShipmentState State { get; set; }
}

[Table("H26pShipmentIndexRows")]
public class H26pShipmentIndexRow
{
    [Key]
    public int Id { get; set; }

    public H26pShipmentState State { get; set; }

    public string Code { get; set; } = "";
}

public class EnumNameInDeclarationSiteExpressionTests
{
    [Fact]
    public void AComputedColumnBuiltFromAnEnumNameMatchesLinq()
    {
        using ModelTestDatabase db = new(
            mb => mb.Entity<H26pShipmentComputedRow>().Computed(r => r.StateName, r => r.State.ToString()),
            nameof(AComputedColumnBuiltFromAnEnumNameMatchesLinq));
        db.Table<H26pShipmentComputedRow>().Schema.CreateTable();

        List<H26pShipmentComputedRow> rows = ComputedRows();
        db.Table<H26pShipmentComputedRow>().AddRange(rows);

        List<string> expected = rows.OrderBy(r => r.Id).Select(r => r.State.ToString()).ToList();
        List<string> actual = db.Table<H26pShipmentComputedRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.StateName)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ACheckConstraintOverAnEnumNameAcceptsTheSameRowsAsLinq()
    {
        using ModelTestDatabase db = new(
            mb => mb.Entity<H26pShipmentCheckRow>().Check(r => r.State.ToString() != "Closed"),
            nameof(ACheckConstraintOverAnEnumNameAcceptsTheSameRowsAsLinq));
        db.Table<H26pShipmentCheckRow>().Schema.CreateTable();

        List<H26pShipmentCheckRow> accepted = CheckRows().Where(r => r.State.ToString() != "Closed").ToList();
        List<H26pShipmentCheckRow> rejected = CheckRows().Where(r => r.State.ToString() == "Closed").ToList();

        db.Table<H26pShipmentCheckRow>().AddRange(accepted);
        foreach (H26pShipmentCheckRow row in rejected)
        {
            Assert.ThrowsAny<Exception>(() => db.Table<H26pShipmentCheckRow>().Add(row));
        }

        List<int> expected = accepted.OrderBy(r => r.Id).Select(r => r.Id).ToList();
        List<int> actual = db.Table<H26pShipmentCheckRow>().OrderBy(r => r.Id).Select(r => r.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AUniqueIndexFilteredByAnEnumNameCoversTheSameRowsAsLinq()
    {
        using ModelTestDatabase db = new(
            mb => mb.Entity<H26pShipmentIndexRow>().Index(
                r => r.Code,
                name: "h26p_shipment_state_name",
                unique: true,
                filter: r => r.State.ToString() == "Active"),
            nameof(AUniqueIndexFilteredByAnEnumNameCoversTheSameRowsAsLinq));
        db.Table<H26pShipmentIndexRow>().Schema.CreateTable();

        List<H26pShipmentIndexRow> candidates = IndexRows();
        List<H26pShipmentIndexRow> accepted = [];
        foreach (H26pShipmentIndexRow row in candidates)
        {
            bool clashes = row.State.ToString() == "Active"
                && accepted.Any(a => a.State.ToString() == "Active" && a.Code == row.Code);
            if (clashes)
            {
                Assert.ThrowsAny<Exception>(() => db.Table<H26pShipmentIndexRow>().Add(row));
                continue;
            }

            accepted.Add(row);
            db.Table<H26pShipmentIndexRow>().Add(row);
        }

        List<int> expected = accepted.OrderBy(r => r.Id).Select(r => r.Id).ToList();
        List<int> actual = db.Table<H26pShipmentIndexRow>().OrderBy(r => r.Id).Select(r => r.Id).ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H26pShipmentComputedRow> ComputedRows()
    {
        return
        [
            new H26pShipmentComputedRow { Id = 1, State = H26pShipmentState.Draft },
            new H26pShipmentComputedRow { Id = 2, State = H26pShipmentState.Active },
            new H26pShipmentComputedRow { Id = 3, State = H26pShipmentState.Closed }
        ];
    }

    private static List<H26pShipmentCheckRow> CheckRows()
    {
        return
        [
            new H26pShipmentCheckRow { Id = 1, State = H26pShipmentState.Draft },
            new H26pShipmentCheckRow { Id = 2, State = H26pShipmentState.Active },
            new H26pShipmentCheckRow { Id = 3, State = H26pShipmentState.Closed }
        ];
    }

    private static List<H26pShipmentIndexRow> IndexRows()
    {
        return
        [
            new H26pShipmentIndexRow { Id = 1, State = H26pShipmentState.Active, Code = "x" },
            new H26pShipmentIndexRow { Id = 2, State = H26pShipmentState.Draft, Code = "x" },
            new H26pShipmentIndexRow { Id = 3, State = H26pShipmentState.Active, Code = "x" }
        ];
    }
}
