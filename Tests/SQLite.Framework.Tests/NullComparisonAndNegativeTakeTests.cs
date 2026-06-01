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

        Assert.Contains(" IS NOT @", db.Table<NullableEntity>().Where(x => x.Value != 5).ToSqlCommand().CommandText);
        Assert.Contains(" IS @", db.Table<NullableEntity>().Where(x => x.Value == 5).ToSqlCommand().CommandText);
        Assert.Contains(" IS NOT @", db.Table<NullableEntity>().Where(x => !(x.Value == 5)).ToSqlCommand().CommandText);
    }

    [Fact]
    public void NonNullableColumnsUsePlainOperators()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Contains("<>", db.Table<Book>().Where(b => b.Id != 5).ToSqlCommand().CommandText);
        Assert.Contains("<>", db.Table<Book>().Where(b => b.Title != "a").ToSqlCommand().CommandText);
        Assert.Contains(" = @", db.Table<Book>().Where(b => b.Title == "a").ToSqlCommand().CommandText);
    }

    [Fact]
    public void MethodResultInequalityIsNullSafe()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Contains(" IS NOT @", db.Table<Book>().Where(b => b.Title.Trim() != "a").ToSqlCommand().CommandText);
    }
}
