using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SQLiteTableCoverageTests
{
    private static string N(string? s) => (s ?? string.Empty).Replace("\r\n", "\n");

    [Fact]
    public void CreateTable_WithoutRowId_AppendsWithoutRowidClause()
    {
        using TestDatabase db = new();
        db.Table<WithoutRowIdEntity>().Schema.CreateTable();

        string? sql = db.ExecuteScalar<string>("SELECT sql FROM sqlite_master WHERE name = 'WithoutRowIdEntity'");

        Assert.EndsWith("WITHOUT ROWID", N(sql));
    }

    [Fact]
    public void CreateTable_WithoutRowId_RoundTripsRow()
    {
        using TestDatabase db = new();
        db.Table<WithoutRowIdEntity>().Schema.CreateTable();

        db.Table<WithoutRowIdEntity>().Add(new WithoutRowIdEntity { Code = "X", Name = "x-name" });

        WithoutRowIdEntity row = db.Table<WithoutRowIdEntity>().Single();
        Assert.Equal("X", row.Code);
        Assert.Equal("x-name", row.Name);
    }

    [Fact]
    public void DropTable_FtsWithTriggers_DropsTriggersBeforeTable()
    {
        using TestDatabase db = new();
        db.Table<Article>().Schema.CreateTable();
        db.Table<ArticleSearch>().Schema.CreateTable();

        long triggersBefore = db.ExecuteScalar<long>("SELECT COUNT(*) FROM sqlite_master WHERE type='trigger' AND name LIKE 'ArticleSearch_sync%'");
        Assert.Equal(3, triggersBefore);

        db.Schema.DropTable<ArticleSearch>();

        long triggersAfter = db.ExecuteScalar<long>("SELECT COUNT(*) FROM sqlite_master WHERE type='trigger' AND name LIKE 'ArticleSearch_sync%'");
        long tableAfter = db.ExecuteScalar<long>("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='ArticleSearch'");
        Assert.Equal(0, triggersAfter);
        Assert.Equal(0, tableAfter);
    }

    [Fact]
    public void CreateTable_FtsWithUnindexedColumn_EmitsUnindexedKeyword()
    {
        using TestDatabase db = new();
        db.Table<Article_Unindexed_Search>().Schema.CreateTable();

        string? sql = db.ExecuteScalar<string>("SELECT sql FROM sqlite_master WHERE name = 'Article_Unindexed_Search'");

        Assert.Contains("Tag UNINDEXED", N(sql));
    }

    [Fact]
    public void CreateTable_FtsContentless_EmitsContentEmpty()
    {
        using TestDatabase db = new();
        db.Table<Article_Contentless_Search>().Schema.CreateTable();

        string? sql = db.ExecuteScalar<string>("SELECT sql FROM sqlite_master WHERE name = 'Article_Contentless_Search'");

        Assert.Contains("content=''", N(sql));
    }

    [Fact]
    public void CreateTable_FtsWithPrefixAttribute_EmitsPrefixOption()
    {
        using TestDatabase db = new();
        db.Table<Article_Prefix_Search>().Schema.CreateTable();

        string? sql = db.ExecuteScalar<string>("SELECT sql FROM sqlite_master WHERE name = 'Article_Prefix_Search'");

        Assert.Contains("prefix='2 3'", N(sql));
    }

    [Fact]
    public void CreateTable_FtsWithExplicitContentRowIdColumn_UsesGivenColumn()
    {
        using TestDatabase db = new();
        db.Table<Article>().Schema.CreateTable();
        db.Table<Article_ExplicitRowIdColumn_Search>().Schema.CreateTable();

        string? sql = db.ExecuteScalar<string>("SELECT sql FROM sqlite_master WHERE name = 'Article_ExplicitRowIdColumn_Search'");

        Assert.Contains("content_rowid='Id'", N(sql));
    }

    [Fact]
    public void Update_HookReturnsFalse_SkipsUpdateReturnsZero()
    {
        using TestDatabase db = new(b => b.OnUpdate<AuditedEntity>((_, _) => false));
        db.Table<AuditedEntity>().Schema.CreateTable();
        db.Table<AuditedEntity>().Add(new AuditedEntity { Name = "original" });

        AuditedEntity row = db.Table<AuditedEntity>().Single();
        row.Name = "renamed";

        int affected = db.Table<AuditedEntity>().Update(row);

        Assert.Equal(0, affected);
        Assert.Equal("original", db.Table<AuditedEntity>().Single().Name);
    }

    [Fact]
    public void AddOrUpdate_HookReturnsFalse_SkipsInsertReturnsZero()
    {
        using TestDatabase db = new(b => b.OnAddOrUpdate<AuditedEntity>((_, _) => false));
        db.Table<AuditedEntity>().Schema.CreateTable();

        int affected = db.Table<AuditedEntity>().AddOrUpdate(new AuditedEntity { Name = "x" });

        Assert.Equal(0, affected);
        Assert.Empty(db.Table<AuditedEntity>().ToList());
    }
}
