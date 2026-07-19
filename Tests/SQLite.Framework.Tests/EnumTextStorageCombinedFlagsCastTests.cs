using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Flags]
public enum H20CastPerms
{
    None = 0,
    Read = 1,
    Write = 2,
    Execute = 4,
}

public enum H20PlainLevel
{
    Low = 1,
    High = 2,
}

[Table("H20EnumCastRows")]
public class H20EnumCastRow
{
    [Key]
    public int Id { get; set; }

    public H20CastPerms Perms { get; set; }

    public H20CastPerms? OptPerms { get; set; }
}

[Table("H20EnumPlainRows")]
public class H20EnumPlainRow
{
    [Key]
    public int Id { get; set; }

    public H20PlainLevel Level { get; set; }
}

public class H20EnumCastValue
{
    public int Id { get; set; }

    public int N { get; set; }
}

public class EnumTextStorageCombinedFlagsCastTests
{
    private static readonly H20EnumCastRow[] Data =
    [
        new H20EnumCastRow { Id = 1, Perms = H20CastPerms.Read, OptPerms = H20CastPerms.Read | H20CastPerms.Write },
        new H20EnumCastRow { Id = 2, Perms = H20CastPerms.Write, OptPerms = null },
        new H20EnumCastRow { Id = 3, Perms = H20CastPerms.Read | H20CastPerms.Write, OptPerms = H20CastPerms.Read },
        new H20EnumCastRow { Id = 4, Perms = H20CastPerms.Write | H20CastPerms.Execute, OptPerms = H20CastPerms.Write | H20CastPerms.Execute },
        new H20EnumCastRow { Id = 5, Perms = (H20CastPerms)9, OptPerms = null },
        new H20EnumCastRow { Id = 6, Perms = H20CastPerms.None, OptPerms = H20CastPerms.None },
    ];

    private static TestDatabase NewDb()
    {
        TestDatabase db = new(b => b.EnumStorage = EnumStorageMode.Text);
        db.Table<H20EnumCastRow>().Schema.CreateTable();
        foreach (H20EnumCastRow r in Data)
        {
            db.Table<H20EnumCastRow>().Add(r);
        }

        return db;
    }

