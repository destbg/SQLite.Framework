using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23fPagedGroupRows")]
public class H23fPagedGroupRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

public class JsonGroupedPagedGroupFilterTests
{
    [Fact]
    public void CountingGroupsFilteredAfterTakeReportsItCannotRun()
    {
        using TestDatabase db = Setup(nameof(CountingGroupsFilteredAfterTakeReportsItCannotRun));

        NotSupportedException error = Assert.Throws<NotSupportedException>(() => db.Table<H23fPagedGroupRow>()
            .Select(r => r.Numbers.GroupBy(n => n).Take(2).Where(g => g.Count() > 1).Count())
            .First());

        Assert.Contains("group aggregate after Take or Skip", error.Message);
    }

    [Fact]
    public void GroupKeysFilteredAfterTakeReportsItCannotRun()
    {
        using TestDatabase db = Setup(nameof(GroupKeysFilteredAfterTakeReportsItCannotRun));

        NotSupportedException error = Assert.Throws<NotSupportedException>(() => db.Table<H23fPagedGroupRow>()
            .Select(r => r.Numbers.GroupBy(n => n).Take(2).Where(g => g.Count() > 1).Select(g => g.Key))
            .First());

        Assert.Contains("group aggregate after Take or Skip", error.Message);
    }

    [Fact]
    public void GroupKeysFilteredBySumAfterTakeReportsItCannotRun()
    {
        using TestDatabase db = Setup(nameof(GroupKeysFilteredBySumAfterTakeReportsItCannotRun));

        NotSupportedException error = Assert.Throws<NotSupportedException>(() => db.Table<H23fPagedGroupRow>()
            .Select(r => r.Numbers.GroupBy(n => n).Take(2).Where(g => g.Sum() > 1).Select(g => g.Key))
            .First());

        Assert.Contains("group aggregate after Take or Skip", error.Message);
    }

    [Fact]
    public void GroupKeysAfterTakeReadTheGroupKeys()
    {
        using TestDatabase db = Setup(nameof(GroupKeysAfterTakeReadTheGroupKeys));

        List<int> expected = Numbers().GroupBy(n => n).Take(2).Select(g => g.Key).ToList();
        List<int> actual = db.Table<H23fPagedGroupRow>()
            .Select(r => r.Numbers.GroupBy(n => n).Take(2).Select(g => g.Key))
            .First()
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<int> Numbers()
    {
        return [1, 2, 3, 1];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] =
            new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32), methodName);
        db.Table<H23fPagedGroupRow>().Schema.CreateTable();
        db.Table<H23fPagedGroupRow>().Add(new H23fPagedGroupRow { Id = 1, Numbers = Numbers() });
        return db;
    }
}
