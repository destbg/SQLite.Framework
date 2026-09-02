using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class JsonPolymorphicTypeCheckTests
{
    [Fact]
    public void Select_AsOperatorMemberAccess_ReadsMemberValue()
    {
        SqlCapture capture = new();
        using TestDatabase db = Db(capture);
        PolyContainer row = new() { Id = 1, Data = new PolyX { X = 11 }, Data2 = [], Data3 = [] };
        db.Table<PolyContainer>().Add(row);
        List<PolyContainer> inMemory = [row];

        List<int> expected = inMemory.Select(c => (c.Data as PolyX)!.X).ToList();
        List<int> actual = db.Table<PolyContainer>().Select(c => (c.Data as PolyX)!.X).ToList();

        Assert.Equal(expected, actual);
        Assert.Equal("""
            SELECT json_extract(p0."Data", '$.X') AS "X"
            FROM "PolyContainers" AS p0
            """, capture.Sql[^1]);
    }

    [Fact]
    public void Select_AsOperatorMemberAccess_MissingMemberReadsTypeDefault()
    {
        using TestDatabase db = Db();
        db.Table<PolyContainer>().Add(new PolyContainer { Id = 1, Data = new PolyX { X = 11 }, Data2 = [], Data3 = [] });
        db.Table<PolyContainer>().Add(new PolyContainer { Id = 2, Data = new PolyY { Y = 7 }, Data2 = [], Data3 = [] });

        List<int> actual = db.Table<PolyContainer>().OrderBy(c => c.Id).Select(c => (c.Data as PolyY)!.Y).ToList();

        Assert.Equal([0, 7], actual);
    }

#if !SQLITECIPHER
    [Fact]
    public void Select_AsOperatorMemberAccess_Jsonb_ReadsMemberValue()
    {
        using TestDatabase db = new(b => b.AddJsonbContext(PolyJsonContext.Default));
        db.Table<PolyContainer>().Schema.CreateTable();
        db.Table<PolyContainer>().Add(new PolyContainer { Id = 1, Data = new PolyX { X = 11 }, Data2 = [], Data3 = [] });

        List<int> actual = db.Table<PolyContainer>().Select(c => (c.Data as PolyX)!.X).ToList();

        Assert.Equal([11], actual);
    }
#endif

    [Fact]
    public void Select_AsOperatorMemberAccess_UnregisteredTarget_ReadsMemberValue()
    {
        using TestDatabase db = new(b => b.AddTypeConverter<PolyBase>(new SQLiteJsonConverter<PolyBase>(PolyJsonContext.Default.PolyBase)));
        db.Table<PolyOnlyContainer>().Schema.CreateTable();
        db.Execute("""INSERT INTO "PolyOnlyContainers" ("Id", "Data") VALUES (1, '{"type":"x","X":11}')""");

        List<int> actual = db.Table<PolyOnlyContainer>().Select(c => (c.Data as PolyX)!.X).ToList();

        Assert.Equal([11], actual);
    }

    [Fact]
    public void Select_AsOperatorNonJsonOperand_ClientEvaluates()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Abc", AuthorId = 1, Price = 1 });
        List<Book> inMemory = db.Table<Book>().ToList();

        List<int> expected = inMemory.Select(c => (c.Title as string)!.Length).ToList();
        List<int> actual = db.Table<Book>().Select(c => (c.Title as string)!.Length).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Select_AsOperatorConditional_ReadsNullForNonMatchingRow()
    {
        using TestDatabase db = Db();
        db.Table<PolyContainer>().Add(new PolyContainer { Id = 1, Data = new PolyX { X = 11 }, Data2 = [], Data3 = [] });
        db.Table<PolyContainer>().Add(new PolyContainer { Id = 2, Data = new PolyY { Y = 7 }, Data2 = [], Data3 = [] });

        List<int?> actual = db.Table<PolyContainer>().OrderBy(c => c.Id)
            .Select(c => (c.Data as PolyX) == null ? (int?)null : ((PolyX)c.Data).X).ToList();

        Assert.Equal([11, null], actual);
    }

    [Fact]
    public void Where_AsOperatorMemberAccess_FiltersRows()
    {
        using TestDatabase db = Db();
        db.Table<PolyContainer>().Add(new PolyContainer { Id = 1, Data = new PolyX { X = 11 }, Data2 = [], Data3 = [] });
        db.Table<PolyContainer>().Add(new PolyContainer { Id = 2, Data = new PolyY { Y = 7 }, Data2 = [], Data3 = [] });
        db.Table<PolyContainer>().Add(new PolyContainer { Id = 3, Data = new PolyX { X = 2 }, Data2 = [], Data3 = [] });
        List<PolyContainer> inMemory = db.Table<PolyContainer>().ToList();

        List<int> expected = inMemory.Where(c => c.Data is PolyX { X: > 5 }).Select(c => c.Id).ToList();
        List<int> actual = db.Table<PolyContainer>().Where(c => (c.Data as PolyX)!.X > 5).Select(c => c.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Select_AsOperatorWholeObject_ReturnsRuntimeType()
    {
        using TestDatabase db = Db();
        PolyContainer row = new() { Id = 1, Data = new PolyX { X = 11 }, Data2 = [], Data3 = [] };
        db.Table<PolyContainer>().Add(row);

        List<PolyX?> actual = db.Table<PolyContainer>().Select(c => c.Data as PolyX).ToList();

        Assert.Equal(11, Assert.Single(actual)!.X);
    }

    [Fact]
    public void Select_IsCheck_ReturnsTrueForMatchingRow()
    {
        using TestDatabase db = Db();
        db.Table<PolyContainer>().Add(new PolyContainer { Id = 1, Data = new PolyX { X = 11 }, Data2 = [], Data3 = [] });
        db.Table<PolyContainer>().Add(new PolyContainer { Id = 2, Data = new PolyY { Y = 7 }, Data2 = [], Data3 = [] });

        List<bool> actual = db.Table<PolyContainer>().OrderBy(c => c.Id).Select(c => c.Data is PolyX).ToList();

        Assert.Equal([true, false], actual);
    }

    [Fact]
    public void Where_IsDerivedType_FiltersByDiscriminator()
    {
        SqlCapture capture = new();
        using TestDatabase db = Db(capture);
        List<PolyContainer> inMemory = SeedRows(db, includeUnknown: true);

        List<int> expected = inMemory.Where(c => c.Data is PolyY).Select(c => c.Id).ToList();
        List<int> actual = db.Table<PolyContainer>().Where(c => c.Data is PolyY).Select(c => c.Id).ToList();

        Assert.Equal(expected, actual);
        Assert.Equal("""
            SELECT p0."Id" AS "Id"
            FROM "PolyContainers" AS p0
            WHERE (json_extract(p0."Data", '$.type') IS NOT NULL AND json_extract(p0."Data", '$.type') = 'y')
            """, capture.Sql[^1]);
    }

    [Fact]
    public void Where_IsDerivedType_MatchesDerivedClosure()
    {
        SqlCapture capture = new();
        using TestDatabase db = Db(capture);
        List<PolyContainer> inMemory =
        [
            new PolyContainer { Id = 1, Data = new PolyX { X = 11 }, Data2 = [], Data3 = [] },
            new PolyContainer { Id = 2, Data = new PolyX2 { X = 5, X2 = 9 }, Data2 = [], Data3 = [] },
            new PolyContainer { Id = 3, Data = new PolyY { Y = 7 }, Data2 = [], Data3 = [] },
        ];
        db.Table<PolyContainer>().AddRange(inMemory);

        List<int> expected = inMemory.Where(c => c.Data is PolyX).Select(c => c.Id).ToList();
        List<int> actual = db.Table<PolyContainer>().Where(c => c.Data is PolyX).Select(c => c.Id).ToList();

        Assert.Equal(expected, actual);
        Assert.Equal("""
            SELECT p0."Id" AS "Id"
            FROM "PolyContainers" AS p0
            WHERE (json_extract(p0."Data", '$.type') IS NOT NULL AND json_extract(p0."Data", '$.type') IN ('x', 'x2'))
            """, capture.Sql[^1]);
    }

    [Fact]
    public void Where_IsBaseType_MatchesAllReadableRows()
    {
        SqlCapture capture = new();
        using TestDatabase db = Db(capture);
        List<PolyContainer> inMemory = SeedRows(db, includeUnknown: true);

        List<int> expected = inMemory.Where(c => c.Data is PolyBase).Select(c => c.Id).ToList();
        List<int> actual = db.Table<PolyContainer>().Where(c => c.Data is PolyBase).Select(c => c.Id).ToList();

        Assert.Equal(expected, actual);
        Assert.Equal("""
            SELECT p0."Id" AS "Id"
            FROM "PolyContainers" AS p0
            WHERE (p0."Data" IS NOT NULL AND (json_extract(p0."Data", '$.type') IS NULL OR json_extract(p0."Data", '$.type') IN ('x', 'x2', 'y')))
            """, capture.Sql[^1]);
    }

    [Fact]
    public void Where_IsDerivedTypeNegated_KeepsNonMatchingRows()
    {
        using TestDatabase db = Db();
        List<PolyContainer> inMemory = SeedRows(db, includeUnknown: false);

        List<int> expected = inMemory.Where(c => !(c.Data is PolyY)).Select(c => c.Id).ToList();
        List<int> actual = db.Table<PolyContainer>().Where(c => !(c.Data is PolyY)).Select(c => c.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Where_AsNotNullCheck_FiltersByDiscriminator()
    {
        using TestDatabase db = Db();
        List<PolyContainer> inMemory = SeedRows(db, includeUnknown: true);

        List<int> expected = inMemory.Where(c => c.Data as PolyY != null).Select(c => c.Id).ToList();
        List<int> actual = db.Table<PolyContainer>().Where(c => c.Data as PolyY != null).Select(c => c.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Where_AsNullCheck_FiltersByDiscriminator()
    {
        SqlCapture capture = new();
        using TestDatabase db = Db(capture);
        List<PolyContainer> inMemory = SeedRows(db, includeUnknown: false);

        List<int> expected = inMemory.Where(c => c.Data as PolyY == null).Select(c => c.Id).ToList();
        List<int> actual = db.Table<PolyContainer>().Where(c => c.Data as PolyY == null).Select(c => c.Id).ToList();

        Assert.Equal(expected, actual);
        Assert.Equal("""
            SELECT p0."Id" AS "Id"
            FROM "PolyContainers" AS p0
            WHERE (json_extract(p0."Data", '$.type') IS NULL OR json_extract(p0."Data", '$.type') <> 'y')
            """, capture.Sql[^1]);
    }

    [Fact]
    public void Where_AsNullCheckNullOnLeft_FiltersByDiscriminator()
    {
        using TestDatabase db = Db();
        List<PolyContainer> inMemory = SeedRows(db, includeUnknown: false);

        List<int> expected = inMemory.Where(c => null == (c.Data as PolyY)).Select(c => c.Id).ToList();
        List<int> actual = db.Table<PolyContainer>().Where(c => null == (c.Data as PolyY)).Select(c => c.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Where_IsInterface_MatchesImplementors()
    {
        using TestDatabase db = Db();
        List<PolyContainer> inMemory = SeedRows(db, includeUnknown: true);

        List<int> expected = inMemory.Where(c => c.Data is IPolyMark).Select(c => c.Id).ToList();
        List<int> actual = db.Table<PolyContainer>().Where(c => c.Data is IPolyMark).Select(c => c.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Where_IsUnrelatedInterface_MatchesNothing()
    {
        SqlCapture capture = new();
        using TestDatabase db = Db(capture);
        SeedRows(db, includeUnknown: true);

        List<int> actual = db.Table<PolyContainer>().Where(c => c.Data is IUnrelatedMark).Select(c => c.Id).ToList();

        Assert.Empty(actual);
        Assert.Equal("""
            SELECT p0."Id" AS "Id"
            FROM "PolyContainers" AS p0
            WHERE 0
            """, capture.Sql[^1]);
    }

    [Fact]
    public void Where_AsNullCheckUnrelatedInterface_MatchesAllRows()
    {
        SqlCapture capture = new();
        using TestDatabase db = Db(capture);
        List<PolyContainer> inMemory = SeedRows(db, includeUnknown: false);

        List<int> actual = db.Table<PolyContainer>().Where(c => (c.Data as IUnrelatedMark) == null).Select(c => c.Id).ToList();

        Assert.Equal(inMemory.Select(c => c.Id).ToList(), actual);
        Assert.Equal("""
            SELECT p0."Id" AS "Id"
            FROM "PolyContainers" AS p0
            WHERE 1
            """, capture.Sql[^1]);
    }

#if !SQLITECIPHER
    [Fact]
    public void Where_IsDerivedType_Jsonb_FiltersByDiscriminator()
    {
        using TestDatabase db = new(b => b.AddJsonbContext(PolyJsonContext.Default));
        db.Table<PolyContainer>().Schema.CreateTable();
        List<PolyContainer> inMemory =
        [
            new PolyContainer { Id = 1, Data = new PolyX { X = 11 }, Data2 = [], Data3 = [] },
            new PolyContainer { Id = 2, Data = new PolyY { Y = 7 }, Data2 = [], Data3 = [] },
        ];
        db.Table<PolyContainer>().AddRange(inMemory);

        List<int> expected = inMemory.Where(c => c.Data is PolyY).Select(c => c.Id).ToList();
        List<int> actual = db.Table<PolyContainer>().Where(c => c.Data is PolyY).Select(c => c.Id).ToList();

        Assert.Equal(expected, actual);
    }
#endif

    [Fact]
    public void Where_IsIntDiscriminator_FiltersByNumber()
    {
        SqlCapture capture = new();
        using TestDatabase db = Db(capture);
        db.Table<IntDiscContainer>().Schema.CreateTable();
        List<IntDiscContainer> inMemory =
        [
            new IntDiscContainer { Id = 1, Data = new IntDiscChild { V = 3 } },
            new IntDiscContainer { Id = 2, Data = new IntDiscBase() },
            new IntDiscContainer { Id = 3, Data = null },
        ];
        db.Table<IntDiscContainer>().AddRange(inMemory);

        List<int> expected = inMemory.Where(c => c.Data is IntDiscChild).Select(c => c.Id).ToList();
        List<int> actual = db.Table<IntDiscContainer>().Where(c => c.Data is IntDiscChild).Select(c => c.Id).ToList();

        Assert.Equal(expected, actual);
        Assert.Equal("""
            SELECT i0."Id" AS "Id"
            FROM "IntDiscContainers" AS i0
            WHERE (json_extract(i0."Data", '$.k') IS NOT NULL AND json_extract(i0."Data", '$.k') = 7)
            """, capture.Sql[^1]);
    }

    [Fact]
    public void Where_IsBaseTypeWithDiscriminatorlessChild_ReadsChildAsBase()
    {
        SqlCapture capture = new();
        using TestDatabase db = Db(capture);
        db.Table<DefaultDiscContainer>().Schema.CreateTable();
        List<DefaultDiscContainer> inMemory =
        [
            new DefaultDiscContainer { Id = 1, Data = new DefaultDiscBase() },
            new DefaultDiscContainer { Id = 2, Data = new DefaultDiscChild { V = 5 } },
            new DefaultDiscContainer { Id = 3, Data = null },
        ];
        db.Table<DefaultDiscContainer>().AddRange(inMemory);

        List<int> expected = inMemory.Where(c => c.Data is DefaultDiscBase).Select(c => c.Id).ToList();
        List<int> actual = db.Table<DefaultDiscContainer>().Where(c => c.Data is DefaultDiscBase).Select(c => c.Id).ToList();

        Assert.Equal(expected, actual);
        Assert.Equal("""
            SELECT d0."Id" AS "Id"
            FROM "DefaultDiscContainers" AS d0
            WHERE (d0."Data" IS NOT NULL AND json_extract(d0."Data", '$.type') IS NULL)
            """, capture.Sql[^1]);
    }

    [Fact]
    public void Where_AsNullCheckDiscriminatorlessBase_MatchesNullRows()
    {
        using TestDatabase db = Db();
        db.Table<DefaultDiscContainer>().Schema.CreateTable();
        db.Table<DefaultDiscContainer>().AddRange([
            new DefaultDiscContainer { Id = 1, Data = new DefaultDiscBase() },
            new DefaultDiscContainer { Id = 2, Data = null },
        ]);

        List<int> actual = db.Table<DefaultDiscContainer>().Where(c => c.Data as DefaultDiscBase == null).Select(c => c.Id).ToList();

        Assert.Equal([2], actual);
    }

    [Fact]
    public void Where_AsNullCheckBaseType_MatchesOnlyNullRows()
    {
        SqlCapture capture = new();
        using TestDatabase db = Db(capture);
        List<PolyContainer> inMemory = SeedRows(db, includeUnknown: false);

        List<int> expected = inMemory.Where(c => c.Data as PolyBase == null).Select(c => c.Id).ToList();
        List<int> actual = db.Table<PolyContainer>().Where(c => c.Data as PolyBase == null).Select(c => c.Id).ToList();

        Assert.Equal(expected, actual);
        Assert.Equal("""
            SELECT p0."Id" AS "Id"
            FROM "PolyContainers" AS p0
            WHERE (p0."Data" IS NULL OR (json_extract(p0."Data", '$.type') IS NOT NULL AND json_extract(p0."Data", '$.type') NOT IN ('x', 'x2', 'y')))
            """, capture.Sql[^1]);
    }

    [Fact]
    public void Where_IsNonJsonColumn_ThrowsNotSupported()
    {
        using TestDatabase db = new();

        Assert.Throws<NotSupportedException>(() => db.Table<Book>().Where(c => c.Title is string).ToList());
    }

    [Fact]
    public void Where_AsNullCheckNonJsonColumn_ThrowsNotSupported()
    {
        using TestDatabase db = new();

        Assert.Throws<NotSupportedException>(() => db.Table<Book>().Where(c => c.Title as string != null).ToList());
    }

    [Fact]
    public void Where_AsNullCheckAgainstValue_ThrowsNotSupported()
    {
        using TestDatabase db = Db();

        Assert.Throws<NotSupportedException>(() => db.Table<PolyContainer>().Where(c => (c.Data as PolyX) == new PolyX()).ToList());
    }

    [Fact]
    public void Where_IsNonPolymorphicJson_ThrowsNotSupported()
    {
        using TestDatabase db = Db();

        Assert.Throws<NotSupportedException>(() => db.Table<PlainJsonContainer>().Where(c => c.Data is PlainJsonChild).ToList());
    }

    [Fact]
    public void Where_IsUnknownDerivedHandling_ThrowsNotSupported()
    {
        using TestDatabase db = Db();

        Assert.Throws<NotSupportedException>(() => db.Table<FallBackContainer>().Where(c => c.Data is FallBackChild).ToList());
    }

    [Fact]
    public void Where_IsIgnoreUnrecognized_ThrowsNotSupported()
    {
        using TestDatabase db = Db();

        Assert.Throws<NotSupportedException>(() => db.Table<IgnoreDiscContainer>().Where(c => c.Data is IgnoreDiscChild).ToList());
    }

    [Fact]
    public void Where_IsMissingDiscriminator_ThrowsNotSupported()
    {
        using TestDatabase db = Db();

        Assert.Throws<NotSupportedException>(() => db.Table<DefaultDiscContainer>().Where(c => c.Data is DefaultDiscChild).ToList());
    }

    [Fact]
    public void Where_IsTypeEqualCheck_MatchesExactType()
    {
        using TestDatabase db = Db();
        List<PolyContainer> inMemory =
        [
            new PolyContainer { Id = 1, Data = new PolyX { X = 11 }, Data2 = [], Data3 = [] },
            new PolyContainer { Id = 2, Data = new PolyX2 { X = 5, X2 = 9 }, Data2 = [], Data3 = [] },
            new PolyContainer { Id = 3, Data = new PolyBase(), Data2 = [], Data3 = [] },
            new PolyContainer { Id = 4, Data = null, Data2 = [], Data3 = [] },
        ];
        db.Table<PolyContainer>().AddRange(inMemory);
        ParameterExpression parameter = Expression.Parameter(typeof(PolyContainer), "c");
        Expression<Func<PolyContainer, bool>> predicate = Expression.Lambda<Func<PolyContainer, bool>>(
            Expression.TypeEqual(Expression.Property(parameter, nameof(PolyContainer.Data)), typeof(PolyX)), parameter);

        List<int> expected = inMemory.Where(predicate.Compile()).Select(c => c.Id).ToList();
        List<int> actual = db.Table<PolyContainer>().Where(predicate).Select(c => c.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Where_GetTypeCheck_MatchesExactBaseType()
    {
        using TestDatabase db = Db();
        List<PolyContainer> inMemory = SeedRows(db, includeUnknown: false);

        List<int> expected = inMemory.Where(c => c.Data != null && c.Data.GetType() == typeof(PolyBase)).Select(c => c.Id).ToList();
        List<int> actual = db.Table<PolyContainer>().Where(c => c.Data != null && c.Data.GetType() == typeof(PolyBase)).Select(c => c.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Where_GetTypeNotEqualCheck_MatchesOtherExactTypes()
    {
        using TestDatabase db = Db();
        List<PolyContainer> inMemory = SeedRows(db, includeUnknown: false);

        List<int> expected = inMemory.Where(c => c.Data != null && c.Data.GetType() != typeof(PolyX)).Select(c => c.Id).ToList();
        List<int> actual = db.Table<PolyContainer>().Where(c => c.Data != null && c.Data.GetType() != typeof(PolyX)).Select(c => c.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Where_GetTypeComparedToNull_ThrowsNotSupported()
    {
        using TestDatabase db = Db();
        Type? missing = null;

        Assert.Throws<NotSupportedException>(() => db.Table<PolyContainer>()
            .Where(c => c.Data != null && c.Data.GetType() == missing)
            .Select(c => c.Id)
            .ToList());
    }

    [Fact]
    public void Where_GetTypeComparedToRowType_ThrowsNotSupported()
    {
        using TestDatabase db = Db();

        Assert.Throws<NotSupportedException>(() => db.Table<PolyContainer>()
            .Where(c => c.Data != null && c.Data.GetType() == c.GetType())
            .Select(c => c.Id)
            .ToList());
    }

    private static List<PolyContainer> SeedRows(TestDatabase db, bool includeUnknown)
    {
        List<PolyContainer> rows =
        [
            new PolyContainer { Id = 1, Data = new PolyX { X = 11 }, Data2 = [], Data3 = [] },
            new PolyContainer { Id = 2, Data = new PolyY { Y = 7 }, Data2 = [], Data3 = [] },
            new PolyContainer { Id = 3, Data = new PolyBase(), Data2 = [], Data3 = [] },
            new PolyContainer { Id = 4, Data = null, Data2 = [], Data3 = [] },
        ];
        db.Table<PolyContainer>().AddRange(rows);
        if (includeUnknown)
        {
            db.Execute("""INSERT INTO "PolyContainers" ("Id", "Data", "Data2", "Data3") VALUES (5, '{"type":"z","Z":1}', '[]', '{}')""");
        }

        return rows;
    }

    private static TestDatabase Db(ISQLiteCommandInterceptor? interceptor = null)
    {
        TestDatabase db = new(b =>
        {
            b.AddJsonContext(PolyJsonContext.Default);
            b.AddJsonContext(PolyTypeTestJsonContext.Default);
            if (interceptor != null)
            {
                b.AddCommandInterceptor(interceptor);
            }
        });
        db.Table<PolyContainer>().Schema.CreateTable();
        return db;
    }

    private sealed class SqlCapture : ISQLiteCommandInterceptor
    {
        public List<string> Sql { get; } = [];

        public void OnExecuting(SQLiteCommand command)
        {
            Sql.Add(command.CommandText);
        }

        public void OnExecuted(SQLiteCommand command, int? rowsAffected) { }
        public void OnFailed(SQLiteCommand command, Exception exception) { }
        public void OnRowRead(SQLiteCommand command, SQLiteDataReader reader) { }
        public void OnReaderClosing(SQLiteCommand command, SQLiteDataReader reader, int readCount) { }
    }
}

public interface IUnrelatedMark
{
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType)]
[JsonDerivedType(typeof(FallBackChild), "c")]
public class FallBackBase
{
}

public class FallBackChild : FallBackBase
{
    public int V { get; set; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type", IgnoreUnrecognizedTypeDiscriminators = true)]
[JsonDerivedType(typeof(IgnoreDiscChild), "c")]
public class IgnoreDiscBase
{
}

public class IgnoreDiscChild : IgnoreDiscBase
{
    public int V { get; set; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(DefaultDiscChild))]
public class DefaultDiscBase
{
}

public class DefaultDiscChild : DefaultDiscBase
{
    public int V { get; set; }
}

public class PlainJsonBase
{
}

public class PlainJsonChild : PlainJsonBase
{
    public int V { get; set; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "k")]
[JsonDerivedType(typeof(IntDiscChild), 7)]
public class IntDiscBase
{
}

public class IntDiscChild : IntDiscBase
{
    public int V { get; set; }
}

[Table("FallBackContainers")]
public class FallBackContainer
{
    [Key]
    public int Id { get; set; }

    public FallBackBase? Data { get; set; }
}

[Table("IgnoreDiscContainers")]
public class IgnoreDiscContainer
{
    [Key]
    public int Id { get; set; }

    public IgnoreDiscBase? Data { get; set; }
}

[Table("DefaultDiscContainers")]
public class DefaultDiscContainer
{
    [Key]
    public int Id { get; set; }

    public DefaultDiscBase? Data { get; set; }
}

[Table("PlainJsonContainers")]
public class PlainJsonContainer
{
    [Key]
    public int Id { get; set; }

    public PlainJsonBase? Data { get; set; }
}

[Table("IntDiscContainers")]
public class IntDiscContainer
{
    [Key]
    public int Id { get; set; }

    public IntDiscBase? Data { get; set; }
}

[JsonSerializable(typeof(FallBackBase))]
[JsonSerializable(typeof(IgnoreDiscBase))]
[JsonSerializable(typeof(DefaultDiscBase))]
[JsonSerializable(typeof(PlainJsonBase))]
[JsonSerializable(typeof(IntDiscBase))]
internal partial class PolyTypeTestJsonContext : JsonSerializerContext;
