using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("QcvRow")]
public class QcvRow
{
    [Key]
    public int Id { get; set; }

    public string Label { get; set; } = "";

    public DateTime When { get; set; }

    public int[] Codes { get; set; } = [];

    public List<int> Numbers { get; set; } = [];
}

public class QcvPosDto
{
    public QcvPosDto(string label)
    {
        Label = label;
    }

    public string Label { get; set; }
}

public class QcvDowPart
{
    public DayOfWeek Dow { get; set; }

    public int Id { get; set; }
}

[JsonSerializable(typeof(int[]))]
[JsonSerializable(typeof(List<int>))]
internal partial class QcvContext : JsonSerializerContext;

public class QueryCompositionVariantTests
{
    private static List<QcvRow> Rows() =>
    [
        new QcvRow { Id = 1, Label = "one", When = new DateTime(2024, 1, 1), Codes = [3, 1], Numbers = [4, 9] },
        new QcvRow { Id = 2, Label = "two", When = new DateTime(2024, 1, 2), Codes = [7], Numbers = [2] },
    ];

    private static TestDatabase Setup(EnumStorageMode enumStorage = EnumStorageMode.Integer)
    {
        TestDatabase db = new(b =>
        {
            b.UseEnumStorage(enumStorage);
            b.TypeConverters[typeof(int[])] = new SQLiteJsonConverter<int[]>(QcvContext.Default.Int32Array);
            b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(QcvContext.Default.ListInt32);
        });
        db.Table<QcvRow>().Schema.CreateTable();
        db.Table<QcvRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void SelectManyOverJsonArrayColumnAtQueryLevelThrows()
    {
        using TestDatabase db = Setup();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<QcvRow>().SelectMany(r => r.Codes).OrderBy(x => x).ToList());

        Assert.Equal("SelectMany over the JSON collection column 'Codes' is not supported at the query level.", ex.Message);
    }

    [Fact]
    public void InlineListLiteralAggregateInPredicateMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Select(r => r.Numbers.Where(x => new List<int> { x, 5 }.Max() > 5).ToList())
            .First(l => l.Count >= 0);
        List<int> actual = db.Table<QcvRow>()
            .Select(r => r.Numbers.Where(x => new List<int> { x, 5 }.Max() > 5).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CountWindowOverDayOfWeekRowsMatchesLinq()
    {
        using TestDatabase db = Setup(EnumStorageMode.Text);

        List<long> expected = Rows().Select(r => (long)Rows().Count).ToList();
        List<long> actual = db.Table<QcvRow>()
            .Select(r => SQLiteWindowFunctions.Count().Over().AsValue())
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InMemoryNestedDtoReadAfterTakeThrows()
    {
        using TestDatabase db = Setup(EnumStorageMode.Text);

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => db.Table<QcvRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { Part = new QcvDowPart { Dow = r.When.DayOfWeek, Id = r.Id } })
            .Take(2)
            .Where(x => x.Part.Dow == DayOfWeek.Monday)
            .Select(x => x.Part.Dow)
            .ToList());

        Assert.Equal(
            "Reading the nested projected object 'Part' after Take, Skip or Distinct is not supported " +
            "when the object is built in memory. Read the member before paging or project the columns you need.",
            ex.Message);
    }

    [Fact]
    public void DayOfWeekDottedColumnSurvivesSubqueryWrap()
    {
        using TestDatabase db = Setup(EnumStorageMode.Text);

        List<DayOfWeek> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { Dow = r.When.DayOfWeek, r.Id })
            .Select(x => new { Part = x })
            .Take(2)
            .Where(y => y.Part.Dow == DayOfWeek.Monday)
            .Select(y => y.Part.Dow)
            .ToList();

        List<DayOfWeek> actual = db.Table<QcvRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { Dow = r.When.DayOfWeek, r.Id })
            .Select(x => new { Part = x })
            .Take(2)
            .Where(y => y.Part.Dow == DayOfWeek.Monday)
            .Select(y => y.Part.Dow)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JsonListContainsColumnArgumentMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows().Where(r => r.Numbers.Contains(r.Id + 1)).Select(r => r.Id).ToList();
        List<int> actual = db.Table<QcvRow>().Where(r => r.Numbers.Contains(r.Id + 1)).Select(r => r.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ClientCallOverPositionalDtoMemberChainMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new QcvPosDto(r.Label))
            .Select(x => CmcClientFns.Pass(x.Label.Length))
            .ToList();

        List<int> actual = db.Table<QcvRow>()
            .OrderBy(r => r.Id)
            .Select(r => new QcvPosDto(r.Label))
            .Select(x => CmcClientFns.Pass(x.Label.Length))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
