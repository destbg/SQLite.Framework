using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<decimal>))]
internal partial class DecimalListContext : JsonSerializerContext;

internal sealed class DecimalListRow
{
    [Key]
    public int Id { get; set; }

    public List<decimal> Values { get; set; } = [];
}

public class JsonListDecimalContainsTextStorageTests
{
    private static TestDatabase Seed()
    {
        TestDatabase db = new(b =>
        {
            b.UseDecimalStorage(DecimalStorageMode.Text);
            b.AddJsonContext(DecimalListContext.Default);
        });
        db.Table<DecimalListRow>().Schema.CreateTable();
        db.Table<DecimalListRow>().Add(new DecimalListRow { Id = 1, Values = [10.5m, 20.25m] });
        return db;
    }

    [Fact]
    public void ContainsBindsStorageFormAndDoesNotMatch()
    {
        using TestDatabase db = Seed();

        List<decimal> values = [10.5m, 20.25m];
        bool inMemory = values.Contains(10.5m);
        Assert.True(inMemory);

        List<int> actual = db.Table<DecimalListRow>().Where(r => r.Values.Contains(10.5m)).Select(r => r.Id).ToList();
        Assert.Equal([], actual);
    }

    [Fact]
    public void IndexOfBindsStorageFormAndDoesNotMatch()
    {
        using TestDatabase db = Seed();

        List<decimal> values = [10.5m, 20.25m];
        int inMemory = values.IndexOf(20.25m);
        Assert.Equal(1, inMemory);

        int actual = db.Table<DecimalListRow>().Select(r => r.Values.IndexOf(20.25m)).First();
        Assert.Equal(-1, actual);
    }

    [Fact]
    public void CountWithPredicateComparesStorageForm()
    {
        using TestDatabase db = Seed();

        List<decimal> values = [10.5m, 20.25m];
        int inMemory = values.Count(v => v > 15m);
        Assert.Equal(1, inMemory);

        int actual = db.Table<DecimalListRow>().Select(r => r.Values.Count(v => v > 15m)).First();
        Assert.Equal(0, actual);
    }
}
