using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class SetOperationTerminalValueRow
{
    [Key]
    public int Id { get; set; }

    public int Value { get; set; }
}

public class SetOperationTerminalParityTests
{
    [Fact]
    public void ScalarAggregatesAfterConcatMatchObjects()
    {
        SetOperationTerminalValueRow[] rows = Rows();
        using TestDatabase db = Seed(rows);
        IEnumerable<int> expected = Values(rows);
        IQueryable<int> actual = Values(db);

        Assert.Equal(expected.Count(), actual.Count());
        Assert.Equal(expected.LongCount(), actual.LongCount());
        Assert.Equal(expected.Sum(), actual.Sum());
        Assert.Equal(expected.Min(), actual.Min());
        Assert.Equal(expected.Max(), actual.Max());
        Assert.Equal(expected.Average(), actual.Average());
    }

    [Fact]
    public void PredicatesAfterConcatMatchObjects()
    {
        SetOperationTerminalValueRow[] rows = Rows();
        using TestDatabase db = Seed(rows);
        IEnumerable<int> expected = Values(rows);
        IQueryable<int> actual = Values(db);

        Assert.Equal(expected.Any(), actual.Any());
        Assert.Equal(expected.All(value => value >= -2), actual.All(value => value >= -2));
        Assert.Equal(expected.Contains(0), actual.Contains(0));
        Assert.Equal(expected.Contains(9), actual.Contains(9));
    }

    [Fact]
    public void PredicateScalarOperatorsAfterConcatMatchObjects()
    {
        SetOperationTerminalValueRow[] rows = Rows();
        using TestDatabase db = Seed(rows);
        IEnumerable<int> expected = Values(rows).OrderBy(value => value);
        IQueryable<int> actual = Values(db).OrderBy(value => value);

        Assert.Equal(expected.First(value => value >= 0), actual.First(value => value >= 0));
        Assert.Equal(expected.FirstOrDefault(value => value == 9), actual.FirstOrDefault(value => value == 9));
        Assert.Equal(expected.Single(value => value == 1), actual.Single(value => value == 1));
        Assert.Equal(expected.SingleOrDefault(value => value == 9), actual.SingleOrDefault(value => value == 9));
    }

    [Fact]
    public void ScalarOperatorDefaultValueOverloadsAfterConcatMatchObjects()
    {
        SetOperationTerminalValueRow[] rows = Rows();
        using TestDatabase db = Seed(rows);
        IEnumerable<int> expected = Values(rows).OrderBy(value => value);
        IQueryable<int> actual = Values(db).OrderBy(value => value);
        IEnumerable<int> emptyExpected = EmptyValues(rows);
        IQueryable<int> emptyActual = EmptyValues(db);

        Assert.Equal(emptyExpected.FirstOrDefault(42), emptyActual.FirstOrDefault(42));
        Assert.Equal(emptyExpected.SingleOrDefault(42), emptyActual.SingleOrDefault(42));
        Assert.Equal(expected.FirstOrDefault(value => value >= 0, 42), actual.FirstOrDefault(value => value >= 0, 42));
        Assert.Equal(expected.FirstOrDefault(value => value == 9, 42), actual.FirstOrDefault(value => value == 9, 42));
        Assert.Equal(expected.SingleOrDefault(value => value == 1, 42), actual.SingleOrDefault(value => value == 1, 42));
        Assert.Equal(expected.SingleOrDefault(value => value == 9, 42), actual.SingleOrDefault(value => value == 9, 42));
    }

    [Fact]
    public void DirectOrdersAfterConcatMatchObjects()
    {
        SetOperationTerminalValueRow[] rows = Rows();
        using TestDatabase db = Seed(rows);

        List<int> expectedDescending = Values(rows).OrderDescending().ToList();
        List<int> actualDescending = Values(db).OrderDescending().ToList();
        List<int?> expectedNullable = NullableValues(rows).Order().ToList();
        List<int?> actualNullable = NullableValues(db).Order().ToList();

        Assert.Equal(expectedDescending, actualDescending);
        Assert.Equal(expectedNullable, actualNullable);
    }

    private static IQueryable<int?> NullableValues(TestDatabase db)
    {
        return db.Table<SetOperationTerminalValueRow>().Where(row => row.Id <= 3).Select(row => (int?)row.Value)
            .Concat(db.Table<SetOperationTerminalValueRow>().Where(row => row.Id > 3).Select(row => (int?)row.Value));
    }

    private static IEnumerable<int?> NullableValues(IEnumerable<SetOperationTerminalValueRow> rows)
    {
        return rows.Where(row => row.Id <= 3).Select(row => (int?)row.Value)
            .Concat(rows.Where(row => row.Id > 3).Select(row => (int?)row.Value));
    }

    private static IQueryable<int> EmptyValues(TestDatabase db)
    {
        return db.Table<SetOperationTerminalValueRow>().Where(row => row.Id < 0).Select(row => row.Value)
            .Concat(db.Table<SetOperationTerminalValueRow>().Where(row => row.Id > 100).Select(row => row.Value));
    }

    private static IEnumerable<int> EmptyValues(IEnumerable<SetOperationTerminalValueRow> rows)
    {
        return rows.Where(row => row.Id < 0).Select(row => row.Value)
            .Concat(rows.Where(row => row.Id > 100).Select(row => row.Value));
    }

    private static SetOperationTerminalValueRow[] Rows()
    {
        return
        [
            new SetOperationTerminalValueRow { Id = 1, Value = -2 },
            new SetOperationTerminalValueRow { Id = 2, Value = -1 },
            new SetOperationTerminalValueRow { Id = 3, Value = 0 },
            new SetOperationTerminalValueRow { Id = 4, Value = 1 },
            new SetOperationTerminalValueRow { Id = 5, Value = 2 }
        ];
    }

    private static TestDatabase Seed(SetOperationTerminalValueRow[] rows)
    {
        TestDatabase db = new();
        db.Table<SetOperationTerminalValueRow>().Schema.CreateTable();
        db.Table<SetOperationTerminalValueRow>().AddRange(rows);
        return db;
    }

    private static IQueryable<int> Values(TestDatabase db)
    {
        return db.Table<SetOperationTerminalValueRow>().Where(row => row.Id <= 3).Select(row => row.Value)
            .Concat(db.Table<SetOperationTerminalValueRow>().Where(row => row.Id > 3).Select(row => row.Value));
    }

    private static IEnumerable<int> Values(IEnumerable<SetOperationTerminalValueRow> rows)
    {
        return rows.Where(row => row.Id <= 3).Select(row => row.Value)
            .Concat(rows.Where(row => row.Id > 3).Select(row => row.Value));
    }
}
