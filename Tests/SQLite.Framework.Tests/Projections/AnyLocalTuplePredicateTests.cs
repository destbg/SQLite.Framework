using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class AnyTupleRow
{
    [Key]
    public int Id { get; set; }

    public int Code { get; set; }

    public string Name { get; set; } = "";

    public string? Tag { get; set; }
}

internal sealed class TupleKey
{
    public int Code { get; set; }

    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string? Tag { get; set; }
}

public class AnyLocalTuplePredicateTests
{
    private static int sourceCalls;

    private static readonly AnyTupleRow[] Data =
    [
        new AnyTupleRow { Id = 1, Code = 10, Name = "a", Tag = "x" },
        new AnyTupleRow { Id = 2, Code = 20, Name = "b", Tag = null },
        new AnyTupleRow { Id = 3, Code = 10, Name = "c", Tag = "y" },
        new AnyTupleRow { Id = 4, Code = 30, Name = "a", Tag = null },
    ];

    [Fact]
    public void MultiColumnNonNullableComposite()
    {
        List<TupleKey> list =
        [
            new TupleKey { Code = 10, Id = 1 },
            new TupleKey { Code = 10, Id = 3 },
            new TupleKey { Code = 99, Id = 99 },
        ];

        AssertSameIds(
            a => list.Any(f => f.Code == a.Code && f.Id == a.Id),
            a => list.Any(f => f.Code == a.Code && f.Id == a.Id),
            [1, 3]);
    }

    [Fact]
    public void MultiColumnWithStringColumnComposite()
    {
        List<TupleKey> list =
        [
            new TupleKey { Code = 10, Name = "a" },
            new TupleKey { Code = 30, Name = "a" },
        ];

        AssertSameIds(
            a => list.Any(f => f.Code == a.Code && f.Name == a.Name),
            a => list.Any(f => f.Code == a.Code && f.Name == a.Name),
            [1, 4]);
    }

    [Fact]
    public void MultiColumnNullValueMatchesNullColumn()
    {
        List<TupleKey> list =
        [
            new TupleKey { Code = 20, Tag = null },
            new TupleKey { Code = 10, Tag = "x" },
        ];

        AssertSameIds(
            a => list.Any(f => f.Code == a.Code && f.Tag == a.Tag),
            a => list.Any(f => f.Code == a.Code && f.Tag == a.Tag),
            [1, 2]);
    }

    [Fact]
    public void MultiColumnAllNullValueRows()
    {
        List<TupleKey> list =
        [
            new TupleKey { Code = 20, Tag = null },
            new TupleKey { Code = 30, Tag = null },
        ];

        AssertSameIds(
            a => list.Any(f => f.Code == a.Code && f.Tag == a.Tag),
            a => list.Any(f => f.Code == a.Code && f.Tag == a.Tag),
            [2, 4]);
    }

    [Fact]
    public void KeyOnLeftValueOnRight()
    {
        List<TupleKey> list =
        [
            new TupleKey { Code = 10, Id = 1 },
        ];

        AssertSameIds(
            a => list.Any(f => a.Code == f.Code && a.Id == f.Id),
            a => list.Any(f => a.Code == f.Code && a.Id == f.Id),
            [1]);
    }

    [Fact]
    public void SingleColumnAny()
    {
        List<TupleKey> list =
        [
            new TupleKey { Code = 10 },
            new TupleKey { Code = 30 },
        ];

        AssertSameIds(
            a => list.Any(f => f.Code == a.Code),
            a => list.Any(f => f.Code == a.Code),
            [1, 3, 4]);
    }

    [Fact]
    public void SingleColumnAnyWithNullValue()
    {
        List<TupleKey> list =
        [
            new TupleKey { Tag = "x" },
            new TupleKey { Tag = null },
        ];

        AssertSameIds(
            a => list.Any(f => f.Tag == a.Tag),
            a => list.Any(f => f.Tag == a.Tag),
            [1, 2, 4]);
    }

    [Fact]
    public void NullLocalElementShortCircuitsTheMemberComparison()
    {
        List<TupleKey?> list = [null, new TupleKey { Code = 99 }];

        AssertSameIds(
            a => list.Any(f => f == null || f.Code == a.Code),
            a => list.Any(f => f == null || f.Code == a.Code),
            [1, 2, 3, 4]);
    }

    [Fact]
    public void EmptyListMultiColumn()
    {
        List<TupleKey> list = [];

        AssertSameIds(
            a => list.Any(f => f.Code == a.Code && f.Id == a.Id),
            a => list.Any(f => f.Code == a.Code && f.Id == a.Id),
            []);
    }

