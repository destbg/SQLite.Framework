using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using SQLite.Framework.Attributes;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Enums;
using SQLite.Framework.Tests.Helpers;

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.

namespace SQLite.Framework.Tests;

public class CoverageGapTests
{
    [Fact]
    public void IndexedAttribute_NameOrderConstructor_SetsProperties()
    {
        IndexedAttribute attr = new("IX_Test", 2);
        Assert.Equal("IX_Test", attr.Name);
        Assert.Equal(2, attr.Order);
        Assert.False(attr.IsUnique);
    }

    [Fact]
    public void ExecuteDelete_OnNonSQLiteQueryable_Throws()
    {
        IQueryable<Book> queryable = Array.Empty<Book>().AsQueryable();
        Assert.Throws<InvalidOperationException>(() => queryable.ExecuteDelete());
    }

    [Fact]
    public void ExecuteDelete_WithPredicate_OnNonSQLiteQueryable_Throws()
    {
        IQueryable<Book> queryable = Array.Empty<Book>().AsQueryable();
        Assert.Throws<InvalidOperationException>(() => queryable.ExecuteDelete(b => b.Id == 1));
    }

    [Fact]
    public void ExecuteUpdate_OnNonSQLiteQueryable_Throws()
    {
        IQueryable<Book> queryable = Array.Empty<Book>().AsQueryable();
        Assert.Throws<InvalidOperationException>(() => queryable.ExecuteUpdate(s => s.Set(b => b.Title, "x")));
    }

    [Fact]
    public void InsertFromQuery_OnNonSQLiteQueryable_Throws()
    {
        using TestDatabase db = new();
        db.Table<BookArchive>().Schema.CreateTable();

        IQueryable<BookArchive> queryable = Array.Empty<BookArchive>().AsQueryable();
        Assert.Throws<InvalidOperationException>(() => db.Table<BookArchive>().InsertFromQuery(queryable));
    }

