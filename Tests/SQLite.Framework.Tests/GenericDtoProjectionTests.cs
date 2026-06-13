using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GenericDtoProjectionTests
{
    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<LedgerRow>().Schema.CreateTable();
        db.Table<LedgerRow>().Add(new LedgerRow { Id = 1, Amount = 30, Note = "first" });
        db.Table<LedgerRow>().Add(new LedgerRow { Id = 2, Amount = 70, Note = "second" });
        return db;
    }

    [Fact]
    public void GenericMethodProjectsClosedGenericDto()
    {
        using TestDatabase db = Seed();

        List<Tagged<int>> rows = ProjectAmounts<int>(db);

        Assert.Equal(2, rows.Count);
        Assert.Equal(new[] { 30, 70 }, rows.Select(r => r.Value).OrderBy(v => v).ToArray());
    }

    [Fact]
    public void GenericMethodProjectsClosedGenericDtoForLong()
    {
        using TestDatabase db = Seed();

        List<Tagged<long>> rows = ProjectAmounts<long>(db);

        Assert.Equal(2, rows.Count);
        Assert.Equal(new[] { 30L, 70L }, rows.Select(r => r.Value).OrderBy(v => v).ToArray());
    }

    [Fact]
    public void GenericMethodProjectsDtoWithTypeParameterOnlyInTypeName()
    {
        using TestDatabase db = Seed();

        List<Labeled<decimal>> rows = ProjectNotes<decimal>(db);

        Assert.Equal(2, rows.Count);
        Assert.Equal(new[] { "first", "second" }, rows.Select(r => r.Label).OrderBy(v => v).ToArray());
    }

    [Fact]
    public void GenericMethodProjectsClosedGenericDtoWithClientCall()
    {
        using TestDatabase db = Seed();

        List<Tagged<int>> rows = ProjectConverted<int>(db);

        Assert.Equal(2, rows.Count);
        Assert.Equal(new[] { -1, -1 }, rows.Select(r => r.Value).ToArray());
    }

    private static List<Tagged<T>> ProjectAmounts<T>(TestDatabase db)
    {
        return db.Table<LedgerRow>().Select(r => new Tagged<T> { Value = (T)(object)r.Amount }).ToList();
    }

    private static List<Tagged<T>> ProjectConverted<T>(TestDatabase db)
    {
        return db.Table<LedgerRow>().Select(r => new Tagged<T> { Value = (T)(object)CommonHelpers.ConvertString(r.Note) }).ToList();
    }

    private static List<Labeled<T>> ProjectNotes<T>(TestDatabase db)
    {
        return db.Table<LedgerRow>().Select(r => new Labeled<T> { Label = r.Note }).ToList();
    }
}

public class LedgerRow
{
    public int Id { get; set; }

    public int Amount { get; set; }

    public string Note { get; set; } = string.Empty;
}

public class Tagged<T>
{
    public T Value { get; set; } = default!;
}

public class Labeled<T>
{
    public string Label { get; set; } = string.Empty;
}