    [Fact]
    public void EmptyListSingleColumn()
    {
        List<TupleKey> list = [];

        AssertSameIds(
            a => list.Any(f => f.Code == a.Code),
            a => list.Any(f => f.Code == a.Code),
            []);
    }

#if !SQLITE_FRAMEWORK_BUNDLED && !SQLITECIPHER && !NO_SQLITEPCL_RAW_BATTERIES
    [Fact]
    public void MultiColumnBelowRowValueVersionUsesNullSafeEquality()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLite.Framework.Enums.SQLiteMinimumVersion.V3_14));
        db.Table<AnyTupleRow>().Schema.CreateTable();
        db.Table<AnyTupleRow>().AddRange(Data);

        List<TupleKey> list =
        [
            new TupleKey { Code = 10, Id = 1 },
            new TupleKey { Code = 10, Id = 3 },
        ];

        List<int> oracle = Data
            .Where(a => list.Any(f => f.Code == a.Code && f.Id == a.Id))
            .Select(r => r.Id)
            .OrderBy(i => i)
            .ToList();
        List<int> actual = db.Table<AnyTupleRow>()
            .Where(a => list.Any(f => f.Code == a.Code && f.Id == a.Id))
            .Select(r => r.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal([1, 3], oracle);
        Assert.Equal(oracle, actual);
    }
