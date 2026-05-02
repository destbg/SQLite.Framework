using SQLite.Framework.Exceptions;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class QueryPatternGapTests
{
    [Fact]
    public void LeftJoin_GroupBy_SumOnOptionalSide_NoMatchYieldsZero()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Author>().AddRange([
            new Author { Id = 1, Name = "WithBooks", Email = "w@x", BirthDate = new DateTime(2000, 1, 1) },
            new Author { Id = 2, Name = "NoBooks", Email = "n@x", BirthDate = new DateTime(2000, 1, 1) },
        ]);
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "T1", AuthorId = 1, Price = 5.0 },
            new Book { Id = 2, Title = "T2", AuthorId = 1, Price = 10.0 },
        ]);

        var rows = (
            from a in db.Table<Author>()
            join b in db.Table<Book>() on a.Id equals b.AuthorId into bg
            from b in bg.DefaultIfEmpty()
            group b by a.Id into g
            select new { Id = g.Key, Total = g.Sum(x => x.Price) }
        ).OrderBy(x => x.Id).ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal(1, rows[0].Id);
        Assert.Equal(15.0, rows[0].Total);
        Assert.Equal(2, rows[1].Id);
        Assert.Equal(0.0, rows[1].Total);
    }

    [Fact]
    public void ExecuteDelete_WithSubqueryContainsPredicate()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Author>().AddRange([
            new Author { Id = 1, Name = "Bad", Email = "b@x", BirthDate = new DateTime(2000, 1, 1) },
            new Author { Id = 2, Name = "Good", Email = "g@x", BirthDate = new DateTime(2000, 1, 1) },
        ]);
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "T1", AuthorId = 1, Price = 5.0 },
            new Book { Id = 2, Title = "T2", AuthorId = 2, Price = 10.0 },
        ]);

        int deleted = db.Table<Book>()
            .Where(b => db.Table<Author>()
                .Where(a => a.Name == "Bad")
                .Select(a => a.Id)
                .Contains(b.AuthorId))
            .ExecuteDelete();

        Assert.Equal(1, deleted);
        List<Book> remaining = db.Table<Book>().OrderBy(b => b.Id).ToList();
        Assert.Single(remaining);
        Assert.Equal(2, remaining[0].Id);
    }

    [Fact]
    public void Where_ChainedStringMethods_TrimToLowerContains()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "  CLEAN code  ", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "Dirty Code", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "Clean Architecture", AuthorId = 1, Price = 3 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => b.Title.Trim().ToLower().Contains("clean"))
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1, 3], ids);
    }

    [Fact]
    public void Select_DateTimeSubtraction_TotalDays()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();

        DateTime epoch = new(2000, 1, 1);
        db.Table<Author>().Add(new Author
        {
            Id = 1,
            Name = "A",
            Email = "a@x",
            BirthDate = new DateTime(2000, 1, 11),
        });

        var rows = db.Table<Author>()
            .Select(a => new { a.Id, Days = (a.BirthDate - epoch).TotalDays })
            .ToList();

        Assert.Single(rows);
        Assert.Equal(10.0, rows[0].Days);
    }

    [Fact]
    public void SelfJoin_OnSameTable_PairsByAuthor()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "T1", AuthorId = 1, Price = 5.0 },
            new Book { Id = 2, Title = "T2", AuthorId = 1, Price = 10.0 },
            new Book { Id = 3, Title = "T3", AuthorId = 2, Price = 20.0 },
        ]);

        var pairs = (
            from b1 in db.Table<Book>()
            join b2 in db.Table<Book>() on b1.AuthorId equals b2.AuthorId
            where b1.Id < b2.Id
            select new { Left = b1.Id, Right = b2.Id }
        ).OrderBy(x => x.Left).ToList();

        Assert.Single(pairs);
        Assert.Equal(1, pairs[0].Left);
        Assert.Equal(2, pairs[0].Right);
    }

    [Fact]
    public void Select_TernaryChain_TranslatesToCase()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Alpha", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "Beta", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "Gamma", AuthorId = 1, Price = 3 },
        ]);

        var rows = db.Table<Book>()
            .Select(b => new
            {
                b.Id,
                Tier = b.Title == "Alpha" ? 1 : b.Title == "Beta" ? 2 : 99,
            })
            .OrderBy(x => x.Id)
            .ToList();

        Assert.Equal(3, rows.Count);
        Assert.Equal(1, rows[0].Tier);
        Assert.Equal(2, rows[1].Tier);
        Assert.Equal(99, rows[2].Tier);
    }

    [Fact]
    public void OrderBy_ComputedExpression_ThenBy_Column()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 4 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 5 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 6 },
        ]);

        List<int> ids = db.Table<Book>()
            .OrderByDescending(b => b.Price * 1.1)
            .ThenBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([3, 2, 1], ids);
    }

    [Fact]
    public void GroupJoin_AggregateOnGroup_IsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Author>().Add(new Author
        {
            Id = 1,
            Name = "A",
            Email = "a@x",
            BirthDate = new DateTime(2000, 1, 1),
        });

        IQueryable<object> query =
            from a in db.Table<Author>()
            join b in db.Table<Book>() on a.Id equals b.AuthorId into bg
            select (object)new { a.Id, BookCount = bg.Count() };

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => query.ToList());
        Assert.Contains("GroupJoin", ex.Message);
        Assert.Contains("DefaultIfEmpty", ex.Message);
    }

    [Fact]
    public void ThreeTableJoin_InnerThenLeft()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Author>().AddRange([
            new Author { Id = 1, Name = "Robert", Email = "r@x", BirthDate = new DateTime(2000, 1, 1) },
            new Author { Id = 2, Name = "Donald", Email = "d@x", BirthDate = new DateTime(2000, 1, 1) },
        ]);
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Clean Code", AuthorId = 1, Price = 5 },
            new Book { Id = 2, Title = "TAOCP", AuthorId = 2, Price = 10 },
        ]);

        var rows = (
            from b in db.Table<Book>()
            join a in db.Table<Author>() on b.AuthorId equals a.Id
            join other in db.Table<Author>() on a.Email equals other.Email into og
            from other in og.DefaultIfEmpty()
            orderby b.Id
            select new { Book = b.Title, Author = a.Name, OtherId = other == null ? 0 : other.Id }
        ).ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal("Clean Code", rows[0].Book);
        Assert.Equal("Robert", rows[0].Author);
        Assert.Equal(1, rows[0].OtherId);
        Assert.Equal("TAOCP", rows[1].Book);
        Assert.Equal("Donald", rows[1].Author);
        Assert.Equal(2, rows[1].OtherId);
    }

    [Fact]
    public void QueryFilter_AppliesToJoinedTable()
    {
        using TestDatabase db = new(b => b.AddQueryFilter<SoftDeletableBook>(s => !s.IsDeleted));
        db.Table<SoftDeletableBook>().Schema.CreateTable();
        db.Table<Author>().Schema.CreateTable();

        db.Table<Author>().AddRange([
            new Author { Id = 1, Name = "Live", Email = "l@x", BirthDate = new DateTime(2000, 1, 1) },
            new Author { Id = 2, Name = "Other", Email = "o@x", BirthDate = new DateTime(2000, 1, 1) },
        ]);
        db.Table<SoftDeletableBook>().AddRange([
            new SoftDeletableBook { Id = 1, Title = "live-1", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "gone-1", IsDeleted = true },
        ]);

        var rows = (
            from a in db.Table<Author>()
            join b in db.Table<SoftDeletableBook>() on a.Id equals b.Id
            select new { a.Id, b.Title }
        ).OrderBy(x => x.Id).ToList();

        Assert.Single(rows);
        Assert.Equal("live-1", rows[0].Title);
    }

    [Fact]
    public void LastOrDefault_IsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Schema.CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().OrderBy(b => b.Id).LastOrDefault());
    }

    [Fact]
    public void DistinctBy_IsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Schema.CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().DistinctBy(b => b.AuthorId).ToList());
    }

    [Fact]
    public void OfType_IsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().OfType<Book>().ToList());
    }

    [Fact]
    public void ElementAt_IsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().OrderBy(b => b.Id).ElementAt(0));
    }

    [Fact]
    public void TakeWhile_IsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().TakeWhile(b => b.Price < 10).ToList());
    }

    [Fact]
    public void Aggregate_IsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Aggregate(0.0, (acc, b) => acc + b.Price));
    }

    [Fact]
    public void MaxBy_IsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().MaxBy(b => b.Price));
    }

    [Fact]
    public void Chunk_IsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Chunk(10).ToList());
    }

    [Fact]
    public void Select_SubqueryAggregateInProjection()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Author>().AddRange([
            new Author { Id = 1, Name = "A", Email = "a@x", BirthDate = new DateTime(2000, 1, 1) },
            new Author { Id = 2, Name = "B", Email = "b@x", BirthDate = new DateTime(2000, 1, 1) },
        ]);
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "T1", AuthorId = 1, Price = 5 },
            new Book { Id = 2, Title = "T2", AuthorId = 1, Price = 10 },
        ]);

        var rows = db.Table<Author>()
            .Select(a => new
            {
                a.Id,
                BookCount = db.Table<Book>().Count(b => b.AuthorId == a.Id),
            })
            .OrderBy(x => x.Id)
            .ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal(2, rows[0].BookCount);
        Assert.Equal(0, rows[1].BookCount);
    }

    [Fact]
    public void Take_Zero_ReturnsEmpty()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        List<Book> rows = db.Table<Book>().Take(0).ToList();

        Assert.Empty(rows);
    }

    [Fact]
    public void Select_CastDoubleToInt()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 5.7 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 10.2 },
        ]);

        List<int> rounded = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Select(b => (int)b.Price)
            .ToList();

        Assert.Equal([5, 10], rounded);
    }

    [Fact]
    public void Skip_WithoutOrderBy_StillWorks()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 3 },
        ]);

        List<Book> rows = db.Table<Book>().Skip(1).ToList();

        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public void OrderBy_ThenDistinct_ProducesDistinctRows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 3 },
        ]);

        List<int> authorIds = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Select(b => b.AuthorId)
            .Distinct()
            .ToList();

        Assert.Equal(2, authorIds.Count);
        Assert.Contains(1, authorIds);
        Assert.Contains(2, authorIds);
    }

    [Fact]
    public void Select_StringConcatWithInt()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        List<string> labels = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Select(b => b.Title + "-" + b.Id)
            .ToList();

        Assert.Equal(["A-1", "B-2"], labels);
    }

    [Fact]
    public void Append_IsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Append(new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 })
                .ToList());
    }

    [Fact]
    public void Prepend_IsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Prepend(new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 })
                .ToList());
    }

    [Fact]
    public void Zip_IsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Zip(db.Table<Book>(), (a, b) => a.Id + b.Id).ToList());
    }

    [Fact]
    public void SequenceEqual_IsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().SequenceEqual(db.Table<Book>()));
    }

    [Fact]
    public void SkipWhile_IsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().SkipWhile(b => b.Price < 10).ToList());
    }

    [Fact]
    public void Where_CastDoubleToInt()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 5.7 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 10.2 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 11.5 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => (int)b.Price == 10)
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([2], ids);
    }

    [Fact]
    public void Where_BitwiseAndOnInt()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 2, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 3, Price = 3 },
            new Book { Id = 4, Title = "D", AuthorId = 4, Price = 4 },
        ]);

        List<int> evens = db.Table<Book>()
            .Where(b => (b.AuthorId & 1) == 0)
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([2, 4], evens);
    }

    [Fact]
    public void Where_BitwiseOrOnInt()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 0, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 2, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 4, Price = 3 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => (b.AuthorId | 1) == 5)
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([3], ids);
    }

    [Fact]
    public void Where_BitwiseXorOnInt()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 5, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 6, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 7, Price = 3 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => (b.AuthorId ^ 3) == 4)
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([3], ids);
    }

    [Fact]
    public void Where_LeftShiftOnInt()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 2, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 3, Price = 3 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => (b.AuthorId << 1) >= 4)
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([2, 3], ids);
    }

    [Fact]
    public void Where_RightShiftOnInt()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 8, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 4, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 3 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => (b.AuthorId >> 1) >= 2)
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1, 2], ids);
    }

    [Fact]
    public void Where_OnesComplementOnInt()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 0, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => ~b.AuthorId == -2)
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([2], ids);
    }

    [Fact]
    public void Where_NegatedBooleanColumn()
    {
        using TestDatabase db = new(b => b.AddQueryFilter<SoftDeletableBook>(s => true));
        db.Table<SoftDeletableBook>().Schema.CreateTable();
        db.Table<SoftDeletableBook>().AddRange([
            new SoftDeletableBook { Id = 1, Title = "live-1", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "live-2", IsDeleted = false },
            new SoftDeletableBook { Id = 3, Title = "gone", IsDeleted = true },
        ]);

        List<int> liveIds = db.Table<SoftDeletableBook>()
            .Where(b => !b.IsDeleted)
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1, 2], liveIds);
    }

    [Fact]
    public void Where_NonCorrelatedSubqueryCount()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Author>().Add(new Author
        {
            Id = 1,
            Name = "A",
            Email = "a@x",
            BirthDate = new DateTime(2000, 1, 1),
        });
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "T1", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "T2", AuthorId = 1, Price = 2 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => db.Table<Author>().Count() > 0)
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1, 2], ids);
    }

    [Fact]
    public void OrderBy_BySubqueryCount()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Author>().AddRange([
            new Author { Id = 1, Name = "Few", Email = "a@x", BirthDate = new DateTime(2000, 1, 1) },
            new Author { Id = 2, Name = "Many", Email = "b@x", BirthDate = new DateTime(2000, 1, 1) },
        ]);
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "T1", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "T2", AuthorId = 2, Price = 2 },
            new Book { Id = 3, Title = "T3", AuthorId = 2, Price = 3 },
            new Book { Id = 4, Title = "T4", AuthorId = 2, Price = 4 },
        ]);

        List<int> orderedIds = db.Table<Author>()
            .OrderByDescending(a => db.Table<Book>().Count(b => b.AuthorId == a.Id))
            .Select(a => a.Id)
            .ToList();

        Assert.Equal([2, 1], orderedIds);
    }

    [Fact]
    public void OrderBy_AnonymousTypeKey_IsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().OrderBy(b => new { b.AuthorId, b.Title }).ToList());
    }

    [Fact]
    public void Where_TitleEqualsNull_OnRequiredString()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => b.Title == null)
            .Select(b => b.Id)
            .ToList();

        Assert.Empty(ids);
    }

    [Fact]
    public void Where_DateTimeLiteralComparison()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();

        db.Table<Author>().AddRange([
            new Author { Id = 1, Name = "Old", Email = "o@x", BirthDate = new DateTime(1900, 1, 1) },
            new Author { Id = 2, Name = "New", Email = "n@x", BirthDate = new DateTime(2024, 6, 1) },
        ]);

        List<int> ids = db.Table<Author>()
            .Where(a => a.BirthDate > new DateTime(2000, 1, 1))
            .Select(a => a.Id)
            .ToList();

        Assert.Equal([2], ids);
    }

    [Fact]
    public void Where_CapturedString_Contains()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Clean Code", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "Refactoring", AuthorId = 1, Price = 2 },
        ]);

        string needle = "lean";
        List<int> ids = db.Table<Book>()
            .Where(b => b.Title.Contains(needle))
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void GroupBy_ThenOrderBy_AggregateValue()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "T1", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "T2", AuthorId = 2, Price = 2 },
            new Book { Id = 3, Title = "T3", AuthorId = 2, Price = 3 },
            new Book { Id = 4, Title = "T4", AuthorId = 2, Price = 4 },
        ]);

        var rows = (
            from b in db.Table<Book>()
            group b by b.AuthorId into g
            orderby g.Count() descending
            select new { AuthorId = g.Key, Count = g.Count() }
        ).ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal(2, rows[0].AuthorId);
        Assert.Equal(3, rows[0].Count);
        Assert.Equal(1, rows[1].AuthorId);
        Assert.Equal(1, rows[1].Count);
    }

    [Fact]
    public void Sum_WithConditionalExpression()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 3 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 7 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 12 },
        ]);

        double total = db.Table<Book>()
            .Sum(b => b.Price > 5 ? b.Price : 0);

        Assert.Equal(19.0, total);
    }

    [Fact]
    public void Select_ValueTuple()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        List<(int Id, string Title)> rows = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Select(b => ValueTuple.Create(b.Id, b.Title))
            .ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal((1, "A"), rows[0]);
        Assert.Equal((2, "B"), rows[1]);
    }

    [Fact]
    public void Select_DateTimeAddDays_FromColumn()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();

        db.Table<Author>().Add(new Author
        {
            Id = 5,
            Name = "A",
            Email = "a@x",
            BirthDate = new DateTime(2000, 1, 1),
        });

        var rows = db.Table<Author>()
            .Select(a => new { a.Id, Shifted = a.BirthDate.AddDays(a.Id) })
            .ToList();

        Assert.Single(rows);
        Assert.Equal(new DateTime(2000, 1, 6), rows[0].Shifted);
    }

    [Fact]
    public void Distinct_ThenCount()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 3 },
        ]);

        int distinctAuthors = db.Table<Book>()
            .Select(b => b.AuthorId)
            .Distinct()
            .Count();

        Assert.Equal(2, distinctAuthors);
    }

    [Fact]
    public void Distinct_ThenSum_OnSingleColumn()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 3 },
            new Book { Id = 4, Title = "D", AuthorId = 3, Price = 4 },
        ]);

        int sumOfDistinctAuthorIds = db.Table<Book>()
            .Select(b => b.AuthorId)
            .Distinct()
            .Sum();

        Assert.Equal(6, sumOfDistinctAuthorIds);
    }

    [Fact]
    public void Distinct_ThenAverage_OnSingleColumn()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 20 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 30 },
            new Book { Id = 4, Title = "D", AuthorId = 3, Price = 40 },
        ]);

        double avg = db.Table<Book>()
            .Select(b => b.AuthorId)
            .Distinct()
            .Average();

        Assert.Equal(2.0, avg);
    }

    [Fact]
    public void Distinct_ThenMax_OnSingleColumn()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 5, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 5, Price = 3 },
        ]);

        int max = db.Table<Book>()
            .Select(b => b.AuthorId)
            .Distinct()
            .Max();

        Assert.Equal(5, max);
    }

    [Fact]
    public void Distinct_ThenMin_OnSingleColumn()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 5, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 3 },
        ]);

        int min = db.Table<Book>()
            .Select(b => b.AuthorId)
            .Distinct()
            .Min();

        Assert.Equal(1, min);
    }

    [Fact]
    public void Distinct_ThenCount_MultiColumnProjection_IsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Select(b => new { b.AuthorId, b.Title })
                .Distinct()
                .Count());
    }

    [Fact]
    public void Distinct_ThenCount_OnTable_IsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Distinct().Count());
    }

    [Fact]
    public void Where_StringPadLeft_RunsThrough()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "BB", AuthorId = 1, Price = 2 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => b.Title.PadLeft(3) == "  A")
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void Where_AlwaysFalse_ReturnsEmpty()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        List<Book> rows = db.Table<Book>().Where(b => false).ToList();

        Assert.Empty(rows);
    }

    [Fact]
    public void Where_ListContains_Works()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 3 },
        ]);

        List<int> wanted = [1, 3];
        List<int> ids = db.Table<Book>()
            .Where(b => wanted.Contains(b.Id))
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1, 3], ids);
    }

    [Fact]
    public void Where_InlineArrayContains()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 3 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => new[] { 1, 3 }.Contains(b.Id))
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1, 3], ids);
    }

    [Fact]
    public void Where_CapturedArrayContains()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 3 },
        ]);

        int[] arr = [1, 3];
        List<int> ids = db.Table<Book>()
            .Where(b => arr.Contains(b.Id))
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1, 3], ids);
    }

    [Fact]
    public void Where_EmptyArrayContains_ReturnsEmpty()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        int[] empty = [];
        List<int> ids = db.Table<Book>()
            .Where(b => empty.Contains(b.Id))
            .Select(b => b.Id)
            .ToList();

        Assert.Empty(ids);
    }

    [Fact]
    public void Where_DateTimeNow_EvaluatesClientSide()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();

        DateTime past = DateTime.UtcNow.AddYears(-1);
        DateTime future = DateTime.UtcNow.AddYears(1);
        db.Table<Author>().AddRange([
            new Author { Id = 1, Name = "Past", Email = "p@x", BirthDate = past },
            new Author { Id = 2, Name = "Future", Email = "f@x", BirthDate = future },
        ]);

        List<int> ids = db.Table<Author>()
            .Where(a => a.BirthDate < DateTime.UtcNow)
            .OrderBy(a => a.Id)
            .Select(a => a.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void Reverse_ReversesQueryOrder()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 3 },
        ]);

        List<int> ids = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Reverse()
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([3, 2, 1], ids);
    }

    [Fact]
    public void Union_TwoQueries_DeduplicatesRows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 3 },
        ]);

        IQueryable<int> low = db.Table<Book>().Where(b => b.Price < 3).Select(b => b.AuthorId);
        IQueryable<int> high = db.Table<Book>().Where(b => b.Price > 1).Select(b => b.AuthorId);

        HashSet<int> result = low.Union(high).ToHashSet();

        Assert.Equal([1, 2], result.OrderBy(x => x));
    }

    [Fact]
    public void Except_RemovesRowsInSecondQuery()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 3 },
        ]);

        IQueryable<int> all = db.Table<Book>().Select(b => b.Id);
        IQueryable<int> twoOnly = db.Table<Book>().Where(b => b.Id == 2).Select(b => b.Id);

        HashSet<int> result = all.Except(twoOnly).ToHashSet();

        Assert.Equal([1, 3], result.OrderBy(x => x));
    }

    [Fact]
    public void Union_ThenOrderBy_OrdersAfterUnion()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 3 },
        ]);

        IQueryable<int> low = db.Table<Book>().Where(b => b.Price < 3).Select(b => b.AuthorId);
        IQueryable<int> high = db.Table<Book>().Where(b => b.Price > 1).Select(b => b.AuthorId);

        List<int> result = low.Union(high).OrderBy(x => x).ToList();

        Assert.Equal([1, 2], result);
    }

    [Fact]
    public void Except_ThenOrderBy_OrdersAfterExcept()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 3 },
        ]);

        IQueryable<int> all = db.Table<Book>().Select(b => b.Id);
        IQueryable<int> twoOnly = db.Table<Book>().Where(b => b.Id == 2).Select(b => b.Id);

        List<int> result = all.Except(twoOnly).OrderBy(x => x).ToList();

        Assert.Equal([1, 3], result);
    }

    [Fact]
    public void OrderBy_StringLength_AsKey()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Long Title", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "Short", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "Mid", AuthorId = 1, Price = 3 },
        ]);

        List<int> ids = db.Table<Book>()
            .OrderBy(b => b.Title.Length)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([3, 2, 1], ids);
    }

    [Fact]
    public void Sum_WithComplexExpression()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 2 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 3 },
        ]);

        double total = db.Table<Book>().Sum(b => b.Price * b.Id + 1);

        Assert.Equal(2 * 1 + 1 + 3 * 2 + 1, total);
    }

    [Fact]
    public void Select_NestedAnonymousType()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
        ]);

        var rows = db.Table<Book>()
            .Select(b => new { b.Id, Inner = new { b.Title, b.Price } })
            .ToList();

        Assert.Single(rows);
        Assert.Equal(1, rows[0].Id);
        Assert.Equal("A", rows[0].Inner.Title);
        Assert.Equal(1.0, rows[0].Inner.Price);
    }

    [Fact]
    public void Select_Chained_TwoProjections()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        List<int> doubled = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .Select(id => id * 2)
            .ToList();

        Assert.Equal([2, 4], doubled);
    }

    [Fact]
    public void Where_AfterSelect_FiltersProjection()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 5 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 9 },
        ]);

        var rows = db.Table<Book>()
            .Select(b => new { b.Id, b.Price })
            .Where(x => x.Price > 3)
            .OrderBy(x => x.Id)
            .ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal(2, rows[0].Id);
        Assert.Equal(3, rows[1].Id);
    }

    [Fact]
    public void Select_Identity_ReturnsAllRows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        List<Book> rows = db.Table<Book>().Select(b => b).OrderBy(b => b.Id).ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal("A", rows[0].Title);
    }

    [Fact]
    public void Select_BooleanComparisonProjection()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 7 },
        ]);

        List<bool> flags = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Select(b => b.Price > 5)
            .ToList();

        Assert.Equal([false, true], flags);
    }

    [Fact]
    public void Where_AlwaysTrue_ReturnsAll()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        List<int> ids = db.Table<Book>().Where(b => true).Select(b => b.Id).ToList();

        Assert.Equal(2, ids.Count);
    }

    [Fact]
    public void Select_UnaryNegateOnColumn()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 5, Price = 1 });

        List<int> rows = db.Table<Book>().Select(b => -b.AuthorId).ToList();

        Assert.Equal([-5], rows);
    }

    [Fact]
    public void Aggregate_MinWithSelectorAfterFilter()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 5 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 1 },
        ]);

        double min = db.Table<Book>()
            .Where(b => b.AuthorId == 1)
            .Min(b => b.Price);

        Assert.Equal(2.0, min);
    }

    [Fact]
    public void Concat_TwoQueries_KeepsDuplicates()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 2, Price = 2 },
        ]);

        IQueryable<int> all = db.Table<Book>().Select(b => b.AuthorId);
        IQueryable<int> a = db.Table<Book>().Where(b => b.Id == 1).Select(b => b.AuthorId);

        List<int> result = all.Concat(a).OrderBy(x => x).ToList();

        Assert.Equal([1, 1, 2], result);
    }

    [Fact]
    public void Intersect_TwoQueries_KeepsCommon()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 2, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 3 },
        ]);

        IQueryable<int> all = db.Table<Book>().Select(b => b.AuthorId);
        IQueryable<int> highPrice = db.Table<Book>().Where(b => b.Price > 1).Select(b => b.AuthorId);

        HashSet<int> result = all.Intersect(highPrice).ToHashSet();

        Assert.Equal([1, 2], result.OrderBy(x => x));
    }

    [Fact]
    public void Math_Sin_TranslatesToSql()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 0 });

        List<double> rows = db.Table<Book>()
            .Select(b => Math.Sin(b.Price))
            .ToList();

        Assert.Single(rows);
        Assert.Equal(0.0, rows[0], 10);
    }

    [Fact]
    public void Math_Atan2_TranslatesToSql()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        List<double> rows = db.Table<Book>()
            .Select(b => Math.Atan2(b.Price, b.Price))
            .ToList();

        Assert.Single(rows);
        Assert.Equal(Math.PI / 4, rows[0], 10);
    }

    [Fact]
    public void Math_Cbrt_HandlesPositiveAndNegativeInputs()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 27 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = -8 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 0 },
        ]);

        List<double> rows = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Select(b => Math.Cbrt(b.Price))
            .ToList();

        Assert.Equal(3, rows.Count);
        Assert.Equal(3.0, rows[0], 10);
        Assert.Equal(-2.0, rows[1], 10);
        Assert.Equal(0.0, rows[2], 10);
    }

    [Fact]
    public void Math_PI_AsConstant()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        List<double> rows = db.Table<Book>()
            .Select(b => b.Price * Math.PI)
            .ToList();

        Assert.Single(rows);
        Assert.Equal(Math.PI, rows[0], 10);
    }

    [Fact]
    public void Math_Round_ThreeArg_WithMidpointRounding()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 0.5 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 1.5 },
        ]);

        List<double> rounded = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Select(b => Math.Round(b.Price, 0, MidpointRounding.AwayFromZero))
            .ToList();

        Assert.Equal([1.0, 2.0], rounded);
    }

    [Fact]
    public void Math_Round_TwoArg_WithMidpointRounding()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1.5 });

        List<double> rounded = db.Table<Book>()
            .Select(b => Math.Round(b.Price, MidpointRounding.AwayFromZero))
            .ToList();

        Assert.Equal([2.0], rounded);
    }


    [Fact]
    public void Math_Round_NonAwayFromZeroMode_IsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1.5 });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Select(b => Math.Round(b.Price, 1, MidpointRounding.ToEven))
                .ToList());
    }

    [Fact]
    public void Where_BoolAnd_NonShortCircuit()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 2, Price = 2 },
        ]);

        bool flag = true;
        List<int> ids = db.Table<Book>()
            .Where(b => (b.AuthorId == 1) & flag)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void Where_BoolOr_NonShortCircuit()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 2, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 3, Price = 3 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => (b.AuthorId == 1) | (b.AuthorId == 3))
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1, 3], ids);
    }

    [Fact]
    public void GroupJoin_ResultSelectorWithUnrelatedMethodCall_StillThrows()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Author>().Add(new Author
        {
            Id = 1,
            Name = "A",
            Email = "a@x",
            BirthDate = new DateTime(2000, 1, 1),
        });

        IQueryable<object> query =
            from a in db.Table<Author>()
            join b in db.Table<Book>() on a.Id equals b.AuthorId into bg
            select (object)new { Abs = Math.Abs(a.Id), BookCount = bg.Count() };

        Assert.Throws<NotSupportedException>(() => query.ToList());
    }

    [Fact]
    public void Distinct_ThenCountWithPredicate()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 3 },
            new Book { Id = 4, Title = "D", AuthorId = 5, Price = 4 },
        ]);

        int count = db.Table<Book>()
            .Select(b => b.AuthorId)
            .Distinct()
            .Count(id => id > 1);

        Assert.Equal(2, count);
    }

    [Fact]
    public void Math_Cos_TranslatesToSql()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 0 });

        double v = db.Table<Book>().Select(b => Math.Cos(b.Price)).First();
        Assert.Equal(1.0, v, 10);
    }

    [Fact]
    public void Math_Tan_TranslatesToSql()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 0 });

        double v = db.Table<Book>().Select(b => Math.Tan(b.Price)).First();
        Assert.Equal(0.0, v, 10);
    }

    [Fact]
    public void Math_Asin_TranslatesToSql()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 0 });

        double v = db.Table<Book>().Select(b => Math.Asin(b.Price)).First();
        Assert.Equal(0.0, v, 10);
    }

    [Fact]
    public void Math_Acos_TranslatesToSql()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        double v = db.Table<Book>().Select(b => Math.Acos(b.Price)).First();
        Assert.Equal(0.0, v, 10);
    }

    [Fact]
    public void Math_Atan_TranslatesToSql()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 0 });

        double v = db.Table<Book>().Select(b => Math.Atan(b.Price)).First();
        Assert.Equal(0.0, v, 10);
    }

    [Fact]
    public void Math_Sinh_TranslatesToSql()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 0 });

        double v = db.Table<Book>().Select(b => Math.Sinh(b.Price)).First();
        Assert.Equal(0.0, v, 10);
    }

    [Fact]
    public void Math_Cosh_TranslatesToSql()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 0 });

        double v = db.Table<Book>().Select(b => Math.Cosh(b.Price)).First();
        Assert.Equal(1.0, v, 10);
    }

    [Fact]
    public void Math_Tanh_TranslatesToSql()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 0 });

        double v = db.Table<Book>().Select(b => Math.Tanh(b.Price)).First();
        Assert.Equal(0.0, v, 10);
    }

    [Fact]
    public void Math_Log2_TranslatesToSql()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 8 });

        double v = db.Table<Book>().Select(b => Math.Log2(b.Price)).First();
        Assert.Equal(3.0, v, 10);
    }

    [Fact]
    public void Math_Asinh_TranslatesToSql()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 0 });

        double v = db.Table<Book>().Select(b => Math.Asinh(b.Price)).First();
        Assert.Equal(0.0, v, 10);
    }

    [Fact]
    public void Math_Acosh_TranslatesToSql()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        double v = db.Table<Book>().Select(b => Math.Acosh(b.Price)).First();
        Assert.Equal(0.0, v, 10);
    }

    [Fact]
    public void Math_Atanh_TranslatesToSql()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 0 });

        double v = db.Table<Book>().Select(b => Math.Atanh(b.Price)).First();
        Assert.Equal(0.0, v, 10);
    }

    [Fact]
    public void Nullable_HasValueProjectAndFilter()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NullableEntity>();
        db.Table<NullableEntity>().AddRange([
            new NullableEntity { Id = 1, Value = 10 },
            new NullableEntity { Id = 2, Value = null },
            new NullableEntity { Id = 3, Value = 30 },
        ]);

        List<int> withValue = db.Table<NullableEntity>()
            .Where(e => e.Value.HasValue)
            .OrderBy(e => e.Id)
            .Select(e => e.Id)
            .ToList();

        Assert.Equal([1, 3], withValue);
    }

    [Fact]
    public void Nullable_ValueAccessInSelect()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NullableEntity>();
        db.Table<NullableEntity>().AddRange([
            new NullableEntity { Id = 1, Value = 10 },
            new NullableEntity { Id = 3, Value = 30 },
        ]);

        List<int> values = db.Table<NullableEntity>()
            .Where(e => e.Value.HasValue)
            .OrderBy(e => e.Id)
            .Select(e => e.Value!.Value)
            .ToList();

        Assert.Equal([10, 30], values);
    }

    [Fact]
    public void Nullable_EqualsNullLiteral_TranslatesToIsNull()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NullableEntity>();
        db.Table<NullableEntity>().AddRange([
            new NullableEntity { Id = 1, Value = 10 },
            new NullableEntity { Id = 2, Value = null },
        ]);

        List<int> ids = db.Table<NullableEntity>()
            .Where(e => e.Value == null)
            .Select(e => e.Id)
            .ToList();

        Assert.Equal([2], ids);
    }

    [Fact]
    public void Nullable_CoalesceInArithmetic()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NullableEntity>();
        db.Table<NullableEntity>().AddRange([
            new NullableEntity { Id = 1, Value = 10 },
            new NullableEntity { Id = 2, Value = null },
        ]);

        List<int> rows = db.Table<NullableEntity>()
            .OrderBy(e => e.Id)
            .Select(e => (e.Value ?? 0) + 1)
            .ToList();

        Assert.Equal([11, 1], rows);
    }

    [Fact]
    public void OrderBy_Nullable_NullsFirst()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NullableEntity>();
        db.Table<NullableEntity>().AddRange([
            new NullableEntity { Id = 1, Value = 10 },
            new NullableEntity { Id = 2, Value = null },
            new NullableEntity { Id = 3, Value = 5 },
        ]);

        List<int> ids = db.Table<NullableEntity>()
            .OrderBy(e => e.Value)
            .Select(e => e.Id)
            .ToList();

        Assert.Equal([2, 3, 1], ids);
    }

    [Fact]
    public void Sum_WithCastInsideSelector()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1.5 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2.5 },
        ]);

        decimal total = db.Table<Book>().Sum(b => (decimal)b.Price);

        Assert.Equal(4.0m, total);
    }

    [Fact]
    public void Where_BoolColumn_DirectTruth()
    {
        using TestDatabase db = new(b => b.AddQueryFilter<SoftDeletableBook>(_ => true));
        db.Table<SoftDeletableBook>().Schema.CreateTable();
        db.Table<SoftDeletableBook>().AddRange([
            new SoftDeletableBook { Id = 1, Title = "kept", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "gone", IsDeleted = true },
        ]);

        List<int> deletedIds = db.Table<SoftDeletableBook>()
            .Where(b => b.IsDeleted)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([2], deletedIds);
    }

    [Fact]
    public void Where_NullableComparedToInt()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NullableEntity>();
        db.Table<NullableEntity>().AddRange([
            new NullableEntity { Id = 1, Value = 10 },
            new NullableEntity { Id = 2, Value = null },
            new NullableEntity { Id = 3, Value = 30 },
        ]);

        List<int> ids = db.Table<NullableEntity>()
            .Where(e => e.Value > 15)
            .OrderBy(e => e.Id)
            .Select(e => e.Id)
            .ToList();

        Assert.Equal([3], ids);
    }

    [Fact]
    public void Where_CoalesceComparedToInt()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NullableEntity>();
        db.Table<NullableEntity>().AddRange([
            new NullableEntity { Id = 1, Value = 10 },
            new NullableEntity { Id = 2, Value = null },
            new NullableEntity { Id = 3, Value = 30 },
        ]);

        List<int> ids = db.Table<NullableEntity>()
            .Where(e => (e.Value ?? 0) > 5)
            .OrderBy(e => e.Id)
            .Select(e => e.Id)
            .ToList();

        Assert.Equal([1, 3], ids);
    }

    [Fact]
    public void ExecuteUpdate_SetFromSubqueryScalar()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Author>().Add(new Author
        {
            Id = 7,
            Name = "A",
            Email = "a@x",
            BirthDate = new DateTime(2000, 1, 1),
        });
        db.Table<Book>().Add(new Book { Id = 1, Title = "T", AuthorId = 1, Price = 5 });

        db.Table<Book>().ExecuteUpdate(s => s
            .Set(b => b.AuthorId, b => db.Table<Author>().Select(a => a.Id).First()));

        Book updated = db.Table<Book>().First();
        Assert.Equal(7, updated.AuthorId);
    }

    [Fact]
    public void Select_DateOnlyFullProperties()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<DateOnlyMethodEntity>();
        db.Table<DateOnlyMethodEntity>().Add(new DateOnlyMethodEntity
        {
            Id = 1,
            Date = new DateOnly(2024, 6, 15),
        });

        var row = db.Table<DateOnlyMethodEntity>()
            .Select(e => new
            {
                e.Date.Year,
                e.Date.Month,
                e.Date.Day,
                e.Date.DayOfWeek,
                e.Date.DayOfYear,
            })
            .First();

        Assert.Equal(2024, row.Year);
        Assert.Equal(6, row.Month);
        Assert.Equal(15, row.Day);
        Assert.Equal(DayOfWeek.Saturday, row.DayOfWeek);
        Assert.Equal(167, row.DayOfYear);
    }

    [Fact]
    public void Select_TimeOnlyFullProperties()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<TimeOnlyMethodEntity>();
        db.Table<TimeOnlyMethodEntity>().Add(new TimeOnlyMethodEntity
        {
            Id = 1,
            Time = new TimeOnly(13, 45, 30),
        });

        var row = db.Table<TimeOnlyMethodEntity>()
            .Select(e => new
            {
                e.Time.Hour,
                e.Time.Minute,
                e.Time.Second,
                e.Time.Ticks,
            })
            .First();

        Assert.Equal(13, row.Hour);
        Assert.Equal(45, row.Minute);
        Assert.Equal(30, row.Second);
        Assert.Equal(new TimeOnly(13, 45, 30).Ticks, row.Ticks);
    }

    [Fact]
    public void Select_SubqueryEntityInProjection()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Author>().AddRange([
            new Author { Id = 1, Name = "Match", Email = "m@x", BirthDate = new DateTime(2000, 1, 1) },
        ]);
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "T1", AuthorId = 1, Price = 5 },
            new Book { Id = 2, Title = "T2", AuthorId = 99, Price = 10 },
        ]);

        var rows = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Select(b => new
            {
                b.Id,
                AuthorName = db.Table<Author>().Where(a => a.Id == b.AuthorId).Select(a => a.Name).FirstOrDefault(),
            })
            .ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal("Match", rows[0].AuthorName);
        Assert.Null(rows[1].AuthorName);
    }

    [Fact]
    public void Nullable_GetValueOrDefault()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NullableEntity>();
        db.Table<NullableEntity>().AddRange([
            new NullableEntity { Id = 1, Value = 10 },
            new NullableEntity { Id = 2, Value = null },
        ]);

        List<int> rows = db.Table<NullableEntity>()
            .OrderBy(e => e.Id)
            .Select(e => e.Value.GetValueOrDefault())
            .ToList();

        Assert.Equal([10, 0], rows);
    }

    [Fact]
    public void Nullable_GetValueOrDefault_WithFallback()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NullableEntity>();
        db.Table<NullableEntity>().AddRange([
            new NullableEntity { Id = 1, Value = 10 },
            new NullableEntity { Id = 2, Value = null },
        ]);

        List<int> rows = db.Table<NullableEntity>()
            .OrderBy(e => e.Id)
            .Select(e => e.Value.GetValueOrDefault(99))
            .ToList();

        Assert.Equal([10, 99], rows);
    }

    [Fact]
    public void Where_StringEquals_OrdinalCaseSensitive()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Test", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "TEST", AuthorId = 1, Price = 2 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => b.Title.Equals("Test", StringComparison.Ordinal))
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void Where_ColumnToColumnComparison()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "1", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "Two", AuthorId = 1, Price = 2 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => b.Title == b.Id.ToString())
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void Select_TimeSpanTotalSeconds_FromColumn()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<TimeOnlyMethodEntity>();
        db.Table<TimeOnlyMethodEntity>().Add(new TimeOnlyMethodEntity
        {
            Id = 1,
            Time = new TimeOnly(1, 0, 0),
        });

        long ticks = db.Table<TimeOnlyMethodEntity>()
            .Select(e => e.Time.Ticks)
            .First();

        Assert.Equal(TimeSpan.FromHours(1).Ticks, ticks);
    }

    [Fact]
    public void Math_Pow_ColumnOnColumn()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 3, Title = "A", AuthorId = 2, Price = 5 });

        double v = db.Table<Book>()
            .Select(b => Math.Pow(b.Price, b.AuthorId))
            .First();

        Assert.Equal(25.0, v);
    }

    [Fact]
    public void Select_NullConditional_OnString()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NullableStringEntity>();
        db.Table<NullableStringEntity>().AddRange([
            new NullableStringEntity { Id = 1, Name = "hello" },
            new NullableStringEntity { Id = 2, Name = null },
        ]);

        List<int?> rows = db.Table<NullableStringEntity>()
            .OrderBy(e => e.Id)
            .Select(e => e.Name == null ? (int?)null : e.Name.Length)
            .ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal(5, rows[0]);
        Assert.Null(rows[1]);
    }

    [Fact]
    public void Where_StringIsNullOrEmpty_OnNullableColumn()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NullableStringEntity>();
        db.Table<NullableStringEntity>().AddRange([
            new NullableStringEntity { Id = 1, Name = "hello" },
            new NullableStringEntity { Id = 2, Name = null },
            new NullableStringEntity { Id = 3, Name = "" },
        ]);

        List<int> ids = db.Table<NullableStringEntity>()
            .Where(e => string.IsNullOrEmpty(e.Name))
            .OrderBy(e => e.Id)
            .Select(e => e.Id)
            .ToList();

        Assert.Equal([2, 3], ids);
    }

    [Fact]
    public void Where_StartsWith_ColumnToColumn()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<TwoStringEntity>();
        db.Table<TwoStringEntity>().AddRange([
            new TwoStringEntity { Id = 1, A = "Hello World", B = "Hello" },
            new TwoStringEntity { Id = 2, A = "Foo", B = "Bar" },
        ]);

        List<int> ids = db.Table<TwoStringEntity>()
            .Where(e => e.A.StartsWith(e.B))
            .Select(e => e.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void Select_TimeSpanFromDays_FromColumn()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 5, Title = "A", AuthorId = 1, Price = 1 });

        long ticks = db.Table<Book>()
            .Select(b => TimeSpan.FromDays(b.Id).Ticks)
            .First();

        Assert.Equal(TimeSpan.FromDays(5).Ticks, ticks);
    }

    [Fact]
    public void Where_Default_KeywordEqualsZero()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 0, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 5, Price = 2 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => b.AuthorId == default)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void Select_EnumToIntCast()
    {
        using TestDatabase db = new();
        db.Table<Publisher>().Schema.CreateTable();
        db.Table<Publisher>().Add(new Publisher
        {
            Id = 1,
            Name = "Pub",
            Type = Enums.PublisherType.Magazine,
        });

        List<int> rows = db.Table<Publisher>()
            .Select(p => (int)p.Type)
            .ToList();

        Assert.Equal([(int)Enums.PublisherType.Magazine], rows);
    }

    [Fact]
    public void Where_CapturedFunc_IsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        Func<int, bool> pred = id => id > 0;
        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Where(b => pred(b.Id)).ToList());
    }

    [Fact]
    public void OnAction_Skip_MidBatch_OtherRowsCommit()
    {
        using TestDatabase db = new(b => b.OnAction((_, entity, action) =>
        {
            if (action == SQLite.Framework.Enums.SQLiteAction.Add && entity is Book book && book.Title == "skip")
            {
                return SQLite.Framework.Enums.SQLiteAction.Skip;
            }
            return action;
        }));

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "keep", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "skip", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "also keep", AuthorId = 1, Price = 3 },
        ]);

        List<int> ids = db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Id).ToList();
        Assert.Equal([1, 3], ids);
    }

    [Fact]
    public void Where_Contains_ColumnToColumn()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<TwoStringEntity>();
        db.Table<TwoStringEntity>().AddRange([
            new TwoStringEntity { Id = 1, A = "Hello World", B = "World" },
            new TwoStringEntity { Id = 2, A = "Hello World", B = "Mars" },
        ]);

        List<int> ids = db.Table<TwoStringEntity>()
            .Where(e => e.A.Contains(e.B))
            .Select(e => e.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void Select_TupleCreate()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        List<Tuple<int, string>> rows = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Select(b => Tuple.Create(b.Id, b.Title))
            .ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal(1, rows[0].Item1);
        Assert.Equal("A", rows[0].Item2);
    }

    [Fact]
    public void Select_CapturedObjectProperty()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        var captured = new { Multiplier = 5.5 };

        List<double> rows = db.Table<Book>()
            .Select(b => b.Price * captured.Multiplier)
            .ToList();

        Assert.Equal([5.5], rows);
    }

    [Fact]
    public void Where_NullableHasValueAndCompareValue()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NullableEntity>();
        db.Table<NullableEntity>().AddRange([
            new NullableEntity { Id = 1, Value = 10 },
            new NullableEntity { Id = 2, Value = null },
            new NullableEntity { Id = 3, Value = 100 },
        ]);

        List<int> ids = db.Table<NullableEntity>()
            .Where(e => e.Value.HasValue && e.Value.Value > 50)
            .Select(e => e.Id)
            .ToList();

        Assert.Equal([3], ids);
    }

    [Fact]
    public void Sum_OnFilteredQuery()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 4 },
        ]);

        double sum = db.Table<Book>()
            .Where(b => b.AuthorId == 1)
            .Sum(b => b.Price);

        Assert.Equal(3.0, sum);
    }

    [Fact]
    public void Where_DateTimeCompare_StaticMethod_IsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Author>().Add(new Author
        {
            Id = 1,
            Name = "A",
            Email = "a@x",
            BirthDate = new DateTime(2020, 1, 1),
        });

        DateTime cutoff = new(2010, 1, 1);
        Assert.Throws<NotSupportedException>(() =>
            db.Table<Author>()
                .Where(a => DateTime.Compare(a.BirthDate, cutoff) > 0)
                .ToList());
    }

    [Fact]
    public void Math_Clamp_ClampsToRange()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = -5 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 5 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 50 },
        ]);

        List<double> rows = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Select(b => Math.Clamp(b.Price, 0.0, 10.0))
            .ToList();

        Assert.Equal([0.0, 5.0, 10.0], rows);
    }

    [Fact]
    public void Where_BoolXor()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 2, Price = 2 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => (b.AuthorId == 1) ^ (b.Price > 1))
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1, 2], ids);
    }

    [Fact]
    public void Where_StringCompareToConstant()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Apple", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "Banana", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "Cherry", AuthorId = 1, Price = 3 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => b.Title.CompareTo("B") > 0)
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([2, 3], ids);
    }

    [Fact]
    public void Select_DateTimeDate_StripsTime()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();

        db.Table<Author>().Add(new Author
        {
            Id = 1,
            Name = "A",
            Email = "a@x",
            BirthDate = new DateTime(2024, 6, 15, 13, 45, 0),
        });

        List<DateTime> rows = db.Table<Author>()
            .Select(a => a.BirthDate.Date)
            .ToList();

        Assert.Single(rows);
        Assert.Equal(new DateTime(2024, 6, 15, 0, 0, 0), rows[0]);
    }

    [Fact]
    public void GroupBy_HavingOnAggregateValue()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "T1", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "T2", AuthorId = 1, Price = 4 },
            new Book { Id = 3, Title = "T3", AuthorId = 2, Price = 100 },
            new Book { Id = 4, Title = "T4", AuthorId = 3, Price = 2 },
        ]);

        var rows = (
            from b in db.Table<Book>()
            group b by b.AuthorId into g
            where g.Sum(x => x.Price) > 5
            orderby g.Key
            select new { AuthorId = g.Key, Total = g.Sum(x => x.Price) }
        ).ToList();

        Assert.Single(rows);
        Assert.Equal(2, rows[0].AuthorId);
        Assert.Equal(100.0, rows[0].Total);
    }

    [Fact]
    public void Where_ChainedMultipleTimes_AndsConditions()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 3 },
            new Book { Id = 4, Title = "D", AuthorId = 1, Price = 4 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => b.AuthorId == 1)
            .Where(b => b.Price > 1)
            .Where(b => b.Title != "D")
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([2], ids);
    }

    [Fact]
    public void OrderBy_ChainedTwice_SecondReplacesFirst()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "C", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "A", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "B", AuthorId = 2, Price = 3 },
        ]);

        List<int> ids = db.Table<Book>()
            .OrderBy(b => b.Title)
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1, 2, 3], ids);
    }

    [Fact]
    public void Take_ChainedTwice_TakesSmallerLimit()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 3 },
            new Book { Id = 4, Title = "D", AuthorId = 1, Price = 4 },
        ]);

        List<int> ids = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Take(3)
            .Take(2)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal(2, ids.Count);
    }

    [Fact]
    public void SkipTake_Composition()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange(Enumerable.Range(1, 10)
            .Select(i => new Book { Id = i, Title = $"T{i}", AuthorId = 1, Price = i }));

        List<int> ids = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Skip(2)
            .Take(5)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([3, 4, 5, 6, 7], ids);
    }

    [Fact]
    public void Select_StringIndexer_FirstChar()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Alpha", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "Bravo", AuthorId = 1, Price = 2 },
        ]);

        List<char> firsts = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Select(b => b.Title[0])
            .ToList();

        Assert.Equal(['A', 'B'], firsts);
    }

    [Fact]
    public void Select_IntToString()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 42, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        List<string> strs = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Select(b => b.Id.ToString())
            .ToList();

        Assert.Equal(["1", "42"], strs);
    }

    [Fact]
    public void Select_StringFormat_IsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Select(b => string.Format("{0}-{1}", b.Id, b.Title))
                .ToList());
    }

    [Fact]
    public void Select_MathTruncate_TranslatesToSql()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 5.7 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = -3.4 },
        ]);

        List<double> rows = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Select(b => Math.Truncate(b.Price))
            .ToList();

        Assert.Equal([5.0, -3.0], rows);
    }

    [Fact]
    public void Select_StringSubstring_SingleArg()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "Hello World", AuthorId = 1, Price = 1 });

        List<string> tails = db.Table<Book>()
            .Select(b => b.Title.Substring(6))
            .ToList();

        Assert.Equal(["World"], tails);
    }

    [Fact]
    public void Select_DoubleToDecimal_Cast()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 5.5 });

        List<decimal> values = db.Table<Book>()
            .Select(b => (decimal)b.Price)
            .ToList();

        Assert.Single(values);
        Assert.Equal(5.5m, values[0]);
    }

    [Fact]
    public void Cast_NoOpToBaseClass()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        List<Book> rows = db.Table<Book>().Cast<Book>().ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void Subquery_NestedThreeLevels()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Author>().AddRange([
            new Author { Id = 1, Name = "Match", Email = "m@x", BirthDate = new DateTime(2000, 1, 1) },
            new Author { Id = 2, Name = "Other", Email = "o@x", BirthDate = new DateTime(2000, 1, 1) },
        ]);
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "T1", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "T2", AuthorId = 2, Price = 2 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => db.Table<Author>()
                .Where(a => a.Name == "Match" && db.Table<Author>().Any(a2 => a2.Id == a.Id))
                .Select(a => a.Id)
                .Contains(b.AuthorId))
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void GroupBy_MultipleAggregatesInOneSelect()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 4 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 8 },
        ]);

        var rows = (
            from b in db.Table<Book>()
            group b by b.AuthorId into g
            orderby g.Key
            select new
            {
                AuthorId = g.Key,
                Count = g.Count(),
                Sum = g.Sum(x => x.Price),
                Avg = g.Average(x => x.Price),
                Max = g.Max(x => x.Price),
                Min = g.Min(x => x.Price),
            }
        ).ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal(1, rows[0].AuthorId);
        Assert.Equal(2, rows[0].Count);
        Assert.Equal(5.0, rows[0].Sum);
        Assert.Equal(2.5, rows[0].Avg);
        Assert.Equal(4.0, rows[0].Max);
        Assert.Equal(1.0, rows[0].Min);
        Assert.Equal(2, rows[1].AuthorId);
        Assert.Equal(1, rows[1].Count);
        Assert.Equal(8.0, rows[1].Sum);
    }
}
