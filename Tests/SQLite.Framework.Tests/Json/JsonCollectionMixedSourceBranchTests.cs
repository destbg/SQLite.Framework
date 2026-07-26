using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class H22zHolder
{
    public List<int>? A { get; set; }

    public List<int>? B { get; set; }
}

[JsonSerializable(typeof(H22zHolder))]
internal partial class H22zHolderContext : JsonSerializerContext;

[Table("H22zJsonRows")]
internal sealed class H22zJsonRow
{
    [Key]
    public int Id { get; set; }

    public List<int>? Numbers { get; set; }

    public List<int>? Spare { get; set; }

    public H22zHolder Holder { get; set; } = new();
}

public class JsonCollectionMixedSourceBranchTests
{
    [Fact]
    public void CoalesceWithCapturedFallbackKeepsTheStoredList()
    {
        using TestDatabase db = CreateDb(nameof(CoalesceWithCapturedFallbackKeepsTheStoredList));
        Seed(db);
        List<int> fallback = [7];

        List<List<int>> actual = db.Table<H22zJsonRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Numbers ?? fallback)
            .ToList();

        Assert.Equal([[1, 2], [7]], actual);
    }

    [Fact]
    public void ConditionalWithCapturedBranchMixesJsonAndParameterSources()
    {
        using TestDatabase db = CreateDb(nameof(ConditionalWithCapturedBranchMixesJsonAndParameterSources));
        Seed(db);
        List<int> fallback = [9];
        int capturedId = 1;

        List<List<int>?> actual = db.Table<H22zJsonRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Id == capturedId ? r.Numbers : fallback)
            .ToList();

        Assert.Equal([[1, 2], [9]], actual);
    }

    [Fact]
    public void CoalesceOfTwoJsonColumnsKeepsTheJsonSource()
    {
        using TestDatabase db = CreateDb(nameof(CoalesceOfTwoJsonColumnsKeepsTheJsonSource));
        Seed(db);

        List<List<int>?> actual = db.Table<H22zJsonRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Numbers ?? r.Spare)
            .ToList();

        Assert.Equal([[1, 2], [5]], actual);
    }

    [Fact]
    public void ConditionalOfTwoJsonColumnsKeepsTheJsonSource()
    {
        using TestDatabase db = CreateDb(nameof(ConditionalOfTwoJsonColumnsKeepsTheJsonSource));
        Seed(db);
        int capturedId = 1;

        List<List<int>?> actual = db.Table<H22zJsonRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Id == capturedId ? r.Numbers : r.Spare)
            .ToList();

        Assert.Equal([[1, 2], [5]], actual);
    }

    [Fact]
    public void CoalesceOfTwoJsonSubListsKeepsTheJsonSource()
    {
        using TestDatabase db = CreateDb(nameof(CoalesceOfTwoJsonSubListsKeepsTheJsonSource));
        Seed(db);

        List<List<int>?> actual = db.Table<H22zJsonRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Holder.A ?? r.Holder.B)
            .ToList();

        Assert.Equal([[11], [22]], actual);
    }

    [Fact]
    public void ConditionalOfTwoJsonSubListsKeepsTheJsonSource()
    {
        using TestDatabase db = CreateDb(nameof(ConditionalOfTwoJsonSubListsKeepsTheJsonSource));
        Seed(db);
        int capturedId = 1;

        List<List<int>?> actual = db.Table<H22zJsonRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Id == capturedId ? r.Holder.A : r.Holder.B)
            .ToList();

        Assert.Equal([[11], [22]], actual);
    }

    [Fact]
    public void CoalesceOfAJsonSubListAndACapturedFallbackMixesSources()
    {
        using TestDatabase db = CreateDb(nameof(CoalesceOfAJsonSubListAndACapturedFallbackMixesSources));
        Seed(db);
        List<int> fallback = [77];

        List<List<int>> actual = db.Table<H22zJsonRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Holder.A ?? fallback)
            .ToList();

        Assert.Equal([[11], [77]], actual);
    }

    [Fact]
    public void ConditionalOfAJsonSubListAndACapturedFallbackMixesSources()
    {
        using TestDatabase db = CreateDb(nameof(ConditionalOfAJsonSubListAndACapturedFallbackMixesSources));
        Seed(db);
        List<int> fallback = [88];
        int capturedId = 1;

        List<List<int>?> actual = db.Table<H22zJsonRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Id == capturedId ? r.Holder.A : fallback)
            .ToList();

        Assert.Equal([[11], [88]], actual);
    }

    private static TestDatabase CreateDb(string methodName)
    {
        return new TestDatabase(
            b =>
            {
                b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32);
                b.TypeConverters[typeof(H22zHolder)] = new SQLiteJsonConverter<H22zHolder>(H22zHolderContext.Default.H22zHolder);
            },
            methodName);
    }

    private static void Seed(TestDatabase db)
    {
        db.Table<H22zJsonRow>().Schema.CreateTable();
        db.Table<H22zJsonRow>().AddRange(
        [
            new H22zJsonRow { Id = 1, Numbers = [1, 2], Spare = [3], Holder = new H22zHolder { A = [11], B = [12] } },
            new H22zJsonRow { Id = 2, Numbers = null, Spare = [5], Holder = new H22zHolder { A = null, B = [22] } }
        ]);
    }
}