#endif

    [Fact]
    public void NoPredicateAnyOverNonEmptyListMatchesEveryRow()
    {
        using TestDatabase db = CreateDb();
        List<TupleKey> list = [new TupleKey { Code = 1 }];

        List<int> oracle = Data.Where(_ => list.Any()).Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<AnyTupleRow>().Where(_ => list.Any()).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal([1, 2, 3, 4], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void NoPredicateAnyOverEmptyListMatchesNoRow()
    {
        using TestDatabase db = CreateDb();
        List<TupleKey> list = [];

        List<int> oracle = Data.Where(_ => list.Any()).Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<AnyTupleRow>().Where(_ => list.Any()).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal([], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void EnumerableContainsStillWorks()
    {
        using TestDatabase db = CreateDb();
        IEnumerable<int> codes = new[] { 10, 30 };

        List<int> oracle = Data.Where(a => codes.Contains(a.Code)).Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<AnyTupleRow>().Where(a => codes.Contains(a.Code)).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal([1, 3, 4], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void FuncVariablePredicateFallsBackToConstant()
    {
        using TestDatabase db = CreateDb();
        List<TupleKey> list = [new TupleKey { Code = 10 }];
        Func<TupleKey, bool> predicate = f => f.Code == 10;

        List<int> oracle = Data.Where(_ => list.Any(predicate)).Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<AnyTupleRow>().Where(_ => list.Any(predicate)).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal([1, 2, 3, 4], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void RangePredicateUsesEveryLocalValue()
    {
        List<TupleKey> list = [new TupleKey { Code = 25 }];

        AssertSameIds(
            a => list.Any(f => f.Code > a.Code),
            a => list.Any(f => f.Code > a.Code),
            [1, 2, 3]);
    }

    [Fact]
    public void PredicateThatOnlyReadsTheLocalElementMatchesEveryRow()
    {
        List<TupleKey> list = [new TupleKey { Code = 10, Id = 10 }];

        AssertSameIds(
            a => list.Any(f => f.Code == f.Id),
            a => list.Any(f => f.Code == f.Id),
            [1, 2, 3, 4]);
    }

    [Fact]
    public void PredicateCanMixTheLocalValueAndRowColumns()
    {
        List<TupleKey> list =
        [
            new TupleKey { Code = 9 },
            new TupleKey { Code = 18 },
        ];

        AssertSameIds(
            a => list.Any(f => f.Code + a.Id == a.Code),
            a => list.Any(f => f.Code + a.Id == a.Code),
            [1, 2]);
    }

    [Fact]
    public void ClientPredicateInAProjectionMatchesLinq()
    {
        using TestDatabase db = CreateDb();
        List<TupleKey> list = [new TupleKey { Code = 10 }, new TupleKey { Code = 30 }];

        List<bool> expected = Data
            .OrderBy(a => a.Id)
            .Select(a => list.Any(f => ClientCode(f.Code) == a.Code))
            .ToList();
        List<bool> actual = db.Table<AnyTupleRow>()
            .OrderBy(a => a.Id)
            .Select(a => list.Any(f => ClientCode(f.Code) == a.Code))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LocalValueArithmeticIsTranslated()
    {
        List<TupleKey> list = [new TupleKey { Code = 5 }];

        AssertSameIds(
            a => list.Any(f => f.Code * 2 == a.Code),
            a => list.Any(f => f.Code * 2 == a.Code),
            [1, 3]);
    }

    [Fact]
    public void CheckedLocalValueConversionIsTranslated()
    {
        List<TupleKey> list = [new TupleKey { Code = 10 }];

        AssertSameIds(
            a => list.Any(f => checked((short)f.Code) == checked((short)a.Code)),
            a => list.Any(f => checked((short)f.Code) == checked((short)a.Code)),
            [1, 3]);
    }

    [Fact]
    public void ComputedRowKeyIsTranslated()
    {
        List<TupleKey> list = [new TupleKey { Code = 11, Id = 1 }];

        AssertSameIds(
            a => list.Any(f => f.Code == a.Code + 1 && f.Id == a.Id),
            a => list.Any(f => f.Code == a.Code + 1 && f.Id == a.Id),
            [1]);
    }

    [Fact]
    public void MethodCallSourceIsEvaluatedOnce()
    {
        using TestDatabase db = CreateDb();
        List<TupleKey> expectedSource = MakeList();
        List<int> expected = Data
            .Where(a => expectedSource.Any(f => f.Code == a.Code && f.Id == a.Id))
            .Select(a => a.Id)
            .ToList();
        sourceCalls = 0;

        List<int> actual = db.Table<AnyTupleRow>()
            .Where(a => MakeList().Any(f => f.Code == a.Code && f.Id == a.Id))
            .Select(a => a.Id)
            .ToList();

        Assert.Equal(expected, actual);
        Assert.Equal(1, sourceCalls);
    }

    [Fact]
    public void NullSourceThrowsTheLinqException()
    {
        using TestDatabase db = CreateDb();
        List<TupleKey>? list = null;

        Assert.Throws<ArgumentNullException>(() => Data
            .Where(a => list!.Any(f => f.Code == a.Code && f.Id == a.Id))
            .ToList());
        Assert.Throws<ArgumentNullException>(() =>
            db.Table<AnyTupleRow>().Where(a => list!.Any(f => f.Code == a.Code && f.Id == a.Id)).ToList());
    }

    [Fact]
    public void NullableScalarValuesKeepNullSemantics()
    {
        List<int?> values = [null, 20, 20];

        AssertSameIds(
            a => values.Any(v => v == a.Code),
            a => values.Any(v => v == a.Code),
            [2]);
    }

    [Fact]
    public void DuplicateValuesAreCounted()
    {
        using TestDatabase db = CreateDb();
        List<int> values = [10, 10, 30];

        List<int> oracle = Data.Where(a => values.Count(v => v == a.Code) == 2).Select(a => a.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<AnyTupleRow>()
            .Where(a => values.Count(v => v == a.Code) == 2)
            .Select(a => a.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal([1, 3], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void AnyPredicateUsesAValuesSourceInSql()
    {
        using TestDatabase db = new();
        List<TupleKey> list = [new TupleKey { Code = 10 }, new TupleKey { Code = 30 }];

        SQLiteCommand command = db.Table<AnyTupleRow>()
            .Where(a => list.Any(f => f.Code * 2 > a.Code))
            .ToSqlCommand();

        Assert.Equal(
            "SELECT a0.\"Id\" AS \"Id\",\n       a0.\"Code\" AS \"Code\",\n       a0.\"Name\" AS \"Name\",\n       a0.\"Tag\" AS \"Tag\"\nFROM \"AnyTupleRow\" AS a0\nWHERE EXISTS (SELECT 1 FROM (VALUES (@p1), (@p2)) AS l1 WHERE (l1.\"column1\" * @p0) > a0.\"Code\")",
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void LargeLocalCollectionKeepsTheParameterCountBounded()
    {
        using TestDatabase db = CreateDb();
        List<int> values = Enumerable.Range(0, 1100).ToList();

        List<int> expected = Data.Where(a => values.Any(v => v == a.Code)).Select(a => a.Id).OrderBy(i => i).ToList();
        IQueryable<AnyTupleRow> query = db.Table<AnyTupleRow>().Where(a => values.Any(v => v == a.Code));
        SQLiteCommand command = query.ToSqlCommand();
        List<int> actual = query.Select(a => a.Id).OrderBy(i => i).ToList();

        Assert.Equal([1, 2, 3, 4], expected);
        Assert.Equal(expected, actual);
        Assert.Equal(500, command.Parameters.Count);
    }

    [Fact]
    public void PredicateThatOnlyReadsTheRowKeepsAnyAndCountSemantics()
    {
        using TestDatabase db = CreateDb();
        List<int> values = [1, 2, 3];

        List<int> expected = Data
            .Where(a => values.Any(_ => a.Code == 10) && values.Count(_ => a.Id <= 3) == 3)
            .Select(a => a.Id)
            .OrderBy(i => i)
            .ToList();
        List<int> actual = db.Table<AnyTupleRow>()
            .Where(a => values.Any(_ => a.Code == 10) && values.Count(_ => a.Id <= 3) == 3)
            .Select(a => a.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal([1, 3], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WholeObjectReferenceComparisonRemainsUnsupported()
    {
        using TestDatabase db = CreateDb();
        TupleKey target = new() { Code = 10 };
        List<TupleKey> values = [target];

        Assert.Throws<NotSupportedException>(() => db.Table<AnyTupleRow>()
            .Where(_ => values.Any(value => value == target))
            .ToList());
    }

    private static int ClientCode(int value)
    {
        return value;
    }

    private static List<TupleKey> MakeList()
    {
        sourceCalls++;
        return [new TupleKey { Code = 10, Id = 1 }];
    }

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new();
        db.Table<AnyTupleRow>().Schema.CreateTable();
        db.Table<AnyTupleRow>().AddRange(Data);
        return db;
    }

    private static void AssertSameIds(Func<AnyTupleRow, bool> oracle, Expression<Func<AnyTupleRow, bool>> query, int[] expected)
    {
        using TestDatabase db = CreateDb();

        List<int> oracleIds = Data.Where(oracle).Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actualIds = db.Table<AnyTupleRow>().Where(query).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, oracleIds);
        Assert.Equal(oracleIds, actualIds);
    }
}
