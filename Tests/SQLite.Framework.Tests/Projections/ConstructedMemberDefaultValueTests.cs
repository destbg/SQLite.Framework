using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22hDefaultValueRows")]
public class H22hDefaultValueRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

public class H22hInitializedSide
{
    public int X { get; set; }

    public int Preset { get; set; } = 5;
}

public class H22hConstructorSide
{
    public H22hConstructorSide(int x)
    {
        X = x;
        Doubled = x * 2;
    }

    public int X { get; }

    public int Doubled { get; set; }
}

public class H22hOuterDto
{
    public int Id { get; set; }

    public H22hInitializedSide? N { get; set; }
}

public class ConstructedMemberDefaultValueTests
{
    [Fact]
    public void MemberWithAPropertyInitializerReadsItsInitialValue()
    {
        using TestDatabase db = Setup(nameof(MemberWithAPropertyInitializerReadsItsInitialValue));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, N = new H22hInitializedSide { X = r.A } })
            .Select(x => x.N.Preset)
            .ToList();

        List<int> actual = db.Table<H22hDefaultValueRow>().OrderBy(r => r.Id)
            .Select(r => new { r.Id, N = new H22hInitializedSide { X = r.A } })
            .Select(x => x.N.Preset)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MemberAssignedByTheObjectConstructorReadsItsValue()
    {
        using TestDatabase db = Setup(nameof(MemberAssignedByTheObjectConstructorReadsItsValue));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, N = new H22hConstructorSide(r.A) })
            .Select(x => x.N.Doubled)
            .ToList();

        List<int> actual = db.Table<H22hDefaultValueRow>().OrderBy(r => r.Id)
            .Select(r => new { r.Id, N = new H22hConstructorSide(r.A) })
            .Select(x => x.N.Doubled)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AssignedMemberOfTheSameObjectStillReadsItsColumn()
    {
        using TestDatabase db = Setup(nameof(AssignedMemberOfTheSameObjectStillReadsItsColumn));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, N = new H22hInitializedSide { X = r.A } })
            .Select(x => x.N.X)
            .ToList();

        List<int> actual = db.Table<H22hDefaultValueRow>().OrderBy(r => r.Id)
            .Select(r => new { r.Id, N = new H22hInitializedSide { X = r.A } })
            .Select(x => x.N.X)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DtoProjectedNestedMemberWithAPropertyInitializerReadsItsInitialValue()
    {
        using TestDatabase db = Setup(nameof(DtoProjectedNestedMemberWithAPropertyInitializerReadsItsInitialValue));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new H22hOuterDto { Id = r.Id, N = new H22hInitializedSide { X = r.A } })
            .Select(x => x.N!.Preset)
            .ToList();

        List<int> actual = db.Table<H22hDefaultValueRow>().OrderBy(r => r.Id)
            .Select(r => new H22hOuterDto { Id = r.Id, N = new H22hInitializedSide { X = r.A } })
            .Select(x => x.N!.Preset)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FilterOnANestedMemberWithAPropertyInitializerKeepsTheMatchingRows()
    {
        using TestDatabase db = Setup(nameof(FilterOnANestedMemberWithAPropertyInitializerKeepsTheMatchingRows));

        List<int> expected = Rows()
            .Select(r => new { r.Id, N = new H22hInitializedSide { X = r.A } })
            .Where(x => x.N.Preset > 0)
            .Select(x => x.Id)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.Table<H22hDefaultValueRow>()
            .Select(r => new { r.Id, N = new H22hInitializedSide { X = r.A } })
            .Where(x => x.N.Preset > 0)
            .Select(x => x.Id)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22hDefaultValueRow> Rows()
    {
        return
        [
            new H22hDefaultValueRow { Id = 1, A = 10 },
            new H22hDefaultValueRow { Id = 2, A = 20 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H22hDefaultValueRow>().Schema.CreateTable();
        db.Table<H22hDefaultValueRow>().AddRange(Rows());
        return db;
    }
}
