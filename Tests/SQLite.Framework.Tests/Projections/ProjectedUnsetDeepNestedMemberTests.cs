using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("DeepUnsetRows")]
public class DeepUnsetRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

public class DeepUnsetLeaf
{
    public int X { get; set; }

    public int Y { get; set; }
}

public class DeepUnsetMiddle
{
    public DeepUnsetLeaf Inner { get; set; } = new();

    public int Own { get; set; }
}

public class DeepUnsetPositional
{
    public DeepUnsetPositional(int x)
    {
        X = x;
    }

    public int X { get; }

    public int Other { get; set; }
}

public class ProjectedUnsetDeepNestedMemberTests
{
    [Fact]
    public void UnsetMemberOnAPositionallyBuiltObjectIsNotResolvedAsAConstructedDefault()
    {
        using TestDatabase db = Setup(nameof(UnsetMemberOnAPositionallyBuiltObjectIsNotResolvedAsAConstructedDefault));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { Built = new DeepUnsetMiddle { Own = r.A }, Positional = new DeepUnsetPositional(r.A) })
            .Select(x => x.Positional.Other)
            .ToList();

        List<int> actual = db.Table<DeepUnsetRow>().OrderBy(r => r.Id)
            .Select(r => new { Built = new DeepUnsetMiddle { Own = r.A }, Positional = new DeepUnsetPositional(r.A) })
            .Select(x => x.Positional.Other)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UnsetMemberTwoLevelsInsideAConstructedObjectReadsAsDefault()
    {
        using TestDatabase db = Setup(nameof(UnsetMemberTwoLevelsInsideAConstructedObjectReadsAsDefault));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Outer = new DeepUnsetMiddle { Inner = new DeepUnsetLeaf { X = r.A } } })
            .Select(x => x.Outer.Inner.Y)
            .ToList();

        List<int> actual = db.Table<DeepUnsetRow>().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Outer = new DeepUnsetMiddle { Inner = new DeepUnsetLeaf { X = r.A } } })
            .Select(x => x.Outer.Inner.Y)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SetMemberTwoLevelsInsideAConstructedObjectReadsItsValue()
    {
        using TestDatabase db = Setup(nameof(SetMemberTwoLevelsInsideAConstructedObjectReadsItsValue));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Outer = new DeepUnsetMiddle { Inner = new DeepUnsetLeaf { X = r.A } } })
            .Select(x => x.Outer.Inner.X)
            .ToList();

        List<int> actual = db.Table<DeepUnsetRow>().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Outer = new DeepUnsetMiddle { Inner = new DeepUnsetLeaf { X = r.A } } })
            .Select(x => x.Outer.Inner.X)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UnsetMemberOnTheMiddleObjectReadsAsDefault()
    {
        using TestDatabase db = Setup(nameof(UnsetMemberOnTheMiddleObjectReadsAsDefault));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Outer = new DeepUnsetMiddle { Inner = new DeepUnsetLeaf { X = r.A } } })
            .Select(x => x.Outer.Own)
            .ToList();

        List<int> actual = db.Table<DeepUnsetRow>().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Outer = new DeepUnsetMiddle { Inner = new DeepUnsetLeaf { X = r.A } } })
            .Select(x => x.Outer.Own)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UnsetMemberUnderAnUnsetNestedObjectReadsAsDefault()
    {
        using TestDatabase db = Setup(nameof(UnsetMemberUnderAnUnsetNestedObjectReadsAsDefault));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Outer = new DeepUnsetMiddle { Own = r.A } })
            .Select(x => x.Outer.Inner.Y)
            .ToList();

        List<int> actual = db.Table<DeepUnsetRow>().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Outer = new DeepUnsetMiddle { Own = r.A } })
            .Select(x => x.Outer.Inner.Y)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<DeepUnsetRow> Rows()
    {
        return
        [
            new DeepUnsetRow { Id = 1, A = 10 },
            new DeepUnsetRow { Id = 2, A = 20 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<DeepUnsetRow>().Schema.CreateTable();
        db.Table<DeepUnsetRow>().AddRange(Rows());
        return db;
    }
}