    [Fact]
    public void CastInProjectionMatchesDotNet()
    {
        using TestDatabase db = NewDb();

        List<int> expected = Data.OrderBy(r => r.Id).Select(r => (int)r.Perms).ToList();
        List<int> actual = db.Table<H20EnumCastRow>().OrderBy(x => x.Id).Select(x => (int)x.Perms).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CastInWhereMatchesDotNet()
    {
        using TestDatabase db = NewDb();

        List<int> expected = Data.Where(r => (int)r.Perms > 2).Select(r => r.Id).OrderBy(id => id).ToList();
        List<int> actual = db.Table<H20EnumCastRow>().Where(x => (int)x.Perms > 2).Select(x => x.Id).OrderBy(id => id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CastInOrderByMatchesDotNet()
    {
        using TestDatabase db = NewDb();

        List<int> expected = Data.OrderBy(r => (int)r.Perms).ThenBy(r => r.Id).Select(r => r.Id).ToList();
        List<int> actual = db.Table<H20EnumCastRow>().OrderBy(x => (int)x.Perms).ThenBy(x => x.Id).Select(x => x.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CastSumMatchesDotNet()
    {
        using TestDatabase db = NewDb();

        int expected = Data.Sum(r => (int)r.Perms);
        int actual = db.Table<H20EnumCastRow>().Sum(x => (int)x.Perms);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CastAverageMatchesDotNet()
    {
        using TestDatabase db = NewDb();

        double expected = Data.Average(r => (int)r.Perms);
        double actual = db.Table<H20EnumCastRow>().Average(x => (int)x.Perms);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CastMinOverCombinedRowsMatchesDotNet()
    {
        using TestDatabase db = NewDb();

        int expected = Data.Where(r => r.Id == 3 || r.Id == 4).Min(r => (int)r.Perms);
        int actual = db.Table<H20EnumCastRow>().Where(x => x.Id == 3 || x.Id == 4).Min(x => (int)x.Perms);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CastGroupByKeyMatchesDotNet()
    {
        using TestDatabase db = NewDb();

        List<(int Key, int Count)> expected = Data
            .GroupBy(r => (int)r.Perms)
            .Select(g => (g.Key, g.Count()))
            .OrderBy(t => t.Key)
            .ToList();
        List<(int Key, int Count)> actual = db.Table<H20EnumCastRow>()
            .GroupBy(x => (int)x.Perms)
            .Select(g => new { g.Key, C = g.Count() })
            .AsEnumerable()
            .Select(a => (a.Key, a.C))
            .OrderBy(t => t.Key)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CastDistinctMatchesDotNet()
    {
        using TestDatabase db = NewDb();

        List<int> expected = Data.Select(r => (int)r.Perms).Distinct().OrderBy(v => v).ToList();
        List<int> actual = db.Table<H20EnumCastRow>().Select(x => (int)x.Perms).Distinct().AsEnumerable().OrderBy(v => v).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CastUnionMatchesDotNet()
    {
        using TestDatabase db = NewDb();

        List<int> expected = Data.Where(r => r.Id > 3).Select(r => (int)r.Perms)
            .Union(Data.Where(r => r.Id <= 3).Select(r => (int)r.Perms))
            .OrderBy(v => v)
            .ToList();
        List<int> actual = db.Table<H20EnumCastRow>().Where(x => x.Id > 3).Select(x => (int)x.Perms)
            .Union(db.Table<H20EnumCastRow>().Where(x => x.Id <= 3).Select(x => (int)x.Perms))
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WindowOrderByCastMatchesDotNet()
    {
        using TestDatabase db = NewDb();

        List<(int Id, int Rn)> expected = Data
            .OrderBy(r => (int)r.Perms)
            .ThenBy(r => r.Id)
            .Select((r, i) => (r.Id, i + 1))
            .OrderBy(t => t.Id)
            .ToList();
        List<(int Id, int Rn)> actual = db.Table<H20EnumCastRow>()
            .Select(x => new { x.Id, Rn = SQLiteWindowFunctions.RowNumber().Over().OrderBy((int)x.Perms).ThenOrderBy(x.Id).AsValue() })
            .AsEnumerable()
            .Select(a => (a.Id, (int)a.Rn))
            .OrderBy(t => t.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JoinOnCastKeysMatchesDotNet()
    {
        using TestDatabase db = NewDb();

        List<(int A, int B)> expected = Data
            .Join(Data, a => (int)a.Perms, b => (int)b.Perms, (a, b) => (a.Id, b.Id))
            .Where(p => p.Item1 < p.Item2)
            .OrderBy(p => p.Item1)
            .ThenBy(p => p.Item2)
            .ToList();
        List<(int A, int B)> actual = db.Table<H20EnumCastRow>()
            .Join(db.Table<H20EnumCastRow>(), a => (int)a.Perms, b => (int)b.Perms, (a, b) => new { A = a.Id, B = b.Id })
            .Where(p => p.A < p.B)
            .OrderBy(p => p.A)
            .ThenBy(p => p.B)
            .AsEnumerable()
            .Select(p => (p.A, p.B))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableCastInProjectionMatchesDotNet()
    {
        using TestDatabase db = NewDb();

        List<int?> expected = Data.OrderBy(r => r.Id).Select(r => (int?)r.OptPerms).ToList();
        List<int?> actual = db.Table<H20EnumCastRow>().OrderBy(x => x.Id).Select(x => (int?)x.OptPerms).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableCastInWhereMatchesDotNet()
    {
        using TestDatabase db = NewDb();

        List<int> expected = Data.Where(r => (int?)r.OptPerms > 2).Select(r => r.Id).OrderBy(id => id).ToList();
        List<int> actual = db.Table<H20EnumCastRow>().Where(x => (int?)x.OptPerms > 2).Select(x => x.Id).OrderBy(id => id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CastToLongMatchesDotNet()
    {
        using TestDatabase db = NewDb();

        List<long> expected = Data.OrderBy(r => r.Id).Select(r => (long)r.Perms).ToList();
        List<long> actual = db.Table<H20EnumCastRow>().OrderBy(x => x.Id).Select(x => (long)x.Perms).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CastThroughCteMatchesDotNet()
    {
        using TestDatabase db = NewDb();

        SQLiteCte<H20EnumCastValue> cte = db.With<H20EnumCastValue>(() =>
            db.Table<H20EnumCastRow>().Select(x => new H20EnumCastValue { Id = x.Id, N = (int)x.Perms }));

        List<int> expected = Data.OrderBy(r => r.Id).Select(r => (int)r.Perms).ToList();
        List<int> actual = cte.OrderBy(c => c.Id).Select(c => c.N).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PlainEnumUndefinedValueCastMatchesDotNet()
    {
        using TestDatabase db = new(b => b.EnumStorage = EnumStorageMode.Text);
        db.Table<H20EnumPlainRow>().Schema.CreateTable();
        H20EnumPlainRow[] rows =
        [
            new H20EnumPlainRow { Id = 1, Level = H20PlainLevel.Low },
            new H20EnumPlainRow { Id = 2, Level = (H20PlainLevel)7 },
            new H20EnumPlainRow { Id = 3, Level = H20PlainLevel.High },
        ];
        foreach (H20EnumPlainRow r in rows)
        {
            db.Table<H20EnumPlainRow>().Add(r);
        }

        List<int> expected = rows.OrderBy(r => r.Id).Select(r => (int)r.Level).ToList();
        List<int> actual = db.Table<H20EnumPlainRow>().OrderBy(x => x.Id).Select(x => (int)x.Level).ToList();

        Assert.Equal(expected, actual);
    }
}
