using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25bGetOnlyRows")]
public class H25bGetOnlyRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }

    public int B { get; set; }
}

public class H25bGetOnlyScaled
{
    public H25bGetOnlyScaled(int value)
    {
        Value = value * 10;
    }

    public int Value { get; }
}

public class H25bGetOnlySwapped
{
    public H25bGetOnlySwapped(int first, int second)
    {
        First = second;
        Second = first;
    }

    public int First { get; }

    public int Second { get; }
}

public class H25bPartiallySet
{
    public H25bPartiallySet(int value)
    {
        Value = value;
    }

    public int Value { get; set; }

    public int Untouched { get; set; }
}

public class GetOnlyConstructorMemberValueTests
{
    [Fact]
    public void AGetOnlyMemberTheConstructorComputesReadsTheArgumentWithItsName()
    {
        using TestDatabase db = Setup(nameof(AGetOnlyMemberTheConstructorComputesReadsTheArgumentWithItsName));

        List<int> arguments = Rows().OrderBy(r => r.Id)
            .Select(r => r.A)
            .ToList();

        List<int> actual = db.Table<H25bGetOnlyRow>().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = new H25bGetOnlyScaled(r.A) })
            .Select(x => x.Part.Value)
            .ToList();

        Assert.Equal(arguments, actual);
    }

    [Fact]
    public void AGetOnlyMemberTheConstructorFillsFromTheOtherArgumentReadsTheArgumentWithItsName()
    {
        using TestDatabase db = Setup(nameof(AGetOnlyMemberTheConstructorFillsFromTheOtherArgumentReadsTheArgumentWithItsName));

        List<int> firstArguments = Rows().OrderBy(r => r.Id)
            .Select(r => r.A)
            .ToList();

        List<int> actual = db.Table<H25bGetOnlyRow>().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = new H25bGetOnlySwapped(r.A, r.B) })
            .Select(x => x.Part.First)
            .ToList();

        Assert.Equal(firstArguments, actual);
    }

    [Fact]
    public void AGetOnlyObjectMaterializedWholeRunsItsConstructor()
    {
        using TestDatabase db = Setup(nameof(AGetOnlyObjectMaterializedWholeRunsItsConstructor));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new H25bGetOnlyScaled(r.A).Value)
            .ToList();

        List<int> actual = db.Table<H25bGetOnlyRow>().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = new H25bGetOnlyScaled(r.A) })
            .AsEnumerable()
            .Select(x => x.Part.Value)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AMemberTheConstructorNeverSetsReadsItsClrDefault()
    {
        using TestDatabase db = Setup(nameof(AMemberTheConstructorNeverSetsReadsItsClrDefault));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new H25bPartiallySet(r.A).Untouched)
            .ToList();

        List<int> actual = db.Table<H25bGetOnlyRow>().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = new H25bPartiallySet(r.A) })
            .Select(x => x.Part.Untouched)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H25bGetOnlyRow> Rows()
    {
        return
        [
            new H25bGetOnlyRow { Id = 1, A = 3, B = 100 },
            new H25bGetOnlyRow { Id = 2, A = 7, B = 200 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H25bGetOnlyRow>().Schema.CreateTable();
        db.Table<H25bGetOnlyRow>().AddRange(Rows());
        return db;
    }
}
