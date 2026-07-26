using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum H22gJsonRank
{
    Bronze = 1,
    Silver = 2,
    Gold = 3,
}

public class H22gJsonRankPayload
{
    public string Name { get; set; } = "";

    public H22gJsonRank? Rank { get; set; }

    public H22gJsonRank Primary { get; set; }

    public H22gJsonRank Fallback { get; set; }
}

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(H22gJsonRankPayload))]
public partial class H22gJsonRankContext : JsonSerializerContext;

[Table("H22gJsonRankRows")]
public class H22gJsonRankRow
{
    [Key]
    public int Id { get; set; }

    public H22gJsonRankPayload Data { get; set; } = new();
}

public class CoalescedJsonStringEnumNumericCastTests
{
    [Fact]
    public void NumericCastOfACoalescedMemberPairMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H22gJsonRankRow> rows);

        List<int> expected = rows.OrderBy(r => r.Id).Select(r => (int)(r.Data.Rank ?? r.Data.Fallback)).ToList();

        List<int> actual = db.Table<H22gJsonRankRow>()
            .OrderBy(r => r.Id)
            .Select(r => (int)(r.Data.Rank ?? r.Data.Fallback))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NumericCastOfAConditionalMemberPairMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H22gJsonRankRow> rows);

        List<int> expected = rows.OrderBy(r => r.Id)
            .Select(r => (int)(r.Id > 1 ? r.Data.Primary : r.Data.Fallback))
            .ToList();

        List<int> actual = db.Table<H22gJsonRankRow>()
            .OrderBy(r => r.Id)
            .Select(r => (int)(r.Id > 1 ? r.Data.Primary : r.Data.Fallback))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static TestDatabase Seed(out List<H22gJsonRankRow> rows)
    {
        TestDatabase db = new(b =>
            b.AddTypeConverter<H22gJsonRankPayload>(
                new SQLiteJsonConverter<H22gJsonRankPayload>(H22gJsonRankContext.Default.H22gJsonRankPayload)));
        db.Table<H22gJsonRankRow>().Schema.CreateTable();
        rows =
        [
            new H22gJsonRankRow
            {
                Id = 1,
                Data = new H22gJsonRankPayload { Name = "a", Rank = H22gJsonRank.Gold, Primary = H22gJsonRank.Bronze, Fallback = H22gJsonRank.Silver }
            },
            new H22gJsonRankRow
            {
                Id = 2,
                Data = new H22gJsonRankPayload { Name = "b", Rank = null, Primary = H22gJsonRank.Silver, Fallback = H22gJsonRank.Gold }
            },
            new H22gJsonRankRow
            {
                Id = 3,
                Data = new H22gJsonRankPayload { Name = "c", Rank = H22gJsonRank.Silver, Primary = H22gJsonRank.Gold, Fallback = H22gJsonRank.Bronze }
            }
        ];
        db.Table<H22gJsonRankRow>().AddRange(rows);
        return db;
    }
}
