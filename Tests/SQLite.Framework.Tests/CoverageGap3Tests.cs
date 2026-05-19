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

    [Fact]
    public void NestedTransaction_Rollback_DoesNotReleaseOuterLock()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        using SQLiteTransaction outer = db.BeginTransaction();
        using (SQLiteTransaction inner = db.BeginTransaction())
        {
            db.Table<Book>().Add(new Book { Title = "inner", AuthorId = 1, Price = 1.0 });
            inner.Rollback();
        }

        db.Table<Book>().Add(new Book { Title = "outer", AuthorId = 1, Price = 1.0 });
        outer.Commit();

        Assert.Single(db.Table<Book>().ToList());
    }

    [Fact]
    public void NestedTransaction_Dispose_DoesNotReleaseOuterLock()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        using SQLiteTransaction outer = db.BeginTransaction();
        using (SQLiteTransaction inner = db.BeginTransaction())
        {
            db.Table<Book>().Add(new Book { Title = "inner", AuthorId = 1, Price = 1.0 });
        }

        db.Table<Book>().Add(new Book { Title = "outer", AuthorId = 1, Price = 1.0 });
        outer.Commit();

        Assert.Single(db.Table<Book>().ToList());
    }

    [Fact]
    public void Select_TopLevelMethodCall_ProducesEmptyPrefix()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Hello", AuthorId = 1, Price = 5 });

        List<string> rows = db.Table<Book>()
            .Select(b => b.Title.ToUpper())
            .ToList();

        Assert.Single(rows);
        Assert.Equal("HELLO", rows[0]);
    }

    [Fact]
    public void Select_TopLevelUntranslatableExpression_ProducesEmptyPrefix()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Hello", AuthorId = 1, Price = 5 });

        List<double> rows = db.Table<Book>()
            .Select(b => InterceptorHelpers.IdentityDouble(b.Price))
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void Select_DoubleInstanceMethodWithUntranslatableReceiver_FallsBack()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 5 });

        var rows = db.Table<Book>()
            .Select(b => new { b.Id, Text = InterceptorHelpers.IdentityDouble(b.Price).ToString() })
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void Select_MemberInitWithNullableSimpleField_HandlesBothNullAndNonNull()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();
        db.Table<NullableEntity>().Add(new NullableEntity { Id = 1, Value = 42 });
        db.Table<NullableEntity>().Add(new NullableEntity { Id = 2, Value = null });

        List<NullableMemberInitProjection> rows = db.Table<NullableEntity>()
            .Select(e => new NullableMemberInitProjection { Id = e.Id, Value = e.Value })
            .OrderBy(p => p.Id)
            .ToList();
        Assert.Equal(2, rows.Count);
        Assert.Equal(42, rows[0].Value);
        Assert.Null(rows[1].Value);
    }

    public class NullableMemberInitProjection
    {
        public int Id { get; set; }
        public int? Value { get; set; }
    }

#if !SQLITE_FRAMEWORK_SOURCE_GENERATOR
    [Fact]
    public void Select_EnumHasFlagWithUntranslatableReceiver_FallsBack()
    {
        using TestDatabase db = new();
        db.Table<Publisher>().Schema.CreateTable();
        db.Table<Publisher>().Add(new Publisher { Id = 1, Name = "x", Type = PublisherType.Magazine });

        var rows = db.Table<Publisher>()
            .Select(p => new { p.Id, Match = InterceptorHelpers.IdentityEnum(p.Type).HasFlag(PublisherType.Magazine) })
            .ToList();

        Assert.Single(rows);
    }
