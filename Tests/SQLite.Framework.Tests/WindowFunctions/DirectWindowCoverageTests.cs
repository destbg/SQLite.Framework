using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;
using SQLite.Framework.Extensions;
using SQLite.Framework.Internals;
using SQLite.Framework.Internals.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DirectWindowCoverageRow
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }

    public List<int> Values { get; set; } = [];
}

public class DirectWindowCoveragePart
{
    public int Value { get; set; }

    public DayOfWeek Day { get; set; }

    public List<int> Values { get; set; } = [];
}

public class DirectWindowCoverageDto
{
    public int Id { get; set; }

    public DirectWindowCoveragePart Part { get; set; } = new();
}

[JsonSerializable(typeof(List<int>))]
internal partial class DirectWindowCoverageJsonContext : JsonSerializerContext;

public class DirectWindowCoverageTests
{
    [Fact]
    public void CompositeWindowGroupKeyProjectsDayOfWeekAndJsonValues()
    {
        using TestDatabase db = Setup(nameof(CompositeWindowGroupKeyProjectsDayOfWeekAndJsonValues));

        SQLiteCommand command = db.Table<DirectWindowCoverageRow>()
            .GroupBy(row => new
            {
                Rank = SQLiteWindowFunctions.DenseRank().OrderBy(row.Id).AsValue(),
                Day = row.When.DayOfWeek,
                Values = row.Values.Where(value => value > 0).ToList()
            })
            .Select(group => group.Count())
            .ToSqlCommand();
        SQLiteCommand positional = db.Table<DirectWindowCoverageRow>()
            .GroupBy(row => new ValueTuple<long, int>(
                SQLiteWindowFunctions.DenseRank().OrderBy(row.Id).AsValue(),
                row.Id))
            .Select(group => group.Count())
            .ToSqlCommand();

        Assert.Contains("DENSE_RANK()", command.CommandText, StringComparison.Ordinal);
        Assert.Contains("__WindowValue.Day", command.CommandText, StringComparison.Ordinal);
        Assert.Contains("__WindowValue.Values", command.CommandText, StringComparison.Ordinal);
        Assert.Contains("__WindowValue.Item1", positional.CommandText, StringComparison.Ordinal);
    }

    [Fact]
    public void JsonCollectionScalarSourceKeepsItsStorageMarker()
    {
        using TestDatabase db = Setup(nameof(JsonCollectionScalarSourceKeepsItsStorageMarker));

        SQLiteCommand command = db.Table<DirectWindowCoverageRow>()
            .Select(row => row.Values.Where(value => value > 0).ToList())
            .Where(values => SQLiteWindowFunctions.RowNumber().OrderBy(values.Count).AsValue() <= 1)
            .ToSqlCommand();

        Assert.Contains("json_group_array", command.CommandText, StringComparison.Ordinal);
    }

    [Fact]
    public void FlatProjectionMarkersSurviveDirectWindowWrap()
    {
        using TestDatabase db = Setup(nameof(FlatProjectionMarkersSurviveDirectWindowWrap));

        SQLiteCommand flat = db.Table<DirectWindowCoverageRow>()
            .Select(row => new
            {
                Day = row.When.DayOfWeek,
                row.Values,
                row.Id
            })
            .Where(row => SQLiteWindowFunctions.RowNumber().OrderBy(row.Id).AsValue() <= 1)
            .ToSqlCommand();

        Assert.Contains("Day", flat.CommandText, StringComparison.Ordinal);
        Assert.Contains("Values", flat.CommandText, StringComparison.Ordinal);
    }

    [Fact]
    public void ExistingWindowAliasGetsAUniquelyNamedProjection()
    {
        using TestDatabase db = Setup(nameof(ExistingWindowAliasGetsAUniquelyNamedProjection));

        SQLiteCommand command = db.Table<DirectWindowCoverageRow>()
            .Select(row => new { __WindowValue = row.Id, row.When })
            .Where(row => SQLiteWindowFunctions.RowNumber().OrderBy(row.When).AsValue() <= 1)
            .ToSqlCommand();

        Assert.Contains("__WindowValue_", command.CommandText, StringComparison.Ordinal);
    }