    [Fact]
    public void ExecuteUpdate_SetOnMethodExpression_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<ArgumentException>(() =>
            db.Table<Book>().ExecuteUpdate(s => s.Set(b => b.Title.ToUpper(), "X")));
    }

    [Fact]
    public void ExecuteUpdate_SetOnNonDirectProperty_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Author other = new()
        {
            Name = "X",
            Email = "X",
            BirthDate = default
        };
        Assert.Throws<ArgumentException>(() =>
            db.Table<Book>().ExecuteUpdate(s => s.Set(b => other.Name, "X")));
    }

    [Fact]
    public void ExecuteUpdate_SetOnField_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<ArgumentException>(() =>
            db.Table<Book>().ExecuteUpdate(s => s.Set(b => ((BookWithField)(object)b).Title, "X")));
    }

    [Fact]
    public void ExecuteUpdate_SetOnDirectField_Throws()
    {
        using TestDatabase db = new();

        Assert.Throws<ArgumentException>(() =>
            db.Table<BookWithField>().ExecuteUpdate(s => s.Set(b => b.Title, "X")));
    }

    [Fact]
    public void ExecuteUpdate_SetExpressionNotSqlExpression_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<ArgumentException>(() =>
            db.Table<Book>().ExecuteUpdate(s => s.Set(b => b.Title,
                b => b is Book ? "a" : "b")));
    }

    [Fact]
    public void Select_PassRowToClientMethod_RowTypeWithoutParameterlessCtor_Throws()
    {
        using TestDatabase db = new();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<BookNoParameterlessCtor>()
                .Select(b => DescribeRow(b))
                .ToSqlCommand());
    }

    private static string DescribeRow(BookNoParameterlessCtor b) => b.Id.ToString();

    [Fact]
    public void FromSql_NullParameters_Throws()
    {
        using TestDatabase db = new();

        Assert.Throws<ArgumentNullException>(() => db.FromSql<Book>("SELECT * FROM Books", parameters: null!));
    }

    [Fact]
    public void FromSql_EmptySql_Throws()
    {
        using TestDatabase db = new();

        Assert.Throws<ArgumentException>(() => db.FromSql<Book>(""));
    }

    [Fact]
    public void FromSql_WhitespaceSql_Throws()
    {
        using TestDatabase db = new();

        Assert.Throws<ArgumentException>(() => db.FromSql<Book>("   "));
    }

    [Fact]
    public void Transaction_Commit_AlreadyCompleted_Throws()
    {
        using TestDatabase db = new();

        SQLiteTransaction tx = db.BeginTransaction();
        tx.Commit();

        Assert.Throws<InvalidOperationException>(() => tx.Commit());
    }

    [Fact]
    public void Transaction_Rollback_AlreadyCompleted_Throws()
    {
        using TestDatabase db = new();

        SQLiteTransaction tx = db.BeginTransaction();
        tx.Rollback();

        Assert.Throws<InvalidOperationException>(() => tx.Rollback());
    }

    [Fact]
    public void Transaction_Rollback_AfterCommit_Throws()
    {
        using TestDatabase db = new();

        SQLiteTransaction tx = db.BeginTransaction();
        tx.Commit();

        Assert.Throws<InvalidOperationException>(() => tx.Rollback());
    }

    [Fact]
    public void Upsert_DoubleConfigure_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Book book = new() { Id = 1, Title = "x", AuthorId = 1, Price = 1 };

        Assert.Throws<InvalidOperationException>(() =>
            db.Table<Book>().Upsert(book, c =>
            {
                UpsertConflictTarget<Book> conflict = c.OnConflict(b => b.Id);
                conflict.DoNothing();
                conflict.DoUpdateAll();
            }));
    }

    [Fact]
    public void Upsert_DoUpdateAll_AllColumnsAreConflict_EmitsDoNothing()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        SqlInspector inspector = new(db, db.TableMapping<Book>());
        string sql = inspector.GetSql(c => c.OnConflict(b => new { b.Id, b.Title, b.AuthorId, b.Price }).DoUpdateAll());

        Assert.Contains("DO NOTHING", sql);
    }

    [Fact]
    public void InsertFromQuery_SourceFromDifferentDatabase_Throws()
    {
        using TestDatabase target = new();
        using TestDatabase otherDb = new(methodName: "Other");

        target.Table<BookArchive>().Schema.CreateTable();
        otherDb.Table<BookArchive>().Schema.CreateTable();

        Assert.Throws<InvalidOperationException>(() =>
            target.Table<BookArchive>().InsertFromQuery(otherDb.Table<BookArchive>()));
    }

    [Fact]
    public void Remove_OnEntityWithoutPrimaryKey_Throws()
    {
        using TestDatabase db = new();
        db.Table<NoKeyEntity>().Schema.CreateTable();

        Assert.ThrowsAny<Exception>(() =>
            db.Table<NoKeyEntity>().Remove(new NoKeyEntity { Name = "x" }));
    }

    [Fact]
    public void AddOrUpdate_UnknownConflictValue_FallsBackToReplace()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Book book = new() { Id = 1, Title = "first", AuthorId = 1, Price = 1 };
        db.Table<Book>().Add(book);

        int affected = db.Table<Book>().AddOrUpdate(
            new Book { Id = 1, Title = "second", AuthorId = 1, Price = 1 },
            (SQLiteConflict)999);

        Assert.Equal(1, affected);
        Assert.Equal("second", db.Table<Book>().Single().Title);
    }

    [Fact]
    public void Schema_CreateIndex_NotMemberExpression_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<InvalidOperationException>(() =>
            db.Schema.CreateIndex<Book>(b => b.Price + 1));
    }

    [Fact]
    public void Schema_CreateIndex_BoxedColumn_StripsConvert()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Schema.CreateIndex<Book>(b => (object)b.Price, name: "IX_Boxed_Price");

        Assert.True(db.Schema.IndexExists("IX_Boxed_Price"));
    }

    [Fact]
    public void CreateCommand_BindParameterByMissingName_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.ThrowsAny<Exception>(() =>
            db.Execute("SELECT * FROM Books WHERE BookId = @existing",
                [new SQLiteParameter { Name = "@missing", Value = 1 }]));
    }

    [Fact]
    public void Functions_Regexp_TranslatesToSqlRegexpOperator()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        SQLiteCommand cmd = db.Table<Book>()
            .Where(b => SQLiteFunctions.Regexp(b.Title, "^A.*"))
            .ToSqlCommand();

        Assert.Contains("REGEXP", cmd.CommandText);
    }

    [Fact]
    public void Functions_Match_StringColumnOverload_BuildsScopedMatch()
    {
        using TestDatabase db = new();
        db.Table<Article>().Schema.CreateTable();
        db.Table<ArticleSearch>().Schema.CreateTable();

        SQLiteCommand cmd = db.Table<ArticleSearch>()
            .Where(a => SQLiteFTS5Functions.Match(a.Title, "native"))
            .ToSqlCommand();

        Assert.Contains("MATCH", cmd.CommandText);
        Assert.Contains("Title", cmd.CommandText);
    }

    [Fact]
    public void Functions_Match_StringColumnOverload_NotMemberAccess_Throws()
    {
        using TestDatabase db = new();
        db.Table<Article>().Schema.CreateTable();
        db.Table<ArticleSearch>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<ArticleSearch>()
                .Where(a => SQLiteFTS5Functions.Match(a.Title.Substring(0), "native"))
                .ToSqlCommand());
    }

    [Fact]
    public void Functions_Snippet_NonFtsEntity_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Select(b => SQLiteFTS5Functions.Snippet(b, b.Title, "<", ">", "...", 5))
                .ToSqlCommand());
    }

    [Fact]
    public void Functions_Snippet_ColumnNotInFtsIndex_Throws()
    {
        using TestDatabase db = new();
        db.Table<Article>().Schema.CreateTable();
        db.Table<ArticleSearch>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<ArticleSearch>()
                .Select(a => SQLiteFTS5Functions.Snippet(a, a.Id.ToString(), "<", ">", "...", 5))
                .ToSqlCommand());
    }

    [Fact]
    public void Guid_InstanceToStringInsideWhere_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Guid g = Guid.NewGuid();
        Assert.ThrowsAny<Exception>(() =>
            db.Table<Book>()
                .Where(b => b.Title == g.ToString())
                .ToSqlCommand());
    }

    [Fact]
    public void Enum_UnsupportedMethod_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.ThrowsAny<Exception>(() =>
            db.Table<Book>()
                .Where(b => Enum.GetUnderlyingType(typeof(BookCategory)) != null)
                .ToSqlCommand());
    }

    [Fact]
    public void Char_UnsupportedMethod_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.ThrowsAny<Exception>(() =>
            db.Table<Book>()
                .Where(b => char.IsControl(b.Title, 0))
                .ToSqlCommand());
    }

    [Fact]
    public void Int_ToString_TranslatesToCastAsText()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        SQLiteCommand cmd = db.Table<Book>()
            .Where(b => b.AuthorId.ToString() == "5")
            .ToSqlCommand();

        Assert.Contains("CAST", cmd.CommandText);
        Assert.Contains("TEXT", cmd.CommandText);
    }

    [Fact]
    public void Int_Parse_TranslatesToCastAsInteger()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        SQLiteCommand cmd = db.Table<Book>()
            .Where(b => int.Parse(b.Title) > 0)
            .ToSqlCommand();

        Assert.Contains("CAST", cmd.CommandText);
        Assert.Contains("INTEGER", cmd.CommandText);
    }

    [Fact]
    public void Double_ToString_TranslatesToCastAsText()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        SQLiteCommand cmd = db.Table<Book>()
            .Where(b => b.Price.ToString() == "5")
            .ToSqlCommand();

        Assert.Contains("CAST", cmd.CommandText);
        Assert.Contains("TEXT", cmd.CommandText);
    }

    [Fact]
    public void Double_Parse_TranslatesToCastAsReal()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        SQLiteCommand cmd = db.Table<Book>()
            .Where(b => double.Parse(b.Title) > 0)
            .ToSqlCommand();

        Assert.Contains("CAST", cmd.CommandText);
        Assert.Contains("REAL", cmd.CommandText);
    }

    [Fact]
    public void TimeOnly_UnsupportedMethod_Throws()
    {
        using TestDatabase db = new();
        db.Table<TimeOnlyMethodEntity>().Schema.CreateTable();

        TimeOnly t = new(8, 0);
        Assert.ThrowsAny<Exception>(() =>
            db.Table<TimeOnlyMethodEntity>()
                .Where(e => e.Time.GetHashCode() == t.GetHashCode())
                .ToSqlCommand());
    }

    [Fact]
    public void TimeOnly_StaticMethodWithColumn_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.ThrowsAny<Exception>(() =>
            db.Table<Book>()
                .Where(b => TimeOnly.Parse(b.Title) > new TimeOnly(0, 0))
                .ToSqlCommand());
    }

    [Fact]
    public void DateOnly_UnsupportedMethod_Throws()
    {
        using TestDatabase db = new();
        db.Table<DateOnlyMethodEntity>().Schema.CreateTable();

        DateOnly d = new(2024, 1, 1);
        Assert.ThrowsAny<Exception>(() =>
            db.Table<DateOnlyMethodEntity>()
                .Where(e => e.Date.CompareTo(d) > 0)
                .ToSqlCommand());
    }

    [Fact]
    public void Snippet_DynamicAuxArgs_WrapInPrintf()
    {
        using TestDatabase db = new();
        db.Table<Article>().Schema.CreateTable();
        db.Table<ArticleSearch>().Schema.CreateTable();

        Article seed = new() { Title = "kryptonite tales", Body = "shines bright", PublishedAt = DateTime.UtcNow };
        db.Table<Article>().Add(seed);

        string before = "{{";
        string after = "}}";
        string ellipsis = "…";
        SQLiteCommand cmd = db.Table<ArticleSearch>()
            .Where(a => SQLiteFTS5Functions.Match(a, "kryptonite"))
            .Select(a => SQLiteFTS5Functions.Snippet(a, a.Title, before, after, ellipsis, 5))
            .ToSqlCommand();

        Assert.Contains("snippet(", cmd.CommandText);
    }

    [Fact]
    public void FtsMatch_DynamicTerm_ProducesPrintfWrappedSql()
    {
        using TestDatabase db = new();
        db.Table<Article>().Schema.CreateTable();
        db.Table<ArticleSearch>().Schema.CreateTable();

        SQLiteCommand cmd = db.Table<ArticleSearch>()
            .Where(a => SQLiteFTS5Functions.Match(a, f => f.Term(a.Title) && f.Term("static")))
            .ToSqlCommand();

        Assert.Contains("printf", cmd.CommandText);
    }

    [Fact]
    public void Enum_NonGenericParse_WithTypeArgument_TranslatesToCase()
    {
        using TestDatabase db = new();
        db.Table<Publisher>().Schema.CreateTable();

        SQLiteCommand cmd = db.Table<Publisher>()
            .Where(p => p.Type == (PublisherType)Enum.Parse(typeof(PublisherType), "Magazine"))
            .ToSqlCommand();

        Assert.Contains("CASE", cmd.CommandText);
    }

    [Fact]
    public void Enum_GenericParse_WithLiteralArgs_FoldsToConstant()
    {
        using TestDatabase db = new();
        db.Table<Publisher>().Schema.CreateTable();
        db.Table<Publisher>().Add(new Publisher
        {
            Id = 1,
            Name = "x",
            Type = PublisherType.Magazine
        });

        Publisher row = db.Table<Publisher>()
            .Single(p => p.Type == Enum.Parse<PublisherType>("Magazine"));

        Assert.Equal(PublisherType.Magazine, row.Type);
    }

    [Fact]
    public void Enum_ToString_TranslatesToCaseExpression()
    {
        using TestDatabase db = new();
        db.Table<Publisher>().Schema.CreateTable();
        db.Table<Publisher>().Add(new Publisher
        {
            Id = 1,
            Name = "x",
            Type = PublisherType.Magazine
        });

        SQLiteCommand cmd = db.Table<Publisher>()
            .Where(p => p.Type.ToString() == "Magazine")
            .ToSqlCommand();

        Assert.Contains("CASE", cmd.CommandText);
    }

    [Fact]
    public void Subquery_Contains_BuildsInClause()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Schema.CreateTable();

        db.Table<Author>().Add(new Author { Id = 1, Name = "a", Email = "a@x", BirthDate = new DateTime(2000, 1, 1) });
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        SQLiteCommand cmd = db.Table<Book>()
            .Where(b => db.Table<Author>().Select(a => a.Id).Contains(b.AuthorId))
            .ToSqlCommand();

        Assert.Contains("IN (", cmd.CommandText);
    }

    [Fact]
    public void Subquery_Any_BuildsExistsClause()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Schema.CreateTable();

        SQLiteCommand cmd = db.Table<Book>()
            .Where(b => db.Table<Author>().Any(a => a.Id == b.AuthorId))
            .ToSqlCommand();

        Assert.Contains("EXISTS", cmd.CommandText);
    }

    [Fact]
    public void Trim_WithCharArrayLiteral_TranslatesToTrim()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "  hello  ", AuthorId = 1, Price = 1 });

        SQLiteCommand cmd = db.Table<Book>()
            .Where(b => b.Title.Trim(' ', '\t') == "hello")
            .ToSqlCommand();

        Assert.Contains("TRIM(", cmd.CommandText);
    }

    [Fact]
    public void Select_ListContainsUntranslatableExpr_FallsBackToClientSide()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 3, Price = 1 });

        List<int> ids = [4, 6, 9];
        var rows = db.Table<Book>()
            .Select(b => new { b.Id, Hit = ids.Contains(InterceptorHelpers.Double(b.AuthorId)) })
            .ToList();

        Assert.Single(rows);
        Assert.True(rows[0].Hit);
    }

    [Fact]
    public void Select_GuidNewGuidThenToString_RunsClientSide()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        var rows = db.Table<Book>()
            .Select(b => new { b.Id, Token = Guid.NewGuid().ToString() })
            .ToList();

        Assert.Single(rows);
        Assert.False(string.IsNullOrEmpty(rows[0].Token));
    }

    [Fact]
    public void Select_IntToStringWithUntranslatableArg_FallsBack()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 5, Price = 1 });

        var rows = db.Table<Book>()
            .Select(b => new { b.Id, Doubled = InterceptorHelpers.Double(b.AuthorId).ToString() })
            .ToList();

        Assert.Single(rows);
        Assert.Equal("10", rows[0].Doubled);
    }

    [Fact]
    public void Select_DoubleParseWithUntranslatableArg_FallsBack()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        var rows = db.Table<Book>()
            .Select(b => new { b.Id, Parsed = double.Parse(InterceptorHelpers.Double(b.AuthorId).ToString()) })
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void Select_IntParseWithUntranslatableArg_FallsBack()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "5", AuthorId = 1, Price = 1 });

        var rows = db.Table<Book>()
            .Select(b => new { b.Id, Parsed = int.Parse(InterceptorHelpers.Identity(b.Title)) })
            .ToList();

        Assert.Single(rows);
        Assert.Equal(5, rows[0].Parsed);
    }

    [Fact]
    public void Select_DoubleInstanceCompareToWithUntranslatableArg_FallsBack()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 5 });

        var rows = db.Table<Book>()
            .Select(b => new { b.Id, Cmp = b.Price.CompareTo(InterceptorHelpers.IdentityDouble(b.Price)) })
            .ToList();

        Assert.Single(rows);
        Assert.Equal(0, rows[0].Cmp);
    }

    [Fact]
    public void Select_EnumHasFlagWithUntranslatableArg_FallsBack()
    {
        using TestDatabase db = new();
        db.Table<Publisher>().Schema.CreateTable();
        db.Table<Publisher>().Add(new Publisher
        {
            Id = 1,
            Name = "x",
            Type = PublisherType.Magazine
        });

        var rows = db.Table<Publisher>()
            .Select(p => new { p.Id, Match = p.Type.HasFlag(InterceptorHelpers.IdentityEnum(p.Type)) })
            .ToList();

        Assert.Single(rows);
        Assert.True(rows[0].Match);
    }

    [Fact]
    public void Select_GuidParseWithUntranslatableArg_FallsBack()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        Guid sample = Guid.NewGuid();
        db.Table<Book>().Add(new Book { Id = 1, Title = sample.ToString(), AuthorId = 1, Price = 1 });

        var rows = db.Table<Book>()
            .Select(b => new { b.Id, Parsed = Guid.Parse(InterceptorHelpers.Identity(b.Title)) })
            .ToList();

        Assert.Single(rows);
        Assert.Equal(sample, rows[0].Parsed);
    }

    [Fact]
    public void Select_EnumGenericParseFallsBackWhenInputClientSide()
    {
        using TestDatabase db = new();
        db.Table<Publisher>().Schema.CreateTable();
        db.Table<Publisher>().Add(new Publisher
        {
            Id = 1,
            Name = "Magazine",
            Type = PublisherType.Magazine
        });

        var rows = db.Table<Publisher>()
            .Select(p => new { p.Id, Parsed = Enum.Parse<PublisherType>(InterceptorHelpers.Identity(p.Name)) })
            .ToList();

        Assert.Single(rows);
        Assert.Equal(PublisherType.Magazine, rows[0].Parsed);
    }

    [Fact]
    public void Select_CustomMethodTranslator_WithUntranslatableArg_FallsBack()
    {
        using TestDatabase db = new(b =>
        {
            System.Reflection.MethodInfo doubleMethod = typeof(InterceptorHelpers)
                .GetMethod(nameof(InterceptorHelpers.Double))!;
            b.MemberTranslators[doubleMethod] = SimpleTranslator.AsSimple((_, args) => $"({args[0]} * 2)");
        });

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 5, Price = 1 });

        var rows = db.Table<Book>()
            .Select(b => new
            {
                b.Id,
                Quad = InterceptorHelpers.Double(InterceptorHelpers.IdentityInt(b.AuthorId))
            })
            .ToList();

        Assert.Single(rows);
        Assert.Equal(10, rows[0].Quad);
    }

    [Fact]
    public void FtsMatch_DynamicTermWithParameters_ForwardsParametersInPrintf()
    {
        using TestDatabase db = new();
        db.Table<Article>().Schema.CreateTable();
        db.Table<ArticleSearch>().Schema.CreateTable();

        string suffix = "_tail";
        SQLiteCommand cmd = db.Table<ArticleSearch>()
            .Where(a => SQLiteFTS5Functions.Match(a, f => f.Term(a.Title + suffix)))
            .ToSqlCommand();

        Assert.Contains("printf", cmd.CommandText);
        Assert.True(cmd.Parameters.Count > 0);
    }

    [Fact]
    public void Enum_HasFlag_TranslatesToBitwiseAnd()
    {
        using TestDatabase db = new();
        db.Table<Publisher>().Schema.CreateTable();
        db.Table<Publisher>().Add(new Publisher
        {
            Id = 1,
            Name = "x",
            Type = PublisherType.Magazine
        });

        SQLiteCommand cmd = db.Table<Publisher>()
            .Where(p => p.Type.HasFlag(PublisherType.Magazine))
            .ToSqlCommand();

        Assert.Contains("&", cmd.CommandText);
    }


    private sealed class SqlInspector : SQLiteTable<Book>
    {
        public SqlInspector(SQLiteDatabase database, TableMapping table) : base(database, table) { }
        public string GetSql(Action<UpsertBuilder<Book>> configure) => GetUpsertInfo(configure).Sql;
    }

    [Fact]
    public void ExecuteUpdate_SetExpressionNotTranslatable_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().ExecuteUpdate(s => s.Set(b => b.Title, b => string.Intern(b.Title))));
    }

    [Fact]
    public void SQLiteCteTyped_GetEnumerator_ExecutesQuery()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "Test",
            AuthorId = 1,
            Price = 1
        });

        SQLiteCte<Book> cte = db.With(() => db.Table<Book>());

        List<Book> results = [];
        foreach (Book book in cte)
        {
            results.Add(book);
        }

        Assert.Single(results);
    }

    [Fact]
    public void SQLiteCte_NonGenericGetEnumerator_ExecutesQuery()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "Test",
            AuthorId = 1,
            Price = 1
        });

        SQLiteCte<Book> cte = db.With(() => db.Table<Book>());

        int count = 0;
        foreach (object _ in (System.Collections.IEnumerable)cte)
        {
            count++;
        }

        Assert.Equal(1, count);
    }

    [Fact]
    public void Queryable_IEnumerable_GetEnumerator_IteratesRows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "Test",
            AuthorId = 1,
            Price = 1
        });

        System.Collections.IEnumerable query = (System.Collections.IEnumerable)db.Table<Book>().Where(b => b.Id == 1);

        int count = 0;
        foreach (object _ in query)
        {
            count++;
        }

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task BeginTransactionAwaiter_OnCompleted_InvokedWhenContended()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        ManualResetEventSlim lockHeld = new(false);
        ManualResetEventSlim releaseSignal = new(false);

        Task lockHolder = Task.Run(() =>
        {
            using SQLiteTransaction tx = db.BeginTransaction();
            lockHeld.Set();
            releaseSignal.Wait();
            tx.Commit();
        }, TestContext.Current.CancellationToken);

        lockHeld.Wait(TestContext.Current.CancellationToken);

        SQLiteBeginTransactionAwaiter awaiter = db.BeginTransactionAsync(ct: TestContext.Current.CancellationToken).GetAwaiter();
        Assert.False(awaiter.IsCompleted);

        TaskCompletionSource tcs = new();
        awaiter.OnCompleted(tcs.SetResult);

        releaseSignal.Set();

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
        SQLiteTransaction tx2 = awaiter.GetResult();
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method
        tx2.Rollback();

        await lockHolder;
    }

    [Fact]
    public void GroupJoin_WithoutDefaultIfEmpty_ThrowsNotSupported()
    {
        using TestDatabase db = new();

        Assert.Throws<NotSupportedException>(() =>
        {
            db.Table<Book>()
                .GroupJoin(
                    db.Table<Author>(),
                    b => b.AuthorId,
                    a => a.Id,
                    (book, authors) => new
                    {
                        book,
                        authors
                    }
                )
                .ToSqlCommand();
        });
    }

    [Fact]
    public void DateOnly_StoredAsText_RoundTrip()
    {
        using TestDatabase db = new(b =>
        {
            b.DateOnlyStorage = DateOnlyStorageMode.Text;
        });
        db.Table<DateOnlyEntity>().Schema.CreateTable();

        db.Table<DateOnlyEntity>().Add(new DateOnlyEntity
        {
            Id = 1,
            Date = new DateOnly(2024, 6, 15)
        });
        DateOnlyEntity result = db.Table<DateOnlyEntity>().First();

        Assert.Equal(new DateOnly(2024, 6, 15), result.Date);
    }

    [Fact]
    public void TimeOnly_StoredAsText_RoundTrip()
    {
        using TestDatabase db = new(b =>
        {
            b.TimeOnlyStorage = TimeOnlyStorageMode.Text;
        });
        db.Table<TimeOnlyEntity>().Schema.CreateTable();

        db.Table<TimeOnlyEntity>().Add(new TimeOnlyEntity
        {
            Id = 1,
            Time = new TimeOnly(14, 30, 45)
        });
        TimeOnlyEntity result = db.Table<TimeOnlyEntity>().First();

        Assert.Equal(new TimeOnly(14, 30, 45), result.Time);
    }

    [Fact]
    public void DateTimeOffset_TextFormatted_WhereProperty_Throws()
    {
        using TestDatabase db = new(b =>
        {
            b.DateTimeOffsetStorage = DateTimeOffsetStorageMode.TextFormatted;
        });
        db.Table<DateTimeOffsetEntity>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<DateTimeOffsetEntity>().Where(e => e.Date.Year == 2024).ToList());
    }

    [Fact]
    public void TimeSpan_Text_WhereProperty_Throws()
    {
        using TestDatabase db = new(b =>
        {
            b.TimeSpanStorage = TimeSpanStorageMode.Text;
        });
        db.Table<TimeSpanEntity>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<TimeSpanEntity>().Where(e => e.Duration.Days == 1).ToList());
    }

    [Fact]
    public void DateOnly_StoredAsText_SelectProperty_ReturnsClientSide()
    {
        using TestDatabase db = new(b =>
        {
            b.DateOnlyStorage = DateOnlyStorageMode.Text;
        });
        db.Table<DateOnlyEntity>().Schema.CreateTable();
        db.Table<DateOnlyEntity>().Add(new DateOnlyEntity
        {
            Id = 1,
            Date = new DateOnly(2024, 6, 15)
        });

        int year = db.Table<DateOnlyEntity>().Select(e => e.Date.Year).First();

        Assert.Equal(2024, year);
    }

    [Fact]
    public void TimeOnly_StoredAsText_SelectProperty_ReturnsClientSide()
    {
        using TestDatabase db = new(b =>
        {
            b.TimeOnlyStorage = TimeOnlyStorageMode.Text;
        });
        db.Table<TimeOnlyEntity>().Schema.CreateTable();
        db.Table<TimeOnlyEntity>().Add(new TimeOnlyEntity
        {
            Id = 1,
            Time = new TimeOnly(14, 30, 45)
        });

        int hour = db.Table<TimeOnlyEntity>().Select(e => e.Time.Hour).First();

        Assert.Equal(14, hour);
    }

    [Fact]
    public void DateOnly_Text_WhereProperty_Throws()
    {
        using TestDatabase db = new(b =>
        {
            b.DateOnlyStorage = DateOnlyStorageMode.Text;
        });
        db.Table<DateOnlyEntity>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<DateOnlyEntity>().Where(e => e.Date.Year == 2024).ToList());
    }

    [Fact]
    public void TimeOnly_Text_WhereProperty_Throws()
    {
        using TestDatabase db = new(b =>
        {
            b.TimeOnlyStorage = TimeOnlyStorageMode.Text;
        });
        db.Table<TimeOnlyEntity>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<TimeOnlyEntity>().Where(e => e.Time.Hour == 14).ToList());
    }

    [Fact]
    public void DateTime_TextFormatted_AddDaysInWhere_Throws()
    {
        using TestDatabase db = new(b =>
        {
            b.DateTimeStorage = DateTimeStorageMode.TextFormatted;
        });
        db.Table<DateTimeEntity>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<DateTimeEntity>().Where(e => e.Date.AddDays(1) > DateTime.Now).ToList());
    }

    [Fact]
    public void Join_WithComputedMethodCallAssignment_ProducesCorrectSql()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            join author in db.Table<Author>() on book.AuthorId equals author.Id
            select new ComputedJoinDto
            {
                UpperTitle = book.Title.ToUpper(),
                AuthorName = author.Name
            }
        ).ToSqlCommand();

        Assert.Contains("UPPER", command.CommandText);
    }

    [Fact]
    public void Select_ToRecordWithComputedConstructorArg_ProducesCorrectSql()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<Book>()
            .Select(b => new SingleStringRecord(b.Title))
            .ToSqlCommand();

        Assert.Contains("BookTitle", command.CommandText);
    }

    [Fact]
    public void Join_WithCapturedQueryableVariable_ProducesSubquery()
    {
        using TestDatabase db = new();

        IQueryable<Author> filteredAuthors = db.Table<Author>().Where(a => a.Id > 0);

        SQLiteCommand command = db.Table<Book>()
            .Join(filteredAuthors, b => b.AuthorId, a => a.Id, (b, a) => new
            {
                b.Title,
                a.Name
            })
            .ToSqlCommand();

        Assert.Contains("SELECT", command.CommandText);
        Assert.Contains("JOIN", command.CommandText);
    }

    private class DateOnlyEntity
    {
        [Key]
        public int Id { get; set; }

        public DateOnly Date { get; set; }
    }

    private class TimeOnlyEntity
    {
        [Key]
        public int Id { get; set; }

        public TimeOnly Time { get; set; }
    }

    private class DateTimeOffsetEntity
    {
        [Key]
        public int Id { get; set; }

        public DateTimeOffset Date { get; set; }
    }

    private class TimeSpanEntity
    {
        [Key]
        public int Id { get; set; }

        public TimeSpan Duration { get; set; }
    }

    private class DateTimeEntity
    {
        [Key]
        public int Id { get; set; }

        public DateTime Date { get; set; }
    }

    [Fact]
    public void MemberInit_NonSimpleTypeMember_MapsNestedColumns()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Author>().Add(new Author
        {
            Id = 1,
            Name = "Alice",
            Email = "alice@example.com",
            BirthDate = new DateTime(1980, 1, 1)
        });
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "Test Book",
            AuthorId = 1,
            Price = 9.99
        });

        BookWithAuthorDto result = (
            from b in db.Table<Book>()
            join a in db.Table<Author>() on b.AuthorId equals a.Id
            select new
            {
                b,
                a
            }
            into x
            select new BookWithAuthorDto
            {
                Title = x.b.Title,
                Author = x.a
            }
        ).First();

        Assert.Equal("Test Book", result.Title);
        Assert.Equal("Alice", result.Author.Name);
    }

    private class BookWithAuthorDto
    {
        public string Title { get; set; } = string.Empty;
        public Author Author { get; set; } = null!;
    }

    private class ComputedJoinDto
    {
        public string UpperTitle { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
    }

    [Fact]
    public void GroupBy_KeyShapes_ExerciseSignatureCompute()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "alpha", AuthorId = 1, Price = 5 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "beta", AuthorId = 1, Price = 15 });
        db.Table<Book>().Add(new Book { Id = 3, Title = "gamma", AuthorId = 2, Price = 25 });

        List<IGrouping<int, Book>> byMember = db.Table<Book>().GroupBy(b => b.AuthorId).ToList();
        Assert.Equal(2, byMember.Count);

        var byAnon = db.Table<Book>().GroupBy(b => new { b.AuthorId, Bucket = b.Price > 10 ? "high" : "low" }).ToList();
        Assert.NotEmpty(byAnon);

        List<IGrouping<bool, Book>> byBinary = db.Table<Book>().GroupBy(b => b.Price > 10).ToList();
        Assert.Equal(2, byBinary.Count);

        List<IGrouping<int, Book>> byUnary = db.Table<Book>().GroupBy(b => -b.AuthorId).ToList();
        Assert.Equal(2, byUnary.Count);

        List<IGrouping<int, Book>> byMethodCall = db.Table<Book>().GroupBy(b => b.Title.Length).ToList();
        Assert.NotEmpty(byMethodCall);

        List<IGrouping<int, Book>> byConditional = db.Table<Book>().GroupBy(b => b.Price > 10 ? 1 : 0).ToList();
        Assert.Equal(2, byConditional.Count);

        List<IGrouping<int, Book>> byConstant = db.Table<Book>().GroupBy(b => 7).ToList();
        Assert.Single(byConstant);
    }


    [Fact]
    public void Select_AnonymousWithNonVisibleMemberInit_RoundTrips()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 7, Price = 1 });

        var rows = db.Table<Book>()
            .Select(b => new
            {
                b.Id,
                Wrap = new SqlTranslatorPrivateWrap { Value = b.AuthorId }
            })
            .ToList();

        Assert.Single(rows);
        Assert.Equal(1, rows[0].Id);
        Assert.Equal(7, rows[0].Wrap.Value);
    }

    private sealed class SqlTranslatorPrivateWrap
    {
        public int Value { get; set; }
        public static int Identity(int x) => x;
    }

    public sealed class SqlTranslatorOuterHolder
    {
        public object? Inner { get; set; }
    }

    [Fact]
    public void Select_AnonWithReflectedMethodCall_RoundTrips()
    {
        using TestDatabase db = new(b =>
        {
            b.MemberTranslators[typeof(SqlTranslatorPrivateWrap).GetMethod(nameof(SqlTranslatorPrivateWrap.Identity))!] =
                SimpleTranslator.AsSimple((_, args) => $"{args[0]}");
        });
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 7, Price = 1 });

        var rows = db.Table<Book>()
            .Select(b => new
            {
                Wrap = new SqlTranslatorPrivateWrap { Value = SqlTranslatorPrivateWrap.Identity(b.AuthorId) }
            })
            .ToList();

        Assert.Single(rows);
        Assert.Equal(7, rows[0].Wrap.Value);
    }

    [Fact]
    public void Select_NonAnonWrappingAnonWithReflectedArg_RoundTrips()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 7, Price = 1 });

        List<SqlTranslatorOuterHolder> rows = db.Table<Book>()
            .Select(b => new SqlTranslatorOuterHolder
            {
                Inner = new { Wrap = new SqlTranslatorPrivateWrap { Value = b.AuthorId } }
            })
            .ToList();

        Assert.Single(rows);
        object inner = rows[0].Inner!;
        object wrap = inner.GetType().GetProperty("Wrap")!.GetValue(inner)!;
        SqlTranslatorPrivateWrap typedWrap = (SqlTranslatorPrivateWrap)wrap;
        Assert.Equal(7, typedWrap.Value);
    }

    [Fact]
    public void Translate_NonIQueryableExpression_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        IQueryProvider provider = db.Table<Book>().Provider;
        IQueryable<Book> q = provider.CreateQuery<Book>(Expression.Parameter(typeof(int), "x"));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => q.ToList());
        Assert.Equal("Expression is not an IQueryable.", ex.Message);
    }

    [Fact]
    public void Translate_MethodCallWithNonQueryableDeclaringType_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        IQueryProvider provider = db.Table<Book>().Provider;
        Expression nonQueryableCall = Expression.Call(
            typeof(string).GetMethod(nameof(string.IsNullOrEmpty), [typeof(string)])!,
            Expression.Constant("hello"));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => provider.Execute<bool>(nonQueryableCall));
        Assert.StartsWith("Unsupported method:", ex.Message);
    }

    [Fact]
    public void Translate_ZeroArgMethodWithNonTableReturnType_Throws()
    {
        using TestDatabase db = new();

        IQueryProvider provider = db.Table<Book>().Provider;
        Expression voidCall = Expression.Call(
            Expression.Constant(db),
            typeof(SQLiteDatabase).GetMethod(nameof(SQLiteDatabase.Dispose))!);

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => provider.Execute<int>(voidCall));
        Assert.StartsWith("Unsupported method:", ex.Message);
    }

    public sealed class SqlTranslatorParameterlessProjection
    {
        public int Value { get; set; }
    }

    [Fact]
    public void Select_ParameterlessConstructorProjection_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 7, Price = 1 });

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => db.Table<Book>()
            .Select(b => new SqlTranslatorParameterlessProjection())
            .ToList());
        Assert.Contains("SqlTranslatorParameterlessProjection", ex.Message);
        Assert.Contains("constructor with arguments", ex.Message);
    }

    [Fact]
    public void Translate_ConstantNonTableQueryable_ThrowsCouldNotIdentifyFromClause()
    {
        using TestDatabase db = new();

        IQueryProvider provider = db.Table<Book>().Provider;
        IQueryable<int> nonSqliteQueryable = new[] { 1, 2, 3 }.AsQueryable();
        Expression constantExpression = Expression.Constant(nonSqliteQueryable);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => provider.Execute<int>(constantExpression));
        Assert.Equal("Could not identify FROM clause.", ex.Message);
    }

    [Fact]
    public void Select_NoMaterializerWithReflectionFallbackDisabled_Throws()
    {
        SQLiteOptionsBuilder builder = new($"FallbackThrowTest_{Guid.NewGuid():N}.db3");
        builder.SelectMaterializers["__no_match__"] = _ => null;
        builder.DisableReflectionFallback();
        SQLiteOptions options = builder.Build();
        File.Delete(options.DatabasePath);

        try
        {
            using SQLiteDatabase db = new(options);
            db.Table<Book>().Schema.CreateTable();
            db.Table<Book>().Add(new Book { Id = 1, Title = "alpha", AuthorId = 7, Price = 1 });

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => db.Table<Book>()
                .Select(b => new SqlTranslatorListContainer { Values = { b.Id, b.AuthorId } })
                .First());

            Assert.Contains("Select projection fell back to runtime reflection", ex.Message);
            Assert.Contains("ReflectionFallbackDisabled is set", ex.Message);
        }
        finally
        {
            for (int i = 0; i < 10 && File.Exists(options.DatabasePath); i++)
            {
                try { File.Delete(options.DatabasePath); break; }
                catch (IOException) { Thread.Sleep(50); }
            }
        }
    }

    [Fact]
    public void Select_MemberInitWithListBinding_RoutesThroughCompilerFallback_IncrementsCounter()
    {
        SQLiteOptionsBuilder builder = new($"FallbackListBindTest_{Guid.NewGuid():N}.db3");
        builder.SelectMaterializers["__no_match__"] = _ => null;
        SQLiteOptions options = builder.Build();
        File.Delete(options.DatabasePath);

        try
        {
            using SQLiteDatabase db = new(options);
            db.Table<Book>().Schema.CreateTable();
            db.Table<Book>().Add(new Book { Id = 1, Title = "alpha", AuthorId = 7, Price = 1 });

            long before = db.SelectCompilerFallbacks;
            Assert.Throws<NotSupportedException>(() => db.Table<Book>()
                .Select(b => new SqlTranslatorListContainer { Values = { b.Id, b.AuthorId } })
                .First());
            long after = db.SelectCompilerFallbacks;

            Assert.True(after > before);
        }
        finally
        {
            for (int i = 0; i < 10 && File.Exists(options.DatabasePath); i++)
            {
                try { File.Delete(options.DatabasePath); break; }
                catch (IOException) { Thread.Sleep(50); }
            }
        }
    }

    public sealed class SqlTranslatorListContainer
    {
        public List<int> Values { get; } = new();
    }

    [Fact]
    public void Select_TopLevelListInitProjection_RoundTrips()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 11, Title = "x", AuthorId = 22, Price = 1 });

        List<List<int>> rows = db.Table<Book>()
            .Select(b => new List<int> { b.Id, b.AuthorId })
            .ToList();

        Assert.Single(rows);
        Assert.Equal(new List<int> { 11, 22 }, rows[0]);
    }

    [Fact]
    public void Select_DictionaryWithMultiArgAdd_RoundTrips()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "alpha", AuthorId = 9, Price = 1 });

        List<Dictionary<int, string>> rows = db.Table<Book>()
            .Select(b => new Dictionary<int, string> { { b.Id, b.Title } })
            .ToList();

        Assert.Single(rows);
        Assert.Equal("alpha", rows[0][1]);
    }

    [Fact]
    public void Where_WithInlineFieldInitializer_FiltersCorrectly()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book
            {
                Id = 1,
                Title = "Match",
                AuthorId = 1,
                Price = 10
            },
            new Book
            {
                Id = 2,
                Title = "Other",
                AuthorId = 1,
                Price = 20
            }
        ]);

        List<Book> results = db.Table<Book>()
            .Where(b => b.Title == new TitleFilter
            {
                Value = "Match"
            }.Value)
            .ToList();

        Assert.Single(results);
        Assert.Equal("Match", results[0].Title);
    }

    private class TitleFilter
    {
        public string Value = string.Empty;
    }

    private record SingleStringRecord(string Title);

    [Fact]
    public void Select_WithMemberListBinding_PopulatesCollection()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "WAL Book",
            AuthorId = 1,
            Price = 9.99
        });

        BookWithTags result = db.Table<Book>()
            .Where(b => b.Id == 1)
            .Select(b => new BookWithTags
            {
                Title = b.Title,
                Tags =
                {
                    "fiction",
                    "bestseller"
                }
            })
            .First();

        Assert.Equal("WAL Book", result.Title);
        Assert.Equal(2, result.Tags.Count);
        Assert.Contains("fiction", result.Tags);
        Assert.Contains("bestseller", result.Tags);
    }

    [Fact]
    public void Select_Chained_WithMemberListBinding_PopulatesCollection()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "WAL Book",
            AuthorId = 1,
            Price = 9.99
        });

        BookWithTags result = db.Table<Book>()
            .Where(b => b.Id == 1)
            .Select(b => b.Title)
            .Select(t => new BookWithTags
            {
                Title = t,
                Tags =
                {
                    "fiction",
                    "bestseller"
                }
            })
            .First();

        Assert.Equal("WAL Book", result.Title);
        Assert.Equal(2, result.Tags.Count);
        Assert.Contains("fiction", result.Tags);
        Assert.Contains("bestseller", result.Tags);
    }

    [Fact]
    public void ConstantEnumCastToInt_Where_FiltersCorrectly()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "A",
            AuthorId = 1,
            Price = 1.0
        });
        db.Table<Book>().Add(new Book
        {
            Id = 2,
            Title = "B",
            AuthorId = 2,
            Price = 2.0
        });

        BookCategory category = BookCategory.Fiction;
        List<Book> results = db.Table<Book>().Where(b => b.AuthorId == (int)category).ToList();

        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    [Fact]
    public void ConstantLongCastToInt_Where_FiltersCorrectly()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "A",
            AuthorId = 1,
            Price = 1.0
        });
        db.Table<Book>().Add(new Book
        {
            Id = 2,
            Title = "B",
            AuthorId = 2,
            Price = 2.0
        });

        long id = 1L;
        List<Book> results = db.Table<Book>().Where(b => b.Id == (int)id).ToList();

        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    [Fact]
    public void CapturedTableVariable_InSubquery_FiltersCorrectly()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Author>().Add(new Author
        {
            Id = 1,
            Name = "Alice",
            Email = "a@a.com",
            BirthDate = DateTime.Today
        });
        db.Table<Author>().Add(new Author
        {
            Id = 2,
            Name = "Bob",
            Email = "b@b.com",
            BirthDate = DateTime.Today
        });
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "Book A",
            AuthorId = 1,
            Price = 1.0
        });

        var books = db.Table<Book>();
        List<Author> results = db.Table<Author>()
            .Where(a => books.Any(b => b.AuthorId == a.Id))
            .ToList();

        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    private class BookWithField
    {
        public string Title = string.Empty;
    }

    private class BookNoParameterlessCtor
    {
        public BookNoParameterlessCtor(int id) { Id = id; }
        public int Id { get; }
    }

    private class NoKeyEntity
    {
        public required string Name { get; set; }
    }


    private enum BookCategory
    {
        Fiction = 1,
        NonFiction = 2
    }

    private class BookWithTags
    {
        public string Title { get; set; } = string.Empty;
        public List<string> Tags { get; } = [];
    }

    [Fact]
    public void VisitBinary_EnumCastOnLeft_ComparesCorrectly()
    {
        using TestDatabase db = new();
        db.Table<EnumEntity>().Schema.CreateTable();
        db.Table<EnumEntity>().Add(new EnumEntity
        {
            Id = 1,
            Category = BookCategory.Fiction
        });
        db.Table<EnumEntity>().Add(new EnumEntity
        {
            Id = 2,
            Category = BookCategory.NonFiction
        });

        List<EnumEntity> results = db.Table<EnumEntity>()
            .Where(e => (int)e.Category == 1)
            .ToList();

        Assert.Single(results);
        Assert.Equal(BookCategory.Fiction, results[0].Category);
    }

    [Fact]
    public void VisitBinary_EnumCastOnRight_ComparesCorrectly()
    {
        using TestDatabase db = new();
        db.Table<EnumEntity>().Schema.CreateTable();
        db.Table<EnumEntity>().Add(new EnumEntity
        {
            Id = 1,
            Category = BookCategory.Fiction
        });
        db.Table<EnumEntity>().Add(new EnumEntity
        {
            Id = 2,
            Category = BookCategory.NonFiction
        });

        int value = 2;
        List<EnumEntity> results = db.Table<EnumEntity>()
            .Where(e => value == (int)e.Category)
            .ToList();

        Assert.Single(results);
        Assert.Equal(BookCategory.NonFiction, results[0].Category);
    }

    [Fact]
    public void VisitBinary_CharComparedToInt_FiltersCorrectly()
    {
        using TestDatabase db = new();
        db.Table<CharEntity>().Schema.CreateTable();
        db.Table<CharEntity>().Add(new CharEntity
        {
            Id = 1,
            Letter = 'A'
        });
        db.Table<CharEntity>().Add(new CharEntity
        {
            Id = 2,
            Letter = 'B'
        });

        List<CharEntity> results = db.Table<CharEntity>()
            .Where(e => e.Letter == 65)
            .ToList();

        Assert.Single(results);
        Assert.Equal('A', results[0].Letter);
    }

    [Fact]
    public void VisitBinary_IntComparedToChar_FiltersCorrectly()
    {
        using TestDatabase db = new();
        db.Table<CharEntity>().Schema.CreateTable();
        db.Table<CharEntity>().Add(new CharEntity
        {
            Id = 1,
            Letter = 'A'
        });
        db.Table<CharEntity>().Add(new CharEntity
        {
            Id = 2,
            Letter = 'B'
        });

        int code = 66;
        List<CharEntity> results = db.Table<CharEntity>()
            .Where(e => code == e.Letter)
            .ToList();

        Assert.Single(results);
        Assert.Equal('B', results[0].Letter);
    }

    [Fact]
    public void VisitConditional_TernaryInSelect_ProducesCaseWhen()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "A",
            AuthorId = 1,
            Price = 10.0
        });
        db.Table<Book>().Add(new Book
        {
            Id = 2,
            Title = "B",
            AuthorId = 2,
            Price = 50.0
        });

        List<string> results = db.Table<Book>()
            .Select(b => b.Price > 20 ? "expensive" : "cheap")
            .ToList();

        Assert.Contains("cheap", results);
        Assert.Contains("expensive", results);
    }

    [Fact]
    public void VisitConditional_TernaryInWhere_FiltersCorrectly()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "A",
            AuthorId = 1,
            Price = 10.0
        });
        db.Table<Book>().Add(new Book
        {
            Id = 2,
            Title = "B",
            AuthorId = 2,
            Price = 50.0
        });

        bool useHighPrice = true;
        double threshold = useHighPrice ? 20.0 : 5.0;
        List<Book> results = db.Table<Book>()
            .Where(b => b.Price > threshold)
            .ToList();

        Assert.Single(results);
    }

    [Fact]
    public void VisitUnary_CastIntToChar_ProducesCharFunction()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 65,
            Title = "A",
            AuthorId = 1,
            Price = 1.0
        });

        SQLiteCommand command = db.Table<Book>()
            .Select(b => (char)b.Id)
            .ToSqlCommand();

        Assert.Contains("CHAR", command.CommandText);
    }

    [Fact]
    public void VisitUnary_CastCharToInt_ProducesUnicodeFunction()
    {
        using TestDatabase db = new();
        db.Table<CharEntity>().Schema.CreateTable();
        db.Table<CharEntity>().Add(new CharEntity
        {
            Id = 1,
            Letter = 'A'
        });

        SQLiteCommand command = db.Table<CharEntity>()
            .Select(e => (int)e.Letter)
            .ToSqlCommand();

        Assert.Contains("UNICODE", command.CommandText);
    }

    [Fact]
    public void VisitUnary_CastEnumToUnderlyingType_PreservesValue()
    {
        using TestDatabase db = new();
        db.Table<EnumEntity>().Schema.CreateTable();
        db.Table<EnumEntity>().Add(new EnumEntity
        {
            Id = 1,
            Category = BookCategory.NonFiction
        });

        int result = db.Table<EnumEntity>()
            .Select(e => (int)e.Category)
            .First();

        Assert.Equal(2, result);
    }

    [Fact]
    public void VisitUnary_Negate_ProducesMinusOperator()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "A",
            AuthorId = 1,
            Price = 10.0
        });

        double result = db.Table<Book>()
            .Select(b => -b.Price)
            .First();

        Assert.Equal(-10.0, result);
    }

    [Fact]
    public void VisitUnary_Not_ProducesNotOperator()
    {
        using TestDatabase db = new();
        db.Table<EnumEntity>().Schema.CreateTable();
        db.Table<EnumEntity>().Add(new EnumEntity
        {
            Id = 1,
            Category = BookCategory.Fiction,
            IsActive = true
        });
        db.Table<EnumEntity>().Add(new EnumEntity
        {
            Id = 2,
            Category = BookCategory.NonFiction,
            IsActive = false
        });

        List<EnumEntity> results = db.Table<EnumEntity>()
            .Where(e => !e.IsActive)
            .ToList();

        Assert.Single(results);
        Assert.Equal(2, results[0].Id);
    }

    [Fact]
    public void VisitBinary_Coalesce_ProducesCoalesceFunction()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();
        db.Table<NullableEntity>().Add(new NullableEntity
        {
            Id = 1,
            Name = null
        });
        db.Table<NullableEntity>().Add(new NullableEntity
        {
            Id = 2,
            Name = "Bob"
        });

        List<string> results = db.Table<NullableEntity>()
            .Select(e => e.Name ?? "Unknown")
            .ToList();

        Assert.Contains("Unknown", results);
        Assert.Contains("Bob", results);
    }

    [Fact]
    public void VisitBinary_NullEquality_ProducesIsNull()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();
        db.Table<NullableEntity>().Add(new NullableEntity
        {
            Id = 1,
            Name = null
        });
        db.Table<NullableEntity>().Add(new NullableEntity
        {
            Id = 2,
            Name = "Bob"
        });

        List<NullableEntity> results = db.Table<NullableEntity>()
            .Where(e => e.Name == null)
            .ToList();

        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    [Fact]
    public void VisitBinary_NullInequality_ProducesIsNotNull()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();
        db.Table<NullableEntity>().Add(new NullableEntity
        {
            Id = 1,
            Name = null
        });
        db.Table<NullableEntity>().Add(new NullableEntity
        {
            Id = 2,
            Name = "Bob"
        });

        List<NullableEntity> results = db.Table<NullableEntity>()
            .Where(e => e.Name != null)
            .ToList();

        Assert.Single(results);
        Assert.Equal(2, results[0].Id);
    }

    [Fact]
    public void VisitBinary_Modulo_ComputesCorrectly()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "A",
            AuthorId = 1,
            Price = 1.0
        });
        db.Table<Book>().Add(new Book
        {
            Id = 2,
            Title = "B",
            AuthorId = 2,
            Price = 2.0
        });
        db.Table<Book>().Add(new Book
        {
            Id = 3,
            Title = "C",
            AuthorId = 3,
            Price = 3.0
        });

        List<Book> results = db.Table<Book>()
            .Where(b => b.Id % 2 == 1)
            .ToList();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void VisitBinary_Multiply_ComputesCorrectly()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "A",
            AuthorId = 1,
            Price = 5.0
        });

        double result = db.Table<Book>()
            .Select(b => b.Price * 3)
            .First();

        Assert.Equal(15.0, result);
    }

    [Fact]
    public void VisitBinary_Divide_ComputesCorrectly()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "A",
            AuthorId = 1,
            Price = 10.0
        });

        double result = db.Table<Book>()
            .Select(b => b.Price / 2)
            .First();

        Assert.Equal(5.0, result);
    }

    [Fact]
    public void VisitBinary_Subtract_ComputesCorrectly()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "A",
            AuthorId = 1,
            Price = 10.0
        });

        double result = db.Table<Book>()
            .Select(b => b.Price - 3)
            .First();

        Assert.Equal(7.0, result);
    }

    [Fact]
    public void VisitUnary_CastToGenericType_ProducesCast()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "A",
            AuthorId = 1,
            Price = 10.5
        });

        SQLiteCommand command = db.Table<Book>()
            .Select(b => (long)b.Price)
            .ToSqlCommand();

        Assert.Contains("CAST", command.CommandText);
    }

    private class EnumEntity
    {
        [Key]
        public int Id { get; set; }

        public BookCategory Category { get; set; }
        public bool IsActive { get; set; }
    }

    private class CharEntity
    {
        [Key]
        public int Id { get; set; }

        public char Letter { get; set; }
    }

    private class NullableEntity
    {
        [Key]
        public int Id { get; set; }

        public string? Name { get; set; }
    }

    private class PropertyVisitorEntity
    {
        [Key]
        public int Id { get; set; }

        public int? NullableValue { get; set; }
        public DateTime DateTimeValue { get; set; }
        public DateTimeOffset DateTimeOffsetValue { get; set; }
    }


    [Fact]
    public void SQLiteDatabase_OpenConnection_WhenAlreadyConnecting_WaitsForLock()
    {
        using TestDatabase db = new();
        db.OpenConnection();

        PropertyInfo isConnectingProp = typeof(SQLiteDatabase).GetProperty(nameof(SQLiteDatabase.IsConnecting))!;
        isConnectingProp.SetValue(db, true);

        db.OpenConnection();

        Assert.True(db.IsConnecting);

        isConnectingProp.SetValue(db, false);
    }

    [Fact]
    public void SQLiteDatabase_OpenConnection_InvalidPath_Throws()
    {
        SQLiteOptionsBuilder builder = new("/no-such-directory-xyz/db.sqlite");
        SQLiteOptions options = builder.Build();
        using SQLiteDatabase db = new(options);

        Assert.Throws<SQLiteException>(() => db.OpenConnection());
    }

    [Fact]
    public void SQLiteDatabase_BeginTransaction_SeparateConnection_InvalidPath_Throws()
    {
        SQLiteOptionsBuilder bad = new("/no-such-directory-xyz/db.sqlite");
        using SQLiteDatabase badDb = new(bad.Build());

        Assert.Throws<SQLiteException>(() => badDb.BeginTransaction(separateConnection: true));
    }

    [Fact]
    public void SQLiteDatabase_Execute_WithSingleSQLiteParameter_PassesThrough()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        int rows = db.Execute(
            "INSERT INTO Books (BookId, BookTitle, BookAuthorId, BookPrice) VALUES (@id, 't', 0, 0)",
            (object)new SQLiteParameter { Name = "@id", Value = 42 });

        Assert.Equal(1, rows);
        Assert.Equal(1, db.Table<Book>().Count(b => b.Id == 42));
    }

    [Fact]
    public void SQLiteOptions_GetConverterTypeForInterface_SecondCall_HitsCache()
    {
        using TestDatabase db = new(b =>
        {
            b.TypeConverters[typeof(List<int>)] =
                new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32);
        });

        List<IList<int>> first = db.CreateCommand("SELECT '[1,2,3]'", []).ExecuteQuery<IList<int>>().ToList();
        List<IList<int>> second = db.CreateCommand("SELECT '[4,5,6]'", []).ExecuteQuery<IList<int>>().ToList();

        Assert.Equal([1, 2, 3], first[0]);
        Assert.Equal([4, 5, 6], second[0]);
    }

    [FullTextSearch(ContentMode = FtsContentMode.External, ContentTable = typeof(SchemaSourceWithoutKey))]
    private class SchemaSearchExternal
    {
        [FullTextRowId]
        public int Id { get; set; }

        [FullTextIndexed]
        public required string Body { get; set; }
    }

    private class SchemaSourceWithoutKey
    {
        public int RowId { get; set; }
        public string Body { get; set; } = "";
    }

    [Fact]
    public void SQLiteSchema_ExternalFtsWithoutSourceKey_Throws()
    {
        using TestDatabase db = new();
        db.Table<SchemaSourceWithoutKey>().Schema.CreateTable();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => db.Table<SchemaSearchExternal>().Schema.CreateTable());
        Assert.Contains("SchemaSearchExternal", ex.Message);
        Assert.Contains("SchemaSourceWithoutKey", ex.Message);
        Assert.Contains("[Key]", ex.Message);
    }

    [Fact]
    public void Queryable_LeftJoin_TranslatesToLeftJoin()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Author>().Add(new Author { Id = 1, Name = "Alice", Email = "a@x", BirthDate = new DateTime(1980, 1, 1) });
        db.Table<Book>().Add(new Book { Id = 1, Title = "T", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "T2", AuthorId = 99, Price = 2 });

        List<int> rows = db.Table<Book>()
            .LeftJoin(db.Table<Author>(), b => b.AuthorId, a => a.Id, (b, a) => b.Id)
            .ToList();

        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public void Queryable_RightJoin_TranslatesToJoin()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Author>().Add(new Author { Id = 1, Name = "Alice", Email = "a@x", BirthDate = new DateTime(1980, 1, 1) });
        db.Table<Book>().Add(new Book { Id = 1, Title = "T", AuthorId = 1, Price = 1 });

        List<int> rows = db.Table<Book>()
            .RightJoin(db.Table<Author>(), b => b.AuthorId, a => a.Id, (b, a) => a.Id)
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void GroupBy_TwiceInSameQuery_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .GroupBy(b => b.AuthorId)
                .GroupBy(g => g.Key)
                .Select(g2 => g2.Key)
                .ToList());
    }

    [Fact]
    public void Where_NonSqlExpression_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Where(b => UnsupportedHelper(b.Title))
                .ToList());
    }

    [Fact]
    public void OrderBy_NonSqlExpression_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .OrderBy(b => UnsupportedHelper(b.Title))
                .ToList());
    }

    private static bool UnsupportedHelper(string s) => s.Length > 0;

    [Fact]
    public void Single_WithNonSqlPredicate_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Single(b => UnsupportedHelper(b.Title)));
    }

    [Fact]
    public void Aggregate_NonSqlExpression_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Sum(b => UnsupportedSelector(b.Title)));
    }

    private static double UnsupportedSelector(string s) => s.Length;

    private static int UnsupportedHelperReturningInt() => 1;
    private static long UnsupportedHelperReturningLong() => 1L;
    private static short UnsupportedHelperReturningShort() => 1;
    private static byte UnsupportedHelperReturningByte() => 1;
    private static sbyte UnsupportedHelperReturningSByte() => 1;
    private static ushort UnsupportedHelperReturningUShort() => 1;
    private static uint UnsupportedHelperReturningUInt() => 1u;
    private static ulong UnsupportedHelperReturningULong() => 1ul;
    private static double UnsupportedHelperReturningDouble() => 1.0;
    private static float UnsupportedHelperReturningFloat() => 1.0f;
    private static decimal UnsupportedHelperReturningDecimal() => 1m;

    private static int UnsupportedRowInt(int x) => x + 1;
    private static long UnsupportedRowLong(long x) => x + 1L;
    private static short UnsupportedRowShort(short x) => (short)(x + 1);
    private static byte UnsupportedRowByte(byte x) => (byte)(x + 1);
    private static sbyte UnsupportedRowSByte(sbyte x) => (sbyte)(x + 1);
    private static ushort UnsupportedRowUShort(ushort x) => (ushort)(x + 1);
    private static uint UnsupportedRowUInt(uint x) => x + 1u;
    private static ulong UnsupportedRowULong(ulong x) => x + 1ul;
    private static double UnsupportedRowDouble(double x) => x + 1.0;
    private static float UnsupportedRowFloat(float x) => x + 1.0f;
    private static decimal UnsupportedRowDecimal(decimal x) => x + 1m;
    private static bool UnsupportedRowBool(int x) => x > 0;

    private class NumericEntity
    {
        [Key]
        public int Id { get; set; }

        public long LongValue { get; set; }
        public short ShortValue { get; set; }
        public byte ByteValue { get; set; }
        public sbyte SByteValue { get; set; }
        public ushort UShortValue { get; set; }
        public uint UIntValue { get; set; }
        public ulong ULongValue { get; set; }
        public double DoubleValue { get; set; }
        public float FloatValue { get; set; }
        public decimal DecimalValue { get; set; }
    }

    private static SQLiteDatabase CreateCompilerFallbackDb()
    {
        SQLiteOptionsBuilder builder = new($"CompilerTest_{Guid.NewGuid():N}.db3");
        builder.SelectMaterializers["__no_match_for_compiler_path__"] = _ => null;
        SQLiteOptions options = builder.Build();
        File.Delete(options.DatabasePath);
        return new SQLiteDatabase(options);
    }

    private class CompilerEntity
    {
        [Key]
        public int Id { get; set; }

        public int Value { get; set; }
    }


    [Fact]
    public void QueryCompilerVisitor_BoolShortCircuit_AndAlso_ReachesCompiler()
    {
        using SQLiteDatabase db = CreateCompilerFallbackDb();
        db.Table<CompilerEntity>().Schema.CreateTable();
        db.Table<CompilerEntity>().Add(new CompilerEntity { Id = 1, Value = 10 });

        List<bool> rows = db.Table<CompilerEntity>()
            .Select(b => UnsupportedRowBool(b.Id) && b.Id > 0)
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void QueryCompilerVisitor_BinaryComparisons_ReachCompiler()
    {
        using SQLiteDatabase db = CreateCompilerFallbackDb();
        db.Table<CompilerEntity>().Schema.CreateTable();
        db.Table<CompilerEntity>().Add(new CompilerEntity { Id = 1, Value = 10 });

        Assert.NotEmpty(db.Table<CompilerEntity>().Select(b => b.Id > UnsupportedRowInt(b.Id)).ToList());
        Assert.NotEmpty(db.Table<CompilerEntity>().Select(b => b.Id >= UnsupportedRowInt(b.Id)).ToList());
        Assert.NotEmpty(db.Table<CompilerEntity>().Select(b => b.Id < UnsupportedRowInt(b.Id)).ToList());
        Assert.NotEmpty(db.Table<CompilerEntity>().Select(b => b.Id <= UnsupportedRowInt(b.Id)).ToList());
        Assert.NotEmpty(db.Table<CompilerEntity>().Select(b => b.Id != UnsupportedRowInt(b.Id)).ToList());
    }

    [Fact]
    public void QueryCompilerVisitor_BoolOps_AllVariants_ReachCompiler()
    {
        using SQLiteDatabase db = CreateCompilerFallbackDb();
        db.Table<CompilerEntity>().Schema.CreateTable();
        db.Table<CompilerEntity>().Add(new CompilerEntity { Id = 1, Value = 10 });

        Assert.NotEmpty(db.Table<CompilerEntity>().Select(b => UnsupportedRowBool(b.Id) && b.Id > 0).ToList());
        Assert.NotEmpty(db.Table<CompilerEntity>().Select(b => UnsupportedRowBool(b.Id) || b.Id > 0).ToList());
        Assert.NotEmpty(db.Table<CompilerEntity>().Select(b => UnsupportedRowBool(b.Id) & b.Id > 0).ToList());
        Assert.NotEmpty(db.Table<CompilerEntity>().Select(b => UnsupportedRowBool(b.Id) | b.Id > 0).ToList());
    }

    [Fact]
    public void QueryCompilerVisitor_ArrayIndex_ReachesCompiler()
    {
        using SQLiteDatabase db = CreateCompilerFallbackDb();
        db.Table<CompilerEntity>().Schema.CreateTable();
        db.Table<CompilerEntity>().Add(new CompilerEntity { Id = 1, Value = 10 });

        int[] arr = [10, 20, 30];
        List<int> rows = db.Table<CompilerEntity>()
            .Select(b => arr[UnsupportedRowInt(b.Id) % 3])
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void QueryCompilerVisitor_UnaryNegate_ReachesCompiler()
    {
        using SQLiteDatabase db = CreateCompilerFallbackDb();
        db.Table<CompilerEntity>().Schema.CreateTable();
        db.Table<CompilerEntity>().Add(new CompilerEntity { Id = 1, Value = 10 });

        List<int> rows = db.Table<CompilerEntity>().Select(b => -UnsupportedRowInt(b.Id)).ToList();
        Assert.Single(rows);
    }

    [Fact]
    public void QueryCompilerVisitor_UnaryNot_ReachesCompiler()
    {
        using SQLiteDatabase db = CreateCompilerFallbackDb();
        db.Table<CompilerEntity>().Schema.CreateTable();
        db.Table<CompilerEntity>().Add(new CompilerEntity { Id = 1, Value = 10 });

        List<bool> rows = db.Table<CompilerEntity>().Select(b => !UnsupportedRowBool(b.Id)).ToList();
        Assert.Single(rows);
    }

    [Fact]
    public void QueryCompilerVisitor_VisitConditional_ReachesCompiler()
    {
        using SQLiteDatabase db = CreateCompilerFallbackDb();
        db.Table<CompilerEntity>().Schema.CreateTable();
        db.Table<CompilerEntity>().Add(new CompilerEntity { Id = 1, Value = 10 });

        List<int> rows = db.Table<CompilerEntity>()
            .Select(b => b.Id > 0 ? UnsupportedHelperReturningInt() : 0)
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void QueryCompilerVisitor_VisitUnary_NotConvertNegate_ReachesCompiler()
    {
        using SQLiteDatabase db = CreateCompilerFallbackDb();
        db.Table<CompilerEntity>().Schema.CreateTable();
        db.Table<CompilerEntity>().Add(new CompilerEntity { Id = 1, Value = 10 });

        List<bool> notRows = db.Table<CompilerEntity>()
            .Select(b => !(UnsupportedHelperReturningInt() == b.Id))
            .ToList();
        Assert.Single(notRows);

        List<long> convertRows = db.Table<CompilerEntity>()
            .Select(b => (long)(b.Id + UnsupportedHelperReturningInt()))
            .ToList();
        Assert.Single(convertRows);
    }

    private static string UnsupportedReturningString() => "abc";

    [Fact]
    public void QueryCompilerVisitor_VisitMember_OnRuntimeOnlyInstance_ReachesCompiler()
    {
        using SQLiteDatabase db = CreateCompilerFallbackDb();
        db.Table<CompilerEntity>().Schema.CreateTable();
        db.Table<CompilerEntity>().Add(new CompilerEntity { Id = 1, Value = 10 });

        List<int> rows = db.Table<CompilerEntity>()
            .Select(b => UnsupportedReturningString().Length + b.Id)
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void QueryCompilerVisitor_VisitNew_AnonProjection_ReachesCompiler()
    {
        using SQLiteDatabase db = CreateCompilerFallbackDb();
        db.Table<CompilerEntity>().Schema.CreateTable();
        db.Table<CompilerEntity>().Add(new CompilerEntity { Id = 1, Value = 10 });

        var rows = db.Table<CompilerEntity>()
            .Select(b => new { Id = b.Id, Computed = UnsupportedHelperReturningInt() })
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void QueryCompilerVisitor_VisitNewArray_ReachesCompiler()
    {
        using SQLiteDatabase db = CreateCompilerFallbackDb();
        db.Table<CompilerEntity>().Schema.CreateTable();
        db.Table<CompilerEntity>().Add(new CompilerEntity { Id = 1, Value = 10 });

        List<int[]> rows = db.Table<CompilerEntity>()
            .Select(b => new[] { b.Id, UnsupportedHelperReturningInt() })
            .ToList();

        Assert.Single(rows);
    }

    public class CompilerMixedContainer
    {
        public string Title { get; set; } = "";
        public List<int> Items { get; } = new();
    }

    [Fact]
    public void QueryCompilerVisitor_VisitMemberInit_MixedAssignmentAndListBinding_ReachesCompiler()
    {
        using SQLiteDatabase db = CreateCompilerFallbackDb();
        db.Table<CompilerEntity>().Schema.CreateTable();
        db.Table<CompilerEntity>().Add(new CompilerEntity { Id = 1, Value = 10 });

        List<CompilerMixedContainer> rows = db.Table<CompilerEntity>()
            .Select(b => new CompilerMixedContainer
            {
                Title = "x" + UnsupportedRowInt(b.Id),
                Items = { 1, 2, 3 }
            })
            .ToList();

        Assert.Single(rows);
        Assert.Equal([1, 2, 3], rows[0].Items);
    }

    public class CompilerInner
    {
        public int X { get; set; }
    }

    public class CompilerOuter
    {
        public CompilerInner Inner { get; } = new();
        public int OuterId { get; set; }
    }

    [Fact]
    public void QueryCompilerVisitor_VisitMemberInit_NestedMemberBinding_ReachesCompiler()
    {
        using SQLiteDatabase db = CreateCompilerFallbackDb();
        db.Table<CompilerEntity>().Schema.CreateTable();
        db.Table<CompilerEntity>().Add(new CompilerEntity { Id = 1, Value = 10 });

        List<CompilerOuter> rows = db.Table<CompilerEntity>()
            .Select(b => new CompilerOuter
            {
                OuterId = b.Id,
                Inner = { X = UnsupportedRowInt(b.Id) }
            })
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void QueryCompilerVisitor_VisitMemberInit_PropertyAssignment_ReachesCompiler()
    {
        using SQLiteDatabase db = CreateCompilerFallbackDb();
        db.Table<CompilerEntity>().Schema.CreateTable();
        db.Table<CompilerEntity>().Add(new CompilerEntity { Id = 1, Value = 10 });

        List<CompilerEntity> rows = db.Table<CompilerEntity>()
            .Select(b => new CompilerEntity { Id = b.Id, Value = UnsupportedRowInt(b.Value) })
            .ToList();

        Assert.Single(rows);
        Assert.Equal(11, rows[0].Value);
    }

    [Fact]
    public void QueryCompilerVisitor_VisitListInit_ReachesCompiler()
    {
        using SQLiteDatabase db = CreateCompilerFallbackDb();
        db.Table<CompilerEntity>().Schema.CreateTable();
        db.Table<CompilerEntity>().Add(new CompilerEntity { Id = 1, Value = 10 });

        List<List<int>> rows = db.Table<CompilerEntity>()
            .Select(b => new List<int> { b.Id, UnsupportedHelperReturningInt() })
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void QueryCompilerVisitor_VisitMethodCall_ReachesCompiler()
    {
        using SQLiteDatabase db = CreateCompilerFallbackDb();
        db.Table<CompilerEntity>().Schema.CreateTable();
        db.Table<CompilerEntity>().Add(new CompilerEntity { Id = 1, Value = 10 });

        List<string> rows = db.Table<CompilerEntity>()
            .Select(b => UnsupportedFormat(b.Id))
            .ToList();

        Assert.Single(rows);
    }

    private static string UnsupportedFormat(int x) => "x" + x;

    [Fact]
    public void QueryCompilerVisitor_BinaryArithmetic_AllNumericTypes_RoundTrip()
    {
        using SQLiteDatabase db = CreateCompilerFallbackDb();
        db.Table<NumericEntity>().Schema.CreateTable();
        db.Table<NumericEntity>().Add(new NumericEntity
        {
            Id = 1,
            LongValue = 10L,
            ShortValue = 5,
            ByteValue = 5,
            SByteValue = 5,
            UShortValue = 5,
            UIntValue = 5u,
            ULongValue = 5ul,
            DoubleValue = 2.5,
            FloatValue = 2.5f,
            DecimalValue = 2.5m
        });

        Assert.NotEmpty(db.Table<NumericEntity>().Select(b => b.LongValue + UnsupportedRowLong(b.LongValue)).ToList());
        Assert.NotEmpty(db.Table<NumericEntity>().Select(b => b.ShortValue + UnsupportedRowShort(b.ShortValue)).ToList());
        Assert.NotEmpty(db.Table<NumericEntity>().Select(b => b.ByteValue + UnsupportedRowByte(b.ByteValue)).ToList());
        Assert.NotEmpty(db.Table<NumericEntity>().Select(b => b.SByteValue + UnsupportedRowSByte(b.SByteValue)).ToList());
        Assert.NotEmpty(db.Table<NumericEntity>().Select(b => b.UShortValue + UnsupportedRowUShort(b.UShortValue)).ToList());
        Assert.NotEmpty(db.Table<NumericEntity>().Select(b => b.UIntValue + UnsupportedRowUInt(b.UIntValue)).ToList());
        Assert.NotEmpty(db.Table<NumericEntity>().Select(b => b.ULongValue + UnsupportedRowULong(b.ULongValue)).ToList());
        Assert.NotEmpty(db.Table<NumericEntity>().Select(b => b.DoubleValue + UnsupportedRowDouble(b.DoubleValue)).ToList());
        Assert.NotEmpty(db.Table<NumericEntity>().Select(b => b.FloatValue + UnsupportedRowFloat(b.FloatValue)).ToList());
        Assert.NotEmpty(db.Table<NumericEntity>().Select(b => b.DecimalValue + UnsupportedRowDecimal(b.DecimalValue)).ToList());

        Assert.NotEmpty(db.Table<NumericEntity>().Select(b => b.LongValue - UnsupportedRowLong(b.LongValue)).ToList());
        Assert.NotEmpty(db.Table<NumericEntity>().Select(b => b.LongValue * UnsupportedRowLong(b.LongValue)).ToList());
        Assert.NotEmpty(db.Table<NumericEntity>().Select(b => b.LongValue / UnsupportedRowLong(b.LongValue)).ToList());
        Assert.NotEmpty(db.Table<NumericEntity>().Select(b => b.LongValue % UnsupportedRowLong(b.LongValue)).ToList());

        Assert.NotEmpty(db.Table<NumericEntity>().Select(b => -b.LongValue + UnsupportedRowLong(b.LongValue)).ToList());
        Assert.NotEmpty(db.Table<NumericEntity>().Select(b => -b.ShortValue + UnsupportedRowShort(b.ShortValue)).ToList());
        Assert.NotEmpty(db.Table<NumericEntity>().Select(b => -b.SByteValue + UnsupportedRowSByte(b.SByteValue)).ToList());
        Assert.NotEmpty(db.Table<NumericEntity>().Select(b => -b.DoubleValue + UnsupportedRowDouble(b.DoubleValue)).ToList());
        Assert.NotEmpty(db.Table<NumericEntity>().Select(b => -b.FloatValue + UnsupportedRowFloat(b.FloatValue)).ToList());
        Assert.NotEmpty(db.Table<NumericEntity>().Select(b => -b.DecimalValue + UnsupportedRowDecimal(b.DecimalValue)).ToList());
    }

    public class WritableHolder
    {
        public string? Title { get; set; }
    }

    [Fact]
    public void Select_ChainedIdentitySelectAfterNonSqlProjection_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        Assert.ThrowsAny<Exception>(() =>
            db.Table<Book>()
                .Select(b => new WritableHolder { Title = UnsupportedHelperString(b.Title) })
                .Select(b => b)
                .ToList());
    }

    private static string UnsupportedHelperString(string s) => s + "x";

    [Fact]
    public void GroupBy_ParameterlessConstructorKey_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().GroupBy(_ => new SqlTranslatorParameterlessProjection()).Select(g => g.Key).ToList());
    }

    [Fact]
    public void GroupBy_WithUnsupportedFunctionOnRowColumn_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().GroupBy(b => UnsupportedSelector(b.Title)).Select(g => g.Key).ToList());
    }

    [Fact]
    public void Join_OnNonSqliteIQueryable_ConstantSource_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        IQueryable<int> external = new[] { 1, 2, 3 }.AsQueryable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Join(external, b => b.Id, x => x, (b, x) => b.Id).ToList());
    }

    [Fact]
    public void Select_ChainedMemberAccessNoMatchingBinding_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 7, Title = "x", AuthorId = 9, Price = 13 });

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Select(b => new Book { Title = b.Title, AuthorId = b.AuthorId, Price = b.Price })
                .Select(b => b.Id)
                .ToList());
        Assert.Contains("'Id'", ex.Message);
        Assert.Contains("inner projection", ex.Message);
    }

    [Fact]
    public void Select_ChainedMemberAccessMatchedBinding_UsesBindingExpression()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 7, Title = "x", AuthorId = 9, Price = 1 });

        List<int> rows = db.Table<Book>()
            .Select(b => new Book { Id = b.Id, Title = b.Title, AuthorId = b.AuthorId, Price = b.Price })
            .Select(b => b.AuthorId)
            .ToList();

        Assert.Single(rows);
        Assert.Equal(9, rows[0]);
    }

    [Fact]
    public void Select_ChainedConstantOuter_ReturnsConstantPerRow()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "y", AuthorId = 2, Price = 2 });

        List<int> rows = db.Table<Book>()
            .Select(b => new Book { Id = b.Id, Title = b.Title, AuthorId = b.AuthorId, Price = b.Price })
            .Select(_ => 5)
            .ToList();

        Assert.Equal([5, 5], rows);
    }

    [Fact]
    public void Join_WithNonRecursiveCte_ResolvesAsCteSource()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "y", AuthorId = 2, Price = 2 });

        SQLiteCte<CteCounter> cte = db.With<CteCounter>(() =>
            db.Values(new CteCounter { N = 1 }).Concat(db.Values(new CteCounter { N = 2 })));

        List<int> rows = (
            from b in db.Table<Book>()
            join c in cte on b.Id equals c.N
            select b.Id
        ).OrderBy(x => x).ToList();

        Assert.Equal([1, 2], rows);
    }

    [Fact]
    public void Join_WithRecursiveCte_ResolvesAsCteSource()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 3, Title = "y", AuthorId = 2, Price = 2 });

        SQLiteCte<CteCounter> cte = db.WithRecursive<CteCounter>(self =>
            db.Values(new CteCounter { N = 1 })
                .Concat(from c in self
                        where c.N < 5
                        select new CteCounter { N = c.N + 1 }));

        List<int> rows = (
            from b in db.Table<Book>()
            join c in cte on b.Id equals c.N
            select b.Id
        ).ToList();

        Assert.Equal(2, rows.Count);
        Assert.Contains(1, rows);
        Assert.Contains(3, rows);
    }

    private class CteCounter
    {
        public int N { get; set; }
    }

    [Fact]
    public void Contains_OnMultiColumnAnon_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        var probe = new { Id = 1, Title = "x" };
        IQueryable<Book> source = db.Table<Book>();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Where(b => source.Select(b2 => new { b2.Id, b2.Title }).Contains(probe)).ToList());
    }

    [Fact]
    public void SQLiteDataReader_Read_StepError_ThrowsSQLiteException()
    {
        using TestDatabase db = new();

        SQLiteCommand cmd = db.CreateCommand("SELECT abs(-9223372036854775808)", []);
        using SQLiteDataReader reader = cmd.ExecuteReader();

        Assert.Throws<SQLiteException>(() => reader.Read());
    }

    [Fact]
    public void PropertyVisitor_NullableValueProperty_FallsThroughToColumn()
    {
        using TestDatabase db = new();
        db.Table<PropertyVisitorEntity>().Schema.CreateTable();
        db.Table<PropertyVisitorEntity>().Add(new PropertyVisitorEntity { Id = 1, NullableValue = 42 });
        db.Table<PropertyVisitorEntity>().Add(new PropertyVisitorEntity { Id = 2, NullableValue = 5 });

        List<PropertyVisitorEntity> results = db.Table<PropertyVisitorEntity>()
            .Where(e => e.NullableValue!.Value > 10)
            .ToList();

        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    [Fact]
    public void PropertyVisitor_DateTimeUnsupportedProperty_FallsThroughToColumn()
    {
        using TestDatabase db = new();
        db.Table<PropertyVisitorEntity>().Schema.CreateTable();
        DateTime stamp = new(2024, 5, 1, 12, 30, 45, DateTimeKind.Utc);
        db.Table<PropertyVisitorEntity>().Add(new PropertyVisitorEntity { Id = 1, DateTimeValue = stamp });

        List<PropertyVisitorEntity> results = db.Table<PropertyVisitorEntity>()
            .Where(e => e.DateTimeValue.Date == stamp)
            .ToList();

        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    [Fact]
    public void PropertyVisitor_DateTimeOffsetUnsupportedProperty_FallsThroughToColumn()
    {
        using TestDatabase db = new();
        db.Table<PropertyVisitorEntity>().Schema.CreateTable();
        DateTimeOffset stamp = new(2024, 5, 1, 12, 30, 45, TimeSpan.Zero);
        db.Table<PropertyVisitorEntity>().Add(new PropertyVisitorEntity { Id = 1, DateTimeOffsetValue = stamp });

        List<PropertyVisitorEntity> results = db.Table<PropertyVisitorEntity>()
            .Where(e => e.DateTimeOffsetValue.Date == stamp.DateTime)
            .ToList();

        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    [Fact]
    public void Count_AfterMultiColumnAnonymousProjection_ReturnsRowCount()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 1, Price = 2 });
        db.Table<Book>().Add(new Book { Id = 3, Title = "c", AuthorId = 1, Price = 3 });

        int count = db.Table<Book>()
            .Select(b => new { b.Id, b.Title })
            .Count();

        Assert.Equal(3, count);
    }

    [Fact]
    public void LongCount_AfterMultiColumnAnonymousProjection_ReturnsRowCount()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 1, Price = 2 });

        long count = db.Table<Book>()
            .Select(b => new { b.Id, b.Title })
            .LongCount();

        Assert.Equal(2L, count);
    }

    [Fact]
    public void Select_NestedMemberInitWithListBinding_PreservesNestedAndPopulatesList()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 7, Title = "T", AuthorId = 1, Price = 1 });

        WrapperWithInnerAndTags result = db.Table<Book>()
            .Select(b => new WrapperWithInnerAndTags
            {
                Inner = new InnerHolder { X = b.Id },
                Tags =
                {
                    "alpha",
                    "beta"
                }
            })
            .First();

        Assert.NotNull(result.Inner);
        Assert.Equal(7, result.Inner.X);
        Assert.Equal(["alpha", "beta"], result.Tags);
    }

    [Fact]
    public void Sum_AfterCastWithoutSelect_ThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });

        NotSupportedException ex = Assert.Throws<NotSupportedException>(
            () => db.Table<Book>().Cast<int>().Sum());

        Assert.Contains("Sum requires a single scalar column", ex.Message);
        Assert.Contains(".Sum(x => x.Column)", ex.Message);
        Assert.Contains(".Select(x => x.Column).Sum()", ex.Message);
    }

    [Fact]
    public void Max_AfterCastWithoutSelect_ThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });

        NotSupportedException ex = Assert.Throws<NotSupportedException>(
            () => db.Table<Book>().Cast<int>().Max());

        Assert.Contains("Max requires a single scalar column", ex.Message);
    }

    [Fact]
    public void Select_DoubleNestedMemberInitWithListBinding_PreservesDeepStructure()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 4, Title = "T", AuthorId = 9, Price = 2 });

        DeepWrapperWithTags result = db.Table<Book>()
            .Select(b => new DeepWrapperWithTags
            {
                Outer = new OuterHolder
                {
                    Inner = new InnerHolder { X = b.AuthorId }
                },
                Tags =
                {
                    "x"
                }
            })
            .First();

        Assert.Equal(9, result.Outer.Inner.X);
        Assert.Equal(["x"], result.Tags);
    }

    private class WrapperWithInnerAndTags
    {
        public InnerHolder Inner { get; set; } = new();
        public List<string> Tags { get; } = [];
    }

    private class InnerHolder
    {
        public int X { get; set; }
    }

    private class DeepWrapperWithTags
    {
        public OuterHolder Outer { get; set; } = new();
        public List<string> Tags { get; } = [];
    }

    private class OuterHolder
    {
        public InnerHolder Inner { get; set; } = new();
    }

    [Fact]
    public void SG_ChainedSelect_DirectMember_FlattensThroughInnerMemberInit()
    {
        using SQLiteDatabase db = CreateCompilerFallbackDb();
        db.Table<CompilerEntity>().Schema.CreateTable();
        db.Table<CompilerEntity>().Add(new CompilerEntity { Id = 7, Value = 99 });

        List<int> ids = db.Table<CompilerEntity>()
            .Select(x => new CompilerEntity { Id = x.Id, Value = x.Value })
            .Select(x => x.Id)
            .ToList();

        Assert.Equal([7], ids);
    }

    [Fact]
    public void SG_ChainedSelect_NonMemberOuter_FlattensViaParameterSubstitution()
    {
        using SQLiteDatabase db = CreateCompilerFallbackDb();
        db.Table<CompilerEntity>().Schema.CreateTable();
        db.Table<CompilerEntity>().Add(new CompilerEntity { Id = 3, Value = 5 });

        List<int> values = db.Table<CompilerEntity>()
            .Select(x => new CompilerEntity { Id = x.Id, Value = x.Value })
            .Select(x => x.Value + 1)
            .ToList();

        Assert.Equal([6], values);
    }

    [Fact]
    public void Where_BinaryRightShift_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Where(b => (b.Id >> 1) > 0).ToList());
    }

    [Fact]
    public void Where_NegateColumn_TranslatesToSql()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });

        List<int> ids = db.Table<Book>()
            .Where(b => -b.Id < 0)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void Where_CapturedHolderMemberAccess_TranslatesToParameter()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 5, Title = "a", AuthorId = 1, Price = 1 });

        var filter = new { Id = 5 };
        List<int> ids = db.Table<Book>()
            .Where(b => b.Id == filter.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([5], ids);
    }

    [Fact]
    public void Where_EnumCastToUnderlyingTypeOnConstant_Translates()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = (int)DayOfWeek.Monday, Price = 1 });

        DayOfWeek day = DayOfWeek.Monday;
        List<int> ids = db.Table<Book>()
            .Where(b => b.AuthorId == (int)day)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void Where_StringEqualsCapturedComplexObject_FallsBack()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "alpha", AuthorId = 1, Price = 1 });

        object captured = new { Title = "alpha" };

        Assert.ThrowsAny<Exception>(() =>
            db.Table<Book>().Where(b => captured.Equals(b.Title)).ToList());
    }

    private static int PredicateCustom<T>(IEnumerable<T> source, Func<T, bool> predicate) => 0;

    [Fact]
    public void PredicateMethodTranslator_UnresolvableInstance_ReturnsOriginalNode()
    {
        MethodInfo method = typeof(CoverageGapTests)
            .GetMethod(nameof(PredicateCustom), BindingFlags.NonPublic | BindingFlags.Static)!;

        SQLiteOptionsBuilder builder = new($"PredicateUnresolvable_{Guid.NewGuid():N}.db3");
        builder.MemberTranslators[method] = SimpleTranslator.AsPredicate(
            (instance, predicate) => $"(predicate({instance}, {predicate}))");

        SQLiteOptions options = builder.Build();
        File.Delete(options.DatabasePath);

        try
        {
            using SQLiteDatabase db = new(options);
            db.Table<Book>().Schema.CreateTable();

            Assert.ThrowsAny<Exception>(() =>
                db.Table<Book>()
                    .Select(b => PredicateCustom<int>(default!, x => x == b.Id))
                    .ToList());
        }
        finally
        {
            for (int i = 0; i < 10 && File.Exists(options.DatabasePath); i++)
            {
                try { File.Delete(options.DatabasePath); break; }
                catch (IOException) { Thread.Sleep(50); }
            }
        }
    }


    [Fact]
    public void SG_ChainedSelect_ConstantOuter_FlattenReturnsNull()
    {
        using SQLiteDatabase db = CreateCompilerFallbackDb();
        db.Table<CompilerEntity>().Schema.CreateTable();
        db.Table<CompilerEntity>().Add(new CompilerEntity { Id = 1, Value = 42 });

        List<int> values = db.Table<CompilerEntity>()
            .Select(x => new CompilerEntity { Id = x.Id, Value = x.Value })
            .Select(_ => 99)
            .ToList();

        Assert.Equal([99], values);
    }

    [Fact]
    public void Contains_NonSimpleCapturedValue_OnSingleColumnObjectSource_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });

        object complex = new ContainsTestComplex();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Select(b => (object)b.Id).Contains(complex));
    }

    [Fact]
    public void Join_WithUnsupportedInnerSourceType_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });

        int[] inner = [1, 2, 3];

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Join(inner, b => b.Id, i => i, (b, i) => b)
                .ToList());

        Assert.Contains("not supported in join", ex.Message);
    }

    private sealed class ContainsTestComplex
    {
    }
}