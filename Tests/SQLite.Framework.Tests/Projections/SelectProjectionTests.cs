using System.Linq.Expressions;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SelectProjectionTests
{
    private static readonly ExpressionType[] BracketTypes =
    [
        ExpressionType.Equal,
        ExpressionType.NotEqual,
        ExpressionType.GreaterThan,
        ExpressionType.LessThan,
        ExpressionType.GreaterThanOrEqual,
        ExpressionType.LessThanOrEqual,
        ExpressionType.AndAlso,
        ExpressionType.OrElse,
        ExpressionType.And,
        ExpressionType.Or,
        ExpressionType.ExclusiveOr,
    ];

    private static readonly ExpressionType[] NonBracketTypes =
    [
        ExpressionType.Add,
        ExpressionType.Subtract,
        ExpressionType.Constant,
    ];

    [Fact]
    public void IsConcatBracketNodeType_TrueForComparisonAndLogicalOperators()
    {
        foreach (ExpressionType nodeType in BracketTypes)
        {
            Assert.True(TranslationPatterns.IsConcatBracketNodeType(nodeType));
        }
    }

    [Fact]
    public void IsConcatBracketNodeType_FalseForOtherOperators()
    {
        foreach (ExpressionType nodeType in NonBracketTypes)
        {
            Assert.False(TranslationPatterns.IsConcatBracketNodeType(nodeType));
        }
    }

    [Fact]
    public void JoinWithScalarResultSelector_SelectsMember()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Add(new Author { Id = 1, Name = "Ann", Email = "a@x", BirthDate = new DateTime(2000, 1, 1) });
        db.Table<Book>().Add(new Book { Id = 1, Title = "T", AuthorId = 1, Price = 1.0 });

        List<string> actual = db.Table<Book>()
            .Join(db.Table<Author>(), b => b.AuthorId, a => a.Id, (b, a) => a.Name)
            .ToList();

        Assert.Equal(["Ann"], actual);
    }

    [Fact]
    public void JoinProjectionWithoutGeneratedMaterializers_UsesRuntimeCompiler()
    {
        using SQLiteDatabase db = new(new SQLiteOptionsBuilder(":memory:").Build());
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Add(new Author { Id = 1, Name = "Ann", Email = "a@x", BirthDate = new DateTime(2000, 1, 1) });
        db.Table<Book>().Add(new Book { Id = 1, Title = "T", AuthorId = 1, Price = 1.0 });

        var actual = db.Table<Book>()
            .Join(db.Table<Author>(), b => b.AuthorId, a => a.Id, (b, a) => new { b.Title, a.Name })
            .ToList();

        Assert.Single(actual);
        Assert.Equal("T", actual[0].Title);
        Assert.Equal("Ann", actual[0].Name);
    }
}
