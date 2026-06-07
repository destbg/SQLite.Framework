using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CovFkParents")]
file sealed class CovFkParent
{
    [Key]
    public int Id { get; set; }
}

[Table("CovFkChildren")]
file sealed class CovFkChild
{
    [Key]
    public int Id { get; set; }

    public int ParentId { get; set; }
}

[Table("CovFkNullableChildren")]
file sealed class CovFkNullableChild
{
    [Key]
    public int Id { get; set; }

    public int? ParentId { get; set; }
}

public class SupplementalBehaviorTests2
{
    [Fact]
    public void ForeignKeyOnUpdateMismatchIsReported()
    {
        using ModelTestDatabase db = new(model =>
        {
            model.Entity<CovFkParent>().HasKey(p => p.Id);
            model.Entity<CovFkChild>().ForeignKey<CovFkParent>(c => c.ParentId, onUpdate: SQLiteForeignKeyAction.Cascade);
        });
        db.Execute("CREATE TABLE \"CovFkParents\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Execute("CREATE TABLE \"CovFkChildren\" (\"Id\" INTEGER PRIMARY KEY, \"ParentId\" INTEGER NOT NULL REFERENCES \"CovFkParents\"(\"Id\"))");

        var result = db.Schema.ValidateModel<CovFkChild>();

        Assert.False(result.IsValid);
    }

    [Fact]
    public void WithColumnsValueIsWrittenOnUpdate()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<Book>().Column("WithCol", SQLiteColumnType.Integer));
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        db.Table<Book>()
            .WithColumns(c => c.Set(b => SQLiteColumn.Of<long>(b, "WithCol"), 7L))
            .Update(new Book { Id = 1, Title = "y", AuthorId = 1, Price = 2 });

        long with = db.Table<Book>().Select(b => SQLiteColumn.Of<long>(b, "WithCol")).Single();

        Assert.Equal(7L, with);
    }

    [Fact]
    public void CaseSensitiveStartsWithAndEndsWith()
    {
        using TestDatabase db = new(b => b.UseCaseSensitiveStringComparison());
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Alpha", AuthorId = 1, Price = 1 });

        Assert.Single(db.Table<Book>().Where(b => b.Title.StartsWith("Al")).ToList());
        Assert.Single(db.Table<Book>().Where(b => b.Title.EndsWith("ha")).ToList());
        Assert.Empty(db.Table<Book>().Where(b => b.Title.StartsWith("al")).ToList());
    }

    [Fact]
    public void CaseSensitiveCompareIsBinary()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Apple", AuthorId = 1, Price = 1 });

        List<int> ids = db.Table<Book>()
            .Where(b => string.Compare(b.Title, "apple", StringComparison.Ordinal) != 0)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal(new[] { 1 }, ids);
    }

    [Fact]
    public void InsertFromQueryFromRawSqlUsesTableColumns()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });

        int inserted = db.Table<Book>().InsertFromQuery(
            db.Table<Book>().FromSql("SELECT \"BookId\", \"BookTitle\", \"BookAuthorId\", \"BookPrice\" FROM \"Books\" WHERE 1 = 0"));

        Assert.Equal(0, inserted);
    }

    [Fact]
    public void ForeignKeyRestrictAndSetDefaultActionsValidate()
    {
        using ModelTestDatabase db = new(model =>
        {
            model.Entity<CovFkParent>().HasKey(p => p.Id);
            model.Entity<CovFkChild>().ForeignKey<CovFkParent>(
                c => c.ParentId,
                onDelete: SQLiteForeignKeyAction.Restrict,
                onUpdate: SQLiteForeignKeyAction.SetDefault);
        });
        db.Execute("CREATE TABLE \"CovFkParents\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Execute("CREATE TABLE \"CovFkChildren\" (\"Id\" INTEGER PRIMARY KEY, \"ParentId\" INTEGER NOT NULL " +
                   "REFERENCES \"CovFkParents\"(\"Id\") ON DELETE RESTRICT ON UPDATE SET DEFAULT)");

        var result = db.Schema.ValidateModel<CovFkChild>();

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ForeignKeySetNullActionValidates()
    {
        using ModelTestDatabase db = new(model =>
        {
            model.Entity<CovFkParent>().HasKey(p => p.Id);
            model.Entity<CovFkNullableChild>().ForeignKey<CovFkParent>(
                c => c.ParentId,
                onDelete: SQLiteForeignKeyAction.SetNull);
        });
        db.Execute("CREATE TABLE \"CovFkParents\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Execute("CREATE TABLE \"CovFkNullableChildren\" (\"Id\" INTEGER PRIMARY KEY, \"ParentId\" INTEGER " +
                   "REFERENCES \"CovFkParents\"(\"Id\") ON DELETE SET NULL)");

        var result = db.Schema.ValidateModel<CovFkNullableChild>();

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CaseSensitiveCharContainsMatches()
    {
        using TestDatabase db = new(b => b.UseCaseSensitiveStringComparison());
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Apple", AuthorId = 1, Price = 1 });

        Assert.Single(db.Table<Book>().Where(b => b.Title.Contains('A')).ToList());
        Assert.Empty(db.Table<Book>().Where(b => b.Title.Contains('a')).ToList());
        Assert.Single(db.Table<Book>().Where(b => b.Title.Contains("apple", StringComparison.OrdinalIgnoreCase)).ToList());
    }

    [Fact]
    public void CaseSensitiveColumnToColumnSearchMatches()
    {
        using TestDatabase db = new(b => b.UseCaseSensitiveStringComparison());
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Apple", AuthorId = 1, Price = 1 });

        Assert.Single(db.Table<Book>().Where(b => b.Title.Contains(b.Title)).ToList());
        Assert.Single(db.Table<Book>().Where(b => b.Title.StartsWith(b.Title)).ToList());
        Assert.Single(db.Table<Book>().Where(b => b.Title.EndsWith(b.Title)).ToList());
    }

    [Fact]
    public void TrimEmptyAndBoundsArraysTrimWhitespace()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "  hi  ", AuthorId = 1, Price = 1 });

        string viaInit = db.Table<Book>().Select(b => b.Title.Trim(new char[] { })).Single();
        string viaBounds = db.Table<Book>().Select(b => b.Title.Trim(new char[0])).Single();
        string viaChars = db.Table<Book>().Select(b => ("xx" + b.Title.Trim()).Trim(new char[] { 'x' })).Single();
        string viaNonZeroBound = db.Table<Book>().Select(b => b.Title.Trim(new char[2])).Single();

        Assert.Equal("hi", viaInit);
        Assert.Equal("hi", viaBounds);
        Assert.Equal("hi", viaChars);
        Assert.Equal("  hi  ", viaNonZeroBound);
    }

    [Fact]
    public void TrimWithSizedArrayTrimsNullCharNotTheSize()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "2hi2", AuthorId = 1, Price = 1 });

        string result = db.Table<Book>().Select(b => b.Title.Trim(new char[2])).Single();

        Assert.Equal("2hi2", result);
        Assert.Equal("2hi2".Trim(new char[2]), result);
    }
}
