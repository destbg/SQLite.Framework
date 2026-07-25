using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H20CmrRow")]
public class H20CmrRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }

    public int B { get; set; }

    public DateTime When { get; set; }
}

public class H20CmrGhostDto
{
    [NotMapped]
    public int A { get; set; }
}

public class H20CmrPayload
{
    public string City { get; set; } = "";

    public int Zip { get; set; }
}

[Table("H20CmrJsonRow")]
public class H20CmrJsonRow
{
    [Key]
    public int Id { get; set; }

    public H20CmrPayload Payload { get; set; } = new();
}

[JsonSerializable(typeof(H20CmrPayload))]
internal partial class H20CmrJsonContext : JsonSerializerContext;

public class CteClientMemberColumnRewriteTests
{
    private static List<H20CmrRow> Rows() =>
    [
        new H20CmrRow { Id = 1, A = 10, B = 100, When = new DateTime(2024, 1, 1) },
        new H20CmrRow { Id = 2, A = 20, B = 200, When = new DateTime(2024, 1, 2) },
        new H20CmrRow { Id = 3, A = 30, B = 300, When = new DateTime(2024, 1, 7) },
        new H20CmrRow { Id = 4, A = 40, B = 400, When = new DateTime(2024, 1, 8) },
    ];

    private static List<H20CmrJsonRow> JsonRows() =>
    [
        new H20CmrJsonRow { Id = 1, Payload = new H20CmrPayload { City = "beta", Zip = 1 } },
        new H20CmrJsonRow { Id = 2, Payload = new H20CmrPayload { City = "alpha", Zip = 2 } },
        new H20CmrJsonRow { Id = 3, Payload = new H20CmrPayload { City = "beta", Zip = 3 } },
        new H20CmrJsonRow { Id = 4, Payload = new H20CmrPayload { City = "gamma", Zip = 4 } },
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H20CmrRow>().Schema.CreateTable();
        db.Table<H20CmrRow>().AddRange(Rows());
        return db;
    }

    private static TestDatabase Setup(EnumStorageMode mode)
    {
        TestDatabase db = new(b => b.UseEnumStorage(mode));
        db.Table<H20CmrRow>().Schema.CreateTable();
        db.Table<H20CmrRow>().AddRange(Rows());
        return db;
    }

    private static TestDatabase SetupJson()
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(H20CmrPayload)] =
            new SQLiteJsonConverter<H20CmrPayload>(H20CmrJsonContext.Default.H20CmrPayload));
        db.Table<H20CmrJsonRow>().Schema.CreateTable();
        db.Table<H20CmrJsonRow>().AddRange(JsonRows());
        return db;
    }

    [Fact]
    public void CteBodyObjectArrayMemberPlainColumnReadMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Select(r => new { r.Id, Arr = new object[] { r.A, r.B } })
            .OrderBy(x => x.Id)
            .Select(x => x.Id).ToList();

        List<int> actual = db.With(() => db.Table<H20CmrRow>()
                .Select(r => new { r.Id, Arr = new object[] { r.A, r.B } }))
            .OrderBy(x => x.Id)
            .Select(x => x.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CteBodyDayOfWeekGroupKeyArrayMemberFilterTextStorageMatchesLinq()
    {
        using TestDatabase db = Setup(EnumStorageMode.Text);

        List<DayOfWeek> expected = Rows()
            .GroupBy(r => r.When.DayOfWeek)
            .Select(g => g.Key)
            .Where(k => k == DayOfWeek.Monday).ToList();

        List<DayOfWeek> actual = db.With(() => db.Table<H20CmrRow>()
                .GroupBy(r => r.When.DayOfWeek)
                .Select(g => new { g.Key, Arr = new[] { g.Key, g.Key } }))
            .Where(x => x.Key == DayOfWeek.Monday)
            .Select(x => x.Key).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CteBodyDayOfWeekGroupKeyArrayMemberOrderedReadMatchesLinq()
    {
        using TestDatabase db = Setup(EnumStorageMode.Text);

        List<DayOfWeek> expected = Rows()
            .GroupBy(r => r.When.DayOfWeek)
            .Select(g => g.Key)
            .OrderBy(k => k).ToList();

        List<DayOfWeek> actual = db.With(() => db.Table<H20CmrRow>()
                .GroupBy(r => r.When.DayOfWeek)
                .Select(g => new { g.Key, Arr = new[] { g.Key, g.Key } }))
            .OrderBy(x => x.Key)
            .Select(x => x.Key).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CteBodyJsonGroupKeyArrayMemberOrderedReadMatchesLinq()
    {
        using TestDatabase db = SetupJson();

        List<string> expected = JsonRows()
            .GroupBy(r => r.Payload.City)
            .Select(g => g.Key)
            .OrderBy(k => k).ToList();

        List<string> actual = db.With(() => db.Table<H20CmrJsonRow>()
                .GroupBy(r => r.Payload.City)
                .Select(g => new { g.Key, Arr = new[] { g.Key, g.Key } }))
            .OrderBy(x => x.Key)
            .Select(x => x.Key).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CteBodyNotMappedDtoMemberPlainColumnReadMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Select(r => new { r.Id, G = new H20CmrGhostDto { A = r.A } })
            .OrderBy(x => x.Id)
            .Select(x => x.Id).ToList();

        List<int> actual = db.With(() => db.Table<H20CmrRow>()
                .Select(r => new { r.Id, G = new H20CmrGhostDto { A = r.A } }))
            .OrderBy(x => x.Id)
            .Select(x => x.Id).ToList();

        Assert.Equal(expected, actual);
    }
}
