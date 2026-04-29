using System.Linq.Expressions;
using System.Reflection;
using SQLite.Framework.Internals;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SQLiteDatabaseCoverageTests
{
    [Fact]
    public void FindRootElementType_ConstantBaseSQLiteTable_ReturnsElementType()
    {
        using TestDatabase db = new();
        SQLiteTable<Book> table = db.Table<Book>();
        ConstantExpression expression = Expression.Constant(table);

        MethodInfo find = typeof(SQLiteDatabase).GetMethod(
            "FindRootElementType",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        Type result = (Type)find.Invoke(null, [expression])!;

        Assert.Equal(typeof(Book), result);
    }

    [Fact]
    public void FindRootElementType_GenericQueryable_ReturnsGenericArgument()
    {
        ParameterExpression p = Expression.Parameter(typeof(IQueryable<Book>), "q");

        MethodInfo find = typeof(SQLiteDatabase).GetMethod(
            "FindRootElementType",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        Type result = (Type)find.Invoke(null, [p])!;

        Assert.Equal(typeof(Book), result);
    }

    [Fact]
    public void FindRootElementType_NonGenericNonConstant_ReturnsExpressionType()
    {
        ParameterExpression p = Expression.Parameter(typeof(string), "s");

        MethodInfo find = typeof(SQLiteDatabase).GetMethod(
            "FindRootElementType",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        Type result = (Type)find.Invoke(null, [p])!;

        Assert.Equal(typeof(string), result);
    }

    [Fact]
    public void GroupBy_GeneratedKeyMaterializer_IsUsedInsteadOfCompiler()
    {
        using TestDatabase db = new(b =>
        {
            string keySignature = ComputeAuthorIdSignature();
            b.GroupByKeyMaterializers[keySignature] = ctx =>
            {
                Book book = (Book)ctx.Input!;
                return book.AuthorId * 100;
            };
        });

        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 });
        db.Table<Book>().Add(new Book { Id = 3, Title = "C", AuthorId = 2, Price = 3 });

        List<IGrouping<int, Book>> groups = db.Table<Book>()
            .GroupBy(b => b.AuthorId)
            .ToList();

        Assert.Equal(2, groups.Count);
        Assert.Contains(groups, g => g.Key == 100);
        Assert.Contains(groups, g => g.Key == 200);
    }

    private static string ComputeAuthorIdSignature()
    {
        ParameterExpression param = Expression.Parameter(typeof(Book), "b");
        MemberExpression body = Expression.Property(param, nameof(Book.AuthorId));
        return SelectSignature.Compute(body);
    }
}
