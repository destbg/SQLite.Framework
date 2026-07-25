using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CondEntityRows")]
public class CondEntityRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }

    public int B { get; set; }

    public string Text { get; set; } = "";
}

public class KeyedBase
{
    [Key]
    public int Id { get; set; }

    public string BaseName { get; set; } = "";
}

[Table("KeyedDerivedRows")]
public class KeyedDerivedRow : KeyedBase
{
    public int Own { get; set; }
}

public class ConditionalEntityProjectionInteractionTests
{
    private static List<CondEntityRow> Rows()
    {
        return
        [
            new CondEntityRow { Id = 1, A = 10, B = 3, Text = "ten" },
            new CondEntityRow { Id = 2, A = 2, B = 8, Text = "two" },
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<CondEntityRow>().Schema.CreateTable();
        db.Table<CondEntityRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void ConditionalBetweenTwoDtoBuilds()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows().OrderBy(r => r.Id).Select(r => r.A > 5 ? new ShapeInnerDto { V = r.A } : new ShapeInnerDto { V = r.B }).Select(d => d.V).ToList();
        Assert.Equal([10, 8], expected);

        List<int> actual = db.Table<CondEntityRow>().OrderBy(r => r.Id).Select(r => r.A > 5 ? new ShapeInnerDto { V = r.A } : new ShapeInnerDto { V = r.B }).AsEnumerable().Select(d => d.V).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConditionalDtoWithComputedMember()
    {
        using TestDatabase db = Setup();

        List<int?> expected = Rows().OrderBy(r => r.Id).Select(r => r.A > 5 ? new ShapeInnerDto { V = r.A + r.B } : null).Select(d => d == null ? (int?)null : d.V).ToList();
        Assert.Equal([13, null], expected);

        List<int?> actual = db.Table<CondEntityRow>().OrderBy(r => r.Id).Select(r => r.A > 5 ? new ShapeInnerDto { V = r.A + r.B } : null).AsEnumerable().Select(d => d == null ? (int?)null : d.V).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConditionalDtoWithCompoundTest()
    {
        using TestDatabase db = Setup();

        List<int?> expected = Rows().OrderBy(r => r.Id).Select(r => r.A > 5 && r.Text == "ten" ? new ShapeInnerDto { V = r.A } : null).Select(d => d == null ? (int?)null : d.V).ToList();
        Assert.Equal([10, null], expected);

        List<int?> actual = db.Table<CondEntityRow>().OrderBy(r => r.Id).Select(r => r.A > 5 && r.Text == "ten" ? new ShapeInnerDto { V = r.A } : null).AsEnumerable().Select(d => d == null ? (int?)null : d.V).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NestedConditionalDtoOrNull()
    {
        using TestDatabase db = Setup();

        List<int?> expected = Rows().OrderBy(r => r.Id).Select(r => r.A > 5 ? new ShapeInnerDto { V = r.A } : r.B > 5 ? new ShapeInnerDto { V = r.B } : null).Select(d => d == null ? (int?)null : d.V).ToList();
        Assert.Equal([10, 8], expected);

        List<int?> actual = db.Table<CondEntityRow>().OrderBy(r => r.Id).Select(r => r.A > 5 ? new ShapeInnerDto { V = r.A } : r.B > 5 ? new ShapeInnerDto { V = r.B } : null).AsEnumerable().Select(d => d == null ? (int?)null : d.V).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConditionalAnonymousObjectOrNullInsideProjection()
    {
        using TestDatabase db = Setup();

        List<string?> expected = Rows().OrderBy(r => r.Id).Select(r => new { r.Id, D = r.A > 5 ? new ShapeMixedRecord(r.A) { Note = r.Text } : null }).Select(a => a.D == null ? null : a.D.Note + a.D.X).ToList();
        Assert.Equal(["ten10", null], expected);

        List<string?> actual = db.Table<CondEntityRow>().OrderBy(r => r.Id).Select(r => new { r.Id, D = r.A > 5 ? new ShapeMixedRecord(r.A) { Note = r.Text } : null }).AsEnumerable().Select(a => a.D == null ? null : a.D.Note + a.D.X).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NonGenericBaseClassWithKeyRoundTrips()
    {
        using TestDatabase db = new();
        db.Table<KeyedDerivedRow>().Schema.CreateTable();
        db.Table<KeyedDerivedRow>().Add(new KeyedDerivedRow { Id = 3, BaseName = "base", Own = 9 });

        List<(int, string, int)> actual = db.Table<KeyedDerivedRow>().AsEnumerable().Select(r => (r.Id, r.BaseName, r.Own)).ToList();
        Assert.Equal([(3, "base", 9)], actual);

        List<int> filtered = db.Table<KeyedDerivedRow>().Where(r => r.Id == 3).Select(r => r.Own).ToList();
        Assert.Equal([9], filtered);
    }
}
