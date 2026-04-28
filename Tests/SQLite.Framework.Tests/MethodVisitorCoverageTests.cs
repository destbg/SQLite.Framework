using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MethodVisitorCoverageTests
{
    [Fact]
    public void StringEquals_WithOrdinalComparison_ProducesExactMatchSql()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<Book>()
            .Where(b => string.Equals(b.Title, "Test", StringComparison.Ordinal))
            .ToSqlCommand();

        Assert.Contains("= @p0", command.CommandText);
        Assert.DoesNotContain("COLLATE NOCASE", command.CommandText);
    }

    [Fact]
    public void StringTrim_WithSingleCharArg_TrimsCharFromBothEnds()
    {
        using TestDatabase db = new();

        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "xTestx",
            AuthorId = 1,
            Price = 10
        });

        List<Book> results = db.Table<Book>()
            .Where(b => b.Title.Trim('x') == "Test")
            .ToList();

        Assert.Single(results);
    }

    [Fact]
    public void StringTrimStart_WithSingleCharArg_TrimsCharFromStart()
    {
        using TestDatabase db = new();

        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "xTest",
            AuthorId = 1,
            Price = 10
        });

        List<Book> results = db.Table<Book>()
            .Where(b => b.Title.TrimStart('x') == "Test")
            .ToList();

        Assert.Single(results);
    }

    [Fact]
    public void StringTrimEnd_WithSingleCharArg_TrimsCharFromEnd()
    {
        using TestDatabase db = new();

        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "Testx",
            AuthorId = 1,
            Price = 10
        });

        List<Book> results = db.Table<Book>()
            .Where(b => b.Title.TrimEnd('x') == "Test")
            .ToList();

        Assert.Single(results);
    }

    [Fact]
    public void DateTimeOffset_TextFormatted_AddDaysInWhere_Throws()
    {
        using TestDatabase db = new(b =>
        {
            b.DateTimeOffsetStorage = DateTimeOffsetStorageMode.TextFormatted;
        });
        db.Schema.CreateTable<DateTimeOffsetMethodEntity>();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<DateTimeOffsetMethodEntity>()
                .Where(e => e.Date.AddDays(1) > DateTimeOffset.Now)
                .ToList());
    }

    [Fact]
    public void TimeSpan_Text_AddInWhere_Throws()
    {
        using TestDatabase db = new(b =>
        {
            b.TimeSpanStorage = TimeSpanStorageMode.Text;
        });
        db.Schema.CreateTable<TimeSpanMethodEntity>();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<TimeSpanMethodEntity>()
                .Where(e => e.Duration.Add(TimeSpan.FromHours(1)) > TimeSpan.FromHours(2))
                .ToList());
    }

    [Fact]
    public void IntToString_OnColumn_CastsAsText()
    {
        using TestDatabase db = new();

        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book
        {
            Id = 42,
            Title = "Test",
            AuthorId = 1,
            Price = 10
        });

        string? result = db.Table<Book>()
            .Select(b => b.Id.ToString())
            .First();

        Assert.Equal("42", result);
    }

    [Fact]
    public void FloatToString_OnColumn_CastsAsText()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<Book>()
            .Select(b => b.Price.ToString())
            .ToSqlCommand();

        Assert.Contains("CAST", command.CommandText);
        Assert.Contains("AS TEXT", command.CommandText);
    }

    [Fact]
    public void FloatParse_OnColumn_CastsAsReal()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<Book>()
            .Where(b => float.Parse(b.Title) > 10.0f)
            .ToSqlCommand();

        Assert.Contains("CAST", command.CommandText);
        Assert.Contains("AS REAL", command.CommandText);
    }

    [Fact]
    public void StringToString_OnColumn_FallsBackToClientEval()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Hello", AuthorId = 1, Price = 1 });

        string result = db.Table<Book>().Select(b => b.Title.ToString()).First();

        Assert.Equal("Hello", result);
    }

    [Fact]
    public void StringPadLeft_SingleIntArg_PadsWithSpace()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "ab", AuthorId = 1, Price = 1 });

        string result = db.Table<Book>().Select(b => b.Title.PadLeft(5)).First();

        Assert.Equal("   ab", result);
    }

    [Fact]
    public void StringPadRight_SingleIntArg_PadsWithSpace()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "ab", AuthorId = 1, Price = 1 });

        string result = db.Table<Book>().Select(b => b.Title.PadRight(5)).First();

        Assert.Equal("ab   ", result);
    }

    [Fact]
    public void StringJoin_NonArrayArgument_FallsBackToClientEval()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        IEnumerable<string> values = new[] { "a", "b", "c" };
        string result = db.Table<Book>().Select(b => string.Join(",", values)).First();

        Assert.Equal("a,b,c", result);
    }

    [Fact]
    public void MathAbs_ConstantArg_EvaluatedAsParameter()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 10 });

        double result = db.Table<Book>().Select(b => Math.Abs(-3.5) + b.Price).First();

        Assert.Equal(13.5, result);
    }

    [Fact]
    public void DateTime_TextFormatted_AddDaysInSelect_ReturnsClientSide()
    {
        using TestDatabase db = new(b =>
        {
            b.DateTimeStorage = DateTimeStorageMode.TextFormatted;
        });
        db.Schema.CreateTable<Author>();
        db.Table<Author>().Add(new Author
        {
            Id = 1,
            Name = "n",
            Email = "e",
            BirthDate = new DateTime(2024, 1, 1)
        });

        DateTime result = db.Table<Author>().Select(a => a.BirthDate.AddDays(1)).First();

        Assert.Equal(new DateTime(2024, 1, 2), result);
    }

    [Fact]
    public void DateTimeOffset_TextFormatted_AddDaysInSelect_ReturnsClientSide()
    {
        using TestDatabase db = new(b =>
        {
            b.DateTimeOffsetStorage = DateTimeOffsetStorageMode.TextFormatted;
        });
        db.Schema.CreateTable<DateTimeOffsetMethodEntity>();
        db.Table<DateTimeOffsetMethodEntity>().Add(new DateTimeOffsetMethodEntity
        {
            Id = 1,
            Date = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)
        });

        DateTimeOffset result = db.Table<DateTimeOffsetMethodEntity>()
            .Select(e => e.Date.AddDays(1))
            .First();

        Assert.Equal(new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero), result);
    }

    [Fact]
    public void TimeSpan_Text_AddInSelect_ReturnsClientSide()
    {
        using TestDatabase db = new(b =>
        {
            b.TimeSpanStorage = TimeSpanStorageMode.Text;
        });
        db.Schema.CreateTable<TimeSpanMethodEntity>();
        db.Table<TimeSpanMethodEntity>().Add(new TimeSpanMethodEntity
        {
            Id = 1,
            Duration = TimeSpan.FromHours(1)
        });

        TimeSpan result = db.Table<TimeSpanMethodEntity>()
            .Select(e => e.Duration.Add(TimeSpan.FromHours(2)))
            .First();

        Assert.Equal(TimeSpan.FromHours(3), result);
    }

    [Fact]
    public void DateOnly_Text_AddDaysInSelect_ReturnsClientSide()
    {
        using TestDatabase db = new(b =>
        {
            b.DateOnlyStorage = DateOnlyStorageMode.Text;
        });
        db.Schema.CreateTable<DateOnlyMethodEntity>();
        db.Table<DateOnlyMethodEntity>().Add(new DateOnlyMethodEntity
        {
            Id = 1,
            Date = new DateOnly(2024, 1, 1)
        });

        DateOnly result = db.Table<DateOnlyMethodEntity>()
            .Select(e => e.Date.AddDays(1))
            .First();

        Assert.Equal(new DateOnly(2024, 1, 2), result);
    }

    [Fact]
    public void TimeOnly_Text_AddHoursInSelect_ReturnsClientSide()
    {
        using TestDatabase db = new(b =>
        {
            b.TimeOnlyStorage = TimeOnlyStorageMode.Text;
        });
        db.Schema.CreateTable<TimeOnlyMethodEntity>();
        db.Table<TimeOnlyMethodEntity>().Add(new TimeOnlyMethodEntity
        {
            Id = 1,
            Time = new TimeOnly(10, 0, 0)
        });

        TimeOnly result = db.Table<TimeOnlyMethodEntity>()
            .Select(e => e.Time.AddHours(2))
            .First();

        Assert.Equal(new TimeOnly(12, 0, 0), result);
    }

    [Fact]
    public void DateTimeParse_AllConstants_EvaluatedAsParameter()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Author>();
        db.Table<Author>().Add(new Author
        {
            Id = 1,
            Name = "n",
            Email = "e",
            BirthDate = new DateTime(2024, 6, 15)
        });

        List<Author> results = db.Table<Author>()
            .Where(a => a.BirthDate > DateTime.Parse("2024-01-01"))
            .ToList();

        Assert.Single(results);
    }

    [Fact]
    public void DateTimeOffsetParse_AllConstants_EvaluatedAsParameter()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<DateTimeOffsetMethodEntity>();
        db.Table<DateTimeOffsetMethodEntity>().Add(new DateTimeOffsetMethodEntity
        {
            Id = 1,
            Date = new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero)
        });

        List<DateTimeOffsetMethodEntity> results = db.Table<DateTimeOffsetMethodEntity>()
            .Where(e => e.Date > DateTimeOffset.Parse("2024-01-01 00:00:00 +00:00"))
            .ToList();

        Assert.Single(results);
    }

    [Fact]
    public void DateTime_UnknownInstanceMethod_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Author>();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Author>()
                .Where(a => a.BirthDate.Subtract(TimeSpan.FromDays(1)).Year > 2000)
                .ToList());
    }

    [Fact]
    public void DateTime_StaticUnknownMethod_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Author>();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Author>()
                .Where(a => DateTime.Compare(a.BirthDate, DateTime.UtcNow) > 0)
                .ToList());
    }

    [Fact]
    public void DateTimeOffset_UnknownInstanceMethod_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<DateTimeOffsetMethodEntity>();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<DateTimeOffsetMethodEntity>()
                .Where(e => e.Date.ToOffset(TimeSpan.Zero).Year > 2000)
                .ToList());
    }

    [Fact]
    public void DateTimeOffset_StaticUnknownMethod_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<DateTimeOffsetMethodEntity>();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<DateTimeOffsetMethodEntity>()
                .Where(e => DateTimeOffset.Compare(e.Date, DateTimeOffset.UtcNow) > 0)
                .ToList());
    }

    [Fact]
    public void TimeSpan_UnknownInstanceMethod_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<TimeSpanMethodEntity>();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<TimeSpanMethodEntity>()
                .Where(e => e.Duration.Multiply(2).TotalDays > 0)
                .ToList());
    }

    [Fact]
    public void TimeSpan_StaticUnknownMethod_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<TimeSpanMethodEntity>();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<TimeSpanMethodEntity>()
                .Where(e => TimeSpan.Compare(e.Duration, TimeSpan.Zero) > 0)
                .ToList());
    }

    [Fact]
    public void DateOnly_UnknownInstanceMethod_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<DateOnlyMethodEntity>();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<DateOnlyMethodEntity>()
                .Where(e => e.Date.CompareTo(new DateOnly(2024, 1, 1)) > 0)
                .ToList());
    }

    [Fact]
    public void DateOnly_StaticUnknownMethod_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<DateOnlyMethodEntity>();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<DateOnlyMethodEntity>()
                .Where(e => DateOnly.FromDayNumber(e.Date.DayNumber) > new DateOnly(2024, 1, 1))
                .ToList());
    }

    [Fact]
    public void DateOnly_FromDateTime_AllConstants_EvaluatesAsParameter()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<DateOnlyMethodEntity>();
        db.Table<DateOnlyMethodEntity>().Add(new DateOnlyMethodEntity { Id = 1, Date = new DateOnly(2024, 6, 15) });

        List<DateOnlyMethodEntity> rows = db.Table<DateOnlyMethodEntity>()
            .Where(e => e.Date > DateOnly.FromDateTime(new DateTime(2024, 1, 1)))
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void TimeOnly_UnknownInstanceMethod_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<TimeOnlyMethodEntity>();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<TimeOnlyMethodEntity>()
                .Where(e => e.Time.CompareTo(new TimeOnly(0, 0)) > 0)
                .ToList());
    }

    [Fact]
    public void TimeOnly_StaticUnknownMethod_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<TimeOnlyMethodEntity>();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<TimeOnlyMethodEntity>()
                .Where(e => TimeOnly.FromTimeSpan(e.Time.ToTimeSpan()) > new TimeOnly(0, 0))
                .ToList());
    }

    [Fact]
    public void TimeOnly_FromDateTime_AllConstants_EvaluatesAsParameter()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<TimeOnlyMethodEntity>();
        db.Table<TimeOnlyMethodEntity>().Add(new TimeOnlyMethodEntity { Id = 1, Time = new TimeOnly(15, 0) });

        List<TimeOnlyMethodEntity> rows = db.Table<TimeOnlyMethodEntity>()
            .Where(e => e.Time > TimeOnly.FromDateTime(new DateTime(2024, 1, 1, 10, 0, 0)))
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void TimeSpan_FromDays_OnConstantArg_EvaluatesAsParameter()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<TimeSpanMethodEntity>();
        db.Table<TimeSpanMethodEntity>().Add(new TimeSpanMethodEntity { Id = 1, Duration = TimeSpan.FromDays(5) });

        List<TimeSpanMethodEntity> rows = db.Table<TimeSpanMethodEntity>()
            .Where(e => e.Duration > TimeSpan.FromDays(2.5))
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void Math_Truncate_OnColumn_DivisionPreservesRealSemantics()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 5.5 });

        double result = db.Table<Book>()
            .Select(f => Math.Truncate(f.Price) / 2)
            .First();

        Assert.Equal(2.5, result);
    }

    [Fact]
    public void Math_Truncate_OnColumn_RoundsTowardZero()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 7.9 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "y", AuthorId = 1, Price = -3.4 });

        List<double> truncated = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Select(b => Math.Truncate(b.Price))
            .ToList();

        Assert.Equal([7.0, -3.0], truncated);
    }

    [Fact]
    public void Math_UnknownStaticMethod_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Where(b => Math.Acos(b.Price) > 0)
                .ToList());
    }

    [Fact]
    public void Guid_NewGuid_InsideSelect_GeneratesParameter()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        Guid result = db.Table<Book>()
            .Select(b => Guid.NewGuid())
            .First();

        Assert.NotEqual(Guid.Empty, result);
    }

    [Fact]
    public void Guid_Parse_AllConstants_EvaluatesAsParameter()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        Guid expected = Guid.Parse("00000000-0000-0000-0000-000000000001");
        List<Book> rows = db.Table<Book>()
            .Where(b => Guid.Parse("00000000-0000-0000-0000-000000000001") == expected)
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void Char_UnknownStaticMethod_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Where(b => char.IsLetter(b.Title[0]))
                .ToList());
    }

    [Fact]
    public void Guid_UnknownInstanceMethod_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Where(b => Guid.Empty.CompareTo(Guid.Empty) > 0 && b.Id > 0)
                .ToList());
    }

    [Fact]
    public void StringConcat_UnknownStaticMethod_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Where(b => string.Format("{0}", b.Title).Length > 0)
                .ToList());
    }

    [Fact]
    public void EnumHasFlag_OnConstants_FoldsViaCheckConstantMethod()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        AttributeTargets target = AttributeTargets.Class | AttributeTargets.Method;
        List<Book> rows = db.Table<Book>()
            .Where(b => target.HasFlag(AttributeTargets.Class) && b.Id > 0)
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void TimeSpan_ToStringWithFormat_OnColumn_FallsBackToClientCall()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<TimeSpanMethodEntity>();
        db.Table<TimeSpanMethodEntity>().Add(new TimeSpanMethodEntity { Id = 1, Duration = TimeSpan.FromHours(2) });

        string result = db.Table<TimeSpanMethodEntity>()
            .Select(e => e.Duration.ToString("c"))
            .First();

        Assert.NotNull(result);
    }

    [Fact]
    public void DateTimeOffset_ToStringWithFormat_OnColumn_FallsBackToClientCall()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<DateTimeOffsetMethodEntity>();
        db.Table<DateTimeOffsetMethodEntity>().Add(new DateTimeOffsetMethodEntity
        {
            Id = 1,
            Date = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)
        });

        string result = db.Table<DateTimeOffsetMethodEntity>()
            .Select(e => e.Date.ToString("yyyy"))
            .First();

        Assert.NotNull(result);
    }

    [Fact]
    public void Queryable_SingleArgSubquery_WithParameters_Inlines()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Schema.CreateTable<Author>();
        db.Table<Author>().Add(new Author { Id = 1, Name = "n", Email = "e", BirthDate = default });
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        List<Author> rows = db.Table<Author>()
            .Where(a => db.Table<Book>().Where(b => b.AuthorId == a.Id).Count() > 0)
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void Queryable_SingleArgSubquery_WithoutParameters_InlinesNullParams()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Schema.CreateTable<Author>();
        db.Table<Author>().Add(new Author { Id = 1, Name = "n", Email = "e", BirthDate = default });

        List<Author> rows = db.Table<Author>()
            .Where(a => db.Table<Book>().LongCount() == 0L)
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void StringContains_CharArgument_BindsCharAsParameter()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

        SQLiteCommand cmd = db.Table<Book>()
            .Where(b => b.Title.Contains('b'))
            .ToSqlCommand();

        Assert.Equal('b', cmd.Parameters[0].Value);
    }

    [Fact]
    public void DateTime_AddDays_WithColumnArgument_BindsBothColumnSqlFragments()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Author>();
        db.Table<Author>().Add(new Author
        {
            Id = 5,
            Name = "n",
            Email = "e",
            BirthDate = new DateTime(2024, 1, 1)
        });

        List<Author> rows = db.Table<Author>()
            .Where(a => a.BirthDate.AddDays(a.Id) > new DateTime(2024, 1, 5))
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void FTS5_Rank_OnJoinedQuerySyntax_ResolvesAlias()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();
        db.Schema.CreateTable<ArticleSearch>();

        SQLiteCommand cmd = (
            from book in db.Table<Article>()
            join s in db.Table<ArticleSearch>() on book.Id equals s.Id
            where SQLiteFunctions.Match(s, "native")
            orderby SQLiteFunctions.Rank(s)
            select book
        ).ToSqlCommand();

        Assert.NotNull(cmd);
    }

    [Fact]
    public void FTS5_Match_ColumnScopedBuilder_WithConstantTerm_BuildsSingleColumnScopedParameter()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();
        db.Schema.CreateTable<ArticleSearch>();

        string term = "native";
        SQLiteCommand cmd = db.Table<ArticleSearch>()
            .Where(a => SQLiteFunctions.Match(a.Title, f => f.Term(term)))
            .ToSqlCommand();

        Assert.Contains(cmd.Parameters, p => string.Equals(p.Value as string, "{Title} : (native)"));
    }

    [Fact]
    public void FTS5_Match_ColumnScopedBuilder_WithDynamicTerm_WrapsWithColumnPrefixAndSuffix()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();
        db.Schema.CreateTable<ArticleSearch>();

        SQLiteCommand cmd = db.Table<ArticleSearch>()
            .Where(a => SQLiteFunctions.Match(a.Title, f => f.Term(a.Body)))
            .ToSqlCommand();

        List<string> parameterValues = cmd.Parameters.Select(p => p.Value?.ToString() ?? string.Empty).ToList();
        Assert.Contains("{Title} : (", parameterValues);
        Assert.Contains(")", parameterValues);
    }

    [Fact]
    public void FTS5_Rank_OnAnonymousProjectionMember_ResolvesAliasFromMemberAccess()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();
        db.Schema.CreateTable<ArticleSearch>();

        SQLiteCommand cmd = db.Table<Article>()
            .Join(
                db.Table<ArticleSearch>(),
                book => book.Id,
                s => s.Id,
                (book, s) => new { Book = book, Search = s })
            .Where(t => SQLiteFunctions.Match(t.Search, "native"))
            .OrderBy(t => SQLiteFunctions.Rank(t.Search))
            .ToSqlCommand();

        Assert.NotNull(cmd);
    }

    [Fact]
    public void EnumParse_NonGenericConstantTypeArg_Translates()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Monday", AuthorId = 1, Price = 1 });

        List<Book> rows = db.Table<Book>()
            .Where(b => (DayOfWeek)Enum.Parse(typeof(DayOfWeek), b.Title) == DayOfWeek.Monday)
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void Char_Parse_AllConstants_EvaluatesAsParameter()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });

        List<Book> rows = db.Table<Book>()
            .Where(b => b.Title.Length > 0 && char.Parse("a") == 'a')
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void Int_Parse_OnColumn_CastsAsInteger()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "42", AuthorId = 1, Price = 1 });

        int result = db.Table<Book>()
            .Where(b => int.Parse(b.Title) == 42)
            .Select(b => int.Parse(b.Title))
            .First();

        Assert.Equal(42, result);
    }

    [Fact]
    public void Int_UnknownInstanceMethod_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Where(b => b.Id.CompareTo(0) > 0)
                .ToList());
    }

    [Fact]
    public void Long_Parse_AllConstants_EvaluatesAsParameter()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        List<Book> rows = db.Table<Book>()
            .Where(b => long.Parse("42") == 42L)
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void Float_UnknownInstanceMethod_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Where(b => b.Price.CompareTo(0.0) > 0)
                .ToList());
    }

    [Fact]
    public void Double_Parse_AllConstants_EvaluatesAsParameter()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 5 });

        List<Book> rows = db.Table<Book>()
            .Where(b => double.Parse("3.14") < b.Price)
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void StringJoin_NonArrayWithNonConstantSeparator_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

        IEnumerable<string> values = new List<string> { "a", "b" };
        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Where(b => string.Join(b.Title, values).Length > 0)
                .ToList());
    }

    private class DateTimeOffsetMethodEntity
    {
        [Key]
        public int Id { get; set; }

        public DateTimeOffset Date { get; set; }
    }

    private class TimeSpanMethodEntity
    {
        [Key]
        public int Id { get; set; }

        public TimeSpan Duration { get; set; }
    }

    [Fact]
    public void Where_CorrelatedAverageWithSelector_TranslatesAndExecutes()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 1, Price = 9 });
        db.Table<Book>().Add(new Book { Id = 3, Title = "c", AuthorId = 2, Price = 2 });

        List<int> ids = db.Table<Book>()
            .Where(b => db.Table<Book>().Where(b2 => b2.AuthorId == b.AuthorId).Average(b2 => b2.Price) >= 5)
            .Select(b => b.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal([1, 2], ids);
    }

    [Fact]
    public void Where_CorrelatedSumWithSelector_TranslatesAndExecutes()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 1, Price = 9 });
        db.Table<Book>().Add(new Book { Id = 3, Title = "c", AuthorId = 2, Price = 2 });

        List<int> ids = db.Table<Book>()
            .Where(b => db.Table<Book>().Where(b2 => b2.AuthorId == b.AuthorId).Sum(b2 => b2.Price) >= 5)
            .Select(b => b.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal([1, 2], ids);
    }

    [Fact]
    public void Where_CorrelatedCountWithPredicate_TranslatesAndExecutes()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 1, Price = 9 });
        db.Table<Book>().Add(new Book { Id = 3, Title = "c", AuthorId = 2, Price = 2 });

        List<int> ids = db.Table<Book>()
            .Where(b => db.Table<Book>().Count(b2 => b2.AuthorId == b.AuthorId) > 1)
            .Select(b => b.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal([1, 2], ids);
    }
}