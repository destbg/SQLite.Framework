using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class NullComparisonAndNegativeTakeTests
{
    private static TestDatabase Nums(params (int id, int? val)[] rows)
    {
        TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();
        foreach ((int id, int? val) in rows)
        {
            db.CreateCommand($"INSERT INTO NullableEntity (\"Id\",\"Value\") VALUES ({id},{(val.HasValue ? val.Value.ToString() : "NULL")})", []).ExecuteNonQuery();
        }

        return db;
    }

    [Fact]
    public void TakeNegativeReturnsEmpty()
    {
        using TestDatabase db = Nums((1, 1), (2, 2), (3, 3));

        Assert.Empty(db.Table<NullableEntity>().Take(-1).ToList());
        Assert.Empty(db.Table<NullableEntity>().OrderBy(x => x.Id).Skip(1).Take(-5).ToList());
    }

    [Fact]
    public void NullableValueInequalityIncludesNulls()
    {
        using TestDatabase db = Nums((1, null), (2, 5), (3, 7));

        Assert.Equal(new[] { 1, 3 }, db.Table<NullableEntity>().Where(x => x.Value != 5).Select(x => x.Id).OrderBy(i => i).ToList());
        Assert.Equal(new[] { 1, 3 }, db.Table<NullableEntity>().Where(x => !(x.Value == 5)).Select(x => x.Id).OrderBy(i => i).ToList());
    }

    [Fact]
    public void NullableStringInequalityIncludesNulls()
    {
        using TestDatabase db = new();
        db.Table<NullableStringEntity>().Schema.CreateTable();
        db.CreateCommand("INSERT INTO NullableStringEntity (\"Id\",\"Name\") VALUES (1,NULL),(2,'a'),(3,'b')", []).ExecuteNonQuery();

        Assert.Equal(new[] { 1, 3 }, db.Table<NullableStringEntity>().Where(x => x.Name != "a").Select(x => x.Id).OrderBy(i => i).ToList());
    }

    [Fact]
    public void NullableEqualityUnchanged()
    {
        using TestDatabase db = Nums((1, null), (2, 5));

        Assert.Equal(new[] { 2 }, db.Table<NullableEntity>().Where(x => x.Value == 5).Select(x => x.Id).ToList());
        Assert.Equal(new[] { 1 }, db.Table<NullableEntity>().Where(x => x.Value == null).Select(x => x.Id).ToList());
    }

    [Fact]
    public void NullableValueOperatorsAreNullSafe()
    {
        using TestDatabase db = Nums((1, 1));

        Assert.Equal("SELECT n0.\"Id\" AS \"Id\",\n       n0.\"Value\" AS \"Value\"\nFROM \"NullableEntity\" AS n0\nWHERE n0.\"Value\" IS NOT @p0", db.Table<NullableEntity>().Where(x => x.Value != 5).ToSqlCommand().CommandText);
        Assert.Equal("SELECT n0.\"Id\" AS \"Id\",\n       n0.\"Value\" AS \"Value\"\nFROM \"NullableEntity\" AS n0\nWHERE n0.\"Value\" IS @p0", db.Table<NullableEntity>().Where(x => x.Value == 5).ToSqlCommand().CommandText);
        Assert.Equal("SELECT n0.\"Id\" AS \"Id\",\n       n0.\"Value\" AS \"Value\"\nFROM \"NullableEntity\" AS n0\nWHERE n0.\"Value\" IS NOT @p0", db.Table<NullableEntity>().Where(x => !(x.Value == 5)).ToSqlCommand().CommandText);
    }

    [Fact]
    public void NonNullableColumnsUsePlainOperators()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Equal("SELECT b0.\"BookId\" AS \"Id\",\n       b0.\"BookTitle\" AS \"Title\",\n       b0.\"BookAuthorId\" AS \"AuthorId\",\n       b0.\"BookPrice\" AS \"Price\"\nFROM \"Books\" AS b0\nWHERE b0.\"BookId\" <> @p0", db.Table<Book>().Where(b => b.Id != 5).ToSqlCommand().CommandText);
        Assert.Equal("SELECT b0.\"BookId\" AS \"Id\",\n       b0.\"BookTitle\" AS \"Title\",\n       b0.\"BookAuthorId\" AS \"AuthorId\",\n       b0.\"BookPrice\" AS \"Price\"\nFROM \"Books\" AS b0\nWHERE b0.\"BookTitle\" <> @p0", db.Table<Book>().Where(b => b.Title != "a").ToSqlCommand().CommandText);
        Assert.Equal("SELECT b0.\"BookId\" AS \"Id\",\n       b0.\"BookTitle\" AS \"Title\",\n       b0.\"BookAuthorId\" AS \"AuthorId\",\n       b0.\"BookPrice\" AS \"Price\"\nFROM \"Books\" AS b0\nWHERE b0.\"BookTitle\" = @p0", db.Table<Book>().Where(b => b.Title == "a").ToSqlCommand().CommandText);
    }

    [Fact]
    public void MethodResultInequalityIsNullSafe()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Equal("SELECT b0.\"BookId\" AS \"Id\",\n       b0.\"BookTitle\" AS \"Title\",\n       b0.\"BookAuthorId\" AS \"AuthorId\",\n       b0.\"BookPrice\" AS \"Price\"\nFROM \"Books\" AS b0\nWHERE TRIM(b0.\"BookTitle\", CHAR(9, 10, 11, 12, 13, 32, 133, 160, 5760, 8192, 8193, 8194, 8195, 8196, 8197, 8198, 8199, 8200, 8201, 8202, 8232, 8233, 8239, 8287, 12288)) IS NOT @p0", db.Table<Book>().Where(b => b.Title.Trim() != "a").ToSqlCommand().CommandText);
    }
}
