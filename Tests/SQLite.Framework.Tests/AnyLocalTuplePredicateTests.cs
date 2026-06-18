using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
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
    public void RangePredicateIsNotTranslated()
    {
        using TestDatabase db = CreateDb();
        List<TupleKey> list = [new TupleKey { Code = 10 }];

        Assert.Throws<NotSupportedException>(() =>
            db.Table<AnyTupleRow>().Where(a => list.Any(f => f.Code > a.Code)).ToList());
    }

    [Fact]
    public void BothSidesReferenceElementIsNotTranslated()
    {
        using TestDatabase db = CreateDb();
        List<TupleKey> list = [new TupleKey { Code = 10, Id = 10 }];

        Assert.Throws<NotSupportedException>(() =>
            db.Table<AnyTupleRow>().Where(a => list.Any(f => f.Code == f.Id)).ToList());
    }

    [Fact]
    public void ValueSideReadingColumnIsNotTranslated()
    {
        using TestDatabase db = CreateDb();
        List<TupleKey> list = [new TupleKey { Code = 10, Id = 1 }];

        Assert.Throws<NotSupportedException>(() =>
            db.Table<AnyTupleRow>().Where(a => list.Any(f => f.Code + a.Id == a.Code)).ToList());
    }

    [Fact]
    public void ValueSideArithmeticIsNotTranslated()
    {
        using TestDatabase db = CreateDb();
        List<TupleKey> list = [new TupleKey { Code = 5 }];

        Assert.Throws<NotSupportedException>(() =>
            db.Table<AnyTupleRow>().Where(a => list.Any(f => f.Code * 2 == a.Code)).ToList());
    }

    [Fact]
    public void NonColumnKeySideIsNotTranslated()
    {
        using TestDatabase db = CreateDb();
        List<TupleKey> list = [new TupleKey { Code = 11, Id = 1 }];

        Assert.Throws<NotSupportedException>(() =>
            db.Table<AnyTupleRow>().Where(a => list.Any(f => f.Code == a.Code + 1 && f.Id == a.Id)).ToList());
    }

    [Fact]
    public void MethodCallSourceIsNotTranslated()
    {
        using TestDatabase db = CreateDb();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<AnyTupleRow>().Where(a => MakeList().Any(f => f.Code == a.Code && f.Id == a.Id)).ToList());
    }

    [Fact]
    public void NullSourceIsNotTranslated()
    {
        using TestDatabase db = CreateDb();
        List<TupleKey>? list = null;

        Assert.Throws<NotSupportedException>(() =>
            db.Table<AnyTupleRow>().Where(a => list!.Any(f => f.Code == a.Code && f.Id == a.Id)).ToList());
    }

    private static List<TupleKey> MakeList() => [new TupleKey { Code = 10, Id = 1 }];

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
