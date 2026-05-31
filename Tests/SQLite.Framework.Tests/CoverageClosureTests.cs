using SQLite.Framework.Extensions;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class OverriddenAddBookTable : SQLiteTable<Book>
{
    public OverriddenAddBookTable(SQLiteDatabase database, TableMapping table) : base(database, table)
    {
    }

    protected override (TableColumn[] Columns, string Sql) GetAddInfo()
    {
        return base.GetAddInfo();
    }
}

public class CoverageClosureTests
{
    [Fact]
    public void SingleAfterTakeReturnsTheSingleMatch()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 1, Price = 2 });

        Book result = db.Table<Book>().Take(5).Single(b => b.Id == 2);

        Assert.Equal(2, result.Id);
    }

    [Fact]
    public void BitwiseOrInsideAndIsBracketed()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();
        db.Table<SoftDeletableBook>().Add(new SoftDeletableBook { Id = 1, Title = "a", IsDeleted = true });

        List<int> ids = db.Table<SoftDeletableBook>()
            .Where(b => (b.IsDeleted | b.IsDeleted) && b.IsDeleted)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal(new[] { 1 }, ids);
    }

    [Fact]
    public void LogicalOrInsideAndIsBracketed()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();
        db.Table<SoftDeletableBook>().Add(new SoftDeletableBook { Id = 1, Title = "a", IsDeleted = true });

        List<int> ids = db.Table<SoftDeletableBook>()
            .Where(b => (b.IsDeleted || b.Id > 0) && b.Id < 100)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal(new[] { 1 }, ids);
    }

    [Fact]
    public void NotOfBitwiseAndIsBracketed()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();
        db.Table<SoftDeletableBook>().Add(new SoftDeletableBook { Id = 1, Title = "a", IsDeleted = false });

        List<int> ids = db.Table<SoftDeletableBook>()
            .Where(b => !(b.IsDeleted & b.IsDeleted))
            .Select(b => b.Id)
            .ToList();

        Assert.Equal(new[] { 1 }, ids);
    }

    [Fact]
    public void NotOfBitwiseOrIsBracketed()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();
        db.Table<SoftDeletableBook>().Add(new SoftDeletableBook { Id = 1, Title = "a", IsDeleted = false });

        List<int> ids = db.Table<SoftDeletableBook>()
            .Where(b => !(b.IsDeleted | b.IsDeleted))
            .Select(b => b.Id)
            .ToList();

        Assert.Equal(new[] { 1 }, ids);
    }

    [Fact]
    public void NotOfLogicalOrIsBracketed()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();
        db.Table<SoftDeletableBook>().Add(new SoftDeletableBook { Id = 1, Title = "a", IsDeleted = false });

        List<int> ids = db.Table<SoftDeletableBook>()
            .Where(b => !(b.IsDeleted || b.Id > 100))
            .Select(b => b.Id)
            .ToList();

        Assert.Equal(new[] { 1 }, ids);
    }

    [Fact]
    public void NotOfLogicalAndIsBracketed()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();
        db.Table<SoftDeletableBook>().Add(new SoftDeletableBook { Id = 1, Title = "a", IsDeleted = false });

        List<int> ids = db.Table<SoftDeletableBook>()
            .Where(b => !(b.IsDeleted && b.Id > 0))
            .Select(b => b.Id)
            .ToList();

        Assert.Equal(new[] { 1 }, ids);
    }

    [Fact]
    public void ProjectionWithConstantBooleanMember()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });

        var rows = db.Table<Book>().Select(b => new { b.Id, Flag = true }).ToList();

        Assert.Single(rows);
        Assert.True(rows[0].Flag);
    }

    [Fact]
    public void ReadDateTimeOffsetFromTextUnderTicksStorage()
    {
        using TestDatabase db = new();

        DateTimeOffset value = db.CreateCommand("SELECT '2020-01-02 03:04:05 +00:00'", [])
            .ExecuteQuery<DateTimeOffset>()
            .First();

        Assert.Equal(new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero), value);
    }

    [Fact]
    public void ReturningAddOnTableOverridingGetAddInfoSkipsFiltering()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        OverriddenAddBookTable table = new(db, db.TableMapping<Book>());
        Book? inserted = table.Returning().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        Assert.NotNull(inserted);
        Assert.Equal(1, inserted.Id);
        Assert.Single(db.Table<Book>().ToList());
    }

    [Fact]
    public void InsertFromQueryWholeEntityMapsByColumn()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });

        int inserted = db.Table<Book>().InsertFromQuery(db.Table<Book>().Where(b => b.Id < 0));

        Assert.Equal(0, inserted);
    }

    [Fact]
    public void AllWithUntranslatablePredicateThrows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().All(b => (object)b.Title is int));
    }

#if !SQLITE_FRAMEWORK_SOURCE_GENERATOR && !SQLITE_FRAMEWORK_REFLECTION_AOT_INCOMPATIBLE
    [Fact]
    public void GroupByCastKeyConvertsNonNullValue()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 7, Price = 1 });

        List<IGrouping<long, Book>> groups = db.Table<Book>().GroupBy(b => (long)b.AuthorId).ToList();

        Assert.Single(groups);
        Assert.Equal(7L, groups[0].Key);
    }

    [Fact]
    public void GroupByNullableKeyGroupsNullsTogether()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();
        db.CreateCommand("INSERT INTO NullableEntity (\"Id\", \"Value\") VALUES (1, NULL)", []).ExecuteNonQuery();
        db.CreateCommand("INSERT INTO NullableEntity (\"Id\", \"Value\") VALUES (2, NULL)", []).ExecuteNonQuery();
        db.CreateCommand("INSERT INTO NullableEntity (\"Id\", \"Value\") VALUES (3, 5)", []).ExecuteNonQuery();

        List<IGrouping<long?, NullableEntity>> groups =
            db.Table<NullableEntity>().GroupBy(b => (long?)b.Value).ToList();

        Assert.Equal(2, groups.Count);
        Assert.Equal(2, groups.Single(g => g.Key == null).Count());
        Assert.Equal(new[] { 3 }, groups.Single(g => g.Key == 5L).Select(e => e.Id).ToArray());
    }
#endif
}
