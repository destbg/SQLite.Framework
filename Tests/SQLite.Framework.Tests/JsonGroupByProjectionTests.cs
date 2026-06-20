using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class JgpRow
{
    [Key]
    public int Id { get; set; }
    public List<int> Numbers { get; set; } = [];
}

public class JsonGroupByProjectionTests
{
    private static readonly List<int> A = [5, 3, 5, 8, 3, 3];

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<JgpRow>().Schema.CreateTable();
        db.Table<JgpRow>().Add(new JgpRow { Id = 1, Numbers = A });
        return db;
    }

    [Fact]
    public void GroupBy_Count_PerGroup()
    {
        using TestDatabase db = CreateDb();
        List<int> oracle = A.GroupBy(x => x).Select(g => g.Count()).OrderBy(c => c).ToList();
        List<int> actual = db.Table<JgpRow>().Select(r => r.Numbers.GroupBy(x => x).Select(g => g.Count())).First().OrderBy(c => c).ToList();
        Assert.Equal([1, 2, 3], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void GroupBy_Key_PerGroup()
    {
        using TestDatabase db = CreateDb();
        List<int> oracle = A.GroupBy(x => x).Select(g => g.Key).OrderBy(c => c).ToList();
        List<int> actual = db.Table<JgpRow>().Select(r => r.Numbers.GroupBy(x => x).Select(g => g.Key)).First().OrderBy(c => c).ToList();
        Assert.Equal([3, 5, 8], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void GroupBy_Sum_PerGroup()
    {
        using TestDatabase db = CreateDb();
        List<int> oracle = A.GroupBy(x => x).Select(g => g.Sum()).OrderBy(c => c).ToList();
        List<int> actual = db.Table<JgpRow>().Select(r => r.Numbers.GroupBy(x => x).Select(g => g.Sum())).First().OrderBy(c => c).ToList();
        Assert.Equal([8, 9, 10], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void GroupBy_Max_PerGroup()
    {
        using TestDatabase db = CreateDb();
        List<int> oracle = A.GroupBy(x => x).Select(g => g.Max()).OrderBy(c => c).ToList();
        List<int> actual = db.Table<JgpRow>().Select(r => r.Numbers.GroupBy(x => x).Select(g => g.Max())).First().OrderBy(c => c).ToList();
        Assert.Equal([3, 5, 8], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void GroupBy_Min_PerGroup()
    {
        using TestDatabase db = CreateDb();
        List<int> oracle = A.GroupBy(x => x).Select(g => g.Min()).OrderBy(c => c).ToList();
        List<int> actual = db.Table<JgpRow>().Select(r => r.Numbers.GroupBy(x => x).Select(g => g.Min())).First().OrderBy(c => c).ToList();
        Assert.Equal([3, 5, 8], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void GroupBy_NonIdentityKey_Count()
    {
        using TestDatabase db = CreateDb();
        List<int> oracle = A.GroupBy(x => x % 2).Select(g => g.Count()).OrderBy(c => c).ToList();
        List<int> actual = db.Table<JgpRow>().Select(r => r.Numbers.GroupBy(x => x % 2).Select(g => g.Count())).First().OrderBy(c => c).ToList();
        Assert.Equal([1, 5], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void Where_Then_GroupBy_Count_FiltersBeforeGrouping()
    {
        using TestDatabase db = CreateDb();
        List<int> oracle = A.Where(x => x > 3).GroupBy(x => x).Select(g => g.Count()).OrderBy(c => c).ToList();
        List<int> actual = db.Table<JgpRow>().Select(r => r.Numbers.Where(x => x > 3).GroupBy(x => x).Select(g => g.Count())).First().OrderBy(c => c).ToList();
        Assert.Equal([1, 2], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void GroupBy_NumberOfGroups_ScalarCount()
    {
        using TestDatabase db = CreateDb();
        int oracle = A.GroupBy(x => x).Count();
        int actual = db.Table<JgpRow>().Select(r => r.Numbers.GroupBy(x => x).Count()).First();
        Assert.Equal(3, oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void GroupBy_Then_Where_OnCount_FiltersGroups()
    {
        using TestDatabase db = CreateDb();
        List<int> oracle = A.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).OrderBy(c => c).ToList();
        List<int> actual = db.Table<JgpRow>().Select(r => r.Numbers.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key)).First().OrderBy(c => c).ToList();
        Assert.Equal([3, 5], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void GroupBy_Then_Where_OnCount_ThenCount_CountsFilteredGroups()
    {
        using TestDatabase db = CreateDb();
        int oracle = A.GroupBy(x => x).Where(g => g.Count() > 1).Count();
        int actual = db.Table<JgpRow>().Select(r => r.Numbers.GroupBy(x => x).Where(g => g.Count() > 1).Count()).First();
        Assert.Equal(2, oracle);
        Assert.Equal(oracle, actual);
    }
}
