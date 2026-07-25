using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ShapeVarietyRows")]
public class ShapeVarietyRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }

    public int B { get; set; }

    public string Text { get; set; } = "";

    public int? N { get; set; }
}

public readonly record struct ShapePoint(int X, int Y);

public record ShapeMixedRecord(int X)
{
    public string? Note { get; set; }
}

public class ShapeRequiredDto
{
    public required int A { get; set; }

    public required string T { get; set; }
}

public class ShapeInitDto
{
    public int A { get; init; }

    public string T { get; init; } = "";
}

public class ShapeInnerDto
{
    public int V { get; set; }
}

public class ShapeOuterDto
{
    public ShapeInnerDto? Inner { get; set; }

    public int S { get; set; }
}

public class ProjectionShapeVarietyTests
{
    private static List<ShapeVarietyRow> Rows()
    {
        return
        [
            new ShapeVarietyRow { Id = 1, A = 10, B = 3, Text = "ten", N = 5 },
            new ShapeVarietyRow { Id = 2, A = 2, B = 8, Text = "two", N = null },
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<ShapeVarietyRow>().Schema.CreateTable();
        db.Table<ShapeVarietyRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void RecordStructProjectionReadsBothFields()
    {
        using TestDatabase db = Setup();

        List<ShapePoint> expected = Rows().OrderBy(r => r.Id).Select(r => new ShapePoint(r.A, r.B)).ToList();
        Assert.Equal([new ShapePoint(10, 3), new ShapePoint(2, 8)], expected);

        List<ShapePoint> actual = db.Table<ShapeVarietyRow>().OrderBy(r => r.Id).Select(r => new ShapePoint(r.A, r.B)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RecordConstructorPlusSettablePropertyProjection()
    {
        using TestDatabase db = Setup();

        List<ShapeMixedRecord> expected = Rows().OrderBy(r => r.Id).Select(r => new ShapeMixedRecord(r.A) { Note = r.Text }).ToList();
        Assert.Equal([new ShapeMixedRecord(10) { Note = "ten" }, new ShapeMixedRecord(2) { Note = "two" }], expected);

        List<ShapeMixedRecord> actual = db.Table<ShapeVarietyRow>().OrderBy(r => r.Id).Select(r => new ShapeMixedRecord(r.A) { Note = r.Text }).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RequiredMembersDtoProjection()
    {
        using TestDatabase db = Setup();

        List<(int, string)> expected = Rows().OrderBy(r => r.Id).Select(r => new ShapeRequiredDto { A = r.A, T = r.Text }).Select(d => (d.A, d.T)).ToList();
        Assert.Equal([(10, "ten"), (2, "two")], expected);

        List<(int, string)> actual = db.Table<ShapeVarietyRow>().OrderBy(r => r.Id).Select(r => new ShapeRequiredDto { A = r.A, T = r.Text }).AsEnumerable().Select(d => (d.A, d.T)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InitOnlyDtoProjection()
    {
        using TestDatabase db = Setup();

        List<(int, string)> expected = Rows().OrderBy(r => r.Id).Select(r => new ShapeInitDto { A = r.A, T = r.Text }).Select(d => (d.A, d.T)).ToList();
        Assert.Equal([(10, "ten"), (2, "two")], expected);

        List<(int, string)> actual = db.Table<ShapeVarietyRow>().OrderBy(r => r.Id).Select(r => new ShapeInitDto { A = r.A, T = r.Text }).AsEnumerable().Select(d => (d.A, d.T)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NestedDtoBuiltInsideOuterDtoProjection()
    {
        using TestDatabase db = Setup();

        List<(int, int)> expected = Rows().OrderBy(r => r.Id).Select(r => new ShapeOuterDto { Inner = new ShapeInnerDto { V = r.A }, S = r.B }).Select(o => (o.Inner!.V, o.S)).ToList();
        Assert.Equal([(10, 3), (2, 8)], expected);

        List<(int, int)> actual = db.Table<ShapeVarietyRow>().OrderBy(r => r.Id).Select(r => new ShapeOuterDto { Inner = new ShapeInnerDto { V = r.A }, S = r.B }).AsEnumerable().Select(o => (o.Inner!.V, o.S)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ArrayOfDtosProjection()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows().OrderBy(r => r.Id).Select(r => new[] { new ShapeInnerDto { V = r.A }, new ShapeInnerDto { V = r.B } }).SelectMany(a => a.Select(d => d.V)).ToList();
        Assert.Equal([10, 3, 2, 8], expected);

        List<int> actual = db.Table<ShapeVarietyRow>().OrderBy(r => r.Id).Select(r => new[] { new ShapeInnerDto { V = r.A }, new ShapeInnerDto { V = r.B } }).AsEnumerable().SelectMany(a => a.Select(d => d.V)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConditionalDtoOrNullProjection()
    {
        using TestDatabase db = Setup();

        List<int?> expected = Rows().OrderBy(r => r.Id).Select(r => r.A > 5 ? new ShapeInnerDto { V = r.A } : null).Select(d => d == null ? (int?)null : d.V).ToList();
        Assert.Equal([10, null], expected);

        List<int?> actual = db.Table<ShapeVarietyRow>().OrderBy(r => r.Id).Select(r => r.A > 5 ? new ShapeInnerDto { V = r.A } : null).AsEnumerable().Select(d => d == null ? (int?)null : d.V).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TupleCreateEightArgumentsProjection()
    {
        using TestDatabase db = Setup();

        List<Tuple<int, int, int, int?, string, int, int, Tuple<int>>> expected = Rows().OrderBy(r => r.Id)
            .Select(r => Tuple.Create(r.Id, r.A, r.B, r.N, r.Text, r.Id + 1, r.A + r.B, r.Id * 2))
            .ToList();
        Assert.Equal(Tuple.Create(1, 10, 3, (int?)5, "ten", 2, 13, 2), expected[0]);

        List<Tuple<int, int, int, int?, string, int, int, Tuple<int>>> actual = db.Table<ShapeVarietyRow>().OrderBy(r => r.Id)
            .Select(r => Tuple.Create(r.Id, r.A, r.B, r.N, r.Text, r.Id + 1, r.A + r.B, r.Id * 2))
            .ToList();
        Assert.Equal(expected, actual);
    }
}
