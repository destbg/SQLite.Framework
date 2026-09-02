using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonConverter(typeof(JsonStringEnumConverter<ProjectedCollectionState>))]
public enum ProjectedCollectionState
{
    None,
    Ready,
    Done
}

public sealed record ProjectedCollectionItem
{
    public string Name { get; init; } = "";

    public bool Enabled { get; init; }

    public ProjectedCollectionState State { get; init; }

    public DateTime When { get; init; }

    public int? Score { get; init; }
}

internal sealed class ProjectedCollectionRow
{
    [Key]
    public int Id { get; set; }

    public List<ProjectedCollectionItem> Items { get; set; } = [];
}

[JsonSerializable(typeof(List<ProjectedCollectionItem>))]
internal partial class ProjectedCollectionJsonContext : JsonSerializerContext;

public class JsonProjectedCollectionMaterializationTests
{
    private static readonly ProjectedCollectionRow[] Rows =
    [
        new ProjectedCollectionRow
        {
            Id = 1,
            Items =
            [
                new ProjectedCollectionItem
                {
                    Name = "alpha",
                    Enabled = true,
                    State = ProjectedCollectionState.Ready,
                    When = new DateTime(2000, 2, 29, 12, 34, 56, DateTimeKind.Utc),
                    Score = null
                },
                new ProjectedCollectionItem
                {
                    Name = "",
                    Enabled = false,
                    State = ProjectedCollectionState.Done,
                    When = new DateTime(2024, 2, 29, 1, 2, 3, DateTimeKind.Utc),
                    Score = 7
                },
                new ProjectedCollectionItem
                {
                    Name = "alpha",
                    Enabled = true,
                    State = ProjectedCollectionState.Ready,
                    When = new DateTime(2000, 2, 29, 12, 34, 56, DateTimeKind.Utc),
                    Score = 7
                }
            ]
        },
        new ProjectedCollectionRow { Id = 2, Items = [] }
    ];

    [Fact]
    public void StringMemberListMatchesObjects()
    {
        using TestDatabase db = Seed();
        List<string> expected = Rows.Where(r => r.Id == 1).Select(r => r.Items.Select(x => x.Name).ToList()).First();
        List<string> actual = db.Table<ProjectedCollectionRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Items.Select(x => x.Name).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BooleanMemberHashSetMatchesObjects()
    {
        using TestDatabase db = Seed();
        HashSet<bool> expected = Rows.Where(r => r.Id == 1).Select(r => r.Items.Select(x => x.Enabled).ToHashSet()).First();
        HashSet<bool> actual = db.Table<ProjectedCollectionRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Items.Select(x => x.Enabled).ToHashSet())
            .First();

        Assert.Equal(expected.Order(), actual.Order());
    }

    [Fact]
    public void EnumMemberArrayMatchesObjects()
    {
        using TestDatabase db = Seed();
        ProjectedCollectionState[] expected = Rows.Where(r => r.Id == 1).Select(r => r.Items.Select(x => x.State).ToArray()).First();
        ProjectedCollectionState[] actual = db.Table<ProjectedCollectionRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Items.Select(x => x.State).ToArray())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TemporalMemberListMatchesObjects()
    {
        using TestDatabase db = Seed();
        List<DateTime> expected = Rows.Where(r => r.Id == 1).Select(r => r.Items.Select(x => x.When).ToList()).First();
        List<DateTime> actual = db.Table<ProjectedCollectionRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Items.Select(x => x.When).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableMemberArrayMatchesObjects()
    {
        using TestDatabase db = Seed();
        int?[] expected = Rows.Where(r => r.Id == 1).Select(r => r.Items.Select(x => x.Score).ToArray()).First();
        int?[] actual = db.Table<ProjectedCollectionRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Items.Select(x => x.Score).ToArray())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ObjectHashSetMatchesObjects()
    {
        using TestDatabase db = Seed();
        HashSet<ProjectedCollectionItem> expected = Rows.Where(r => r.Id == 1).Select(r => r.Items.Where(x => x.Score.HasValue).ToHashSet()).First();
        HashSet<ProjectedCollectionItem> actual = db.Table<ProjectedCollectionRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Items.Where(x => x.Score.HasValue).ToHashSet())
            .First();

        Assert.Equal(expected.OrderBy(x => x.Name).ThenBy(x => x.When), actual.OrderBy(x => x.Name).ThenBy(x => x.When));
    }

    [Fact]
    public void EmptyArrayListAndHashSetMatchObjects()
    {
        using TestDatabase db = Seed();
        string[] expectedArray = Rows.Where(r => r.Id == 2).Select(r => r.Items.Select(x => x.Name).ToArray()).First();
        string[] actualArray = db.Table<ProjectedCollectionRow>()
            .Where(r => r.Id == 2)
            .Select(r => r.Items.Select(x => x.Name).ToArray())
            .First();
        List<bool> expectedList = Rows.Where(r => r.Id == 2).Select(r => r.Items.Select(x => x.Enabled).ToList()).First();
        List<bool> actualList = db.Table<ProjectedCollectionRow>()
            .Where(r => r.Id == 2)
            .Select(r => r.Items.Select(x => x.Enabled).ToList())
            .First();
        HashSet<ProjectedCollectionState> expectedSet = Rows.Where(r => r.Id == 2).Select(r => r.Items.Select(x => x.State).ToHashSet()).First();
        HashSet<ProjectedCollectionState> actualSet = db.Table<ProjectedCollectionRow>()
            .Where(r => r.Id == 2)
            .Select(r => r.Items.Select(x => x.State).ToHashSet())
            .First();

        Assert.Equal(expectedArray, actualArray);
        Assert.Equal(expectedList, actualList);
        Assert.Equal(expectedSet, actualSet);
    }

    [Fact]
    public void ReflectionDisabledRejectsAnUnregisteredRawCollection()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder(":memory:").DisableReflectionFallback().Build();
        using SQLiteDatabase db = new(options);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            db.Query<List<string>>("SELECT '[\"a\"]'").Single());

        Assert.Contains("Collection materializer", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ObjectElementReadRequiresJsonMetadata()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder(":memory:").Build();
        using JsonDocument document = JsonDocument.Parse("{}");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            options.ReadJsonElement<ProjectedCollectionItem>(document.RootElement));

        Assert.Contains(typeof(ProjectedCollectionItem).FullName!, exception.Message, StringComparison.Ordinal);
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<ProjectedCollectionItem>)] =
                new SQLiteJsonConverter<List<ProjectedCollectionItem>>(ProjectedCollectionJsonContext.Default.ListProjectedCollectionItem));
        db.Table<ProjectedCollectionRow>().Schema.CreateTable();
        db.Table<ProjectedCollectionRow>().AddRange(Rows);
        return db;
    }
}
