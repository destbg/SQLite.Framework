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
    public void Select_NullConditional_OnNullableString()
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
    public void Where_CapturedHashSetContains()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 3 },
        ]);

        HashSet<int> wanted = [1, 3];
        List<int> ids = db.Table<Book>()
            .Where(b => wanted.Contains(b.Id))
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1, 3], ids);
    }

    [Fact]
    public void Where_CapturedIEnumerableContains()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        IEnumerable<int> ids = new[] { 2 };
        List<int> result = db.Table<Book>()
            .Where(b => ids.Contains(b.Id))
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([2], result);
    }

    [Fact]
    public void Where_DisjunctionOfEquality()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 2, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 3, Price = 3 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => b.Id == 1 || b.Id == 3)
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1, 3], ids);
    }

    [Fact]
    public void Where_AnyExists_OnUncorrelated()
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
        db.Table<Book>().Add(new Book { Id = 1, Title = "T", AuthorId = 1, Price = 1 });

        bool any = db.Table<Book>()
            .Where(b => db.Table<Author>().Any(a => a.Id == b.AuthorId))
            .Any();

        Assert.True(any);
    }

    [Fact]
    public void Sum_OnPrimitiveSelectNoSelector()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 3 },
        ]);

        double total = db.Table<Book>().Select(b => b.Price).Sum();

        Assert.Equal(6.0, total);
    }

    [Fact]
    public void Where_NullableBoolEqualsTrue()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NullableBoolEntity>();
        db.Table<NullableBoolEntity>().AddRange([
            new NullableBoolEntity { Id = 1, Flag = true },
            new NullableBoolEntity { Id = 2, Flag = false },
            new NullableBoolEntity { Id = 3, Flag = null },
        ]);

        List<int> ids = db.Table<NullableBoolEntity>()
            .Where(e => e.Flag == true)
            .Select(e => e.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void Where_NullableBoolCoalesce()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NullableBoolEntity>();
        db.Table<NullableBoolEntity>().AddRange([
            new NullableBoolEntity { Id = 1, Flag = true },
            new NullableBoolEntity { Id = 2, Flag = false },
            new NullableBoolEntity { Id = 3, Flag = null },
        ]);

        List<int> ids = db.Table<NullableBoolEntity>()
            .Where(e => e.Flag ?? false)
            .Select(e => e.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void GroupBy_OrderByAggregate_DoubleSort()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "T", AuthorId = 1, Price = 5 },
            new Book { Id = 2, Title = "T", AuthorId = 2, Price = 100 },
            new Book { Id = 3, Title = "T", AuthorId = 2, Price = 200 },
            new Book { Id = 4, Title = "T", AuthorId = 3, Price = 50 },
        ]);

        var rows = (
            from b in db.Table<Book>()
            group b by b.AuthorId into g
            orderby g.Sum(x => x.Price) descending, g.Key
            select new { AuthorId = g.Key, Total = g.Sum(x => x.Price) }
        ).ToList();

        Assert.Equal(3, rows.Count);
        Assert.Equal(2, rows[0].AuthorId);
        Assert.Equal(300.0, rows[0].Total);
        Assert.Equal(3, rows[1].AuthorId);
        Assert.Equal(1, rows[2].AuthorId);
    }

    [Fact]
    public void SelectMany_TwoSourceQueries_CrossProduct()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Author>().AddRange([
            new Author { Id = 1, Name = "A", Email = "a@x", BirthDate = new DateTime(2000, 1, 1) },
            new Author { Id = 2, Name = "B", Email = "b@x", BirthDate = new DateTime(2000, 1, 1) },
        ]);
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "X", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "Y", AuthorId = 1, Price = 2 },
        ]);

        var rows = db.Table<Author>()
            .SelectMany(_ => db.Table<Book>(), (a, b) => new { a.Name, b.Title })
            .OrderBy(r => r.Name)
            .ThenBy(r => r.Title)
            .ToList();

        Assert.Equal(4, rows.Count);
    }

    [Fact]
    public void Sum_OverIntCast()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 3 },
        ]);

        long sum = db.Table<Book>().Sum(b => (long)b.Id);

        Assert.Equal(6L, sum);
    }

    [Fact]
    public void Sum_DirectOnIntColumn()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 2, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 3, Price = 3 },
        ]);

        int sum = db.Table<Book>().Sum(b => b.Id);

        Assert.Equal(6, sum);
    }

    [Fact]
    public void Aggregate_AfterTake_IsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 3 },
        ]);

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().OrderBy(b => b.Id).Take(2).Count());

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().OrderBy(b => b.Id).Take(2).Sum(b => b.Price));
    }

    [Fact]
    public void OrderByDesc_First_LimitOne()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 5 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 100 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 50 },
        ]);

        Book max = db.Table<Book>()
            .OrderByDescending(b => b.Price)
            .First();

        Assert.Equal(2, max.Id);
    }

    [Fact]
    public void Where_MathAbs_OnColumn()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = -3 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 7 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => Math.Abs(b.Price) > 5)
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([2], ids);
    }

    [Fact]
    public void Where_MathSign_OnComputed()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 3 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 10 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => Math.Sign(b.Price - 5) > 0)
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([2], ids);
    }

    [Fact]
    public void Select_StringTernary_DifferentBranchTypes()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "X", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "Hello World", AuthorId = 1, Price = 2 },
        ]);

        List<string> rows = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Select(b => b.Title.Length > 5 ? "long" : "short")
            .ToList();

        Assert.Equal(["short", "long"], rows);
    }

    [Fact]
    public void Where_Take_ZeroEdgeCase()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        List<Book> empty = db.Table<Book>().Skip(0).Take(0).ToList();

        Assert.Empty(empty);
    }

    [Fact]
    public void Where_ComputedExpressionInPredicate()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 5 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => (b.Price + 1) * 2 > 10)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([2], ids);
    }

    [Fact]
    public void Where_CapturedDeepPropertyAccess()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        var captured = new { Inner = new { Title = "B" } };

        List<int> ids = db.Table<Book>()
            .Where(b => b.Title == captured.Inner.Title)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([2], ids);
    }

    [Fact]
    public void Where_CapturedNullableInt()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NullableEntity>();
        db.Table<NullableEntity>().AddRange([
            new NullableEntity { Id = 1, Value = 5 },
            new NullableEntity { Id = 2, Value = 10 },
        ]);

        int? captured = 5;
        List<int> ids = db.Table<NullableEntity>()
            .Where(e => e.Value == captured)
            .Select(e => e.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void First_WithPredicate()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "X", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "Y", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "Y", AuthorId = 2, Price = 3 },
        ]);

        Book b = db.Table<Book>()
            .OrderBy(x => x.Id)
            .First(x => x.Title == "Y");

        Assert.Equal(2, b.Id);
    }

    [Fact]
    public void Where_StringIndexOf_ColumnToColumn()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<TwoStringEntity>();
        db.Table<TwoStringEntity>().AddRange([
            new TwoStringEntity { Id = 1, A = "Hello World", B = "World" },
            new TwoStringEntity { Id = 2, A = "Foo", B = "Bar" },
        ]);

        List<int> ids = db.Table<TwoStringEntity>()
            .Where(e => e.A.IndexOf(e.B) >= 0)
            .Select(e => e.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void Sum_OnEmpty_ReturnsZero()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        double total = db.Table<Book>().Sum(b => b.Price);

        Assert.Equal(0.0, total);
    }

    [Fact]
    public void Max_OnEmpty_ThrowsInvalidOperation()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<InvalidOperationException>(() => db.Table<Book>().Max(b => b.Price));
    }

    [Fact]
    public void Min_OnEmpty_ThrowsInvalidOperation()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<InvalidOperationException>(() => db.Table<Book>().Min(b => b.Price));
    }

    [Fact]
    public void Average_OnEmpty_ThrowsInvalidOperation()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<InvalidOperationException>(() => db.Table<Book>().Average(b => b.Price));
    }

    [Fact]
    public void Max_OnEmptyNullable_ReturnsNull()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NullableEntity>();

        int? max = db.Table<NullableEntity>().Max(e => e.Value);

        Assert.Null(max);
    }

    [Fact]
    public void Where_DecimalArithmetic()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().AddRange([
            new NumericType { Id = 1, DecimalValue = 10m, BlobValue = null },
            new NumericType { Id = 2, DecimalValue = 5m, BlobValue = null },
        ]);

        List<int> ids = db.Table<NumericType>()
            .Where(n => n.DecimalValue * 1.5m > 10m)
            .Select(n => n.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void ExecuteUpdate_MultipleSets_Chained()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Old", AuthorId = 1, Price = 5 });

        db.Table<Book>().Where(b => b.Id == 1).ExecuteUpdate(s => s
            .Set(b => b.Title, "New")
            .Set(b => b.Price, b => b.Price * 2));

        Book b = db.Table<Book>().First();
        Assert.Equal("New", b.Title);
        Assert.Equal(10.0, b.Price);
    }

    [Fact]
    public void ExecuteDelete_WithAnySubquery()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Author>().Add(new Author
        {
            Id = 1,
            Name = "Active",
            Email = "a@x",
            BirthDate = new DateTime(2000, 1, 1),
        });
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "T1", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "T2", AuthorId = 99, Price = 2 },
        ]);

        int n = db.Table<Book>()
            .Where(b => !db.Table<Author>().Any(a => a.Id == b.AuthorId))
            .ExecuteDelete();

        Assert.Equal(1, n);
        List<int> remaining = db.Table<Book>().Select(b => b.Id).ToList();
        Assert.Equal([1], remaining);
    }

    [Fact]
    public void Where_MathMin_OnColumnAndConst()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 3 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 20 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => Math.Min(b.Price, 10.0) > 5)
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([2], ids);
    }

    [Fact]
    public void Where_SameColumnTwice_InContains()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Hello", AuthorId = 1, Price = 1 });

        List<int> ids = db.Table<Book>()
            .Where(b => b.Title.Contains(b.Title))
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1], ids);
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

    [Fact]
    public void Union_FollowedByTake_PagesAcrossUnion()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 3 },
            new Book { Id = 4, Title = "D", AuthorId = 2, Price = 4 },
        ]);

        IQueryable<int> low = db.Table<Book>().Where(b => b.Price < 3).Select(b => b.Id);
        IQueryable<int> high = db.Table<Book>().Where(b => b.Price > 2).Select(b => b.Id);

        List<int> firstTwo = low.Union(high).OrderBy(x => x).Take(2).ToList();

        Assert.Equal([1, 2], firstTwo);
    }

    [Fact]
    public void Union_FollowedBySkipTake_PagesAcrossUnion()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 3 },
            new Book { Id = 4, Title = "D", AuthorId = 2, Price = 4 },
        ]);

        IQueryable<int> low = db.Table<Book>().Where(b => b.Price < 3).Select(b => b.Id);
        IQueryable<int> high = db.Table<Book>().Where(b => b.Price > 2).Select(b => b.Id);

        List<int> middle = low.Union(high).OrderBy(x => x).Skip(1).Take(2).ToList();

        Assert.Equal([2, 3], middle);
    }

    [Fact]
    public void Concat_FollowedByCount_ThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        IQueryable<int> a = db.Table<Book>().Select(b => b.Id);
        IQueryable<int> b2 = db.Table<Book>().Select(b => b.Id);

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => a.Concat(b2).Count());
        Assert.Contains("Concat/Union/Intersect/Except", ex.Message);
    }

    [Fact]
    public void Union_FollowedBySum_ThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        IQueryable<double> a = db.Table<Book>().Select(b => b.Price);
        IQueryable<double> b2 = db.Table<Book>().Select(b => b.Price);

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => a.Union(b2).Sum());
        Assert.Contains("Concat/Union/Intersect/Except", ex.Message);
    }

    [Fact]
    public void Concat_FollowedByWhere_ThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        IQueryable<int> a = db.Table<Book>().Select(b => b.Id);
        IQueryable<int> b2 = db.Table<Book>().Select(b => b.Id);

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => a.Concat(b2).Where(x => x > 0).ToList());
        Assert.Contains("Concat/Union/Intersect/Except", ex.Message);
    }

    [Fact]
    public void Concat_FollowedByNonIdentitySelect_ThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        IQueryable<int> a = db.Table<Book>().Select(b => b.Id);
        IQueryable<int> b2 = db.Table<Book>().Select(b => b.Id);

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => a.Concat(b2).Select(x => x + 100).ToList());
        Assert.Contains("Concat/Union/Intersect/Except", ex.Message);
    }

    [Fact]
    public void Concat_FollowedByDistinct_ThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        IQueryable<int> a = db.Table<Book>().Select(b => b.Id);
        IQueryable<int> b2 = db.Table<Book>().Select(b => b.Id);

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => a.Concat(b2).Distinct().ToList());
        Assert.Contains("Concat/Union/Intersect/Except", ex.Message);
    }

    [Fact]
    public void Concat_FollowedByGroupBy_ThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        IQueryable<Book> a = db.Table<Book>();
        IQueryable<Book> b2 = db.Table<Book>();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            a.Concat(b2).GroupBy(b => b.AuthorId).Select(g => g.Key).ToList());
        Assert.Contains("Concat/Union/Intersect/Except", ex.Message);
    }

    [Fact]
    public void Concat_FollowedByReverse_ThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        IQueryable<int> a = db.Table<Book>().Select(b => b.Id);
        IQueryable<int> b2 = db.Table<Book>().Select(b => b.Id);

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => a.Concat(b2).Reverse().ToList());
        Assert.Contains("Concat/Union/Intersect/Except", ex.Message);
    }

    [Fact]
    public void Concat_FollowedByContains_ThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        IQueryable<int> a = db.Table<Book>().Select(b => b.Id);
        IQueryable<int> b2 = db.Table<Book>().Select(b => b.Id);

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => a.Concat(b2).Contains(1));
        Assert.Contains("Concat/Union/Intersect/Except", ex.Message);
    }

    [Fact]
    public void Concat_FollowedByFirstWithPredicate_ThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        IQueryable<int> a = db.Table<Book>().Select(b => b.Id);
        IQueryable<int> b2 = db.Table<Book>().Select(b => b.Id);

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => a.Concat(b2).First(x => x == 1));
        Assert.Contains("Concat/Union/Intersect/Except", ex.Message);
    }

    [Fact]
    public void Concat_ThenChainedConcat_Works()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        IQueryable<int> a = db.Table<Book>().Select(b => b.Id);
        IQueryable<int> b2 = db.Table<Book>().Select(b => b.Id);
        IQueryable<int> c = db.Table<Book>().Select(b => b.Id);

        List<int> result = a.Concat(b2).Concat(c).OrderBy(x => x).ToList();

        Assert.Equal([1, 1, 1, 2, 2, 2], result);
    }

    [Fact]
    public void Concat_ThenChainedExcept_Works()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        IQueryable<int> a = db.Table<Book>().Where(b => b.Id == 1).Select(b => b.Id);
        IQueryable<int> b2 = db.Table<Book>().Where(b => b.Id == 2).Select(b => b.Id);
        IQueryable<int> c = db.Table<Book>().Where(b => b.Id == 1).Select(b => b.Id);

        List<int> result = a.Concat(b2).Except(c).OrderBy(x => x).ToList();

        Assert.Equal([2], result);
    }

    [Fact]
    public void Concat_FollowedByIdentitySelect_Works()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        IQueryable<int> a = db.Table<Book>().Select(b => b.Id);
        IQueryable<int> b2 = db.Table<Book>().Select(b => b.Id);

        List<int> result = a.Concat(b2).Select(x => x).OrderBy(x => x).ToList();

        Assert.Equal([1, 1, 2, 2], result);
    }

    [Fact]
    public void Concat_FollowedByFirstNoPredicate_Works()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
        ]);

        IQueryable<int> a = db.Table<Book>().Select(b => b.Id);
        IQueryable<int> b2 = db.Table<Book>().Select(b => b.Id);

        int first = a.Concat(b2).First();

        Assert.Equal(1, first);
    }

    [Fact]
    public void Concat_FollowedByAnyNoPredicate_Works()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
        ]);

        IQueryable<int> a = db.Table<Book>().Select(b => b.Id);
        IQueryable<int> b2 = db.Table<Book>().Select(b => b.Id);

        bool any = a.Concat(b2).Any();

        Assert.True(any);
    }

    [Fact]
    public void Select_DoubleNestedAnonymousType()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Outer", AuthorId = 7, Price = 9.5 },
        ]);

        var row = db.Table<Book>()
            .Select(b => new
            {
                b.Id,
                Inner = new
                {
                    b.Title,
                    Deep = new { b.AuthorId, b.Price },
                },
            })
            .Single();

        Assert.Equal(1, row.Id);
        Assert.Equal("Outer", row.Inner.Title);
        Assert.Equal(7, row.Inner.Deep.AuthorId);
        Assert.Equal(9.5, row.Inner.Deep.Price);
    }

    [Fact]
    public void GroupBy_ComputedBooleanKey()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 5 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 12 },
            new Book { Id = 4, Title = "D", AuthorId = 1, Price = 20 },
        ]);

        var rows = (
            from b in db.Table<Book>()
            group b by b.Price > 10 into g
            orderby g.Key
            select new { Expensive = g.Key, Count = g.Count() }
        ).ToList();

        Assert.Equal(2, rows.Count);
        Assert.False(rows[0].Expensive);
        Assert.Equal(2, rows[0].Count);
        Assert.True(rows[1].Expensive);
        Assert.Equal(2, rows[1].Count);
    }

    [Fact]
    public void Select_MultipleCorrelatedCountSubqueries()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Author>().AddRange([
            new Author { Id = 1, Name = "A1", Email = "a@x", BirthDate = new DateTime(2000, 1, 1) },
            new Author { Id = 2, Name = "A2", Email = "b@x", BirthDate = new DateTime(2000, 1, 1) },
        ]);
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "X", AuthorId = 1, Price = 5 },
            new Book { Id = 2, Title = "Y", AuthorId = 1, Price = 15 },
            new Book { Id = 3, Title = "Z", AuthorId = 2, Price = 25 },
        ]);

        var rows = db.Table<Author>()
            .OrderBy(a => a.Id)
            .Select(a => new
            {
                a.Id,
                Cheap = db.Table<Book>().Count(b => b.AuthorId == a.Id && b.Price < 10),
                Expensive = db.Table<Book>().Count(b => b.AuthorId == a.Id && b.Price >= 10),
            })
            .ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal(1, rows[0].Id);
        Assert.Equal(1, rows[0].Cheap);
        Assert.Equal(1, rows[0].Expensive);
        Assert.Equal(2, rows[1].Id);
        Assert.Equal(0, rows[1].Cheap);
        Assert.Equal(1, rows[1].Expensive);
    }

    [Fact]
    public void Concat_OnEntityType_MaterializesAllRows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        IQueryable<Book> low = db.Table<Book>().Where(b => b.Price < 3);
        IQueryable<Book> high = db.Table<Book>().Where(b => b.Price > 0);

        List<Book> rows = low.Concat(high).OrderBy(b => b.Id).ToList();

        Assert.Equal(4, rows.Count);
        Assert.Equal([1, 1, 2, 2], rows.Select(r => r.Id));
        Assert.Equal(["A", "A", "B", "B"], rows.Select(r => r.Title));
    }

    [Fact]
    public void GroupBy_FollowedByTake_LimitsGroups()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 2, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 3, Price = 3 },
        ]);

        var rows = db.Table<Book>()
            .GroupBy(b => b.AuthorId)
            .OrderBy(g => g.Key)
            .Take(2)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal(1, rows[0].Id);
        Assert.Equal(2, rows[1].Id);
    }

    [Fact]
    public void Where_OnDateTimeColumn_AgainstClientNow()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();

        DateTime past = new(2000, 1, 1);
        DateTime future = new(2099, 1, 1);

        db.Table<Author>().AddRange([
            new Author { Id = 1, Name = "Old", Email = "a@x", BirthDate = past },
            new Author { Id = 2, Name = "Young", Email = "b@x", BirthDate = future },
        ]);

        DateTime now = DateTime.UtcNow;

        List<int> oldIds = db.Table<Author>()
            .Where(a => a.BirthDate < now)
            .OrderBy(a => a.Id)
            .Select(a => a.Id)
            .ToList();

        Assert.Equal([1], oldIds);
    }

    [Fact]
    public void Where_OnDateTimeAdd_TranslatesToSql()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();

        db.Table<Author>().AddRange([
            new Author { Id = 1, Name = "Eligible", Email = "a@x", BirthDate = new DateTime(1990, 1, 1) },
            new Author { Id = 2, Name = "Recent", Email = "b@x", BirthDate = new DateTime(2025, 1, 1) },
        ]);

        DateTime cutoff = new(2020, 1, 1);

        List<int> ids = db.Table<Author>()
            .Where(a => a.BirthDate.AddYears(20) < cutoff)
            .OrderBy(a => a.Id)
            .Select(a => a.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void Select_NullableArithmetic_AddingNullPropagatesNull()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();

        db.Table<NullableEntity>().AddRange([
            new NullableEntity { Id = 1, Value = 10 },
            new NullableEntity { Id = 2, Value = null },
        ]);

        List<int?> results = db.Table<NullableEntity>()
            .OrderBy(e => e.Id)
            .Select(e => e.Value + 5)
            .ToList();

        Assert.Equal([15, null], results);
    }

    [Fact]
    public void ExecuteUpdate_WithTernaryExpression()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Cheap", AuthorId = 1, Price = 5 },
            new Book { Id = 2, Title = "Pricey", AuthorId = 1, Price = 20 },
        ]);

        db.Table<Book>().ExecuteUpdate(s =>
            s.Set(b => b.Price, b => b.Price > 10 ? b.Price * 0.9 : b.Price + 1));

        List<Book> result = db.Table<Book>().OrderBy(b => b.Id).ToList();

        Assert.Equal(6.0, result[0].Price);
        Assert.Equal(18.0, result[1].Price);
    }

    [Fact]
    public void ExecuteUpdate_WithStringConcatenation()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        db.Table<Book>().ExecuteUpdate(s =>
            s.Set(b => b.Title, b => b.Title + "_v2"));

        List<string> titles = db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Title).ToList();

        Assert.Equal(["A_v2", "B_v2"], titles);
    }

    [Fact]
    public void ExecuteUpdate_WithSubqueryInValue()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Author>().AddRange([
            new Author { Id = 1, Name = "X", Email = "x@x", BirthDate = new DateTime(2000, 1, 1) },
            new Author { Id = 2, Name = "Y", Email = "y@x", BirthDate = new DateTime(2000, 1, 1) },
        ]);
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 5 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 7 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 3 },
        ]);

        db.Table<Author>().ExecuteUpdate(s =>
            s.Set(a => a.Name, a => "Books:" + db.Table<Book>().Count(b => b.AuthorId == a.Id)));

        List<string> names = db.Table<Author>().OrderBy(a => a.Id).Select(a => a.Name).ToList();

        Assert.Equal(["Books:2", "Books:1"], names);
    }

    [Fact]
    public void Where_NullCheck_OnNullableColumn()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();

        db.Table<NullableEntity>().AddRange([
            new NullableEntity { Id = 1, Value = 10 },
            new NullableEntity { Id = 2, Value = null },
            new NullableEntity { Id = 3, Value = 20 },
        ]);

        List<int> nullIds = db.Table<NullableEntity>()
            .Where(e => e.Value == null)
            .Select(e => e.Id)
            .ToList();

        List<int> nonNullIds = db.Table<NullableEntity>()
            .Where(e => e.Value != null)
            .OrderBy(e => e.Id)
            .Select(e => e.Id)
            .ToList();

        Assert.Equal([2], nullIds);
        Assert.Equal([1, 3], nonNullIds);
    }

    [Fact]
    public void Select_NullCoalescing_OnNullableColumn()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();

        db.Table<NullableEntity>().AddRange([
            new NullableEntity { Id = 1, Value = 10 },
            new NullableEntity { Id = 2, Value = null },
        ]);

        List<int> values = db.Table<NullableEntity>()
            .OrderBy(e => e.Id)
            .Select(e => e.Value ?? -1)
            .ToList();

        Assert.Equal([10, -1], values);
    }

    [Fact]
    public void OrderBy_NullableColumn_NullsLast()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();

        db.Table<NullableEntity>().AddRange([
            new NullableEntity { Id = 1, Value = 10 },
            new NullableEntity { Id = 2, Value = null },
            new NullableEntity { Id = 3, Value = 5 },
        ]);

        List<int> ids = db.Table<NullableEntity>()
            .OrderByDescending(e => e.Value)
            .Select(e => e.Id)
            .ToList();

        Assert.Equal([1, 3, 2], ids);
    }

    [Fact]
    public void Average_OnIntegerColumn_ReturnsDouble()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 2, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 4, Price = 3 },
        ]);

        double avg = db.Table<Book>().Average(b => b.AuthorId);

        Assert.Equal(7.0 / 3.0, avg, 6);
    }

    [Fact]
    public void OfType_ThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() => db.Table<Book>().OfType<Book>().ToList());
    }

    [Fact]
    public void Zip_ThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        IQueryable<int> ids = db.Table<Book>().Select(b => b.Id);
        IQueryable<int> ids2 = db.Table<Book>().Select(b => b.Id);

        Assert.Throws<NotSupportedException>(() => ids.Zip(ids2, (a, b) => a + b).ToList());
    }

    [Fact]
    public void Distinct_ThenWhere_FiltersAfterDedup()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 3 },
        ]);

        List<int> result = db.Table<Book>()
            .Select(b => b.AuthorId)
            .Distinct()
            .Where(x => x == 1)
            .ToList();

        Assert.Equal([1], result);
    }

    [Fact]
    public void Distinct_ThenSelect_AppliesProjection()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 3 },
        ]);

        List<int> result = db.Table<Book>()
            .Select(b => b.AuthorId)
            .Distinct()
            .Select(x => x + 100)
            .OrderBy(x => x)
            .ToList();

        Assert.Equal([101, 102], result);
    }

    [Fact]
    public void Take_AfterTake_TakesTighterLimit()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 3 },
            new Book { Id = 4, Title = "D", AuthorId = 1, Price = 4 },
        ]);

        List<int> tight = db.Table<Book>().OrderBy(b => b.Id).Take(2).Take(5).Select(b => b.Id).ToList();
        List<int> loose = db.Table<Book>().OrderBy(b => b.Id).Take(5).Take(2).Select(b => b.Id).ToList();

        Assert.Equal([1, 2], tight);
        Assert.Equal([1, 2], loose);
    }

    [Fact]
    public void Skip_AfterSkip_AccumulatesOffset()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 3 },
            new Book { Id = 4, Title = "D", AuthorId = 1, Price = 4 },
            new Book { Id = 5, Title = "E", AuthorId = 1, Price = 5 },
        ]);

        List<int> result = db.Table<Book>().OrderBy(b => b.Id).Skip(2).Skip(2).Select(b => b.Id).ToList();

        Assert.Equal([5], result);
    }

    [Fact]
    public void Skip_AfterTake_SkipsWithinTakenWindow()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 3 },
            new Book { Id = 4, Title = "D", AuthorId = 1, Price = 4 },
            new Book { Id = 5, Title = "E", AuthorId = 1, Price = 5 },
        ]);

        List<int> result = db.Table<Book>().OrderBy(b => b.Id).Take(3).Skip(2).Select(b => b.Id).ToList();

        Assert.Equal([3], result);
    }

    [Fact]
    public void Skip_AfterTake_BeyondTakenWindow_ReturnsEmpty()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 3 },
        ]);

        List<int> result = db.Table<Book>().OrderBy(b => b.Id).Take(2).Skip(5).Select(b => b.Id).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void SkipTakeSkip_NestedPaging()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 3 },
            new Book { Id = 4, Title = "D", AuthorId = 1, Price = 4 },
            new Book { Id = 5, Title = "E", AuthorId = 1, Price = 5 },
            new Book { Id = 6, Title = "F", AuthorId = 1, Price = 6 },
        ]);

        List<int> result = db.Table<Book>().OrderBy(b => b.Id).Skip(1).Take(4).Skip(1).Select(b => b.Id).ToList();

        Assert.Equal([3, 4, 5], result);
    }

    [Fact]
    public void OrderBy_AfterOrderBy_OverridesEarlierKey()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "B", AuthorId = 1, Price = 3 },
            new Book { Id = 2, Title = "A", AuthorId = 2, Price = 2 },
            new Book { Id = 3, Title = "B", AuthorId = 1, Price = 1 },
        ]);

        List<int> ids = db.Table<Book>()
            .OrderBy(b => b.AuthorId)
            .OrderBy(b => b.Title)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([2, 1, 3], ids);
    }

    [Fact]
    public void OrderBy_ThenBy_AppliesAsSecondaryKey()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "B", AuthorId = 1, Price = 3 },
            new Book { Id = 2, Title = "A", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "B", AuthorId = 2, Price = 1 },
        ]);

        List<int> ids = db.Table<Book>()
            .OrderBy(b => b.Title)
            .ThenBy(b => b.AuthorId)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([2, 1, 3], ids);
    }

    [Fact]
    public void OrderByDescending_AfterOrderBy_OverridesEarlierKey()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 3 },
            new Book { Id = 2, Title = "B", AuthorId = 2, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 1 },
        ]);

        List<int> ids = db.Table<Book>()
            .OrderBy(b => b.AuthorId)
            .OrderByDescending(b => b.Title)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([3, 2, 1], ids);
    }

    [Fact]
    public void OrderByDescending_AfterOrderByDescending_OverridesEarlierKey()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 3 },
            new Book { Id = 2, Title = "B", AuthorId = 2, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 1 },
        ]);

        List<int> ids = db.Table<Book>()
            .OrderByDescending(b => b.AuthorId)
            .OrderByDescending(b => b.Title)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([3, 2, 1], ids);
    }

    [Fact]
    public void OrderBy_ThenByDescending_AppliesAsSecondaryDescending()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "B", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "A", AuthorId = 1, Price = 3 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 2 },
        ]);

        List<int> ids = db.Table<Book>()
            .OrderBy(b => b.AuthorId)
            .ThenByDescending(b => b.Price)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([2, 3, 1], ids);
    }

    [Fact]
    public void OrderByDescending_ThenBy_AppliesAsSecondaryAscending()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "B", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "A", AuthorId = 1, Price = 3 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 2 },
        ]);

        List<int> ids = db.Table<Book>()
            .OrderByDescending(b => b.AuthorId)
            .ThenBy(b => b.Price)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1, 3, 2], ids);
    }

    [Fact]
    public void Where_StringComparisonOperators()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Apple", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "Banana", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "Cherry", AuthorId = 1, Price = 3 },
        ]);

        string b = "B";

        List<int> greaterIds = db.Table<Book>()
            .Where(x => string.Compare(x.Title, b) > 0)
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .ToList();

        Assert.Equal([2, 3], greaterIds);
    }

    [Fact]
    public void Where_NegationOfBoolean()
    {
        using TestDatabase db = new();
        db.Table<NullableBoolEntity>().Schema.CreateTable();

        db.Table<NullableBoolEntity>().AddRange([
            new NullableBoolEntity { Id = 1, Flag = true },
            new NullableBoolEntity { Id = 2, Flag = false },
            new NullableBoolEntity { Id = 3, Flag = null },
        ]);

        List<int> ids = db.Table<NullableBoolEntity>()
            .Where(e => !(e.Flag ?? false))
            .OrderBy(e => e.Id)
            .Select(e => e.Id)
            .ToList();

        Assert.Equal([2, 3], ids);
    }

    [Fact]
    public void Where_DoubleNegation()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => !!(b.Id == 1))
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void Where_BooleanExclusiveOr()
    {
        using TestDatabase db = new();
        db.Table<NullableBoolEntity>().Schema.CreateTable();

        db.Table<NullableBoolEntity>().AddRange([
            new NullableBoolEntity { Id = 1, Flag = true },
            new NullableBoolEntity { Id = 2, Flag = false },
        ]);

        List<int> ids = db.Table<NullableBoolEntity>()
            .Where(e => (e.Flag ?? false) ^ true)
            .Select(e => e.Id)
            .ToList();

        Assert.Equal([2], ids);
    }

    [Fact]
    public void GroupBy_HavingClause_FiltersGroups()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 3 },
        ]);

        var rows = db.Table<Book>()
            .GroupBy(b => b.AuthorId)
            .Where(g => g.Count() > 1)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToList();

        Assert.Single(rows);
        Assert.Equal(1, rows[0].Id);
        Assert.Equal(2, rows[0].Count);
    }

    [Fact]
    public void Where_BeforeGroupBy_AndAfter_FiltersBoth()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 5 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 15 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 20 },
            new Book { Id = 4, Title = "D", AuthorId = 2, Price = 25 },
        ]);

        var rows = db.Table<Book>()
            .Where(b => b.Price >= 10)
            .GroupBy(b => b.AuthorId)
            .Where(g => g.Count() >= 2)
            .Select(g => new { Id = g.Key, Total = g.Sum(b => b.Price) })
            .ToList();

        Assert.Single(rows);
        Assert.Equal(2, rows[0].Id);
        Assert.Equal(45.0, rows[0].Total);
    }

    [Fact]
    public void TripleSelectMany_CrossJoinThree()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);
        db.Table<Author>().AddRange([
            new Author { Id = 1, Name = "X", Email = "x@x", BirthDate = new DateTime(2000, 1, 1) },
        ]);

        int count = (
            from b1 in db.Table<Book>()
            from b2 in db.Table<Book>()
            from a in db.Table<Author>()
            select new { b1.Id, B2 = b2.Id, A = a.Id }
        ).Count();

        Assert.Equal(4, count);
    }

    [Fact]
    public void Query_WithLetClause_BindsComputedExpression()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 20 },
        ]);

        var rows = (
            from b in db.Table<Book>()
            let doubled = b.Price * 2
            where doubled > 25
            select new { b.Id, Doubled = doubled }
        ).ToList();

        Assert.Single(rows);
        Assert.Equal(2, rows[0].Id);
        Assert.Equal(40.0, rows[0].Doubled);
    }

    [Fact]
    public void Select_NestedTernary_ProducesNestedCase()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 5 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 15 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 30 },
        ]);

        List<string> tiers = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Select(b => b.Price < 10 ? "low" : b.Price < 25 ? "mid" : "high")
            .ToList();

        Assert.Equal(["low", "mid", "high"], tiers);
    }

    [Fact]
    public void Select_InterpolatedString_ThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Select(b => $"id={b.Id}").ToList());
    }

    [Fact]
    public void Where_ByEntityReference_ThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
        ]);

        Book target = db.Table<Book>().First(b => b.Id == 1);

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Count(b => b == target));
    }

    [Fact]
    public void Where_ConstantTrue_ReturnsAllRows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        bool flag = true;

        int count = db.Table<Book>().Count(b => flag);

        Assert.Equal(2, count);
    }

    [Fact]
    public void Where_ConstantFalse_ReturnsEmpty()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
        ]);

        bool flag = false;

        int count = db.Table<Book>().Count(b => flag);

        Assert.Equal(0, count);
    }

    [Fact]
    public void Skip_Zero_IsNoOp()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        List<int> ids = db.Table<Book>().OrderBy(b => b.Id).Skip(0).Select(b => b.Id).ToList();

        Assert.Equal([1, 2], ids);
    }

    [Fact]
    public void SQLiteFunctions_MinThreeArgs_ReturnsSmallest()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 5, Price = 10 },
        ]);

        int result = db.Table<Book>().Select(b => SQLiteFunctions.Min(b.AuthorId, 3, 7)).First();

        Assert.Equal(3, result);
    }

    [Fact]
    public void SQLiteFunctions_MaxThreeArgs_ReturnsLargest()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 5, Price = 10 },
        ]);

        int result = db.Table<Book>().Select(b => SQLiteFunctions.Max(b.AuthorId, 3, 7)).First();

        Assert.Equal(7, result);
    }

    [Fact]
    public void Where_MultipleWhereCalls_ChainAsAnd()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 5 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 15 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 20 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => b.Price > 10)
            .Where(b => b.AuthorId == 1)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([2], ids);
    }

    [Fact]
    public void Select_DirectThenLength_ProducesLengthSql()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Apple", AuthorId = 1, Price = 1 },
        ]);

        int len = db.Table<Book>().Select(b => b.Title.Length).First();

        Assert.Equal(5, len);
    }

    [Fact]
    public void Select_Scalar_ThenWhereByValue_FiltersCorrectly()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Apple", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "Banana", AuthorId = 1, Price = 2 },
        ]);

        List<string> result = db.Table<Book>()
            .Select(b => b.Title)
            .Where(t => t == "Apple")
            .ToList();

        Assert.Equal(["Apple"], result);
    }

    [Fact]
    public void Select_ScalarString_ThenSelectLength_AppliesLengthSql()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Apple", AuthorId = 1, Price = 1 },
        ]);

        int len = db.Table<Book>()
            .Select(b => b.Title)
            .Select(t => t.Length)
            .First();

        Assert.Equal(5, len);
    }

    [Fact]
    public void Select_ScalarString_ThenSelectMethodCall_TranslatesMethod()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Apple", AuthorId = 1, Price = 1 },
        ]);

        string upper = db.Table<Book>()
            .Select(b => b.Title)
            .Select(t => t.ToUpper())
            .First();

        Assert.Equal("APPLE", upper);
    }

    [Fact]
    public void Select_Scalar_ThenWhereByMember_FiltersByDerivedValue()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Apple", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "Hi", AuthorId = 1, Price = 2 },
        ]);

        List<string> result = db.Table<Book>()
            .Select(b => b.Title)
            .Where(t => t.Length > 3)
            .ToList();

        Assert.Equal(["Apple"], result);
    }

    [Fact]
    public void Select_Scalar_ThenSum_AggregatesProjectedColumn()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 20 },
        ]);

        double total = db.Table<Book>().Select(b => b.Price).Sum();

        Assert.Equal(30.0, total);
    }

    [Fact]
    public void Select_Scalar_ThenWhereStartsWith_FiltersByDerivedString()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Apple", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "Banana", AuthorId = 1, Price = 2 },
        ]);

        List<string> result = db.Table<Book>()
            .Select(b => b.Title)
            .Where(t => t.StartsWith("A"))
            .ToList();

        Assert.Equal(["Apple"], result);
    }

    [Fact]
    public void Select_ThreeLevelChained_ResolvesEachStep()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Apple", AuthorId = 1, Price = 1 },
        ]);

        int len = db.Table<Book>()
            .Select(b => b.Title)
            .Select(t => t.ToUpper())
            .Select(u => u.Length)
            .First();

        Assert.Equal(5, len);
    }

    [Fact]
    public void ProbeSubquery_AnyContainsResult()
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
            .Where(b => db.Table<Author>().Any(a => a.Id == b.AuthorId && a.Name.StartsWith("M")))
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void Where_MultipleArgStringMethod()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Hello World", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "Hi", AuthorId = 1, Price = 2 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => b.Title.Substring(0, 5) == "Hello")
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void GroupBy_StringKey_WithStringMethod()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Apple", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "Avocado", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "Banana", AuthorId = 1, Price = 3 },
        ]);

        var rows = db.Table<Book>()
            .GroupBy(b => b.Title.Substring(0, 1))
            .OrderBy(g => g.Key)
            .Select(g => new { Letter = g.Key, Count = g.Count() })
            .ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal("A", rows[0].Letter);
        Assert.Equal(2, rows[0].Count);
        Assert.Equal("B", rows[1].Letter);
        Assert.Equal(1, rows[1].Count);
    }

    [Fact]
    public void Distinct_ThenOrderBy_OrdersDistinctValues()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "B", AuthorId = 2, Price = 1 },
            new Book { Id = 2, Title = "A", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "B", AuthorId = 2, Price = 3 },
            new Book { Id = 4, Title = "A", AuthorId = 1, Price = 4 },
        ]);

        List<int> result = db.Table<Book>()
            .Select(b => b.AuthorId)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        Assert.Equal([1, 2], result);
    }

    [Fact]
    public void Where_LocalArrayContains_TranslatesToIn()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 3 },
        ]);

        int[] wanted = [1, 3];

        List<int> ids = db.Table<Book>()
            .Where(b => wanted.Contains(b.Id))
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1, 3], ids);
    }

    [Fact]
    public void Where_ContainsOnEmptyArray_ReturnsZeroRows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
        ]);

        int[] empty = [];

        int count = db.Table<Book>().Count(b => empty.Contains(b.Id));

        Assert.Equal(0, count);
    }

    [Fact]
    public void Where_NegatedContains_TranslatesToNotIn()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 3 },
        ]);

        int[] excluded = [2];

        List<int> ids = db.Table<Book>()
            .Where(b => !excluded.Contains(b.Id))
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1, 3], ids);
    }

    [Fact]
    public void Select_StringConcatWithIntColumn_ConcatenatesAsText()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
        ]);

        string result = db.Table<Book>().Select(b => b.Title + " " + b.Id).First();

        Assert.Equal("A 1", result);
    }

    [Fact]
    public void Where_SelfEquality_AlwaysTrue()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        int count = db.Table<Book>().Count(b => b.Id == b.Id);

        Assert.Equal(2, count);
    }

    [Fact]
    public void Where_TwoColumnComparison_FiltersByPair()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 2, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 5, Price = 3 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => b.Id == b.AuthorId)
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1, 2], ids);
    }

    [Fact]
    public void Select_NullCoalescingChain_CascadesValues()
    {
        using TestDatabase db = new();
        db.Table<NullableStringEntity>().Schema.CreateTable();

        db.Table<NullableStringEntity>().AddRange([
            new NullableStringEntity { Id = 1, Name = "A" },
            new NullableStringEntity { Id = 2, Name = null },
        ]);

        List<string> names = db.Table<NullableStringEntity>()
            .OrderBy(e => e.Id)
            .Select(e => e.Name ?? "missing")
            .ToList();

        Assert.Equal(["A", "missing"], names);
    }

    [Fact]
    public void Where_MultipleSubqueries_InOneFilter()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Author>().AddRange([
            new Author { Id = 1, Name = "A1", Email = "a@x", BirthDate = new DateTime(2000, 1, 1) },
            new Author { Id = 2, Name = "A2", Email = "b@x", BirthDate = new DateTime(2000, 1, 1) },
        ]);
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "T1", AuthorId = 1, Price = 5 },
            new Book { Id = 2, Title = "T2", AuthorId = 1, Price = 50 },
            new Book { Id = 3, Title = "T3", AuthorId = 2, Price = 7 },
        ]);

        List<int> authorIds = db.Table<Author>()
            .Where(a =>
                db.Table<Book>().Any(b => b.AuthorId == a.Id && b.Price < 10) &&
                db.Table<Book>().Any(b => b.AuthorId == a.Id && b.Price > 20))
            .OrderBy(a => a.Id)
            .Select(a => a.Id)
            .ToList();

        Assert.Equal([1], authorIds);
    }

    [Fact]
    public void Where_FilteredEnumerableContains_ThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
        ]);

        int[] inMemory = [1, 2, 3];

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Where(b => inMemory.Where(x => x > 0).Contains(b.Id)).Count());
    }

    [Fact]
    public void Where_NullOnLeftSideOfEquality()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();

        db.Table<NullableEntity>().AddRange([
            new NullableEntity { Id = 1, Value = 10 },
            new NullableEntity { Id = 2, Value = null },
        ]);

        List<int> ids = db.Table<NullableEntity>()
            .Where(e => null == e.Value)
            .Select(e => e.Id)
            .ToList();

        Assert.Equal([2], ids);
    }

    [Fact]
    public void GroupBy_OrderByAggregateInProjection()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 5 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 100 },
        ]);

        var rows = db.Table<Book>()
            .GroupBy(b => b.AuthorId)
            .Select(g => new { Id = g.Key, Total = g.Sum(b => b.Price) })
            .OrderByDescending(r => r.Total)
            .ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal(2, rows[0].Id);
        Assert.Equal(100.0, rows[0].Total);
    }

    [Fact]
    public void OrderBy_ComputedExpression_OnMultipleColumns()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Apple", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "Hi", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "Mango", AuthorId = 1, Price = 3 },
        ]);

        List<int> ids = db.Table<Book>()
            .OrderBy(b => b.Title.Length + b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([2, 1, 3], ids);
    }

    [Fact]
    public void Where_AfterOrderBy_FiltersAndKeepsOrder()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "C", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "A", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "B", AuthorId = 1, Price = 3 },
        ]);

        List<string> titles = db.Table<Book>()
            .OrderBy(b => b.Title)
            .Where(b => b.Id != 2)
            .Select(b => b.Title)
            .ToList();

        Assert.Equal(["B", "C"], titles);
    }

    [Fact]
    public void Where_ChainedAndOr_RespectsPrecedence()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 5 },
            new Book { Id = 2, Title = "B", AuthorId = 2, Price = 10 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 100 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => b.AuthorId == 1 && (b.Price < 10 || b.Price > 50))
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1, 3], ids);
    }

    [Fact]
    public void SelfJoin_ViaSelectMany_OnDifferentRows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 3 },
        ]);

        var pairs = (
            from b1 in db.Table<Book>()
            from b2 in db.Table<Book>()
            where b1.AuthorId == b2.AuthorId && b1.Id < b2.Id
            select new { L = b1.Id, R = b2.Id }
        ).OrderBy(p => p.L).ToList();

        Assert.Single(pairs);
        Assert.Equal(1, pairs[0].L);
        Assert.Equal(2, pairs[0].R);
    }

    [Fact]
    public void Aggregate_ThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
        ]);

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Select(b => b.Id).Aggregate((a, b) => a + b));
    }

    [Fact]
    public void First_WithPredicate_AfterWhere_AppliesBoth()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 5 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 15 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 25 },
        ]);

        Book result = db.Table<Book>().Where(b => b.Price > 10).First(b => b.Id > 2);

        Assert.Equal(3, result.Id);
    }

    [Fact]
    public void LongCount_ReturnsLong()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        long n = db.Table<Book>().LongCount();

        Assert.Equal(2L, n);
    }

    [Fact]
    public void Where_AnonymousTypeEquality_ThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
        ]);

        var target = new { AuthorId = 1, Price = 1.0 };

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Count(b => new { b.AuthorId, b.Price } == target));
    }

    [Fact]
    public void Where_ScalarSubqueryWithMemberAccess()
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
                .Where(a => a.Id == b.AuthorId)
                .Select(a => a.Name)
                .First()
                .StartsWith("M"))
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void Where_AnyAgainstMissingForeignKey_ReturnsEmpty()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "T", AuthorId = 99, Price = 1 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => db.Table<Author>().Any(a => a.Id == b.AuthorId))
            .Select(b => b.Id)
            .ToList();

        Assert.Empty(ids);
    }

    [Fact]
    public void ExecuteUpdate_ChainedSetCalls_AllApply()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
        ]);

        db.Table<Book>().ExecuteUpdate(s => s
            .Set(b => b.Title, "X")
            .Set(b => b.AuthorId, 99)
            .Set(b => b.Price, 100));

        Book b = db.Table<Book>().First();

        Assert.Equal("X", b.Title);
        Assert.Equal(99, b.AuthorId);
        Assert.Equal(100.0, b.Price);
    }

    [Fact]
    public void GroupBy_OverEmptySource_ReturnsNoGroups()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        var rows = db.Table<Book>()
            .GroupBy(b => b.AuthorId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToList();

        Assert.Empty(rows);
    }

    [Fact]
    public void Where_MethodOnCoalescedString_TranslatesAfterCoalesce()
    {
        using TestDatabase db = new();
        db.Table<NullableStringEntity>().Schema.CreateTable();

        db.Table<NullableStringEntity>().AddRange([
            new NullableStringEntity { Id = 1, Name = "Apple" },
            new NullableStringEntity { Id = 2, Name = null },
            new NullableStringEntity { Id = 3, Name = "Avocado" },
        ]);

        List<int> ids = db.Table<NullableStringEntity>()
            .Where(e => (e.Name ?? "").StartsWith("A"))
            .OrderBy(e => e.Id)
            .Select(e => e.Id)
            .ToList();

        Assert.Equal([1, 3], ids);
    }

    [Fact]
    public void Select_MathRoundOnColumn_RoundsToInt()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1.6 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2.4 },
        ]);

        List<double> rounded = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Select(b => Math.Round(b.Price))
            .ToList();

        Assert.Equal([2.0, 2.0], rounded);
    }

    [Fact]
    public void Where_DivisionWithRemainder()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 2, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 3, Price = 3 },
            new Book { Id = 4, Title = "D", AuthorId = 4, Price = 4 },
        ]);

        List<int> evenIds = db.Table<Book>()
            .Where(b => b.Id % 2 == 0)
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([2, 4], evenIds);
    }

    [Fact]
    public void Select_StringInterpolatedExpressionFallsBack_OnUnsupportedConcat()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Hello", AuthorId = 5, Price = 1 },
        ]);

        List<string> formatted = db.Table<Book>()
            .Select(b => string.Concat(b.Title, "-", b.AuthorId.ToString()))
            .ToList();

        Assert.Single(formatted);
        Assert.Contains("Hello", formatted[0]);
    }

    [Fact]
    public void Where_ChainedStringMethods_TranslateSequentially()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "  Hello World  ", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "Hi", AuthorId = 1, Price = 2 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => b.Title.Trim().ToLower().StartsWith("hello"))
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void Select_StringReplaceInProjection_TranslatesToReplace()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Hello", AuthorId = 1, Price = 1 },
        ]);

        string result = db.Table<Book>().Select(b => b.Title.Replace("l", "L")).Single();

        Assert.Equal("HeLLo", result);
    }

    [Fact]
    public void OrderBy_ConditionalKey_TranslatesAsCase()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "C", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "A", AuthorId = 2, Price = 2 },
            new Book { Id = 3, Title = "B", AuthorId = 1, Price = 3 },
        ]);

        List<int> ids = db.Table<Book>()
            .OrderBy(b => b.AuthorId == 1 ? 0 : 1)
            .ThenBy(b => b.Title)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([3, 1, 2], ids);
    }

    [Fact]
    public void Where_NegatedStringContains_FiltersByAbsence()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Apple", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "Hi", AuthorId = 1, Price = 2 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => !b.Title.Contains("a"))
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([2], ids);
    }

    [Fact]
    public void Distinct_AfterDistinct_StaysDistinct()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "A", AuthorId = 1, Price = 2 },
        ]);

        List<int> ids = db.Table<Book>()
            .Select(b => b.AuthorId)
            .Distinct()
            .Distinct()
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void Reverse_AfterOrderBy_FlipsOrder()
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
    public void Where_ContainsOnSubquerySelect_TranslatesToInSubquery()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Author>().AddRange([
            new Author { Id = 1, Name = "Match", Email = "m@x", BirthDate = new DateTime(2000, 1, 1) },
        ]);
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "T1", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "T2", AuthorId = 99, Price = 2 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => db.Table<Author>().Select(a => a.Id).Contains(b.AuthorId))
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void Select_NullableArithmetic_BothNullable_AddsNonNull()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();

        db.Table<NullableEntity>().AddRange([
            new NullableEntity { Id = 1, Value = 10 },
        ]);

        int? sum = db.Table<NullableEntity>().Select(e => e.Value + e.Value).First();

        Assert.Equal(20, sum);
    }

    [Fact]
    public void OrderBy_DateTimeYear_OrdersByExtractedPart()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();

        db.Table<Author>().AddRange([
            new Author { Id = 1, Name = "A", Email = "a@x", BirthDate = new DateTime(1990, 1, 1) },
            new Author { Id = 2, Name = "B", Email = "b@x", BirthDate = new DateTime(1985, 6, 15) },
            new Author { Id = 3, Name = "C", Email = "c@x", BirthDate = new DateTime(2000, 3, 10) },
        ]);

        List<int> ids = db.Table<Author>()
            .OrderBy(a => a.BirthDate.Year)
            .Select(a => a.Id)
            .ToList();

        Assert.Equal([2, 1, 3], ids);
    }

    [Fact]
    public void Where_NullableHasValue_FiltersNonNull()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();

        db.Table<NullableEntity>().AddRange([
            new NullableEntity { Id = 1, Value = 10 },
            new NullableEntity { Id = 2, Value = null },
            new NullableEntity { Id = 3, Value = 20 },
        ]);

        List<int> ids = db.Table<NullableEntity>()
            .Where(e => e.Value.HasValue)
            .OrderBy(e => e.Id)
            .Select(e => e.Id)
            .ToList();

        Assert.Equal([1, 3], ids);
    }

    [Fact]
    public void Select_MathAbsOnNullable_HandlesNull()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();

        db.Table<NullableEntity>().AddRange([
            new NullableEntity { Id = 1, Value = -10 },
            new NullableEntity { Id = 2, Value = null },
        ]);

        List<int?> abs = db.Table<NullableEntity>()
            .OrderBy(e => e.Id)
            .Select(e => Math.Abs(e.Value ?? 0))
            .Cast<int?>()
            .ToList();

        Assert.Equal([10, 0], abs);
    }

    [Fact]
    public void GroupBy_DistinctSelectInsideGroup_ThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
        ]);

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .GroupBy(b => b.AuthorId)
                .Select(g => new { Id = g.Key, Distinct = g.Select(b => b.Title).Distinct().Count() })
                .ToList());
    }

    [Fact]
    public void OrderBy_NullableCoalesced_PutsNullsLast()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();

        db.Table<NullableEntity>().AddRange([
            new NullableEntity { Id = 1, Value = 10 },
            new NullableEntity { Id = 2, Value = null },
            new NullableEntity { Id = 3, Value = 5 },
        ]);

        List<int> ids = db.Table<NullableEntity>()
            .OrderBy(e => e.Value ?? int.MaxValue)
            .Select(e => e.Id)
            .ToList();

        Assert.Equal([3, 1, 2], ids);
    }

    [Fact]
    public void Concat_ThenIntersect_ChainsAsSetOperations()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        ]);

        IQueryable<int> a = db.Table<Book>().Where(b => b.Id == 1).Select(b => b.Id);
        IQueryable<int> b = db.Table<Book>().Where(b => b.Id == 2).Select(b => b.Id);
        IQueryable<int> c = db.Table<Book>().Select(b => b.Id);

        List<int> result = a.Concat(b).Intersect(c).OrderBy(x => x).ToList();

        Assert.Equal([1, 2], result);
    }

    [Fact]
    public void Where_RangeCheck_TranslatesAsAnd()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 5 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 15 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 25 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => b.Price >= 10 && b.Price <= 20)
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([2], ids);
    }

    [Fact]
    public void Select_FirstThenMember_OnEntity_ThrowsClearError()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Author>().AddRange([
            new Author { Id = 1, Name = "Match", Email = "m@x", BirthDate = new DateTime(2000, 1, 1) },
        ]);
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "T1", AuthorId = 1, Price = 1 },
        ]);

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Select(b => db.Table<Author>().First(a => a.Id == b.AuthorId).Name)
                .ToList());

        Assert.Contains("entity-typed scalar subquery", ex.Message);
        Assert.Contains("Select(x => x.Name)", ex.Message);
    }

    [Fact]
    public void Where_FirstThenMember_OnEntity_ThrowsClearError()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Author>().AddRange([
            new Author { Id = 1, Name = "Match", Email = "m@x", BirthDate = new DateTime(2000, 1, 1) },
        ]);
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "T1", AuthorId = 1, Price = 1 },
        ]);

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Where(b => db.Table<Author>().First(a => a.Id == b.AuthorId).Name.StartsWith("M"))
                .ToList());
    }

    [Fact]
    public void Where_StringEqualsWithIgnoreCase_TranslatesAsCollateNoCase()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Apple", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "BANANA", AuthorId = 1, Price = 2 },
        ]);

        int matchedApple = db.Table<Book>().Count(b => b.Title.Equals("apple", StringComparison.OrdinalIgnoreCase));
        int matchedBanana = db.Table<Book>().Count(b => b.Title.Equals("banana", StringComparison.OrdinalIgnoreCase));

        Assert.Equal(1, matchedApple);
        Assert.Equal(1, matchedBanana);
    }

    [Fact]
    public void Where_StringEqualsWithOrdinal_StaysCaseSensitive()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Apple", AuthorId = 1, Price = 1 },
        ]);

        int caseSensitiveMatch = db.Table<Book>().Count(b => b.Title.Equals("apple", StringComparison.Ordinal));

        Assert.Equal(0, caseSensitiveMatch);
    }

    [Fact]
    public void Where_StringEquals_NoComparison_StillMatchesCaseSensitively()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Apple", AuthorId = 1, Price = 1 },
        ]);

        int match = db.Table<Book>().Count(b => b.Title.Equals("Apple"));

        Assert.Equal(1, match);
    }

    [Fact]
    public void Where_EmptyStringVsNull_ArePreservedSeparately()
    {
        using TestDatabase db = new();
        db.Table<NullableStringEntity>().Schema.CreateTable();

        db.Table<NullableStringEntity>().AddRange([
            new NullableStringEntity { Id = 1, Name = "" },
            new NullableStringEntity { Id = 2, Name = null },
            new NullableStringEntity { Id = 3, Name = "x" },
        ]);

        int emptyCount = db.Table<NullableStringEntity>().Count(e => e.Name == "");
        int nullCount = db.Table<NullableStringEntity>().Count(e => e.Name == null);
        int isNullOrEmptyCount = db.Table<NullableStringEntity>().Count(e => string.IsNullOrEmpty(e.Name));

        Assert.Equal(1, emptyCount);
        Assert.Equal(1, nullCount);
        Assert.Equal(2, isNullOrEmptyCount);
    }

    [Fact]
    public void Where_IntEqualsWithBoxedObject_TranslatesAsEquality()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 5, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 7, Price = 2 },
        ]);

        object target = 5;

        int count = db.Table<Book>().Count(b => b.AuthorId.Equals(target));

        Assert.Equal(1, count);
    }

    [Fact]
    public void Where_NullableBoolColumn_FollowsSqlNullSemantics()
    {
        using TestDatabase db = new();
        db.Table<NullableBoolEntity>().Schema.CreateTable();

        db.Table<NullableBoolEntity>().AddRange([
            new NullableBoolEntity { Id = 1, Flag = true },
            new NullableBoolEntity { Id = 2, Flag = false },
            new NullableBoolEntity { Id = 3, Flag = null },
        ]);

        int countTrue = db.Table<NullableBoolEntity>().Count(e => e.Flag == true);
        int countCoalesced = db.Table<NullableBoolEntity>().Count(e => e.Flag ?? false);

        Assert.Equal(1, countTrue);
        Assert.Equal(1, countCoalesced);
    }

    [Fact]
    public void Where_DateTimeSubtraction_ReturnsTimeSpan()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();

        db.Table<Author>().AddRange([
            new Author { Id = 1, Name = "A", Email = "a@x", BirthDate = new DateTime(2000, 1, 1) },
        ]);

        DateTime now = new(2010, 1, 1);

        List<int> ids = db.Table<Author>()
            .Where(a => (now - a.BirthDate).TotalDays > 365)
            .Select(a => a.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void Max_OnDateTimeColumn_ReturnsLatest()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();

        db.Table<Author>().AddRange([
            new Author { Id = 1, Name = "A", Email = "a@x", BirthDate = new DateTime(2000, 1, 1) },
            new Author { Id = 2, Name = "B", Email = "b@x", BirthDate = new DateTime(1990, 6, 15) },
            new Author { Id = 3, Name = "C", Email = "c@x", BirthDate = new DateTime(2010, 3, 10) },
        ]);

        DateTime maxBirth = db.Table<Author>().Max(a => a.BirthDate);

        Assert.Equal(new DateTime(2010, 3, 10), maxBirth);
    }

    [Fact]
    public void GroupBy_DateTimeYear_GroupsByExtractedYear()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();

        db.Table<Author>().AddRange([
            new Author { Id = 1, Name = "A", Email = "a@x", BirthDate = new DateTime(2000, 1, 1) },
            new Author { Id = 2, Name = "B", Email = "b@x", BirthDate = new DateTime(2000, 6, 15) },
            new Author { Id = 3, Name = "C", Email = "c@x", BirthDate = new DateTime(2010, 3, 10) },
        ]);

        var rows = db.Table<Author>()
            .GroupBy(a => a.BirthDate.Year)
            .OrderBy(g => g.Key)
            .Select(g => new { Year = g.Key, Count = g.Count() })
            .ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal(2000, rows[0].Year);
        Assert.Equal(2, rows[0].Count);
        Assert.Equal(2010, rows[1].Year);
        Assert.Equal(1, rows[1].Count);
    }

    [Fact]
    public void ExecuteUpdate_ColumnReferencingItselfTwice()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 10 },
        ]);

        db.Table<Book>().ExecuteUpdate(s => s.Set(b => b.Price, b => b.Price + b.Price * 0.1));

        double price = db.Table<Book>().Select(b => b.Price).First();

        Assert.Equal(11.0, price);
    }

    [Fact]
    public void Where_RepeatsComputedExpressionInBoundsCheck()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 50 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 100 },
        ]);

        List<int> ids = db.Table<Book>()
            .Where(b => b.Price * 2 > 50 && b.Price * 2 < 150)
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([2], ids);
    }

    [Fact]
    public void Where_BoolColumnDirectly_TruthyFilter()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        db.Table<SoftDeletableBook>().AddRange([
            new SoftDeletableBook { Id = 1, Title = "A", IsDeleted = true },
            new SoftDeletableBook { Id = 2, Title = "B", IsDeleted = false },
            new SoftDeletableBook { Id = 3, Title = "C", IsDeleted = true },
        ]);

        List<int> ids = db.Table<SoftDeletableBook>()
            .Where(b => b.IsDeleted)
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([1, 3], ids);
    }

    [Fact]
    public void Where_NegatedBoolColumn_FalsyFilter()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        db.Table<SoftDeletableBook>().AddRange([
            new SoftDeletableBook { Id = 1, Title = "A", IsDeleted = true },
            new SoftDeletableBook { Id = 2, Title = "B", IsDeleted = false },
        ]);

        List<int> ids = db.Table<SoftDeletableBook>()
            .Where(b => !b.IsDeleted)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([2], ids);
    }

    [Fact]
    public void Select_BoolColumnInTernary_TranslatesAsCase()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        db.Table<SoftDeletableBook>().AddRange([
            new SoftDeletableBook { Id = 1, Title = "A", IsDeleted = true },
            new SoftDeletableBook { Id = 2, Title = "B", IsDeleted = false },
        ]);

        List<string> labels = db.Table<SoftDeletableBook>()
            .OrderBy(b => b.Id)
            .Select(b => b.IsDeleted ? "deleted" : "active")
            .ToList();

        Assert.Equal(["deleted", "active"], labels);
    }

    [Fact]
    public void Where_ChainedBoolMethodAndComparison()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Apple", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "Banana", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "Hi", AuthorId = 1, Price = 3 },
        ]);

        int count = db.Table<Book>().Count(b => b.Title.Contains("a") && b.Title.Length > 4);

        Assert.Equal(2, count);
    }

    [Fact]
    public void Min_OnNullableColumn_SkipsNulls()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();

        db.Table<NullableEntity>().AddRange([
            new NullableEntity { Id = 1, Value = 10 },
            new NullableEntity { Id = 2, Value = null },
            new NullableEntity { Id = 3, Value = 5 },
        ]);

        int? minVal = db.Table<NullableEntity>().Min(e => e.Value);

        Assert.Equal(5, minVal);
    }

    [Fact]
    public void Where_NullableLocalVarWithValue_FiltersByValue()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 2, Price = 2 },
        ]);

        int? wantedAuthorId = 2;

        List<int> ids = db.Table<Book>()
            .Where(b => b.AuthorId == wantedAuthorId)
            .Select(b => b.Id)
            .ToList();

        Assert.Equal([2], ids);
    }

    [Fact]
    public void Where_NullableLocalVarSetToNull_TranslatesToIsNull()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();

        db.Table<NullableEntity>().AddRange([
            new NullableEntity { Id = 1, Value = 10 },
            new NullableEntity { Id = 2, Value = null },
        ]);

        int? wanted = null;

        List<int> ids = db.Table<NullableEntity>()
            .Where(e => e.Value == wanted)
            .OrderBy(e => e.Id)
            .Select(e => e.Id)
            .ToList();

        Assert.Equal([2], ids);
    }

    [Fact]
    public void Select_ConvertToInt32_OnDoubleColumn()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 5.7 },
        ]);

        int result = db.Table<Book>().Select(b => Convert.ToInt32(b.Price)).First();

        Assert.Equal(6, result);
    }

    [Fact]
    public void Select_ConvertToStringOnIntColumn_StringifiesNumber()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 7, Title = "A", AuthorId = 1, Price = 1 },
        ]);

        string result = db.Table<Book>().Select(b => Convert.ToString(b.Id)).First();

        Assert.Equal("7", result);
    }

    [Fact]
    public void Where_IntParseOnStringColumn_TranslatesAsCast()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "5", AuthorId = 1, Price = 1 },
        ]);

        int count = db.Table<Book>().Count(b => int.Parse(b.Title) > 3);

        Assert.Equal(1, count);
    }

    [Fact]
    public void AddRange_OneThousandRows_AllInserted()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();

        List<NullableEntity> rows = Enumerable.Range(1, 1000)
            .Select(i => new NullableEntity { Id = i, Value = i * 2 })
            .ToList();

        db.Table<NullableEntity>().AddRange(rows);

        int count = db.Table<NullableEntity>().Count();
        int sum = db.Table<NullableEntity>().Sum(e => e.Value ?? 0);

        Assert.Equal(1000, count);
        Assert.Equal(1001000, sum);
    }

    [Fact]
    public void UpdateRange_OneHundredRows_AllUpdated()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();

        db.Table<NullableEntity>().AddRange(
            Enumerable.Range(1, 100).Select(i => new NullableEntity { Id = i, Value = i }).ToList()
        );

        List<NullableEntity> updated = db.Table<NullableEntity>()
            .Select(e => new NullableEntity { Id = e.Id, Value = (e.Value ?? 0) + 1000 })
            .ToList();

        db.Table<NullableEntity>().UpdateRange(updated);

        int updatedSum = db.Table<NullableEntity>().Sum(e => e.Value ?? 0);

        Assert.Equal(105050, updatedSum);
    }

    [Fact]
    public void ExecuteDelete_WithWhere_RemovesMatchingRows()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();

        db.Table<NullableEntity>().AddRange([
            new NullableEntity { Id = 1, Value = 10 },
            new NullableEntity { Id = 2, Value = 20 },
            new NullableEntity { Id = 3, Value = 30 },
        ]);

        int deleted = db.Table<NullableEntity>().Where(e => e.Id != 2).ExecuteDelete();
        int remaining = db.Table<NullableEntity>().Count();

        Assert.Equal(2, deleted);
        Assert.Equal(1, remaining);
    }

    [Fact]
    public void Decimal_NinePlaces_RoundTripsExactly()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();

        db.Table<NumericType>().AddRange([
            new NumericType
            {
                Id = 1,
                DecimalValue = 10.123456789m,
                ByteValue = 0,
                SByteValue = 0,
                ShortValue = 0,
                UShortValue = 0,
                IntValue = 0,
                UIntValue = 0,
                LongValue = 0,
                ULongValue = 0,
                FloatValue = 0,
                DoubleValue = 0,
                CharValue = 'a',
            },
        ]);

        decimal val = db.Table<NumericType>().Select(n => n.DecimalValue).First();

        Assert.Equal(10.123456789m, val);
    }

    [Fact]
    public void NumericType_LongMaxValue_RoundTrips()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();

        db.Table<NumericType>().AddRange([
            new NumericType
            {
                Id = 1,
                LongValue = long.MaxValue,
                IntValue = 0,
                ShortValue = 0,
                ByteValue = 0,
                SByteValue = 0,
                UIntValue = 0,
                ULongValue = 0,
                UShortValue = 0,
                DoubleValue = 0,
                FloatValue = 0,
                DecimalValue = 0,
                CharValue = 'a',
            },
        ]);

        long result = db.Table<NumericType>().Select(n => n.LongValue).First();

        Assert.Equal(long.MaxValue, result);
    }

    [Fact]
    public void Schema_TableExists_ReturnsTrueAfterCreate()
    {
        using TestDatabase db = new();

        bool before = db.Schema.TableExists("Books");
        db.Table<Book>().Schema.CreateTable();
        bool after = db.Schema.TableExists("Books");

        Assert.False(before);
        Assert.True(after);
    }

    [Fact]
    public void Where_StringLength_OnColumn()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Long Title", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "Hi", AuthorId = 1, Price = 2 },
        ]);

        List<int> ids = db.Table<Book>().Where(b => b.Title.Length >= 5).Select(b => b.Id).ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void GroupBy_MinMaxInsideAggregate()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 3 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 1 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 5 },
        ]);

        var row = db.Table<Book>()
            .GroupBy(b => b.AuthorId)
            .Select(g => new
            {
                Min = g.Min(b => b.Price),
                Max = g.Max(b => b.Price),
            })
            .Single();

        Assert.Equal(1.0, row.Min);
        Assert.Equal(5.0, row.Max);
    }
}
