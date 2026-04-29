using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Extensions;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Enums;
using SQLite.Framework.Tests.Helpers;
using SQLite.Framework.Tests.Interfaces;

// ReSharper disable AccessToDisposedClosure

namespace SQLite.Framework.Tests;

public class OtherTests
{
    [Fact]
    public void TestUniqueness()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "test",
            AuthorId = 1,
            Price = 10
        });

        Assert.Throws<SQLiteException>(() =>
        {
            db.Table<Book>().Add(new Book
            {
                Id = 2,
                Title = "test",
                AuthorId = 1,
                Price = 10
            });
        });
    }

    [Fact]
    public void QueryableContainsWithPassingArgument()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where (
                from b in db.Table<Book>()
                where b.Title == "test" && book.AuthorId == b.AuthorId
                select b.Title
            ).Contains("test 2")
            select book
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal("test", command.Parameters[0].Value);
        Assert.Equal("test 2", command.Parameters[1].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE @p2 IN (
                         SELECT b1.BookTitle AS "Title"
                         FROM "Books" AS b1
                         WHERE b1.BookTitle = @p0 AND b0.BookAuthorId = b1.BookAuthorId
                     )
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void OrderBys()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            orderby book.Title, book.Id descending
            select book
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     ORDER BY b0.BookTitle ASC, b0.BookId DESC
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void TakeSkip()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<Book>().Take(1).Skip(2).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     LIMIT 1
                     OFFSET 2
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Union()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<Book>().Where(f => f.Id == 1).Union(db.Table<Book>()).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookId = @p0
                     UNION
                     SELECT b1.BookId AS "Id",
                            b1.BookTitle AS "Title",
                            b1.BookAuthorId AS "AuthorId",
                            b1.BookPrice AS "Price"
                     FROM "Books" AS b1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Concat()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<Book>().Where(f => f.Id == 1).Concat(db.Table<Book>()).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookId = @p0
                     UNION ALL
                     SELECT b1.BookId AS "Id",
                            b1.BookTitle AS "Title",
                            b1.BookAuthorId AS "AuthorId",
                            b1.BookPrice AS "Price"
                     FROM "Books" AS b1
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void CheckTableMappingCached()
    {
        using TestDatabase db = new();

        TableMapping firstTableMapping = db.TableMapping<Book>();
        TableMapping secondTableMapping = db.TableMapping<Book>();

        Assert.Same(firstTableMapping, secondTableMapping);
    }

    [Fact]
    public void CheckTableMappingCachedNonGeneric()
    {
        using TestDatabase db = new();

        TableMapping firstTableMapping = db.TableMapping(typeof(Book));
        TableMapping secondTableMapping = db.TableMapping(typeof(Book));

        Assert.Same(firstTableMapping, secondTableMapping);
    }

    [Fact]
    public void CheckEnum()
    {
        using TestDatabase db = new();

        db.Table<Publisher>().Schema.CreateTable();

        db.Table<Publisher>().Add(new Publisher
        {
            Id = 1,
            Name = "test",
            Type = PublisherType.Magazine
        });

        Publisher publisher = db.Table<Publisher>().First(f => f.Id == 1);

        Assert.NotNull(publisher);
        Assert.Equal(1, publisher.Id);
        Assert.Equal("test", publisher.Name);
        Assert.Equal(PublisherType.Magazine, publisher.Type);
    }

    [Fact]
    public void CheckParameterToString()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<Book>()
            .Where(f => f.Id == 1)
            .ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal("@p0 = 1", command.Parameters[0].ToString());
    }

    [Fact]
    public void RollbackTransaction()
    {
        using TestDatabase db = new();

        db.Table<Publisher>().Schema.CreateTable();

        using SQLiteTransaction transaction = db.BeginTransaction();

        db.Table<Publisher>().Add(new Publisher
        {
            Id = 1,
            Name = "test",
            Type = PublisherType.Magazine
        });

        transaction.Rollback();

        Assert.Throws<InvalidOperationException>(() =>
        {
            Publisher publisher = db.Table<Publisher>().First(f => f.Id == 1);
        });
    }

    [Fact]
    public void AutoRollbackTransaction()
    {
        using TestDatabase db = new();

        db.Table<Publisher>().Schema.CreateTable();

        {
            using SQLiteTransaction transaction = db.BeginTransaction();

            db.Table<Publisher>().Add(new Publisher
            {
                Id = 1,
                Name = "test",
                Type = PublisherType.Magazine
            });
        }

        Assert.Throws<InvalidOperationException>(() =>
        {
            Publisher publisher = db.Table<Publisher>().First(f => f.Id == 1);
        });
    }

    [Fact]
    public void EnumerateTable()
    {
        using TestDatabase db = new();

        db.Table<Publisher>().Schema.CreateTable();

        db.Table<Publisher>().Add(new Publisher
        {
            Id = 1,
            Name = "test",
            Type = PublisherType.Magazine
        });

        foreach (Publisher publisher in db.Table<Publisher>())
        {
            Assert.Equal(1, publisher.Id);
            Assert.Equal("test", publisher.Name);
            Assert.Equal(PublisherType.Magazine, publisher.Type);
        }
    }

    [Fact]
    public void RequiredAttributeInTable()
    {
        using TestDatabase db = new();

        db.Table<RequiredEntity>().Schema.CreateTable();

        db.Table<RequiredEntity>().Add(new RequiredEntity
        {
            Date = "2000"
        });

        RequiredEntity publisher = db.Table<RequiredEntity>().First();

        Assert.Equal(1, publisher.Id);
        Assert.Equal("2000", publisher.Date);
    }

    [Fact]
    public void CheckTableMappingExists()
    {
        using TestDatabase db = new();

        db.Table<Publisher>().Schema.CreateTable();

        Assert.Single(db.TableMappings);
    }

    [Fact]
    public void TableColumn_IsFtsRowId_TrueOnlyForRowIdColumn()
    {
        using TestDatabase db = new();

        TableMapping mapping = db.TableMapping<ArticleSearch>();

        TableColumn rowId = mapping.Columns.Single(c => c.IsFtsRowId);
        Assert.Equal("rowid", rowId.Name);
        Assert.Equal(nameof(ArticleSearch.Id), rowId.PropertyInfo.Name);

        Assert.All(mapping.Columns.Where(c => c.PropertyInfo.Name != nameof(ArticleSearch.Id)),
            c => Assert.False(c.IsFtsRowId));
    }

    [Fact]
    public void TableColumn_IsFtsRowId_FalseOnNonFtsTable()
    {
        using TestDatabase db = new();

        TableMapping mapping = db.TableMapping<Book>();

        Assert.All(mapping.Columns, c => Assert.False(c.IsFtsRowId));
    }

    [Fact]
    public void GetNonGenericTable()
    {
        using TestDatabase db = new();

        db.Table<Publisher>().Schema.CreateTable();

        db.Table<Publisher>().Add(new Publisher
        {
            Id = 1,
            Name = "test",
            Type = PublisherType.Magazine
        });

        SQLiteTable table = db.Table(typeof(Publisher));

        foreach (Publisher publisher in table)
        {
            Assert.Equal(1, publisher.Id);
            Assert.Equal("test", publisher.Name);
            Assert.Equal(PublisherType.Magazine, publisher.Type);
        }
    }

    [Fact]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties, typeof(BaseCastEntity))]
    public void CastTable()
    {
        using TestDatabase db = new();

        db.Table<CastEntity>().Schema.CreateTable();

        db.Table<CastEntity>().Add(new CastEntity
        {
            Text = "test"
        });

        List<BaseCastEntity> table = db.Table(typeof(CastEntity))
            .Cast<BaseCastEntity>()
            .ToList();

        Assert.Single(table);
        Assert.Equal(1, table[0].Id);
        Assert.IsNotType<CastEntity>(table[0]);
    }

    [Fact]
    public void Where_InterfaceCastInsideLambda_FiltersByInterfaceProperty()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "live", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "dead", IsDeleted = true },
        });

        List<SoftDeletableBook> live = db.Table<SoftDeletableBook>()
            .Where(f => !((ISoftDelete)f).IsDeleted)
            .ToList();

        Assert.Single(live);
        Assert.Equal("live", live[0].Title);
    }

    [Fact]
    public void Cast_ToInterface_WhereByInterfaceProperty_FiltersCorrectly()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "live", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "dead", IsDeleted = true },
        });

        List<ISoftDelete> live = db.Table<SoftDeletableBook>()
            .Cast<ISoftDelete>()
            .Where(f => !f.IsDeleted)
            .ToList();

        Assert.Single(live);
        Assert.False(live[0].IsDeleted);
    }

    [Fact]
    public void Select_InterfaceCastInsideLambda_ProjectsInterfaceProperty()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "live", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "dead", IsDeleted = true },
        });

        List<bool> deletedFlags = db.Table<SoftDeletableBook>()
            .OrderBy(b => b.Id)
            .Select(f => ((ISoftDelete)f).IsDeleted)
            .ToList();

        Assert.Equal([false, true], deletedFlags);
    }

    [Fact]
    public void Cast_ToInterface_SelectInterfaceProperty_Projects()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "live", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "dead", IsDeleted = true },
        });

        List<bool> deletedFlags = db.Table<SoftDeletableBook>()
            .Cast<ISoftDelete>()
            .Select(f => f.IsDeleted)
            .ToList();

        Assert.Equal(2, deletedFlags.Count);
        Assert.Contains(false, deletedFlags);
        Assert.Contains(true, deletedFlags);
    }

    [Fact]
    public void GenericConstrainedMethod_FiltersByInterfaceProperty()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "live", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "dead", IsDeleted = true },
        });

        List<SoftDeletableBook> live = FilterNotDeleted(db.Table<SoftDeletableBook>());

        Assert.Single(live);
        Assert.Equal("live", live[0].Title);
    }

    [Fact]
    public void GenericConstrainedMethod_FirstByInterfaceProperty()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "live", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "dead", IsDeleted = true },
        });

        SoftDeletableBook first = FirstNotDeleted(db.Table<SoftDeletableBook>());

        Assert.Equal("live", first.Title);
    }

    [Fact]
    public void GenericConstrainedMethod_SelectInterfaceProperty()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "live", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "dead", IsDeleted = true },
        });

        List<bool> flags = ProjectDeletedFlags(db.Table<SoftDeletableBook>());

        Assert.Equal(2, flags.Count);
        Assert.Contains(true, flags);
        Assert.Contains(false, flags);
    }

    private static List<T> FilterNotDeleted<T>(IQueryable<T> source)
        where T : ISoftDelete
    {
        return source.Where(f => !f.IsDeleted).ToList();
    }

    private static T FirstNotDeleted<T>(IQueryable<T> source)
        where T : ISoftDelete
    {
        return source.Where(f => !f.IsDeleted).First();
    }

    private static List<bool> ProjectDeletedFlags<T>(IQueryable<T> source)
        where T : ISoftDelete
    {
        return source.Select(f => f.IsDeleted).ToList();
    }

    [Fact]
    public void GenericConstrainedMethod_OrderByInterfaceProperty()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "a", IsDeleted = true },
            new SoftDeletableBook { Id = 2, Title = "b", IsDeleted = false },
        });

        List<SoftDeletableBook> ordered = OrderByDeleted(db.Table<SoftDeletableBook>());

        Assert.Equal("b", ordered[0].Title);
        Assert.Equal("a", ordered[1].Title);
    }

    [Fact]
    public void GenericConstrainedMethod_CountByInterfaceProperty()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "a", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "b", IsDeleted = true },
            new SoftDeletableBook { Id = 3, Title = "c", IsDeleted = false },
        });

        int liveCount = CountNotDeleted(db.Table<SoftDeletableBook>());

        Assert.Equal(2, liveCount);
    }

    [Fact]
    public void GenericConstrainedMethod_AnyByInterfaceProperty()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        db.Table<SoftDeletableBook>().Add(new SoftDeletableBook { Id = 1, Title = "a", IsDeleted = false });

        Assert.False(AnyDeleted(db.Table<SoftDeletableBook>()));

        db.Table<SoftDeletableBook>().Add(new SoftDeletableBook { Id = 2, Title = "b", IsDeleted = true });

        Assert.True(AnyDeleted(db.Table<SoftDeletableBook>()));
    }

    [Fact]
    public void GenericConstrainedMethod_AllByInterfaceProperty()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "a", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "b", IsDeleted = false },
        });

        Assert.True(AllNotDeleted(db.Table<SoftDeletableBook>()));

        db.Table<SoftDeletableBook>().Add(new SoftDeletableBook { Id = 3, Title = "c", IsDeleted = true });

        Assert.False(AllNotDeleted(db.Table<SoftDeletableBook>()));
    }

    [Fact]
    public void GenericConstrainedMethod_ExecuteDeleteByInterfaceProperty()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "a", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "b", IsDeleted = true },
            new SoftDeletableBook { Id = 3, Title = "c", IsDeleted = true },
        });

        int affected = HardDeleteSoftDeleted(db.Table<SoftDeletableBook>());

        Assert.Equal(2, affected);
        Assert.Single(db.Table<SoftDeletableBook>().ToList());
    }

    [Fact]
    public void GenericConstrainedMethod_GroupByInterfaceProperty()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "a", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "b", IsDeleted = true },
            new SoftDeletableBook { Id = 3, Title = "c", IsDeleted = false },
        });

        Dictionary<bool, int> counts = GroupByDeletedCount(db.Table<SoftDeletableBook>());

        Assert.Equal(2, counts[false]);
        Assert.Equal(1, counts[true]);
    }

    private static List<T> OrderByDeleted<T>(IQueryable<T> source)
        where T : ISoftDelete
    {
        return source.OrderBy(f => f.IsDeleted).ToList();
    }

    private static int CountNotDeleted<T>(IQueryable<T> source)
        where T : ISoftDelete
    {
        return source.Count(f => !f.IsDeleted);
    }

    private static bool AnyDeleted<T>(IQueryable<T> source)
        where T : ISoftDelete
    {
        return source.Any(f => f.IsDeleted);
    }

    private static bool AllNotDeleted<T>(IQueryable<T> source)
        where T : ISoftDelete
    {
        return source.All(f => !f.IsDeleted);
    }

    private static int HardDeleteSoftDeleted<T>(IQueryable<T> source)
        where T : ISoftDelete
    {
        return source.Where(f => f.IsDeleted).ExecuteDelete();
    }

    private static Dictionary<bool, int> GroupByDeletedCount<T>(IQueryable<T> source)
        where T : ISoftDelete
    {
        return source
            .GroupBy(f => f.IsDeleted)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToDictionary(x => x.Key, x => x.Count);
    }

    [Fact]
    public void GenericConstrainedMethod_MultipleInterfaceConstraints_FiltersByBoth()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "a", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "b", IsDeleted = true },
            new SoftDeletableBook { Id = 3, Title = "c", IsDeleted = false },
        });

        SoftDeletableBook? hit = FindLiveById(db.Table<SoftDeletableBook>(), 3);

        Assert.NotNull(hit);
        Assert.Equal("c", hit.Title);
    }

    [Fact]
    public void GenericConstrainedClass_RepositoryPattern_FiltersAndCounts()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "a", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "b", IsDeleted = true },
        });

        Repository<SoftDeletableBook> repo = new(db.Table<SoftDeletableBook>());

        Assert.Equal(1, repo.LiveCount());
        Assert.Single(repo.Live());
    }

    [Fact]
    public void GenericConstrainedMethod_TakeWithInterfacePredicate_LimitsResult()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "a", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "b", IsDeleted = false },
            new SoftDeletableBook { Id = 3, Title = "c", IsDeleted = false },
        });

        List<SoftDeletableBook> top2 = TopLive(db.Table<SoftDeletableBook>(), 2);

        Assert.Equal(2, top2.Count);
    }

    [Fact]
    public void GenericConstrainedMethod_DistinctByInterfaceProperty()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "a", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "b", IsDeleted = false },
            new SoftDeletableBook { Id = 3, Title = "c", IsDeleted = true },
        });

        List<bool> distinctFlags = DistinctDeletedFlags(db.Table<SoftDeletableBook>());

        Assert.Equal(2, distinctFlags.Count);
        Assert.Contains(false, distinctFlags);
        Assert.Contains(true, distinctFlags);
    }

    [Fact]
    public void GenericConstrainedMethod_ExecuteUpdate_SetsInterfaceProperty()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "a", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "b", IsDeleted = false },
        });

        int affected = SoftDeleteAll(db.Table<SoftDeletableBook>());

        Assert.Equal(2, affected);
        Assert.All(db.Table<SoftDeletableBook>().ToList(), b => Assert.True(b.IsDeleted));
    }

    [Fact]
    public void GenericConstrainedMethod_AggregateMaxId()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "a", IsDeleted = false },
            new SoftDeletableBook { Id = 7, Title = "b", IsDeleted = false },
            new SoftDeletableBook { Id = 5, Title = "c", IsDeleted = true },
        });

        Assert.Equal(7, MaxIdAcrossAll(db.Table<SoftDeletableBook>()));
        Assert.Equal(7, MaxIdLive(db.Table<SoftDeletableBook>()));
    }

    [Fact]
    public void GenericConstrainedMethod_ChainedFilterOrderTake()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "a", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "b", IsDeleted = true },
            new SoftDeletableBook { Id = 3, Title = "c", IsDeleted = false },
            new SoftDeletableBook { Id = 4, Title = "d", IsDeleted = false },
        });

        List<SoftDeletableBook> top = NewestLive(db.Table<SoftDeletableBook>(), 2);

        Assert.Equal(2, top.Count);
        Assert.Equal(4, top[0].Id);
        Assert.Equal(3, top[1].Id);
    }

    private static T? FindLiveById<T>(IQueryable<T> source, int id)
        where T : IEntity, ISoftDelete
    {
        return source.Where(f => f.Id == id && !f.IsDeleted).FirstOrDefault();
    }

    private static List<T> TopLive<T>(IQueryable<T> source, int count)
        where T : ISoftDelete
    {
        return source.Where(f => !f.IsDeleted).Take(count).ToList();
    }

    private static List<bool> DistinctDeletedFlags<T>(IQueryable<T> source)
        where T : ISoftDelete
    {
        return source.Select(f => f.IsDeleted).Distinct().ToList();
    }

    private static int SoftDeleteAll<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(IQueryable<T> source)
        where T : ISoftDelete
    {
        return source.ExecuteUpdate(s => s.Set(f => f.IsDeleted, true));
    }

    private static int MaxIdAcrossAll<T>(IQueryable<T> source)
        where T : IEntity
    {
        return source.Max(f => f.Id);
    }

    private static int MaxIdLive<T>(IQueryable<T> source)
        where T : IEntity, ISoftDelete
    {
        return source.Where(f => !f.IsDeleted).Max(f => f.Id);
    }

    private static List<T> NewestLive<T>(IQueryable<T> source, int count)
        where T : IEntity, ISoftDelete
    {
        return source
            .Where(f => !f.IsDeleted)
            .OrderByDescending(f => f.Id)
            .Take(count)
            .ToList();
    }

    private sealed class Repository<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>
        where T : ISoftDelete
    {
        private readonly IQueryable<T> source;

        public Repository(IQueryable<T> source)
        {
            this.source = source;
        }

        public List<T> Live() => source.Where(f => !f.IsDeleted).ToList();

        public int LiveCount() => source.Count(f => !f.IsDeleted);
    }

    [Fact]
    public void GenericConstrainedMethod_SumOverIdInterface()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "a", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "b", IsDeleted = false },
            new SoftDeletableBook { Id = 3, Title = "c", IsDeleted = true },
        });

        Assert.Equal(6, SumIds(db.Table<SoftDeletableBook>()));
        Assert.Equal(3, SumLiveIds(db.Table<SoftDeletableBook>()));
        Assert.Equal(1, MinIdLive(db.Table<SoftDeletableBook>()));
        Assert.Equal(1.5, AverageLiveId(db.Table<SoftDeletableBook>()));
    }

    [Fact]
    public async Task GenericConstrainedMethod_AsyncWhere_FiltersAsync()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        await db.Table<SoftDeletableBook>().AddRangeAsync(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "a", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "b", IsDeleted = true },
        }, ct: TestContext.Current.CancellationToken);

        List<SoftDeletableBook> live = await FilterNotDeletedAsync(db.Table<SoftDeletableBook>(), TestContext.Current.CancellationToken);

        Assert.Single(live);
        Assert.Equal("a", live[0].Title);
    }

    [Fact]
    public void GenericConstrainedMethod_NestedGenericChain_WorksThroughLayers()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "a", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "b", IsDeleted = true },
        });

        int liveCount = OuterLiveCount(db.Table<SoftDeletableBook>());

        Assert.Equal(1, liveCount);
    }

    [Fact]
    public void GenericConstrainedMethod_ExtensionMethod_FiltersAndCounts()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "a", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "b", IsDeleted = true },
        });

        int count = db.Table<SoftDeletableBook>().LiveCount();

        Assert.Equal(1, count);
    }

    [Fact]
    public void GenericConstrainedMethod_TwoTypeParameters_ProjectByInterfaceKey()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 7, Title = "a", IsDeleted = false },
            new SoftDeletableBook { Id = 9, Title = "b", IsDeleted = true },
        });

        Dictionary<int, bool> dict = ToDeletedById<SoftDeletableBook, int>(db.Table<SoftDeletableBook>(), f => f.Id);

        Assert.False(dict[7]);
        Assert.True(dict[9]);
    }

    [Fact]
    public void GenericConstrainedMethod_ExecuteDeleteByPredicateBuiltFromInterface()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "a", IsDeleted = true },
            new SoftDeletableBook { Id = 2, Title = "b", IsDeleted = false },
        });

        int affected = ExecuteDeleteWithPredicate(db.Table<SoftDeletableBook>(), f => f.IsDeleted);

        Assert.Equal(1, affected);
        Assert.Single(db.Table<SoftDeletableBook>().ToList());
    }

    [Fact]
    public void GenericConstrainedMethod_AddItem_ConstrainedByInterface()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        int added = AddNotDeleted(db.Table<SoftDeletableBook>(), new SoftDeletableBook { Id = 1, Title = "added", IsDeleted = false });

        Assert.Equal(1, added);
        Assert.Single(db.Table<SoftDeletableBook>().ToList());
    }

    private static int SumIds<T>(IQueryable<T> source)
        where T : IEntity
    {
        return source.Sum(f => f.Id);
    }

    private static int SumLiveIds<T>(IQueryable<T> source)
        where T : IEntity, ISoftDelete
    {
        return source.Where(f => !f.IsDeleted).Sum(f => f.Id);
    }

    private static int MinIdLive<T>(IQueryable<T> source)
        where T : IEntity, ISoftDelete
    {
        return source.Where(f => !f.IsDeleted).Min(f => f.Id);
    }

    private static double AverageLiveId<T>(IQueryable<T> source)
        where T : IEntity, ISoftDelete
    {
        return source.Where(f => !f.IsDeleted).Average(f => f.Id);
    }

    private static Task<List<T>> FilterNotDeletedAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(IQueryable<T> source, CancellationToken ct)
        where T : ISoftDelete
    {
        return source.Where(f => !f.IsDeleted).ToListAsync(ct);
    }

    private static int OuterLiveCount<T>(IQueryable<T> source)
        where T : ISoftDelete
    {
        return InnerLiveCount(source);
    }

    private static int InnerLiveCount<T>(IQueryable<T> source)
        where T : ISoftDelete
    {
        return source.Count(f => !f.IsDeleted);
    }

    private static Dictionary<TKey, bool> ToDeletedById<T, TKey>(IQueryable<T> source, Expression<Func<T, TKey>> keySelector)
        where T : ISoftDelete
        where TKey : notnull
    {
        return source.ToDictionary(keySelector.Compile(), f => f.IsDeleted);
    }

    private static int ExecuteDeleteWithPredicate<T>(IQueryable<T> source, Expression<Func<T, bool>> predicate)
        where T : ISoftDelete
    {
        return source.Where(predicate).ExecuteDelete();
    }

    private static int AddNotDeleted<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(SQLiteTable<T> table, T item)
        where T : ISoftDelete
    {
        item.IsDeleted = false;
        return table.Add(item);
    }

    [Fact]
    public void QueryTableByOnlySQL()
    {
        using TestDatabase db = new();

        db.OpenConnection();

        SQLiteCommand command = new(db)
        {
            CommandText = """
                          CREATE TABLE IF NOT EXISTS "TestTable" (
                              "Id" INTEGER PRIMARY KEY AUTOINCREMENT,
                              "Name" TEXT NOT NULL
                          )
                          """,
            Parameters = new List<SQLiteParameter>()
        };

        command.ExecuteNonQuery();

        SQLiteCommand insertCommand = new(db)
        {
            CommandText = "INSERT INTO \"TestTable\" (\"Name\") VALUES (@name)",
            Parameters = new List<SQLiteParameter>
            {
                new()
                {
                    Name = "@name",
                    Value = "Test Name"
                }
            }
        };

        insertCommand.ExecuteNonQuery();

        SQLiteCommand queryCommand = new(db)
        {
            CommandText = "SELECT * FROM \"TestTable\"",
            Parameters = new List<SQLiteParameter>()
        };

        using SQLiteDataReader reader = queryCommand.ExecuteReader();

        Assert.True(reader.Read());
        Assert.Equal(1, reader.GetValue(0, SQLiteColumnType.Integer, typeof(int)));
        Assert.Equal("Test Name", reader.GetValue(1, SQLiteColumnType.Text, typeof(string)));
    }

    [Fact]
    public void CheckCallingOpenConnectionTwice()
    {
        using TestDatabase db = new();

        db.OpenConnection();
        db.OpenConnection();

        Assert.True(db.IsConnected);
    }

    [Fact]
    public async Task CheckCallingOpenConnectionFromDifferentThreads()
    {
        using TestDatabase db = new();

        Task task1 = Task.Run(db.OpenConnection, TestContext.Current.CancellationToken);
        Task task2 = Task.Run(db.OpenConnection, TestContext.Current.CancellationToken);
        await Task.WhenAll(task1, task2);

        Assert.True(db.IsConnected);
    }

    [Fact]
    public void FromSqlCompilesToSqlAndReturnsResult()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "FromSqlTest",
            AuthorId = 2,
            Price = 99
        });

        const string sql = "SELECT * FROM \"Books\" WHERE \"BookTitle\" = @title";
        IQueryable<Book> table = db.Table<Book>()
            .FromSql(sql, new SQLiteParameter
            {
                Name = "@title",
                Value = "FromSqlTest"
            })
            .Where(f => f.Id == 1);

        SQLiteCommand command = table.ToSqlCommand();
        Assert.Equal($"""
                      SELECT b0.BookId AS "Id",
                             b0.BookTitle AS "Title",
                             b0.BookAuthorId AS "AuthorId",
                             b0.BookPrice AS "Price"
                      FROM ({sql}) AS b0
                      WHERE b0.BookId = @p1
                      """.Replace("\r\n", "\n"), command.CommandText.Replace("\r\n", "\n"));
        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal("@title", command.Parameters[0].Name);
        Assert.Equal("FromSqlTest", command.Parameters[0].Value);
        Assert.Equal("@p1", command.Parameters[1].Name);
        Assert.Equal(1, command.Parameters[1].Value);

        List<Book> result = table.ToList();
        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("FromSqlTest", result[0].Title);
        Assert.Equal(2, result[0].AuthorId);
        Assert.Equal(99, result[0].Price);
    }

    [Fact]
    public void FromSqlJoinCompilesToSqlAndReturnsResult()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "FromSqlTest",
            AuthorId = 2,
            Price = 99
        });

        const string sql = "SELECT * FROM \"Books\" WHERE \"BookTitle\" = @title";

        IQueryable<Book> table =
            from book in db.Table<Book>()
            join b in db.Table<Book>().FromSql(sql, new SQLiteParameter
            {
                Name = "@title",
                Value = "FromSqlTest"
            }) on book.Id equals b.Id
            where book.Id == 1
            select book;

        SQLiteCommand command = table.ToSqlCommand();
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     JOIN (
                         SELECT b1.BookId AS "Id",
                            b1.BookTitle AS "Title",
                            b1.BookAuthorId AS "AuthorId",
                            b1.BookPrice AS "Price"
                         FROM (SELECT * FROM "Books" WHERE "BookTitle" = @title) AS b1
                     ) AS b2 ON b0.BookId = b2.Id
                     WHERE b0.BookId = @p1
                     """.Replace("\r\n", "\n"), command.CommandText.Replace("\r\n", "\n"));
        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal("@p1", command.Parameters[0].Name);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal("@title", command.Parameters[1].Name);
        Assert.Equal("FromSqlTest", command.Parameters[1].Value);

        List<Book> result = table.ToList();
        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("FromSqlTest", result[0].Title);
        Assert.Equal(2, result[0].AuthorId);
        Assert.Equal(99, result[0].Price);
    }

    [Fact]
    public void FromSqlInnerCompilesToSqlAndReturnsResult()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "FromSqlTest",
            AuthorId = 2,
            Price = 99
        });

        const string sql = "SELECT * FROM \"Books\" WHERE \"BookTitle\" = @title";

        IQueryable<Book> table =
            from book in db.Table<Book>()
            where db.Table<Book>().FromSql(sql, new SQLiteParameter
            {
                Name = "@title",
                Value = "FromSqlTest"
            }).Select(f => f.Id).Contains(book.Id)
            select book;

        SQLiteCommand command = table.ToSqlCommand();
        Assert.Equal("""
                     SELECT b0.BookId AS "Id",
                            b0.BookTitle AS "Title",
                            b0.BookAuthorId AS "AuthorId",
                            b0.BookPrice AS "Price"
                     FROM "Books" AS b0
                     WHERE b0.BookId IN (
                         SELECT b1.BookId AS "Id"
                         FROM (SELECT * FROM "Books" WHERE "BookTitle" = @title) AS b1
                     )
                     """.Replace("\r\n", "\n"), command.CommandText.Replace("\r\n", "\n"));
        Assert.Single(command.Parameters);
        Assert.Equal("@title", command.Parameters[0].Name);
        Assert.Equal("FromSqlTest", command.Parameters[0].Value);

        List<Book> result = table.ToList();
        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("FromSqlTest", result[0].Title);
        Assert.Equal(2, result[0].AuthorId);
        Assert.Equal(99, result[0].Price);
    }

    [Fact]
    public void NullableResult()
    {
        using TestDatabase db = new();

        db.Table<RequiredEntity>().Schema.CreateTable();
        db.Table<RequiredEntity>().Add(new RequiredEntity
        {
            Id = 1,
            Date = "Test"
        });

        NullableDTO entity = db.Table<RequiredEntity>()
            .Select(f => new NullableDTO
            {
                Id = f.Id,
                Title = f.Date
            })
            .First(f => f.Id == 1);

        Assert.Equal(1, entity.Id);
    }

    [Fact]
    public void GenericCastResult()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "Book 1",
            AuthorId = 1,
            Price = 99
        });

        GenericMethod<Book>(db, 1);

        static void GenericMethod<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(TestDatabase db, int id)
            where T : class, IEntity
        {
            IEntity? result = db.Table<T>().FirstOrDefault(f => f.Id == id);

            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
        }
    }

    private class BaseCastEntity
    {
        [Key]
        [AutoIncrement]
        public int Id { get; set; }
    }

    private class CastEntity : BaseCastEntity
    {
        public required string Text { get; set; }
    }

    private class RequiredEntity
    {
        [Key]
        [AutoIncrement]
        public int Id { get; set; }

        [Required]
        public string? Date { get; set; }
    }

    private class NullableDTO
    {
        public int? Id { get; set; }
        public string? Title { get; set; }
    }
}