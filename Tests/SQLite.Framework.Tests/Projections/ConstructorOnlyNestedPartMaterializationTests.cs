using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25bCtorOnlyRows")]
public class H25bCtorOnlyRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

public class H25bCtorOnlyPart
{
    public H25bCtorOnlyPart(int amount)
    {
        Amount = amount;
    }

    public int Amount { get; }
}

public class H25bCtorOnlyHolder
{
    public int Id { get; set; }

    public H25bCtorOnlyPart? Part { get; set; }
}

public class H25bCtorOnlyLone
{
    public H25bCtorOnlyPart? Part { get; set; }
}

public class ConstructorOnlyNestedPartMaterializationTests
{
    [Fact]
    public void ANestedPartWithoutASettablePropertyIsBuiltFromItsColumns()
    {
        using TestDatabase db = Setup(nameof(ANestedPartWithoutASettablePropertyIsBuiltFromItsColumns));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new H25bCtorOnlyHolder { Id = r.Id, Part = new H25bCtorOnlyPart(r.A) })
            .Select(h => h.Part?.Amount ?? -1)
            .ToList();

        Assert.Equal(new List<int> { 3, 7 }, expected);

        List<int> actual = db.Table<H25bCtorOnlyRow>().OrderBy(r => r.Id)
            .Select(r => new H25bCtorOnlyHolder { Id = r.Id, Part = new H25bCtorOnlyPart(r.A) })
            .AsEnumerable()
            .Select(h => h.Part?.Amount ?? -1)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AWholeRowCarriedThroughTwoWrapsKeepsItsConstructorOnlyPart()
    {
        using TestDatabase db = Setup(nameof(AWholeRowCarriedThroughTwoWrapsKeepsItsConstructorOnlyPart));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new H25bCtorOnlyLone { Part = new H25bCtorOnlyPart(r.A) })
            .Select(p => new { X = p })
            .Select(y => new { W = y.X })
            .Select(x => x.W?.Part?.Amount ?? -1)
            .ToList();

        Assert.Equal(new List<int> { 3, 7 }, expected);

        List<int> actual = db.Table<H25bCtorOnlyRow>().OrderBy(r => r.Id)
            .Select(r => new H25bCtorOnlyLone { Part = new H25bCtorOnlyPart(r.A) })
            .Select(p => new { X = p })
            .Select(y => new { W = y.X })
            .AsEnumerable()
            .Select(x => x.W?.Part?.Amount ?? -1)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H25bCtorOnlyRow> Rows()
    {
        return
        [
            new H25bCtorOnlyRow { Id = 1, A = 3 },
            new H25bCtorOnlyRow { Id = 2, A = 7 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H25bCtorOnlyRow>().Schema.CreateTable();
        db.Table<H25bCtorOnlyRow>().AddRange(Rows());
        return db;
    }
}
