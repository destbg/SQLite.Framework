using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

// ReSharper disable AccessToDisposedClosure

namespace SQLite.Framework.Tests;

public class CteTests
{
    private static string N(string s) => s.Replace("\r\n", "\n");

    [Fact]
    public void With_SimpleSelect_GeneratesWithClause()
    {
        using TestDatabase db = new();

        SQLiteCte<Book> cte = db.With(() => db.Table<Book>());

        SQLiteCommand command = (from b in cte select b).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal(
            N("""
            WITH cte0 AS (
                SELECT b1.BookId AS "Id",
                   b1.BookTitle AS "Title",
                   b1.BookAuthorId AS "AuthorId",
                   b1.BookPrice AS "Price"
                FROM "Books" AS b1
            )
            SELECT b0.Id AS "Id",
                   b0.Title AS "Title",
                   b0.AuthorId AS "AuthorId",
                   b0.Price AS "Price"
            FROM cte0 AS b0
            """), N(command.CommandText));
    }

    [Fact]
    public void With_WhereFilter_PassesFilterIntoCte()
    {
        using TestDatabase db = new();

        SQLiteCte<Book> cte = db.With(() => db.Table<Book>().Where(b => b.AuthorId == 1));

        SQLiteCommand command = (from b in cte select b).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1, command.Parameters[0].Value);
        Assert.Equal(
            N("""
            WITH cte0 AS (
                SELECT b1.BookId AS "Id",
                   b1.BookTitle AS "Title",
                   b1.BookAuthorId AS "AuthorId",
                   b1.BookPrice AS "Price"
                FROM "Books" AS b1
                WHERE b1.BookAuthorId = @p0
            )
            SELECT b0.Id AS "Id",
                   b0.Title AS "Title",
                   b0.AuthorId AS "AuthorId",
                   b0.Price AS "Price"
            FROM cte0 AS b0
            """), N(command.CommandText));
    }

    [Fact]
    public void With_SelectProjection_ProjectsColumns()
    {
        using TestDatabase db = new();

        SQLiteCte<Book> cte = db.With(() =>
            from b in db.Table<Book>()
            where b.Price > 10
            select b);

        SQLiteCommand command = (from b in cte
            select new
            {
                b.Id,
                b.Title
            }).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(10.0, command.Parameters[0].Value);
        Assert.Equal(
            N("""
            WITH cte0 AS (
                SELECT b1.BookId AS "Id",
                   b1.BookTitle AS "Title",
                   b1.BookAuthorId AS "AuthorId",
                   b1.BookPrice AS "Price"
                FROM "Books" AS b1
                WHERE b1.BookPrice > @p0
            )
            SELECT b0.Id AS "Id",
                   b0.Title AS "Title"
            FROM cte0 AS b0
            """), N(command.CommandText));
    }

    [Fact]
    public void With_JoinWithTable_GeneratesJoin()
    {
        using TestDatabase db = new();

        SQLiteCte<Book> cte = db.With(() => db.Table<Book>().Where(b => b.Price > 5));

        SQLiteCommand command = (
            from b in cte
            join a in db.Table<Author>() on b.AuthorId equals a.Id
            select new
            {
                b.Title,
                AuthorName = a.Name
            }
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(5.0, command.Parameters[0].Value);
        Assert.Equal(
            N("""
            WITH cte0 AS (
                SELECT b1.BookId AS "Id",
                   b1.BookTitle AS "Title",
                   b1.BookAuthorId AS "AuthorId",
                   b1.BookPrice AS "Price"
                FROM "Books" AS b1
                WHERE b1.BookPrice > @p0
            )
            SELECT b0.Title AS "Title",
                   a1.AuthorName AS "AuthorName"
            FROM cte0 AS b0
            JOIN "Authors" AS a1 ON b0.AuthorId = a1.AuthorId
            """), N(command.CommandText));
    }

    [Fact]
    public void Values_SimpleScalar_GeneratesValuesClause()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Values(42).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(42, command.Parameters[0].Value);
        Assert.Equal(
            N("""
            SELECT i0."column__1" AS "2"
            FROM (SELECT @p1 AS "column__1") AS i0
            """), N(command.CommandText));
    }

    [Fact]
    public void Values_AnonymousObject_GeneratesValuesClause()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Values(new
        {
            Col = "hello",
            Num = 7
        }).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal("hello", command.Parameters.Single(p => p.Name == "@p1").Value);
        Assert.Equal(7, command.Parameters.Single(p => p.Name == "@p2").Value);
        Assert.Equal(
            N("""
            SELECT f0."Col" AS "Col",
                   f0."Num" AS "Num"
            FROM (SELECT @p1 AS "Col", @p2 AS "Num") AS f0
            """), N(command.CommandText));
    }

    [Fact]
    public void With_ValuesQuery_GeneratesWithValues()
    {
        using TestDatabase db = new();

        SQLiteCte<int> cte = db.With(() => db.Values(1));

        SQLiteCommand command = (from v in cte select v).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1, command.Parameters.Single(p => p.Name == "@p1").Value);
        Assert.Equal(
            N("""
            WITH cte0 AS (
                SELECT i1."column__1" AS "2"
                FROM (SELECT @p1 AS "column__1") AS i1
            )
            SELECT *
            FROM cte0 AS i0
            """), N(command.CommandText));
    }

    [Fact]
    public void WithRecursive_CountingCte_GeneratesWithRecursiveClause()
    {
        using TestDatabase db = new();

        SQLiteCte<Cnt> cte = db.WithRecursive<Cnt>(self =>
            db.Values(new Cnt
                {
                    X = 1
                })
                .Concat(from c in self
                    where c.X < 10
                    select new Cnt
                    {
                        X = c.X + 1
                    }));

        SQLiteCommand command = (from c in cte orderby c.X select c).ToSqlCommand();

        Assert.Equal(3, command.Parameters.Count);
        Assert.Equal(1, command.Parameters.Single(p => p.Name == "@p1").Value);
        Assert.Equal(10, command.Parameters.Single(p => p.Name == "@p2").Value);
        Assert.Equal(1, command.Parameters.Single(p => p.Name == "@p3").Value);
        Assert.Equal(
            N("""
            WITH RECURSIVE cte0 AS (
                SELECT c1."X" AS "X"
                FROM (SELECT @p1 AS "X") AS c1
                UNION ALL
                    SELECT (c2.X + @p3) AS "X"
                FROM cte0 AS c2
                WHERE c2.X < @p2
            )
            SELECT c0.X AS "X"
            FROM cte0 AS c0
            ORDER BY c0.X ASC
            """), N(command.CommandText));
    }

    [Fact]
    public void WithRecursive_Fibonacci_GeneratesSql()
    {
        using TestDatabase db = new();

        SQLiteCte<Fib> cte = db.WithRecursive<Fib>(self =>
            db.Values(new Fib
                {
                    A = 0,
                    B = 1
                })
                .Concat(from f in self
                    where f.B < 100
                    select new Fib
                    {
                        A = f.B,
                        B = f.A + f.B
                    }));

        SQLiteCommand command = (from f in cte select f).ToSqlCommand();

        Assert.Equal(3, command.Parameters.Count);
        Assert.Equal(0, command.Parameters.Single(p => p.Name == "@p2").Value);
        Assert.Equal(1, command.Parameters.Single(p => p.Name == "@p3").Value);
        Assert.Equal(100, command.Parameters.Single(p => p.Name == "@p4").Value);
        Assert.Equal(
            N("""
            WITH RECURSIVE cte0 AS (
                SELECT f1."A" AS "A",
                   f1."B" AS "B"
                FROM (SELECT @p2 AS "A", @p3 AS "B") AS f1
                UNION ALL
                    SELECT f2.B AS "A",
                   (f2.A + f2.B) AS "B"
                FROM cte0 AS f2
                WHERE f2.B < @p4
            )
            SELECT f0.A AS "A",
                   f0.B AS "B"
            FROM cte0 AS f0
            """), N(command.CommandText));
    }

    [Fact]
    public void WithRecursive_Fibonacci_Execute_ReturnsFibonacciSequence()
    {
        using TestDatabase db = new();

        SQLiteCte<Fib> cte = db.WithRecursive<Fib>(self =>
            db.Values(new Fib
                {
                    A = 0,
                    B = 1
                })
                .Concat(from f in self
                    where f.B < 100
                    select new Fib
                    {
                        A = f.B,
                        B = f.A + f.B
                    }));

        List<Fib> results = (from f in cte select f).ToList();

        Assert.Equal(12, results.Count);
        Assert.Equal(0, results[0].A);
        Assert.Equal(1, results[0].B);
        Assert.Equal(89, results[11].A);
        Assert.Equal(144, results[11].B);
    }

    [Fact]
    public void WithRecursive_OrgBfs_GeneratesBfsSql()
    {
        using TestDatabase db = new();

        SQLiteCte<Org> org = db.With(() => db.Table<Org>());
        SQLiteCte<OrgLevel> cte = db.WithRecursive<OrgLevel>(self =>
            (from o in org
                where o.Boss == null
                select new OrgLevel
                {
                    Name = o.Name,
                    Level = 1
                })
            .Concat(from o in org
                join p in self on o.Boss equals p.Name
                select new OrgLevel
                {
                    Name = o.Name,
                    Level = p.Level + 1
                }));

        SQLiteCommand command = (from n in cte orderby n.Level, n.Name select n).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(1, command.Parameters.Single(p => p.Name == "@p1").Value);
        Assert.Equal(1, command.Parameters.Single(p => p.Name == "@p2").Value);
        Assert.Equal(
            N("""
            WITH RECURSIVE cte0 AS (
                    SELECT o2.Name AS "Name",
                   o2.Boss AS "Boss"
                    FROM "Org" AS o2
            ),
            cte1 AS (
                SELECT o1.Name AS "Name",
                   @p1 AS "Level"
                FROM cte0 AS o1
                WHERE o1.Boss IS NULL
                UNION ALL
                    SELECT o3.Name AS "Name",
                   (o4.Level + @p2) AS "Level"
                FROM cte0 AS o3
                JOIN cte1 AS o4 ON o3.Boss = o4.Name
            )
            SELECT o0.Name AS "Name",
                   o0.Level AS "Level"
            FROM cte1 AS o0
            ORDER BY o0.Level ASC, o0.Name ASC
            """), N(command.CommandText));
    }

    [Fact]
    public void WithRecursive_WorksForAlice_GeneratesUnionSql()
    {
        using TestDatabase db = new();

        SQLiteCte<Org> org = db.With(() => db.Table<Org>());
        SQLiteCte<WorksFor> cte = db.WithRecursive<WorksFor>(self =>
            (from o in org
                where o.Name == "Alice"
                select new WorksFor
                {
                    Name = o.Name
                })
            .Union(from o in org
                join w in self on o.Boss equals w.Name
                select new WorksFor
                {
                    Name = o.Name
                }));

        SQLiteCommand command = (from w in cte select w).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal("Alice", command.Parameters.Single(p => p.Name == "@p0").Value);
        Assert.Equal(
            N("""
            WITH RECURSIVE cte0 AS (
                    SELECT o2.Name AS "Name",
                   o2.Boss AS "Boss"
                    FROM "Org" AS o2
            ),
            cte1 AS (
                SELECT o1.Name AS "Name"
                FROM cte0 AS o1
                WHERE o1.Name = @p0
                UNION
                    SELECT o3.Name AS "Name"
                FROM cte0 AS o3
                JOIN cte1 AS w1 ON o3.Boss = w1.Name
            )
            SELECT w0.Name AS "Name"
            FROM cte1 AS w0
            """), N(command.CommandText));
    }

    [Fact]
    public void WithRecursive_Sudoku_GeneratesSudokuSql()
    {
        using TestDatabase db = new();

        SQLiteCte<SudInput> input = db.With(() =>
            db.Values(new SudInput
            {
                Sud = "53..7....6..195....98....6.8...6...34..8.3..17...2...6.6....28....419..5....8..79"
            }));

        SQLiteCte<SudDigit> digits = db.WithRecursive<SudDigit>(self =>
            db.Values(new SudDigit
                {
                    Z = "1",
                    Lp = 1
                })
                .Concat(from d in self
                    where d.Lp < 9
                    select new SudDigit
                    {
                        Z = (d.Lp + 1).ToString(),
                        Lp = d.Lp + 1
                    }));

        SQLiteCte<SudX> x = db.WithRecursive<SudX>(self =>
            (from i in input
                select new SudX
                {
                    S = i.Sud,
                    Ind = i.Sud.IndexOf('.') + 1
                })
            .Concat(
                from xr in self
                from z in digits
                where xr.Ind > 0
                      && !(from lp in digits
                          where z.Z == xr.S.Substring(((xr.Ind - 1) / 9) * 9 + lp.Lp - 1, 1)
                                || z.Z == xr.S.Substring((xr.Ind - 1) % 9 + (lp.Lp - 1) * 9, 1)
                                || z.Z == xr.S.Substring(((xr.Ind - 1) / 3) % 3 * 3 + ((xr.Ind - 1) / 27) * 27 + lp.Lp + ((lp.Lp - 1) / 3) * 6 - 1, 1)
                          select lp).Any()
                select new SudX
                {
                    S = xr.S.Substring(0, xr.Ind - 1) + z.Z + xr.S.Substring(xr.Ind),
                    Ind = (xr.S.Substring(0, xr.Ind - 1) + z.Z + xr.S.Substring(xr.Ind)).IndexOf('.') + 1
                }
            ));

        SQLiteCommand command = (from xr in x where xr.Ind == 0 select xr.S).ToSqlCommand();

        Assert.Equal(38, command.Parameters.Count);
        Assert.Equal(
            N("""
            WITH RECURSIVE cte0 AS (
                    SELECT s2."Sud" AS "Sud"
                    FROM (SELECT @p1 AS "Sud") AS s2
            ),
            cte1 AS (
                    SELECT s5."Z" AS "Z",
                   s5."Lp" AS "Lp"
                    FROM (SELECT @p6 AS "Z", @p7 AS "Lp") AS s5
                    UNION ALL
                            SELECT CAST((s6.Lp + @p9) AS TEXT) AS "Z",
                   (s6.Lp + @p10) AS "Lp"
                    FROM cte1 AS s6
                    WHERE s6.Lp < @p8
            ),
            cte2 AS (
                SELECT s1.Sud AS "S",
                   (INSTR(s1.Sud, @p2) - 1 + @p3) AS "Ind"
                FROM cte0 AS s1
                UNION ALL
                    SELECT SUBSTR(s3.S, @p34 + 1, (s3.Ind - @p35)) || s4.Z || SUBSTR(s3.S, s3.Ind + 1) AS "S",
                   (INSTR(SUBSTR(s3.S, @p37 + 1, (s3.Ind - @p38)) || s4.Z || SUBSTR(s3.S, s3.Ind + 1), @p36) - 1 + @p39) AS "Ind"
                FROM cte2 AS s3
                CROSS JOIN cte1 AS s4
                WHERE s3.Ind > @p11 AND NOT EXISTS (
                    SELECT 1
                    FROM cte1 AS s7
                    WHERE s4.Z = SUBSTR(s3.S, (((((s3.Ind - @p12) / @p13) * @p14) + s7.Lp) - @p15) + 1, @p16) OR s4.Z = SUBSTR(s3.S, (((s3.Ind - @p17) % @p18) + ((s7.Lp - @p19) * @p20)) + 1, @p21) OR s4.Z = SUBSTR(s3.S, ((((((((s3.Ind - @p22) / @p23) % @p24) * @p25) + (((s3.Ind - @p26) / @p27) * @p28)) + s7.Lp) + (((s7.Lp - @p29) / @p30) * @p31)) - @p32) + 1, @p33)
            )
            )
            SELECT s0.S AS "S"
            FROM cte2 AS s0
            WHERE s0.Ind = @p40
            """), N(command.CommandText));
    }

    [Fact]
    public void WithRecursive_Sudoku_Execute_SolvesPuzzle()
    {
        using TestDatabase db = new();

        const string puzzle = "53..7....6..195....98....6.8...6...34..8.3..17...2...6.6....28....419..5....8..79";

        SQLiteCte<SudInput> input = db.With(() => db.Values(new SudInput
        {
            Sud = puzzle
        }));

        SQLiteCte<SudDigit> digits = db.WithRecursive<SudDigit>(self =>
            db.Values(new SudDigit
                {
                    Z = "1",
                    Lp = 1
                })
                .Concat(from d in self
                    where d.Lp < 9
                    select new SudDigit
                    {
                        Z = (d.Lp + 1).ToString(),
                        Lp = d.Lp + 1
                    }));

        SQLiteCte<SudX> x = db.WithRecursive<SudX>(self =>
            (from i in input
                select new SudX
                {
                    S = i.Sud,
                    Ind = i.Sud.IndexOf('.') + 1
                })
            .Concat(
                from xr in self
                from z in digits
                where xr.Ind > 0
                      && !(from lp in digits
                          where z.Z == xr.S.Substring(((xr.Ind - 1) / 9) * 9 + lp.Lp - 1, 1)
                                || z.Z == xr.S.Substring((xr.Ind - 1) % 9 + (lp.Lp - 1) * 9, 1)
                                || z.Z == xr.S.Substring(((xr.Ind - 1) / 3) % 3 * 3 + ((xr.Ind - 1) / 27) * 27 + lp.Lp + ((lp.Lp - 1) / 3) * 6 - 1, 1)
                          select lp).Any()
                select new SudX
                {
                    S = xr.S.Substring(0, xr.Ind - 1) + z.Z + xr.S.Substring(xr.Ind),
                    Ind = (xr.S.Substring(0, xr.Ind - 1) + z.Z + xr.S.Substring(xr.Ind)).IndexOf('.') + 1
                }
            ));

        string solution = (from xr in x where xr.Ind == 0 select xr.S).First();

        Assert.Equal(81, solution.Length);
        Assert.DoesNotContain('.', solution);
        for (int i = 0; i < 81; i++)
        {
            if (puzzle[i] != '.')
                Assert.Equal(puzzle[i], solution[i]);
        }
    }

    [Fact]
    public void WithRecursive_TreeTraversal_GeneratesRecursiveQuery()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<TreeNode>();

        SQLiteCte<TreeNode> cte = db.WithRecursive<TreeNode>(self =>
            db.Table<TreeNode>().Where(n => n.ParentId == null)
                .Concat(from n in db.Table<TreeNode>()
                    join p in self on n.ParentId equals p.Id
                    select n));

        List<TreeNode> result = (from n in cte select n).ToList();
        Assert.NotNull(result);
    }

    [Fact]
    public void With_ExecuteQuery_ReturnsResults()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().AddRange([
            new Book
            {
                Id = 1,
                Title = "LINQ in Action",
                AuthorId = 1,
                Price = 29.99
            },
            new Book
            {
                Id = 2,
                Title = "CLR via C#",
                AuthorId = 2,
                Price = 49.99
            }
        ]);

        SQLiteCte<Book> cte = db.With(() => db.Table<Book>().Where(b => b.Price > 30));

        List<Book> results = (from b in cte select b).ToList();

        Assert.Single(results);
        Assert.Equal("CLR via C#", results[0].Title);
    }

    [Fact]
    public void WithRecursive_Execute_ReturnsSequence()
    {
        using TestDatabase db = new();

        SQLiteCte<Cnt> cte = db.WithRecursive<Cnt>(self =>
            db.Values(new Cnt
                {
                    X = 1
                })
                .Concat(from c in self
                    where c.X < 5
                    select new Cnt
                    {
                        X = c.X + 1
                    }));

        List<Cnt> results = (from c in cte select c).ToList();

        Assert.Equal(5, results.Count);
        Assert.Equal([1, 2, 3, 4, 5], results.Select(c => c.X).ToArray());
    }

    [Fact]
    public void With_SameCteUsedTwiceViaCrossJoin_RegisteredOnce()
    {
        using TestDatabase db = new();

        SQLiteCte<Book> cte = db.With(() => db.Table<Book>().Where(b => b.Price > 10));

        SQLiteCommand command = (
            from b1 in cte
            from b2 in cte
            where b1.AuthorId == b2.AuthorId && b1.Id != b2.Id
            select new
            {
                b1.Title,
                Other = b2.Title
            }
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(10.0, command.Parameters[0].Value);
        Assert.Equal(
            N("""
            WITH cte0 AS (
                SELECT b1.BookId AS "Id",
                   b1.BookTitle AS "Title",
                   b1.BookAuthorId AS "AuthorId",
                   b1.BookPrice AS "Price"
                FROM "Books" AS b1
                WHERE b1.BookPrice > @p0
            )
            SELECT b0.Title AS "Title",
                   b2.Title AS "Other"
            FROM cte0 AS b0
            CROSS JOIN cte0 AS b2
            WHERE b0.AuthorId = b2.AuthorId AND b0.Id <> b2.Id
            """), N(command.CommandText));
    }

    [Fact]
    public void With_SameCteUsedTwiceViaJoin_RegisteredOnce()
    {
        using TestDatabase db = new();

        SQLiteCte<Book> cte = db.With(() => db.Table<Book>());

        SQLiteCommand command = (
            from b1 in cte
            join b2 in cte on b1.AuthorId equals b2.AuthorId
            where b1.Id != b2.Id
            select new
            {
                b1.Id,
                b2.Title
            }
        ).ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal(
            N("""
            WITH cte0 AS (
                SELECT b1.BookId AS "Id",
                   b1.BookTitle AS "Title",
                   b1.BookAuthorId AS "AuthorId",
                   b1.BookPrice AS "Price"
                FROM "Books" AS b1
            )
            SELECT b0.Id AS "Id",
                   b2.Title AS "Title"
            FROM cte0 AS b0
            JOIN cte0 AS b2 ON b0.AuthorId = b2.AuthorId
            WHERE b0.Id <> b2.Id
            """), N(command.CommandText));
    }

    [Fact]
    public void With_SameCteUsedInSubquery_RegisteredOnce()
    {
        using TestDatabase db = new();

        SQLiteCte<Book> cte = db.With(() => db.Table<Book>().Where(b => b.Price > 20));

        SQLiteCommand command = (
            from b in cte
            where (from b2 in cte where b2.AuthorId == b.AuthorId select b2).Any()
            select b
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(20.0, command.Parameters[0].Value);
        Assert.Equal(
            N("""
            WITH cte0 AS (
                SELECT b1.BookId AS "Id",
                   b1.BookTitle AS "Title",
                   b1.BookAuthorId AS "AuthorId",
                   b1.BookPrice AS "Price"
                FROM "Books" AS b1
                WHERE b1.BookPrice > @p0
            )
            SELECT b0.Id AS "Id",
                   b0.Title AS "Title",
                   b0.AuthorId AS "AuthorId",
                   b0.Price AS "Price"
            FROM cte0 AS b0
            WHERE EXISTS (
                SELECT 1
                FROM cte0 AS b2
                WHERE b2.AuthorId = b0.AuthorId
            )
            """), N(command.CommandText));
    }

    private class Cnt
    {
        public int X { get; set; }
    }

    private class Fib
    {
        public int A { get; set; }
        public int B { get; set; }
    }

    private class Org
    {
        public required string Name { get; set; }
        public string? Boss { get; set; }
    }

    private class OrgLevel
    {
        public required string Name { get; set; }
        public int Level { get; set; }
    }

    private class WorksFor
    {
        public required string Name { get; set; }
    }

    private class SudInput
    {
        public string Sud { get; set; } = "";
    }

    private class SudDigit
    {
        public string Z { get; set; } = "";
        public int Lp { get; set; }
    }

    private class SudX
    {
        public string S { get; set; } = "";
        public int Ind { get; set; }
    }

    private class TreeNode
    {
        public int Id { get; set; }
        public int? ParentId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}