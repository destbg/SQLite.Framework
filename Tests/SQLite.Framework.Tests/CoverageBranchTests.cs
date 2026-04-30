using System.Reflection;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CoverageBranchTests
{
    [Fact]
    public void Contains_StringComparisonOrdinal_DoesNotAddCollation()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Hello", AuthorId = 0, Price = 1 });

        List<Book> rows = db.Table<Book>()
            .Where(b => b.Title.Contains("Hell", StringComparison.Ordinal))
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void StartsWith_StringComparisonOrdinal_DoesNotAddCollation()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Hello", AuthorId = 0, Price = 1 });

        List<Book> rows = db.Table<Book>()
            .Where(b => b.Title.StartsWith("Hel", StringComparison.Ordinal))
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void EndsWith_StringComparisonOrdinal_DoesNotAddCollation()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Hello", AuthorId = 0, Price = 1 });

        List<Book> rows = db.Table<Book>()
            .Where(b => b.Title.EndsWith("llo", StringComparison.Ordinal))
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void EnumToString_ConstantEnumValue_ProjectsName()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 0, Price = 1 });

        PublisherType constant = PublisherType.Magazine;

        List<string> names = db.Table<Book>()
            .Select(b => constant.ToString())
            .ToList();

        Assert.Equal("Magazine", names[0]);
    }

    [Fact]
    public void Printf_FormatStringWithoutExtraArgs_ProducesPrintfCall()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 0, Price = 1 });

        List<string> rows = db.Table<Book>()
            .Select(b => SQLiteFunctions.Printf("just-text"))
            .ToList();

        Assert.Equal("just-text", rows[0]);
    }

    [Fact]
    public void JsonChain_OrderByDescending_Last_FlipsDescToAsc()
    {
        using TestDatabase db = MakeJsonDb();
        db.Table<JsonRowB>().Add(new JsonRowB { Id = 1, Tags = ["a", "b", "c"] });

        string result = db.Table<JsonRowB>()
            .Select(r => r.Tags.OrderByDescending(x => x).Last())
            .First();

        Assert.Equal("a", result);
    }

    [Fact]
    public void JsonChain_OrderByDescending_LastOrDefault_FlipsDescToAsc()
    {
        using TestDatabase db = MakeJsonDb();
        db.Table<JsonRowB>().Add(new JsonRowB { Id = 1, Tags = ["a", "b", "c"] });

        string? result = db.Table<JsonRowB>()
            .Select(r => r.Tags.OrderByDescending(x => x).LastOrDefault())
            .First();

        Assert.Equal("a", result);
    }

    [Fact]
    public void PadLeft_WidthFromColumn_NoCapturedParams_AppendsSpaceParameter()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "ab", AuthorId = 4, Price = 1 });

        List<string> rows = db.Table<Book>()
            .Select(b => b.Title.PadLeft(b.AuthorId))
            .ToList();

        Assert.Equal("  ab", rows[0]);
    }

    [Fact]
    public void PadRight_WidthFromColumn_NoCapturedParams_AppendsSpaceParameter()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "ab", AuthorId = 4, Price = 1 });

        List<string> rows = db.Table<Book>()
            .Select(b => b.Title.PadRight(b.AuthorId))
            .ToList();

        Assert.Equal("ab  ", rows[0]);
    }

    [Fact]
    public void JsonChain_Where_FirstOrDefault_HitsHandler()
    {
        using TestDatabase db = MakeJsonDb();
        db.Table<JsonRowB>().Add(new JsonRowB { Id = 1, Tags = ["a", "b", "c"] });

        string? result = db.Table<JsonRowB>()
            .Select(r => r.Tags.Where(x => x != "a").FirstOrDefault())
            .First();

        Assert.Equal("b", result);
    }

    [Fact]
    public void JsonChain_Where_SingleOrDefault_HitsHandler()
    {
        using TestDatabase db = MakeJsonDb();
        db.Table<JsonRowB>().Add(new JsonRowB { Id = 1, Tags = ["a", "b", "c"] });

        string? result = db.Table<JsonRowB>()
            .Select(r => r.Tags.Where(x => x == "b").SingleOrDefault())
            .First();

        Assert.Equal("b", result);
    }

    [Fact]
    public void JsonChain_OrderByDescending_Reverse_FlipsDescToAsc()
    {
        using TestDatabase db = MakeJsonDb();
        db.Table<JsonRowB>().Add(new JsonRowB { Id = 1, Tags = ["a", "b", "c"] });

        List<string> result = db.Table<JsonRowB>()
            .Select(r => r.Tags.OrderByDescending(x => x).Reverse().ToList())
            .First();

        Assert.Equal(["a", "b", "c"], result);
    }

    [Fact]
    public void List_Exists_PredicateWithoutCapturedParameters_DoesNotPanic()
    {
        using TestDatabase db = MakeJsonDb();
        db.Table<JsonRowB>().Add(new JsonRowB { Id = 1, Tags = ["a", "b"] });

        bool result = db.Table<JsonRowB>()
            .Select(r => r.Tags.Exists(x => x == x))
            .First();

        Assert.True(result);
    }

    [Fact]
    public void Array_Exists_PredicateWithoutCapturedParameters_DoesNotPanic()
    {
        using TestDatabase db = new(b =>
        {
            b.TypeConverters[typeof(string[])] =
                new SQLiteJsonConverter<string[]>(CoverageJsonContext.Default.StringArray);
        });
        db.Schema.CreateTable<ArrayRowB>();
        db.Table<ArrayRowB>().Add(new ArrayRowB { Id = 1, Tags = new[] { "a", "b" } });

        bool result = db.Table<ArrayRowB>()
            .Select(r => Array.Exists(r.Tags, x => x == x))
            .First();

        Assert.True(result);
    }

    [Theory]
    [InlineData(System.Linq.Expressions.ExpressionType.AndAlso, true, true, true)]
    [InlineData(System.Linq.Expressions.ExpressionType.AndAlso, false, true, false)]
    [InlineData(System.Linq.Expressions.ExpressionType.AndAlso, true, false, false)]
    [InlineData(System.Linq.Expressions.ExpressionType.OrElse, false, false, false)]
    [InlineData(System.Linq.Expressions.ExpressionType.OrElse, true, false, true)]
    [InlineData(System.Linq.Expressions.ExpressionType.OrElse, false, true, true)]
    [InlineData(System.Linq.Expressions.ExpressionType.And, true, true, true)]
    [InlineData(System.Linq.Expressions.ExpressionType.And, false, true, false)]
    [InlineData(System.Linq.Expressions.ExpressionType.Or, false, false, false)]
    [InlineData(System.Linq.Expressions.ExpressionType.Or, false, true, true)]
    public void QueryCompiler_BinaryBooleanOps_ShortCircuitBranchesCovered(System.Linq.Expressions.ExpressionType nodeType, bool left, bool right, bool expected)
    {
        System.Linq.Expressions.BinaryExpression node = System.Linq.Expressions.Expression.MakeBinary(
            nodeType,
            System.Linq.Expressions.Expression.Constant(left),
            System.Linq.Expressions.Expression.Constant(right));

        SQLite.Framework.Internals.Visitors.QueryCompilerVisitor visitor = new();
        SQLite.Framework.Internals.Models.CompiledExpression compiled =
            (SQLite.Framework.Internals.Models.CompiledExpression)visitor.Visit(node);

        SQLite.Framework.Models.SQLiteQueryContext ctx = new();
        Assert.Equal(expected, compiled.Call(ctx));
    }

    [Fact]
    public void QueryCompiler_CompareValues_NullVsNonComparableThrowsNotSupported()
    {
        MethodInfo method = typeof(SQLite.Framework.Internals.Visitors.QueryCompilerVisitor)
            .GetMethod("CompareValues", BindingFlags.Static | BindingFlags.NonPublic)!;

        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(
            () => method.Invoke(null, new object?[] { null, new object() }));

        Assert.IsType<NotSupportedException>(ex.InnerException);
    }

    [Fact]
    public void Join_StaticMethodSourceReturningTable_TranslatesViaQueryable()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Schema.CreateTable<Article>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });
        db.Table<Article>().Add(new Article { Id = 1, Title = "y", Body = "b", PublishedAt = DateTime.UtcNow });

        TableSourceFactory.SharedDb = db;

        MethodInfo booksMethod = typeof(TableSourceFactory).GetMethod(nameof(TableSourceFactory.GetBooks))!;
        System.Linq.Expressions.MethodCallExpression booksSource = System.Linq.Expressions.Expression.Call(booksMethod);

        System.Linq.Expressions.ParameterExpression aParam = System.Linq.Expressions.Expression.Parameter(typeof(Article), "a");
        System.Linq.Expressions.ParameterExpression bParam = System.Linq.Expressions.Expression.Parameter(typeof(Book), "b");
        System.Linq.Expressions.LambdaExpression outerKey = System.Linq.Expressions.Expression.Lambda<Func<Article, int>>(
            System.Linq.Expressions.Expression.Property(aParam, nameof(Article.Id)), aParam);
        System.Linq.Expressions.LambdaExpression innerKey = System.Linq.Expressions.Expression.Lambda<Func<Book, int>>(
            System.Linq.Expressions.Expression.Property(bParam, nameof(Book.Id)), bParam);
        System.Linq.Expressions.LambdaExpression result = System.Linq.Expressions.Expression.Lambda<Func<Article, Book, int>>(
            System.Linq.Expressions.Expression.Property(bParam, nameof(Book.Id)), aParam, bParam);

        System.Linq.Expressions.MethodCallExpression joinCall = System.Linq.Expressions.Expression.Call(
            typeof(Queryable),
            nameof(Queryable.Join),
            new[] { typeof(Article), typeof(Book), typeof(int), typeof(int) },
            db.Table<Article>().Expression,
            booksSource,
            System.Linq.Expressions.Expression.Quote(outerKey),
            System.Linq.Expressions.Expression.Quote(innerKey),
            System.Linq.Expressions.Expression.Quote(result));

        IQueryable<int> query = ((IQueryProvider)db).CreateQuery<int>(joinCall);
        List<int> ids = query.ToList();

        Assert.Single(ids);
    }

    [Fact]
    public void Query_StaticMethodSourceReturningTable_TranslatesViaQueryable()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        TableSourceFactory.SharedDb = db;

        MethodInfo booksMethod = typeof(TableSourceFactory).GetMethod(nameof(TableSourceFactory.GetBooks))!;
        System.Linq.Expressions.MethodCallExpression source = System.Linq.Expressions.Expression.Call(booksMethod);

        System.Linq.Expressions.ParameterExpression bParam = System.Linq.Expressions.Expression.Parameter(typeof(Book), "b");
        System.Linq.Expressions.LambdaExpression predicate = System.Linq.Expressions.Expression.Lambda<Func<Book, bool>>(
            System.Linq.Expressions.Expression.GreaterThan(
                System.Linq.Expressions.Expression.Property(bParam, nameof(Book.Id)),
                System.Linq.Expressions.Expression.Constant(0)),
            bParam);

        System.Linq.Expressions.MethodCallExpression whereCall = System.Linq.Expressions.Expression.Call(
            typeof(Queryable),
            nameof(Queryable.Where),
            new[] { typeof(Book) },
            source,
            System.Linq.Expressions.Expression.Quote(predicate));

        IQueryable<Book> query = ((IQueryProvider)db).CreateQuery<Book>(whereCall);
        List<Book> result = query.ToList();

        Assert.Single(result);
    }

    [Fact]
    public void GroupJoin_MultipleJoinsWithMismatchedFirst_FindsCorrectGroupJoin()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Schema.CreateTable<Author>();
        db.Schema.CreateTable<Article>();
        db.Table<Author>().Add(new Author { Id = 1, Name = "n", Email = "e", BirthDate = DateTime.UtcNow });
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        SQLiteCommand command = (
            from book in db.Table<Book>()
            join author in db.Table<Author>() on book.AuthorId equals author.Id
            join article in db.Table<Article>() on book.Id equals article.Id into articleGroup
            from article in articleGroup.DefaultIfEmpty()
            select new { book.Id }
        ).ToSqlCommand();

        Assert.Contains("LEFT JOIN", command.CommandText);
    }

    [Fact]
    public void Select_MemberInitWithListBindAndPublicField_AssignsField()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 7, Title = "x", AuthorId = 0, Price = 1 });

        List<TargetWithFields> rows = db.Table<Book>()
            .Select(b => new TargetWithFields
            {
                MyField = b.Id,
                Items = { 1, 2 }
            })
            .ToList();

        Assert.Single(rows);
        Assert.Equal(7, rows[0].MyField);
    }

    [Fact]
    public void QueryCompiler_VisitMemberMemberBinding_FieldMember_ReturnsFieldType()
    {
        System.Reflection.FieldInfo innerField = typeof(MmbOuter).GetField(nameof(MmbOuter.InnerField))!;
        System.Reflection.PropertyInfo xProp = typeof(MmbInner).GetProperty(nameof(MmbInner.X))!;

        System.Linq.Expressions.MemberMemberBinding mmb = System.Linq.Expressions.Expression.MemberBind(
            innerField,
            System.Linq.Expressions.Expression.Bind(xProp, System.Linq.Expressions.Expression.Constant(0)));

        SQLite.Framework.Internals.Visitors.QueryCompilerVisitor visitor = new();
        MethodInfo method = typeof(SQLite.Framework.Internals.Visitors.QueryCompilerVisitor)
            .GetMethod("VisitMemberMemberBindingExpression", BindingFlags.Instance | BindingFlags.NonPublic)!;

        SQLite.Framework.Internals.Models.CompiledExpression compiled =
            (SQLite.Framework.Internals.Models.CompiledExpression)method.Invoke(visitor, new object?[] { mmb })!;

        Assert.Equal(typeof(MmbInner), compiled.Type);
    }

    private static TestDatabase MakeJsonDb()
    {
        TestDatabase db = new(b =>
        {
            b.TypeConverters[typeof(List<string>)] =
                new SQLiteJsonConverter<List<string>>(CoverageJsonContext.Default.ListString);
        });
        db.Schema.CreateTable<JsonRowB>();
        return db;
    }
}

internal class JsonRowB
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }

    public required List<string> Tags { get; set; }
}

internal static class TableSourceFactory
{
    public static TestDatabase SharedDb = null!;

    public static SQLiteTable<Book> GetBooks() => SharedDb.Table<Book>();

    public static SQLiteTable<Book> Books => SharedDb.Table<Book>();
}

internal class TargetWithFields
{
    public int MyField;
    public List<int> Items { get; } = [];
}

internal class MmbOuter
{
    public MmbInner InnerField = new();
}

internal class MmbInner
{
    public int X { get; set; }
}

internal class ArrayRowB
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }

    public required string[] Tags { get; set; }
}

[System.Text.Json.Serialization.JsonSerializable(typeof(List<string>))]
[System.Text.Json.Serialization.JsonSerializable(typeof(string[]))]
internal partial class CoverageJsonContext : System.Text.Json.Serialization.JsonSerializerContext;

