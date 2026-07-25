using System.Linq.Expressions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ManualExpressionTreeConstantQueryableTests
{
    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Add(new Author { Id = 1, Name = "a", Email = "e", BirthDate = new DateTime(2000, 1, 1) });
        db.Table<Author>().Add(new Author { Id = 2, Name = "b", Email = "e", BirthDate = new DateTime(2000, 1, 1) });
        db.Table<Book>().Add(new Book { Id = 1, Title = "t1", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "t2", AuthorId = 2, Price = 2 });
        return db;
    }

    [Fact]
    public void Where_ContainsOverConstantQueryable_InlinesSubquery()
    {
        using TestDatabase db = Seed();

        IQueryable<int> authorIds = db.Table<Author>().Where(a => a.Id == 1).Select(a => a.Id);

        ParameterExpression b = Expression.Parameter(typeof(Book), "b");
        MethodCallExpression contains = Expression.Call(
            typeof(System.Linq.Queryable),
            nameof(System.Linq.Queryable.Contains),
            [typeof(int)],
            Expression.Constant(authorIds),
            Expression.Property(b, nameof(Book.AuthorId)));
        Expression<Func<Book, bool>> predicate = Expression.Lambda<Func<Book, bool>>(contains, b);

        List<int> oracle = new[] { (Id: 1, AuthorId: 1), (Id: 2, AuthorId: 2) }
            .Where(x => x.AuthorId == 1)
            .Select(x => x.Id)
            .ToList();

        List<int> actual = db.Table<Book>()
            .Where(predicate)
            .Select(x => x.Id)
            .ToList();

        Assert.Equal(oracle, actual);
    }
}
