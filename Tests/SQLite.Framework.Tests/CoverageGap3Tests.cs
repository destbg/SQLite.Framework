using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CoverageGap3Tests
{
    [Fact]
    public void StringJoin_WithEmptyArrayLiteralAndColumnSeparator_ReturnsEmptyString()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        string result = db.Table<Book>()
            .Select(b => string.Join(b.Title, new string[] { }))
            .First();

        Assert.Equal("", result);
    }

    [Fact]
    public void SQLiteFunctions_In_WithEmptyArrayLiteral_NeverMatches()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 1, Price = 2 });

        List<int> ids = db.Table<Book>()
            .Where(b => SQLiteFunctions.In(b.Id, new int[] { }))
            .Select(b => b.Id)
            .ToList();

        Assert.Empty(ids);
    }

    [Fact]
    public void Where_TwiceChainedBeforeAll_EmitsNotExistsWithAndJoinedConditions()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 5 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 1, Price = 6 });

        bool all = db.Table<Book>()
            .Where(b => b.AuthorId == 1)
            .Where(b => b.Price > 0)
            .All(b => b.Price < 10);

        Assert.True(all);
    }

    [Fact]
    public void GroupBy_WithMultipleHavingClauses_JoinsThemWithAnd()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 1, Price = 2 });
        db.Table<Book>().Add(new Book { Id = 3, Title = "c", AuthorId = 2, Price = 3 });

        List<int> authorIds = db.Table<Book>()
            .GroupBy(b => b.AuthorId)
            .Where(g => g.Count() > 1)
            .Where(g => g.Sum(b => b.Price) > 0)
            .Select(g => g.Key)
            .ToList();

        Assert.Equal([1], authorIds);
    }

    [Fact]
    public void EnumToString_OnParameterizedExpression_AppendsConstantParametersAfterObjectParameters()
    {
        using TestDatabase db = new();
        db.Table<Publisher>().Schema.CreateTable();
        db.Table<Publisher>().Add(new Publisher { Id = 1, Name = "X", Type = PublisherType.Book });

        PublisherType captured = PublisherType.Magazine;
        string result = db.Table<Publisher>()
            .Select(p => (p.Type | captured).ToString())
            .First();

        Assert.Equal("Newspaper", result);
    }

    [Fact]
    public void EnumParse_StringArgumentIsParameterized_AppendsConstantParametersAfterStringArgParameters()
    {
        using TestDatabase db = new();
        db.Table<Publisher>().Schema.CreateTable();
        db.Table<Publisher>().Add(new Publisher { Id = 1, Name = "Book", Type = PublisherType.Book });

        string suffix = "";
        List<Publisher> rows = db.Table<Publisher>()
            .Where(p => p.Type == Enum.Parse<PublisherType>(p.Name + suffix))
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void Query_With256Parameters_AssignsAllUniqueNames()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        for (int i = 0; i < 300; i++)
        {
            db.Table<Book>().Add(new Book { Id = i + 1, Title = "t" + i, AuthorId = 1, Price = i });
        }

        int[] wanted = Enumerable.Range(1, 260).ToArray();
        int matched = db.Table<Book>()
            .Where(b => SQLiteFunctions.In(b.Id, wanted))
            .Count();

        Assert.Equal(260, matched);
    }

#if !SQLITE_FRAMEWORK_SOURCE_GENERATOR
    [Fact]
    public void ExecuteQuery_NestedClassPropertyOfNestedClassProperty_RecursesIntoBuildInternal()
    {
        using TestDatabase db = new();

        CoverageNestedOuter result = db.CreateCommand(
                "SELECT 1 AS Id, 'tag' AS \"Mid.Tag\", 42 AS \"Mid.Deep.X\"",
                [])
            .ExecuteQuery<CoverageNestedOuter>()
            .First();

        Assert.Equal(1, result.Id);
        Assert.Equal("tag", result.Mid.Tag);
        Assert.Equal(42, result.Mid.Deep.X);
    }

    [Fact]
    public void ExecuteQuery_NestedStructProperty_BuildsStructViaReflectionMaterializer()
    {
        using TestDatabase db = new();

        CoverageStructHolder result = db.CreateCommand(
                "SELECT 1 AS Id, 7 AS \"Inner.Value\"",
                [])
            .ExecuteQuery<CoverageStructHolder>()
            .First();

        Assert.Equal(1, result.Id);
        Assert.Equal(7, result.Inner.Value);
    }
#endif

    public class CoverageNestedOuter
    {
        public int Id { get; set; }
        public CoverageNestedMid Mid { get; set; } = new();
    }

    public class CoverageNestedMid
    {
        public string Tag { get; set; } = "";
        public CoverageNestedDeep Deep { get; set; } = new();
    }

    public class CoverageNestedDeep
    {
        public int X { get; set; }
    }

    public class CoverageStructHolder
    {
        public int Id { get; set; }
        public CoverageNestedStruct Inner { get; set; }
    }

    public struct CoverageNestedStruct
    {
        public int Value { get; set; }
    }
}
