using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ClientLeafRows")]
public class ClientLeafRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }

    public int B { get; set; }

    public string Text { get; set; } = "";

    public int? N { get; set; }

    public double? D { get; set; }
}

public static class ClientLeafHelpers
{
    public static string Mix(string t, int? n, int a)
    {
        return t + "|" + (n == null ? "-" : n.Value.ToString()) + "|" + a;
    }

    public static int Triple(int v)
    {
        return v * 3 + 1;
    }

    public static int Pack(int a, int b)
    {
        return a * 100 + b;
    }

    public static ShapePoint Point(int a, int b)
    {
        return new ShapePoint(a, b);
    }

    public static double? Scale(double? d, int factor)
    {
        return d * factor;
    }
}

public class ClientEvalLeafVarietyTests
{
    private static List<ClientLeafRow> Rows()
    {
        return
        [
            new ClientLeafRow { Id = 1, A = 4, B = 7, Text = "x", N = 9, D = 1.5 },
            new ClientLeafRow { Id = 2, A = 6, B = 2, Text = "y", N = null, D = null },
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<ClientLeafRow>().Schema.CreateTable();
        db.Table<ClientLeafRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void ClientHelperOverMultipleColumnsIncludingNullable()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows().OrderBy(r => r.Id).Select(r => ClientLeafHelpers.Mix(r.Text, r.N, r.A)).ToList();
        Assert.Equal(["x|9|4", "y|-|6"], expected);

        List<string> actual = db.Table<ClientLeafRow>().OrderBy(r => r.Id).Select(r => ClientLeafHelpers.Mix(r.Text, r.N, r.A)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ClientHelperMixedWithTranslatedColumnsInAnonymousProjection()
    {
        using TestDatabase db = Setup();

        List<(int, string, int)> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, M = ClientLeafHelpers.Mix(r.Text, r.N, r.A), S = r.A + r.B })
            .Select(a => (a.Id, a.M, a.S))
            .ToList();
        Assert.Equal([(1, "x|9|4", 11), (2, "y|-|6", 8)], expected);

        List<(int, string, int)> actual = db.Table<ClientLeafRow>().OrderBy(r => r.Id)
            .Select(r => new { r.Id, M = ClientLeafHelpers.Mix(r.Text, r.N, r.A), S = r.A + r.B })
            .AsEnumerable()
            .Select(a => (a.Id, a.M, a.S))
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NestedClientHelperCalls()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows().OrderBy(r => r.Id).Select(r => ClientLeafHelpers.Pack(ClientLeafHelpers.Triple(r.A), r.B)).ToList();
        Assert.Equal([1307, 1902], expected);

        List<int> actual = db.Table<ClientLeafRow>().OrderBy(r => r.Id).Select(r => ClientLeafHelpers.Pack(ClientLeafHelpers.Triple(r.A), r.B)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ClientHelperProducingStructResult()
    {
        using TestDatabase db = Setup();

        List<ShapePoint> expected = Rows().OrderBy(r => r.Id).Select(r => ClientLeafHelpers.Point(r.A, r.B)).ToList();
        Assert.Equal([new ShapePoint(4, 7), new ShapePoint(6, 2)], expected);

        List<ShapePoint> actual = db.Table<ClientLeafRow>().OrderBy(r => r.Id).Select(r => ClientLeafHelpers.Point(r.A, r.B)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ClientHelperOverNullableDoubleColumn()
    {
        using TestDatabase db = Setup();

        List<double?> expected = Rows().OrderBy(r => r.Id).Select(r => ClientLeafHelpers.Scale(r.D, 4)).ToList();
        Assert.Equal([6.0, null], expected);

        List<double?> actual = db.Table<ClientLeafRow>().OrderBy(r => r.Id).Select(r => ClientLeafHelpers.Scale(r.D, 4)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ClientHelperOverGroupKeyOnly()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows().GroupBy(r => r.A)
            .Select(g => ClientLeafHelpers.Triple(g.Key))
            .OrderBy(v => v)
            .ToList();
        Assert.Equal([13, 19], expected);

        List<int> actual = db.Table<ClientLeafRow>().GroupBy(r => r.A)
            .Select(g => ClientLeafHelpers.Triple(g.Key))
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ClientHelperOverGroupAggregateOnly()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows().GroupBy(r => r.Text)
            .Select(g => ClientLeafHelpers.Triple(g.Count()))
            .OrderBy(v => v)
            .ToList();
        Assert.Equal([4, 4], expected);

        List<int> actual = db.Table<ClientLeafRow>().GroupBy(r => r.Text)
            .Select(g => ClientLeafHelpers.Triple(g.Count()))
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ClientHelperOverGroupByProjection()
    {
        using TestDatabase db = Setup();

        List<(string, string)> expected = Rows().GroupBy(r => r.Text)
            .Select(g => new { g.Key, M = ClientLeafHelpers.Mix(g.Key, null, g.Count()) })
            .OrderBy(a => a.Key)
            .Select(a => (a.Key, a.M))
            .ToList();
        Assert.Equal([("x", "x|-|1"), ("y", "y|-|1")], expected);

        List<(string, string)> actual = db.Table<ClientLeafRow>().GroupBy(r => r.Text)
            .Select(g => new { g.Key, M = ClientLeafHelpers.Mix(g.Key, null, g.Count()) })
            .AsEnumerable()
            .OrderBy(a => a.Key)
            .Select(a => (a.Key, a.M))
            .ToList();
        Assert.Equal(expected, actual);
    }
}
