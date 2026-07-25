using SQLite.Framework.Tests.Entities;

namespace SQLite.Framework.Tests;

public class SelectCheckedArithmeticTests : IDisposable
{
    private readonly string databasePath = $"SelectChecked_{Guid.NewGuid():N}.db3";

    public void Dispose()
    {
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }

    private SQLiteDatabase CreateDatabase()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder(databasePath).Build();
        return new SQLiteDatabase(options);
    }

    private static void Seed(SQLiteDatabase db)
    {
        db.Table<Book>().Schema.CreateTable();
        db.Execute("INSERT INTO Books (\"BookId\", \"BookTitle\", \"BookAuthorId\", \"BookPrice\") VALUES (1, 'A', 1, 2.5)");
        db.Execute("INSERT INTO Books (\"BookId\", \"BookTitle\", \"BookAuthorId\", \"BookPrice\") VALUES (2, 'B', 3, 4.5)");
    }

    [Fact]
    public void Select_CheckedAdd_ShouldClientEvalNotThrow()
    {
        using SQLiteDatabase db = CreateDatabase();
        Seed(db);

        List<int> result = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Select(b => checked(b.Id + b.AuthorId))
            .ToList();

        Assert.Equal([2, 5], result);
    }

    [Fact]
    public void Select_CheckedSubtract_ShouldClientEvalNotThrow()
    {
        using SQLiteDatabase db = CreateDatabase();
        Seed(db);

        List<int> result = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Select(b => checked(b.Id - b.AuthorId))
            .ToList();

        Assert.Equal([0, -1], result);
    }

    [Fact]
    public void Select_CheckedMultiply_ShouldClientEvalNotThrow()
    {
        using SQLiteDatabase db = CreateDatabase();
        Seed(db);

        List<int> result = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Select(b => checked(b.Id * b.AuthorId))
            .ToList();

        Assert.Equal([1, 6], result);
    }

    [Fact]
    public void Select_CheckedNegate_ShouldClientEvalNotThrow()
    {
        using SQLiteDatabase db = CreateDatabase();
        Seed(db);

        List<int> result = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Select(b => checked(-b.Id))
            .ToList();

        Assert.Equal([-1, -2], result);
    }

    [Fact]
    public void Select_CheckedAdd_InsideAnonymousType_ShouldClientEvalNotThrow()
    {
        using SQLiteDatabase db = CreateDatabase();
        Seed(db);

        var result = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Select(b => new { b.Title, Sum = checked(b.Id + b.AuthorId) })
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(2, result[0].Sum);
        Assert.Equal(5, result[1].Sum);
    }
}
