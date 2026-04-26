using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Attributes;
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
        db.Schema.CreateTable<BookArchive>();

        IQueryable<BookArchive> queryable = Array.Empty<BookArchive>().AsQueryable();
        Assert.Throws<InvalidOperationException>(() => db.Table<BookArchive>().InsertFromQuery(queryable));
    }

    [Fact]
    public void ExecuteUpdate_SetOnMethodExpression_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

        Assert.Throws<ArgumentException>(() =>
            db.Table<Book>().ExecuteUpdate(s => s.Set(b => b.Title.ToUpper(), "X")));
    }

    [Fact]
    public void ExecuteUpdate_SetOnNonDirectProperty_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

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
        db.Schema.CreateTable<Book>();

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
        db.Schema.CreateTable<Book>();

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
        db.Schema.CreateTable<Book>();

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
        db.Schema.CreateTable<Book>();

        SqlInspector inspector = new(db, db.TableMapping<Book>());
        string sql = inspector.GetSql(c => c.OnConflict(b => new { b.Id, b.Title, b.AuthorId, b.Price }).DoUpdateAll());

        Assert.Contains("DO NOTHING", sql);
    }

    [Fact]
    public void InsertFromQuery_SourceFromDifferentDatabase_Throws()
    {
        using TestDatabase target = new();
        using TestDatabase otherDb = new(methodName: "Other");

        target.Schema.CreateTable<BookArchive>();
        otherDb.Schema.CreateTable<BookArchive>();

        Assert.Throws<InvalidOperationException>(() =>
            target.Table<BookArchive>().InsertFromQuery(otherDb.Table<BookArchive>()));
    }

    [Fact]
    public void Remove_OnEntityWithoutPrimaryKey_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NoKeyEntity>();

        Assert.ThrowsAny<Exception>(() =>
            db.Table<NoKeyEntity>().Remove(new NoKeyEntity { Name = "x" }));
    }

    [Fact]
    public void AddOrUpdate_UnknownConflictValue_FallsBackToReplace()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

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
        db.Schema.CreateTable<Book>();

        Assert.Throws<InvalidOperationException>(() =>
            db.Schema.CreateIndex<Book>(b => b.Price + 1));
    }

    [Fact]
    public void Schema_CreateIndex_BoxedColumn_StripsConvert()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

        db.Schema.CreateIndex<Book>(b => (object)b.Price, name: "IX_Boxed_Price");

        Assert.True(db.Schema.IndexExists("IX_Boxed_Price"));
    }

    [Fact]
    public void CreateCommand_BindParameterByMissingName_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

        Assert.ThrowsAny<Exception>(() =>
            db.Execute("SELECT * FROM Books WHERE BookId = @existing",
                [new SQLiteParameter { Name = "@missing", Value = 1 }]));
    }

    [Fact]
    public void Functions_Regexp_TranslatesToSqlRegexpOperator()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

        SQLiteCommand cmd = db.Table<Book>()
            .Where(b => SQLiteFunctions.Regexp(b.Title, "^A.*"))
            .ToSqlCommand();

        Assert.Contains("REGEXP", cmd.CommandText);
    }

    [Fact]
    public void Functions_Match_StringColumnOverload_BuildsScopedMatch()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();
        db.Schema.CreateTable<ArticleSearch>();

        SQLiteCommand cmd = db.Table<ArticleSearch>()
            .Where(a => SQLiteFunctions.Match(a.Title, "native"))
            .ToSqlCommand();

        Assert.Contains("MATCH", cmd.CommandText);
        Assert.Contains("Title", cmd.CommandText);
    }

    [Fact]
    public void Functions_Match_StringColumnOverload_NotMemberAccess_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();
        db.Schema.CreateTable<ArticleSearch>();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<ArticleSearch>()
                .Where(a => SQLiteFunctions.Match(a.Title.Substring(0), "native"))
                .ToSqlCommand());
    }

    [Fact]
    public void Functions_Snippet_NonFtsEntity_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Select(b => SQLiteFunctions.Snippet(b, b.Title, "<", ">", "...", 5))
                .ToSqlCommand());
    }

    [Fact]
    public void Functions_Snippet_ColumnNotInFtsIndex_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();
        db.Schema.CreateTable<ArticleSearch>();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<ArticleSearch>()
                .Select(a => SQLiteFunctions.Snippet(a, a.Id.ToString(), "<", ">", "...", 5))
                .ToSqlCommand());
    }

    [Fact]
    public void Guid_InstanceToStringInsideWhere_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

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
        db.Schema.CreateTable<Book>();

        Assert.ThrowsAny<Exception>(() =>
            db.Table<Book>()
                .Where(b => Enum.GetUnderlyingType(typeof(BookCategory)) != null)
                .ToSqlCommand());
    }

    [Fact]
    public void Char_UnsupportedMethod_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

        Assert.ThrowsAny<Exception>(() =>
            db.Table<Book>()
                .Where(b => char.IsControl(b.Title, 0))
                .ToSqlCommand());
    }

    [Fact]
    public void Int_ToString_TranslatesToCastAsText()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

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
        db.Schema.CreateTable<Book>();

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
        db.Schema.CreateTable<Book>();

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
        db.Schema.CreateTable<Book>();

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
        db.Schema.CreateTable<TimeOnlyMethodEntity>();

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
        db.Schema.CreateTable<Book>();

        Assert.ThrowsAny<Exception>(() =>
            db.Table<Book>()
                .Where(b => TimeOnly.Parse(b.Title) > new TimeOnly(0, 0))
                .ToSqlCommand());
    }

    [Fact]
    public void DateOnly_UnsupportedMethod_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<DateOnlyMethodEntity>();

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
        db.Schema.CreateTable<Article>();
        db.Schema.CreateTable<ArticleSearch>();

        Article seed = new() { Title = "kryptonite tales", Body = "shines bright", PublishedAt = DateTime.UtcNow };
        db.Table<Article>().Add(seed);

        string before = "{{";
        string after = "}}";
        string ellipsis = "…";
        SQLiteCommand cmd = db.Table<ArticleSearch>()
            .Where(a => SQLiteFunctions.Match(a, "kryptonite"))
            .Select(a => SQLiteFunctions.Snippet(a, a.Title, before, after, ellipsis, 5))
            .ToSqlCommand();

        Assert.Contains("snippet(", cmd.CommandText);
    }

    [Fact]
    public void FtsMatch_DynamicTerm_ProducesPrintfWrappedSql()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();
        db.Schema.CreateTable<ArticleSearch>();

        SQLiteCommand cmd = db.Table<ArticleSearch>()
            .Where(a => SQLiteFunctions.Match(a, f => f.Term(a.Title) && f.Term("static")))
            .ToSqlCommand();

        Assert.Contains("printf", cmd.CommandText);
    }

    [Fact]
    public void Enum_NonGenericParse_WithTypeArgument_TranslatesToCase()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Publisher>();

        SQLiteCommand cmd = db.Table<Publisher>()
            .Where(p => p.Type == (PublisherType)Enum.Parse(typeof(PublisherType), "Magazine"))
            .ToSqlCommand();

        Assert.Contains("CASE", cmd.CommandText);
    }

    [Fact]
    public void Enum_GenericParse_WithLiteralArgs_FoldsToConstant()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Publisher>();
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
        db.Schema.CreateTable<Publisher>();
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
        db.Schema.CreateTable<Book>();
        db.Schema.CreateTable<Author>();

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
        db.Schema.CreateTable<Book>();
        db.Schema.CreateTable<Author>();

        SQLiteCommand cmd = db.Table<Book>()
            .Where(b => db.Table<Author>().Any(a => a.Id == b.AuthorId))
            .ToSqlCommand();

        Assert.Contains("EXISTS", cmd.CommandText);
    }

    [Fact]
    public void Trim_WithCharArrayLiteral_TranslatesToTrim()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
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
        db.Schema.CreateTable<Book>();
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
        db.Schema.CreateTable<Book>();
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
        db.Schema.CreateTable<Book>();
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
        db.Schema.CreateTable<Book>();
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
        db.Schema.CreateTable<Book>();
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
        db.Schema.CreateTable<Book>();
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
        db.Schema.CreateTable<Publisher>();
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
        db.Schema.CreateTable<Book>();
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
        db.Schema.CreateTable<Publisher>();
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
            b.MethodTranslators[doubleMethod] = (_, args) => $"({args[0]} * 2)";
        });

        db.Schema.CreateTable<Book>();
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
        db.Schema.CreateTable<Article>();
        db.Schema.CreateTable<ArticleSearch>();

        string suffix = "_tail";
        SQLiteCommand cmd = db.Table<ArticleSearch>()
            .Where(a => SQLiteFunctions.Match(a, f => f.Term(a.Title + suffix)))
            .ToSqlCommand();

        Assert.Contains("printf", cmd.CommandText);
        Assert.True(cmd.Parameters.Count > 0);
    }

    [Fact]
    public void Enum_HasFlag_TranslatesToBitwiseAnd()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Publisher>();
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


    [Fact]
    public void MethodCallInterceptor_ReceivesVisitorAndCanReadOptions()
    {
        bool wasCalled = false;
        SQLiteOptions? capturedOptions = null;

        using TestDatabase db = new(b => b.AddMethodCallInterceptor((node, visitor) =>
        {
            if (node.Method.DeclaringType == typeof(InterceptorHelpers) && node.Method.Name == nameof(InterceptorHelpers.Double))
            {
                wasCalled = true;
                capturedOptions = visitor.Options;
            }
            return null;
        }));

        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 5, Price = 1 });

        Assert.ThrowsAny<Exception>(() =>
            db.Table<Book>().Where(b => InterceptorHelpers.Double(b.AuthorId) > 0).ToList());

        Assert.True(wasCalled);
        Assert.Same(db.Options, capturedOptions);
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
        db.Schema.CreateTable<Book>();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().ExecuteUpdate(s => s.Set(b => b.Title, b => string.Intern(b.Title))));
    }

    [Fact]
    public void SQLiteCteTyped_GetEnumerator_ExecutesQuery()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
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
        db.Schema.CreateTable<Book>();
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
        db.Schema.CreateTable<Book>();
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
        db.Schema.CreateTable<Book>();

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
        db.Schema.CreateTable<DateOnlyEntity>();

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
        db.Schema.CreateTable<TimeOnlyEntity>();

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
        db.Schema.CreateTable<DateTimeOffsetEntity>();

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
        db.Schema.CreateTable<TimeSpanEntity>();

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
        db.Schema.CreateTable<DateOnlyEntity>();
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
        db.Schema.CreateTable<TimeOnlyEntity>();
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
        db.Schema.CreateTable<DateOnlyEntity>();

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
        db.Schema.CreateTable<TimeOnlyEntity>();

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
        db.Schema.CreateTable<DateTimeEntity>();

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
        db.Schema.CreateTable<Book>();
        db.Schema.CreateTable<Author>();
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
    public void Where_WithInlineFieldInitializer_FiltersCorrectly()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
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
        db.Schema.CreateTable<Book>();
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
        db.Schema.CreateTable<Book>();
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
        db.Schema.CreateTable<Book>();
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
        db.Schema.CreateTable<Book>();
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
        db.Schema.CreateTable<Book>();
        db.Schema.CreateTable<Author>();
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
        db.Schema.CreateTable<EnumEntity>();
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
        db.Schema.CreateTable<EnumEntity>();
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
        db.Schema.CreateTable<CharEntity>();
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
        db.Schema.CreateTable<CharEntity>();
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
        db.Schema.CreateTable<Book>();
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
        db.Schema.CreateTable<Book>();
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
        db.Schema.CreateTable<Book>();
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
        db.Schema.CreateTable<CharEntity>();
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
        db.Schema.CreateTable<EnumEntity>();
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
        db.Schema.CreateTable<Book>();
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
        db.Schema.CreateTable<EnumEntity>();
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
        db.Schema.CreateTable<NullableEntity>();
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
        db.Schema.CreateTable<NullableEntity>();
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
        db.Schema.CreateTable<NullableEntity>();
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
        db.Schema.CreateTable<Book>();
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
        db.Schema.CreateTable<Book>();
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
        db.Schema.CreateTable<Book>();
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
        db.Schema.CreateTable<Book>();
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
        db.Schema.CreateTable<Book>();
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
}