    [Fact]
    public void DayOfWeekScalarSourceKeepsItsStorageMarker()
    {
        using TestDatabase db = Setup(nameof(DayOfWeekScalarSourceKeepsItsStorageMarker));

        DayOfWeek result = db.Table<DirectWindowCoverageRow>()
            .Select(row => row.When.DayOfWeek)
            .First(day => SQLiteWindowFunctions.RowNumber().OrderBy(day).AsValue() <= 1);

        Assert.Equal(DayOfWeek.Tuesday, result);
    }

    [Fact]
    public void ClientProjectionBeforeDirectWindowPredicateIsRejected()
    {
        using TestDatabase db = Setup(nameof(ClientProjectionBeforeDirectWindowPredicateIsRejected));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => db.Table<DirectWindowCoverageRow>()
            .Select(row => CmcClientFns.Pass(row.Id))
            .Where(value => SQLiteWindowFunctions.RowNumber().OrderBy(value).AsValue() <= 1)
            .ToList());

        Assert.Contains("projection that runs in memory", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReverseBeforeDirectWindowPredicateIsRejected()
    {
        using TestDatabase db = Setup(nameof(ReverseBeforeDirectWindowPredicateIsRejected));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => db.Table<DirectWindowCoverageRow>()
            .OrderBy(row => row.Id)
            .Reverse()
            .Where(row => SQLiteWindowFunctions.RowNumber().OrderBy(row.Id).AsValue() <= 1)
            .ToList());

        Assert.Contains("Reverse cannot run before Where", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ConstructedNestedSourceSurvivesDirectWindowWrap()
    {
        using TestDatabase db = Setup(nameof(ConstructedNestedSourceSurvivesDirectWindowWrap));

        List<int> values = db.Table<DirectWindowCoverageRow>()
            .Select(row => new DirectWindowCoverageDto
            {
                Id = row.Id,
                Part = new DirectWindowCoveragePart
                {
                    Value = row.Id + 10,
                    Day = row.When.DayOfWeek,
                    Values = row.Values
                }
            })
            .Where(row => SQLiteWindowFunctions.RowNumber().OrderBy(row.Id).AsValue() <= 1)
            .Select(row => row.Part.Value)
            .ToList();

        Assert.Equal([11], values);
    }

    [Fact]
    public void OptionalRowsSurviveDirectWindowWrap()
    {
        using TestDatabase db = Setup(nameof(OptionalRowsSurviveDirectWindowWrap));

        IQueryable<DirectWindowCoverageRow?> source =
            from left in db.Table<DirectWindowCoverageRow>()
            join right in db.Table<DirectWindowCoverageRow>() on left.Id equals right.Id + 100 into matches
            from right in matches.DefaultIfEmpty()
            select right;

        List<DirectWindowCoverageRow?> rows = source
            .Where(row => SQLiteWindowFunctions.RowNumber().OrderBy(row!.Id).AsValue() <= 1)
            .ToList();

        Assert.Single(rows);
        Assert.Null(rows[0]);

        var nestedSource =
            from left in db.Table<DirectWindowCoverageRow>()
            join right in db.Table<DirectWindowCoverageRow>() on left.Id equals right.Id + 100 into matches
            from right in matches.DefaultIfEmpty()
            select new { left.Id, Right = right };
        SQLiteCommand command = nestedSource
            .Where(row => SQLiteWindowFunctions.RowNumber().OrderBy(1).AsValue() <= 1)
            .ToSqlCommand();
        Assert.Contains("__WindowValue", command.CommandText, StringComparison.Ordinal);
    }

    [Fact]
    public void DirectWindowProjectionRejectsANonSqlExpression()
    {
        using TestDatabase db = Setup(nameof(DirectWindowProjectionRejectsANonSqlExpression));
        SQLTranslator translator = new(db);
        SQLTranslator innerTranslator = new(db);
        MethodInfo method = typeof(SQLTranslator).GetMethod(
            "ProjectDirectWindowValue",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() =>
            method.Invoke(translator, [innerTranslator, Expression.Constant(1), "Value", "v0"]));

        Assert.IsType<NotSupportedException>(exception.InnerException);
    }

    [Fact]
    public void DirectWindowMappingRejectsAnUnflattenedNestedConstructor()
    {
        using TestDatabase db = new(null, nameof(DirectWindowMappingRejectsAnUnflattenedNestedConstructor));
        db.Table<H26qConstructorMemberRow>().Schema.CreateTable();
        IQueryable<H26qConstructorMemberOuter> source = db.Table<H26qConstructorMemberRow>()
            .Select(row => new H26qConstructorMemberOuter(new H26qConstructorMemberChild { Value = row.Value }));
        SQLTranslator innerTranslator = new(db);
        innerTranslator.Visit(source.Expression);
        SQLTranslator translator = new(db);
        MethodInfo method = typeof(SQLTranslator).GetMethod(
            "MapDirectWindowSourceColumns",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() =>
            method.Invoke(translator, [innerTranslator, innerTranslator.Selects.Count, typeof(H26qConstructorMemberOuter), "h9"]));

        Assert.IsType<NotSupportedException>(exception.InnerException);
    }

    [Fact]
    public void DirectWindowMappingCarriesNestedAndFlatJsonMarkers()
    {
        using TestDatabase db = Setup(nameof(DirectWindowMappingCarriesNestedAndFlatJsonMarkers));
        MethodInfo method = typeof(SQLTranslator).GetMethod(
            "MapDirectWindowSourceColumns",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        IQueryable<DirectWindowCoverageDto> nestedSource = db.Table<DirectWindowCoverageRow>()
            .Select(row => new DirectWindowCoverageDto
            {
                Id = row.Id,
                Part = new DirectWindowCoveragePart
                {
                    Value = row.Id,
                    Day = row.When.DayOfWeek,
                    Values = row.Values
                }
            });
        SQLTranslator nestedInner = new(db);
        nestedInner.Visit(nestedSource.Expression);
        SQLiteExpression nestedJson = Assert.Single(nestedInner.Selects, select => select.IdentifierText == "Part.Values");
        nestedJson.WithJsonSource();
        method.Invoke(new SQLTranslator(db), [nestedInner, nestedInner.Selects.Count, nestedSource.ElementType, "n9"]);

        var flatSource = db.Table<DirectWindowCoverageRow>()
            .Select(row => new { row.Id, row.Values });
        SQLTranslator flatInner = new(db);
        flatInner.Visit(flatSource.Expression);
        SQLiteExpression flatJson = Assert.Single(flatInner.Selects, select => select.IdentifierText == "Values");
        flatJson.WithJsonSource();
        method.Invoke(new SQLTranslator(db), [flatInner, flatInner.Selects.Count, flatSource.ElementType, "f9"]);
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(builder => builder.TypeConverters[typeof(List<int>)] =
            new SQLiteJsonConverter<List<int>>(DirectWindowCoverageJsonContext.Default.ListInt32), methodName);
        db.Table<DirectWindowCoverageRow>().Schema.CreateTable();
        db.Table<DirectWindowCoverageRow>().AddRange(
        [
            new DirectWindowCoverageRow
            {
                Id = 1,
                When = new DateTime(2026, 9, 1),
                Values = [1, 2]
            },
            new DirectWindowCoverageRow
            {
                Id = 2,
                When = new DateTime(2026, 9, 2),
                Values = [3]
            }
        ]);
        return db;
    }
}