#endif

    [Fact]
    public void Select_StringSubstringWithUntranslatableReceiver_FallsBack()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Hello", AuthorId = 1, Price = 5 });

        var rows = db.Table<Book>()
            .Select(b => new { b.Id, Sub = InterceptorHelpers.Identity(b.Title).Substring(0, 2) })
            .ToList();

        Assert.Single(rows);
        Assert.Equal("He", rows[0].Sub);
    }

    [Fact]
    public void Select_DateTimeAddDaysWithUntranslatableReceiver_FallsBack()
    {
        using TestDatabase db = new();
        db.Table<DateEntity>().Schema.CreateTable();
        db.Table<DateEntity>().Add(new DateEntity { Id = 1, Date = new DateTime(2000, 1, 1) });

        var rows = db.Table<DateEntity>()
            .Select(d => new { d.Id, Plus = InterceptorHelpers.IdentityDateTime(d.Date).AddDays(1) })
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void Select_DateTimeOffsetAddDaysWithUntranslatableReceiver_FallsBack()
    {
        using TestDatabase db = new();
        db.Table<DateOffsetEntity>().Schema.CreateTable();
        db.Table<DateOffsetEntity>().Add(new DateOffsetEntity { Id = 1, Date = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero) });

        var rows = db.Table<DateOffsetEntity>()
            .Select(d => new { d.Id, Plus = InterceptorHelpers.IdentityDateTimeOffset(d.Date).AddDays(1) })
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void Select_TimeSpanAddWithUntranslatableReceiver_FallsBack()
    {
        using TestDatabase db = new();
        db.Table<TimeSpanEntity>().Schema.CreateTable();
        db.Table<TimeSpanEntity>().Add(new TimeSpanEntity { Id = 1, Span = TimeSpan.FromMinutes(30) });

        var rows = db.Table<TimeSpanEntity>()
            .Select(t => new { t.Id, Plus = InterceptorHelpers.IdentityTimeSpan(t.Span).Add(TimeSpan.FromMinutes(1)) })
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void Select_DateOnlyToString_FallsBack()
    {
        using TestDatabase db = new();
        db.Table<DateOnlyEntity>().Schema.CreateTable();
        db.Table<DateOnlyEntity>().Add(new DateOnlyEntity { Id = 1, Date = new DateOnly(2000, 1, 1) });

        var rows = db.Table<DateOnlyEntity>()
            .Select(d => new { d.Id, T = d.Date.ToString() })
            .ToList();
        Assert.Single(rows);
    }

    [Fact]
    public void Select_TimeOnlyToString_FallsBack()
    {
        using TestDatabase db = new();
        db.Table<TimeOnlyEntity>().Schema.CreateTable();
        db.Table<TimeOnlyEntity>().Add(new TimeOnlyEntity { Id = 1, Time = new TimeOnly(12, 0) });

        var rows = db.Table<TimeOnlyEntity>()
            .Select(d => new { d.Id, T = d.Time.ToString() })
            .ToList();
        Assert.Single(rows);
    }

    [Fact]
    public void Select_DateOnlyAddDaysWithUntranslatableReceiver_FallsBack()
    {
        using TestDatabase db = new();
        db.Table<DateOnlyEntity>().Schema.CreateTable();
        db.Table<DateOnlyEntity>().Add(new DateOnlyEntity { Id = 1, Date = new DateOnly(2000, 1, 1) });

        var rows = db.Table<DateOnlyEntity>()
            .Select(d => new { d.Id, Plus = InterceptorHelpers.IdentityDateOnly(d.Date).AddDays(1) })
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void Select_TimeOnlyAddWithUntranslatableReceiver_FallsBack()
    {
        using TestDatabase db = new();
        db.Table<TimeOnlyEntity>().Schema.CreateTable();
        db.Table<TimeOnlyEntity>().Add(new TimeOnlyEntity { Id = 1, Time = new TimeOnly(12, 0) });

        var rows = db.Table<TimeOnlyEntity>()
            .Select(t => new { t.Id, Plus = InterceptorHelpers.IdentityTimeOnly(t.Time).AddHours(1) })
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void Select_DateTimeAddDaysWithUntranslatableArg_FallsBack()
    {
        using TestDatabase db = new();
        db.Table<DateEntity>().Schema.CreateTable();
        db.Table<DateEntity>().Add(new DateEntity { Id = 1, Date = new DateTime(2000, 1, 1) });

        var rows = db.Table<DateEntity>()
            .Select(d => new { d.Id, Plus = d.Date.AddDays(InterceptorHelpers.IdentityDouble(1.0)) })
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void Select_DateTimeOffsetAddDaysWithUntranslatableArg_FallsBack()
    {
        using TestDatabase db = new();
        db.Table<DateOffsetEntity>().Schema.CreateTable();
        db.Table<DateOffsetEntity>().Add(new DateOffsetEntity { Id = 1, Date = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero) });

        var rows = db.Table<DateOffsetEntity>()
            .Select(d => new { d.Id, Plus = d.Date.AddDays(InterceptorHelpers.IdentityDouble(1.0)) })
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void Select_TimeSpanAddWithUntranslatableArg_FallsBack()
    {
        using TestDatabase db = new();
        db.Table<TimeSpanEntity>().Schema.CreateTable();
        db.Table<TimeSpanEntity>().Add(new TimeSpanEntity { Id = 1, Span = TimeSpan.FromMinutes(30) });

        var rows = db.Table<TimeSpanEntity>()
            .Select(t => new { t.Id, Plus = t.Span.Add(InterceptorHelpers.IdentityTimeSpan(TimeSpan.FromMinutes(1))) })
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void Select_DateOnlyAddDaysWithUntranslatableArg_FallsBack()
    {
        using TestDatabase db = new();
        db.Table<DateOnlyEntity>().Schema.CreateTable();
        db.Table<DateOnlyEntity>().Add(new DateOnlyEntity { Id = 1, Date = new DateOnly(2000, 1, 1) });

        var rows = db.Table<DateOnlyEntity>()
            .Select(d => new { d.Id, Plus = d.Date.AddDays(InterceptorHelpers.IdentityInt(1)) })
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void Select_TimeOnlyAddWithUntranslatableArg_FallsBack()
    {
        using TestDatabase db = new();
        db.Table<TimeOnlyEntity>().Schema.CreateTable();
        db.Table<TimeOnlyEntity>().Add(new TimeOnlyEntity { Id = 1, Time = new TimeOnly(12, 0) });

        var rows = db.Table<TimeOnlyEntity>()
            .Select(t => new { t.Id, Plus = t.Time.Add(InterceptorHelpers.IdentityTimeSpan(TimeSpan.FromHours(1))) })
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void Select_StringSubstringWithUntranslatableArg_FallsBack()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Hello", AuthorId = 1, Price = 5 });

        var rows = db.Table<Book>()
            .Select(b => new { b.Id, Sub = b.Title.Substring(InterceptorHelpers.IdentityInt(0), 2) })
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void DateTime_AddYears_BothObjAndArgWithParameters_PropagatesAll()
    {
        using TestDatabase db = SetupDateDb();

        DateTime result = (
            from a in db.Table<DateEntity>()
            where a.Id == 1
            select a.Date.AddDays(1).AddYears(a.Date.Year - 1999)
        ).First();

        Assert.NotEqual(default, result);
    }

    [Fact]
    public void DateTime_ChainedAddDaysAddYears_NonConstantArg_PropagatesParameters()
    {
        using TestDatabase db = SetupDateDb();

        DateTime result = (
            from a in db.Table<DateEntity>()
            where a.Id == 1
            select a.Date.AddDays(1).AddYears(a.Id)
        ).First();

        Assert.NotEqual(default, result);
    }

    [Fact]
    public void DateTimeOffset_AddDays_NonConstantArg_PropagatesParameters()
    {
        using TestDatabase db = SetupDateOffsetDb();

        DateTimeOffset result = (
            from a in db.Table<DateOffsetEntity>()
            where a.Id == 1
            select a.Date.AddDays(1).AddYears(a.Id)
        ).First();

        Assert.NotEqual(default, result);
    }

    [Fact]
    public void TimeSpan_Add_OnChainedTimeSpan_PropagatesParameters()
    {
        using TestDatabase db = SetupTimeSpanDb();

        TimeSpan ts = (
            from a in db.Table<TimeSpanEntity>()
            where a.Id == 1
            select a.Span.Add(a.Span)
        ).First();

        Assert.True(ts.Ticks > 0);
    }

    [Fact]
    public void DateOnly_AddDays_OnChained_PropagatesParameters()
    {
        using TestDatabase db = SetupDateOnlyDb();

        DateOnly d = (
            from a in db.Table<DateOnlyEntity>()
            where a.Id == 1
            select a.Date.AddDays(1).AddDays(a.Id)
        ).First();

        Assert.NotEqual(default, d);
    }

    [Fact]
    public void TimeOnly_AddMinutes_OnChained_PropagatesParameters()
    {
        using TestDatabase db = SetupTimeOnlyDb();

        TimeOnly t = (
            from a in db.Table<TimeOnlyEntity>()
            where a.Id == 1
            select a.Time.AddHours(1).AddMinutes(a.Id)
        ).First();

        Assert.NotEqual(default, t);
    }

    private static TestDatabase SetupDateDb()
    {
        TestDatabase db = new();
        db.Table<DateEntity>().Schema.CreateTable();
        db.Table<DateEntity>().Add(new DateEntity { Id = 1, Date = new DateTime(2000, 1, 1) });
        return db;
    }

    private static TestDatabase SetupDateOffsetDb()
    {
        TestDatabase db = new();
        db.Table<DateOffsetEntity>().Schema.CreateTable();
        db.Table<DateOffsetEntity>().Add(new DateOffsetEntity { Id = 1, Date = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero) });
        return db;
    }

    private static TestDatabase SetupTimeSpanDb()
    {
        TestDatabase db = new();
        db.Table<TimeSpanEntity>().Schema.CreateTable();
        db.Table<TimeSpanEntity>().Add(new TimeSpanEntity { Id = 1, Span = TimeSpan.FromMinutes(30) });
        return db;
    }

    private static TestDatabase SetupDateOnlyDb()
    {
        TestDatabase db = new();
        db.Table<DateOnlyEntity>().Schema.CreateTable();
        db.Table<DateOnlyEntity>().Add(new DateOnlyEntity { Id = 1, Date = new DateOnly(2000, 1, 1) });
        return db;
    }

    private static TestDatabase SetupTimeOnlyDb()
    {
        TestDatabase db = new();
        db.Table<TimeOnlyEntity>().Schema.CreateTable();
        db.Table<TimeOnlyEntity>().Add(new TimeOnlyEntity { Id = 1, Time = new TimeOnly(12, 0) });
        return db;
    }

    public class DateEntity
    {
        [System.ComponentModel.DataAnnotations.Key]
        public int Id { get; set; }
        public DateTime Date { get; set; }
    }

    public class DateOffsetEntity
    {
        [System.ComponentModel.DataAnnotations.Key]
        public int Id { get; set; }
        public DateTimeOffset Date { get; set; }
    }

    public class TimeSpanEntity
    {
        [System.ComponentModel.DataAnnotations.Key]
        public int Id { get; set; }
        public TimeSpan Span { get; set; }
    }

    public class DateOnlyEntity
    {
        [System.ComponentModel.DataAnnotations.Key]
        public int Id { get; set; }
        public DateOnly Date { get; set; }
    }

    public class TimeOnlyEntity
    {
        [System.ComponentModel.DataAnnotations.Key]
        public int Id { get; set; }
        public TimeOnly Time { get; set; }
    }

    public class NullableAutoIncEntity
    {
        [System.ComponentModel.DataAnnotations.Key]
        [SQLite.Framework.Attributes.AutoIncrement]
        public int? Id { get; set; }
        public required string Name { get; set; }
    }

    [Fact]
    public void Add_NullableAutoIncrementPrimaryKey_BackfillsViaUnderlyingType()
    {
        using TestDatabase db = new();
        db.Table<NullableAutoIncEntity>().Schema.CreateTable();

        NullableAutoIncEntity entity = new() { Name = "test" };
        db.Table<NullableAutoIncEntity>().Add(entity);

        Assert.NotNull(entity.Id);
        Assert.True(entity.Id.Value > 0);
    }

    [Fact]
    public void Select_EnumCastToNullableUnderlying_StripsConvert()
    {
        using TestDatabase db = new();
        db.Table<Publisher>().Schema.CreateTable();
        db.Table<Publisher>().Add(new Publisher { Id = 1, Name = "x", Type = PublisherType.Book });

        List<int?> rows = db.Table<Publisher>().Select(p => (int?)p.Type).ToList();
        Assert.Single(rows);
    }

    [Fact]
    public void Where_SubqueryAll_EmitsExists()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 5 });

        SQLiteCommand cmd = db.Table<Book>()
            .Where(outer => db.Table<Book>().All(inner => inner.Price > 0))
            .ToSqlCommand();

        Assert.Contains("EXISTS", cmd.CommandText);
    }

    [Fact]
    public void Select_TimeOnlyHourOnChainedTime_PropagatesParameters()
    {
        using TestDatabase db = new();
        db.Table<TimeOnlyEntity>().Schema.CreateTable();
        db.Table<TimeOnlyEntity>().Add(new TimeOnlyEntity { Id = 1, Time = new TimeOnly(12, 0) });

        int hour = (
            from t in db.Table<TimeOnlyEntity>()
            where t.Id == 1
            select t.Time.Add(TimeSpan.FromMinutes(1)).Hour
        ).First();
        Assert.True(hour >= 0);
    }

    [Fact]
    public void Select_DateTimeYearOnChainedDate_PropagatesParameters()
    {
        using TestDatabase db = new();
        db.Table<DateEntity>().Schema.CreateTable();
        db.Table<DateEntity>().Add(new DateEntity { Id = 1, Date = new DateTime(2000, 1, 1) });

        int year = (
            from a in db.Table<DateEntity>()
            where a.Id == 1
            select a.Date.AddDays(1).Year
        ).First();
        Assert.Equal(2000, year);
    }

    [Fact]
    public void Select_ConvertToObject_StripsToInner()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        List<object> rows = db.Table<Book>().Select(b => (object)b.Id).ToList();
        Assert.Single(rows);
    }

    [Fact]
    public void Default_TypedLambda_WithNonConvertUnaryBody_IsHandled()
    {
        using TestDatabase db = new();
        int captured = 5;
        db.Schema.Table<DateEntity>()
            .Default(b => b.Id, () => -captured)
            .CreateTable();

        Assert.True(db.Schema.TableExists<DateEntity>());
    }

    [Fact]
    public void StringBuilderPool_ReturnLargeBuilder_DoesNotCache()
    {
        System.Text.StringBuilder sb = SQLite.Framework.Internals.Helpers.StringBuilderPool.Rent();
        sb.Append('x', 5000);
        SQLite.Framework.Internals.Helpers.StringBuilderPool.Return(sb);

        System.Text.StringBuilder another = SQLite.Framework.Internals.Helpers.StringBuilderPool.Rent();
        Assert.NotSame(sb, another);
    }

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
