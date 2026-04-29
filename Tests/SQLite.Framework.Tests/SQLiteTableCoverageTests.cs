using SQLite.Framework.Enums;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;
using SQLitePCL;

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

    [Fact]
    public void AddRange_WithOnActionHook_NoTransaction_RoutesThroughRunRangeElseBranch()
    {
        using TestDatabase db = new(b => b.OnAction((_, _, action) => action));
        db.Table<Article>().Schema.CreateTable();

        Article[] rows =
        [
            new() { Title = "t1", Body = "b1", PublishedAt = DateTime.UtcNow },
            new() { Title = "t2", Body = "b2", PublishedAt = DateTime.UtcNow },
        ];

        int affected = db.Table<Article>().AddRange(rows, runInTransaction: false);

        Assert.Equal(2, affected);
        Assert.Equal(2, db.Table<Article>().Count());
    }

    [Fact]
    public void AddRange_WithOnActionHookAndPerEntityCancel_RunRangeSkipsCancelledRow()
    {
        using TestDatabase db = new(b => b
            .OnAction((_, _, action) => action)
            .OnAdd<AuditedEntity>((_, e) => e.Name != "skip"));
        db.Table<AuditedEntity>().Schema.CreateTable();

        int affected = db.Table<AuditedEntity>().AddRange(new[]
        {
            new AuditedEntity { Name = "a" },
            new AuditedEntity { Name = "skip" },
            new AuditedEntity { Name = "c" },
        });

        Assert.Equal(2, affected);
        Assert.Equal(2, db.Table<AuditedEntity>().Count());
        Assert.DoesNotContain(db.Table<AuditedEntity>().ToList(), e => e.Name == "skip");
    }

    [Fact]
    public void AddRange_PreparedPath_PrepareFails_ThrowsSQLiteException()
    {
        using TestDatabase db = new();
        db.Table<Article>().Schema.CreateTable();
        BadSqlArticleTable table = new(db, db.TableMapping(typeof(Article)));

        Article[] rows = [new() { Title = "t", Body = "b", PublishedAt = DateTime.UtcNow }];

        Assert.Throws<SQLiteException>(() => table.AddRange(rows));
    }

    [Fact]
    public void AddRange_PreparedPath_StepFails_ThrowsSQLiteException()
    {
        using TestDatabase db = new();
        db.Table<WithoutRowIdEntity>().Schema.CreateTable();

        WithoutRowIdEntity[] rows =
        [
            new() { Code = "X", Name = "first" },
            new() { Code = "X", Name = "duplicate" },
        ];

        Assert.Throws<SQLiteException>(() => db.Table<WithoutRowIdEntity>().AddRange(rows));
    }

    [Fact]
    public void AddRange_WithEntityWriters_AllResolved_RoundTrips()
    {
        using TestDatabase db = new(b =>
        {
            b.EntityWriters[typeof(WithoutRowIdEntity)] = new Dictionary<string, SQLiteEntityColumnWriter>
            {
                [nameof(WithoutRowIdEntity.Code)] = static (stmt, idx, item, _) =>
                    raw.sqlite3_bind_text(stmt, idx, ((WithoutRowIdEntity)item).Code),
                [nameof(WithoutRowIdEntity.Name)] = static (stmt, idx, item, _) =>
                    raw.sqlite3_bind_text(stmt, idx, ((WithoutRowIdEntity)item).Name),
            };
        });
        db.Table<WithoutRowIdEntity>().Schema.CreateTable();

        WithoutRowIdEntity[] rows =
        [
            new() { Code = "A", Name = "alice" },
            new() { Code = "B", Name = "bob" },
        ];

        int affected = db.Table<WithoutRowIdEntity>().AddRange(rows);

        Assert.Equal(2, affected);
        List<WithoutRowIdEntity> stored = db.Table<WithoutRowIdEntity>().OrderBy(e => e.Code).ToList();
        Assert.Equal("alice", stored[0].Name);
        Assert.Equal("bob", stored[1].Name);
    }

    [Fact]
    public void AddRange_WithEntityWriters_PartiallyResolved_FallsBackToReflection()
    {
        using TestDatabase db = new(b =>
        {
            b.EntityWriters[typeof(WithoutRowIdEntity)] = new Dictionary<string, SQLiteEntityColumnWriter>
            {
                [nameof(WithoutRowIdEntity.Code)] = static (stmt, idx, item, _) =>
                    raw.sqlite3_bind_text(stmt, idx, ((WithoutRowIdEntity)item).Code),
            };
        });
        db.Table<WithoutRowIdEntity>().Schema.CreateTable();

        WithoutRowIdEntity[] rows =
        [
            new() { Code = "A", Name = "alice" },
            new() { Code = "B", Name = "bob" },
        ];

        int affected = db.Table<WithoutRowIdEntity>().AddRange(rows);

        Assert.Equal(2, affected);
        List<WithoutRowIdEntity> stored = db.Table<WithoutRowIdEntity>().OrderBy(e => e.Code).ToList();
        Assert.Equal("alice", stored[0].Name);
        Assert.Equal("bob", stored[1].Name);
    }

    [Fact]
    public void AddRange_AutoIncrement_LongKey_FillsId()
    {
        using TestDatabase db = new();
        db.Table<LongKeyEntity>().Schema.CreateTable();

        LongKeyEntity row = new() { Name = "x" };
        int affected = db.Table<LongKeyEntity>().AddRange(new[] { row });

        Assert.Equal(1, affected);
        Assert.True(row.Id > 0L);
    }

    [Fact]
    public void AddRange_AutoIncrement_ShortKey_FillsId()
    {
        using TestDatabase db = new();
        db.Table<ShortKeyEntity>().Schema.CreateTable();

        ShortKeyEntity row = new() { Name = "x" };
        int affected = db.Table<ShortKeyEntity>().AddRange(new[] { row });

        Assert.Equal(1, affected);
        Assert.True(row.Id > 0);
    }

    [Fact]
    public void AddRange_AutoIncrement_ByteKey_FillsId()
    {
        using TestDatabase db = new();
        db.Table<ByteKeyEntity>().Schema.CreateTable();

        ByteKeyEntity row = new() { Name = "x" };
        int affected = db.Table<ByteKeyEntity>().AddRange(new[] { row });

        Assert.Equal(1, affected);
        Assert.True(row.Id > 0);
    }

    [Fact]
    public void AddRange_AutoIncrement_SByteKey_FillsId()
    {
        using TestDatabase db = new();
        db.Table<SByteKeyEntity>().Schema.CreateTable();

        SByteKeyEntity row = new() { Name = "x" };
        int affected = db.Table<SByteKeyEntity>().AddRange(new[] { row });

        Assert.Equal(1, affected);
        Assert.True(row.Id > 0);
    }

    [Fact]
    public void AddRange_AutoIncrement_UIntKey_FillsId()
    {
        using TestDatabase db = new();
        db.Table<UIntKeyEntity>().Schema.CreateTable();

        UIntKeyEntity row = new() { Name = "x" };
        int affected = db.Table<UIntKeyEntity>().AddRange(new[] { row });

        Assert.Equal(1, affected);
        Assert.True(row.Id > 0u);
    }

    [Fact]
    public void AddRange_AutoIncrement_ULongKey_FillsId()
    {
        using TestDatabase db = new();
        db.Table<ULongKeyEntity>().Schema.CreateTable();

        ULongKeyEntity row = new() { Name = "x" };
        int affected = db.Table<ULongKeyEntity>().AddRange(new[] { row });

        Assert.Equal(1, affected);
        Assert.True(row.Id > 0ul);
    }

    [Fact]
    public void AddRange_AutoIncrement_UShortKey_FillsId_HitsConvertChangeTypeFallback()
    {
        using TestDatabase db = new();
        db.Table<UShortKeyEntity>().Schema.CreateTable();

        UShortKeyEntity row = new() { Name = "x" };
        int affected = db.Table<UShortKeyEntity>().AddRange(new[] { row });

        Assert.Equal(1, affected);
        Assert.True(row.Id > 0);
    }

    private sealed class BadSqlArticleTable : SQLiteTable<Article>
    {
        public BadSqlArticleTable(SQLiteDatabase database, TableMapping table)
            : base(database, table)
        {
        }

        protected override (TableColumn[] Columns, string Sql) GetAddInfo()
        {
            (TableColumn[] columns, _) = base.GetAddInfo();
            return (columns, "INSERT GARBAGE @!%");
        }
    }
}
