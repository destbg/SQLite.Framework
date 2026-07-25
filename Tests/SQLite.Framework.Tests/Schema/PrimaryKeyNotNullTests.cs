using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("StrKeyEntities")]
file sealed class StrKeyEntity
{
    [Key]
    public required string Code { get; set; }

    public required string Name { get; set; }
}

[Table("GuidKeyEntities")]
file sealed class GuidKeyEntity
{
    [Key]
    public Guid Id { get; set; }

    public required string Name { get; set; }
}

[Table("IntKeyEntities")]
file sealed class IntKeyEntity
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }
}

public class PrimaryKeyNotNullTests
{
    [Fact]
    public void StringKey_DdlDeclaresNotNull()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<StrKeyEntity>();

        string? ddl = db.ExecuteScalar<string?>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'StrKeyEntities'");

        Assert.NotNull(ddl);
        Assert.Equal("CREATE TABLE \"StrKeyEntities\" (\"Code\" TEXT NOT NULL PRIMARY KEY, \"Name\" TEXT NOT NULL)", ddl);
    }

    [Fact]
    public void StringKey_RejectsNullKeyInsert()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<StrKeyEntity>();

        Assert.ThrowsAny<Exception>(() =>
            db.Execute("INSERT INTO StrKeyEntities (\"Code\", \"Name\") VALUES (NULL, 'x')"));
    }

    [Fact]
    public void StringKey_AcceptsNonNullKey_RoundTrips()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<StrKeyEntity>();

        db.Table<StrKeyEntity>().Add(new StrKeyEntity { Code = "abc", Name = "n" });

        StrKeyEntity row = db.Table<StrKeyEntity>().Single();
        Assert.Equal("abc", row.Code);
    }

    [Fact]
    public void GuidKey_RejectsNullKeyInsert()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<GuidKeyEntity>();

        Assert.ThrowsAny<Exception>(() =>
            db.Execute("INSERT INTO GuidKeyEntities (\"Id\", \"Name\") VALUES (NULL, 'x')"));
    }

    [Fact]
    public void IntegerKey_NullInsertAutoGeneratesRowId()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<IntKeyEntity>();

        db.Execute("INSERT INTO IntKeyEntities (\"Name\") VALUES ('a')");
        db.Execute("INSERT INTO IntKeyEntities (\"Name\") VALUES ('b')");

        List<int> ids = db.Table<IntKeyEntity>().OrderBy(r => r.Name).Select(r => r.Id).ToList();
        Assert.Equal(new List<int> { 1, 2 }, ids);
    }
}
