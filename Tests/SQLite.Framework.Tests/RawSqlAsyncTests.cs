using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class RawSqlAsyncTests
{
    [Table("QueryItems")]
    private class QueryItem
    {
        [Key]
        public required int Id { get; set; }
        public string? Name { get; set; }
        public required double Price { get; set; }
        public required int AuthorId { get; set; }
    }

    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<QueryItem>().Schema.CreateTable();
        db.Table<QueryItem>().AddRange(new[]
        {
            new QueryItem { Id = 1, Name = "Alpha", Price = 10, AuthorId = 1 },
            new QueryItem { Id = 2, Name = "Beta",  Price = 20, AuthorId = 1 },
            new QueryItem { Id = 3, Name = "Gamma", Price = 30, AuthorId = 2 },
        });
        return db;
    }

    [Fact]
    public async Task QueryAsync_WithParameterArray_ReturnsAllRows()
    {
        using TestDatabase db = SetupDatabase();

        List<QueryItem> result = await db.QueryAsync<QueryItem>(
            "SELECT * FROM QueryItems",
            Array.Empty<SQLiteParameter>()
        );

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task QueryAsync_WithParameterArray_FiltersRows()
    {
        using TestDatabase db = SetupDatabase();

        List<QueryItem> result = await db.QueryAsync<QueryItem>(
            "SELECT * FROM QueryItems WHERE Price >= @price ORDER BY Id",
            new[] { new SQLiteParameter { Name = "@price", Value = 20d } }
        );

        Assert.Equal(2, result.Count);
        Assert.Equal("Beta", result[0].Name);
        Assert.Equal("Gamma", result[1].Name);
    }

    [Fact]
    public async Task QueryAsync_WithAnonymousObject_FiltersRows()
    {
        using TestDatabase db = SetupDatabase();

        List<QueryItem> result = await db.QueryAsync<QueryItem>(
            "SELECT * FROM QueryItems WHERE AuthorId = @authorId ORDER BY Id",
            new { authorId = 1 }
        );

        Assert.Equal(2, result.Count);
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("Beta", result[1].Name);
    }

    [Fact]
    public async Task QueryFirstAsync_WithParameterArray_ReturnsRow()
    {
        using TestDatabase db = SetupDatabase();

        QueryItem result = await db.QueryFirstAsync<QueryItem>(
            "SELECT * FROM QueryItems WHERE Id = @id",
            new[] { new SQLiteParameter { Name = "@id", Value = 2 } }
        );

        Assert.Equal(2, result.Id);
        Assert.Equal("Beta", result.Name);
    }

    [Fact]
    public async Task QueryFirstAsync_WithAnonymousObject_ReturnsRow()
    {
        using TestDatabase db = SetupDatabase();

        QueryItem result = await db.QueryFirstAsync<QueryItem>(
            "SELECT * FROM QueryItems WHERE Id = @id",
            new { id = 1 }
        );

        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task QueryFirstAsync_WithParameterArray_NoRows_Throws()
    {
        using TestDatabase db = SetupDatabase();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await db.QueryFirstAsync<QueryItem>(
                "SELECT * FROM QueryItems WHERE Id = @id",
                new[] { new SQLiteParameter { Name = "@id", Value = 999 } })
        );
    }

    [Fact]
    public async Task QueryFirstAsync_WithAnonymousObject_NoRows_Throws()
    {
        using TestDatabase db = SetupDatabase();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await db.QueryFirstAsync<QueryItem>(
                "SELECT * FROM QueryItems WHERE Id = @id",
                new { id = 999 })
        );
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_WithParameterArray_ReturnsRow()
    {
        using TestDatabase db = SetupDatabase();

        QueryItem? result = await db.QueryFirstOrDefaultAsync<QueryItem>(
            "SELECT * FROM QueryItems WHERE Id = @id",
            new[] { new SQLiteParameter { Name = "@id", Value = 1 } }
        );

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_WithParameterArray_NoRows_ReturnsNull()
    {
        using TestDatabase db = SetupDatabase();

        QueryItem? result = await db.QueryFirstOrDefaultAsync<QueryItem>(
            "SELECT * FROM QueryItems WHERE Id = @id",
            new[] { new SQLiteParameter { Name = "@id", Value = 999 } }
        );

        Assert.Null(result);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_WithAnonymousObject_NoRows_ReturnsNull()
    {
        using TestDatabase db = SetupDatabase();

        QueryItem? result = await db.QueryFirstOrDefaultAsync<QueryItem>(
            "SELECT * FROM QueryItems WHERE Id = @id",
            new { id = 999 }
        );

        Assert.Null(result);
    }

    [Fact]
    public async Task QuerySingleAsync_WithParameterArray_ReturnsRow()
    {
        using TestDatabase db = SetupDatabase();

        QueryItem result = await db.QuerySingleAsync<QueryItem>(
            "SELECT * FROM QueryItems WHERE Id = @id",
            new[] { new SQLiteParameter { Name = "@id", Value = 2 } }
        );

        Assert.Equal(2, result.Id);
    }

    [Fact]
    public async Task QuerySingleAsync_WithAnonymousObject_ReturnsRow()
    {
        using TestDatabase db = SetupDatabase();

        QueryItem result = await db.QuerySingleAsync<QueryItem>(
            "SELECT * FROM QueryItems WHERE Id = @id",
            new { id = 3 }
        );

        Assert.Equal(3, result.Id);
    }

    [Fact]
    public async Task QuerySingleAsync_WithParameterArray_NoRows_Throws()
    {
        using TestDatabase db = SetupDatabase();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await db.QuerySingleAsync<QueryItem>(
                "SELECT * FROM QueryItems WHERE Id = @id",
                new[] { new SQLiteParameter { Name = "@id", Value = 999 } })
        );
    }

    [Fact]
    public async Task QuerySingleAsync_WithAnonymousObject_MultipleRows_Throws()
    {
        using TestDatabase db = SetupDatabase();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await db.QuerySingleAsync<QueryItem>(
                "SELECT * FROM QueryItems WHERE AuthorId = @authorId",
                new { authorId = 1 })
        );
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_WithParameterArray_ReturnsRow()
    {
        using TestDatabase db = SetupDatabase();

        QueryItem? result = await db.QuerySingleOrDefaultAsync<QueryItem>(
            "SELECT * FROM QueryItems WHERE Id = @id",
            new[] { new SQLiteParameter { Name = "@id", Value = 1 } }
        );

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_WithParameterArray_NoRows_ReturnsNull()
    {
        using TestDatabase db = SetupDatabase();

        QueryItem? result = await db.QuerySingleOrDefaultAsync<QueryItem>(
            "SELECT * FROM QueryItems WHERE Id = @id",
            new[] { new SQLiteParameter { Name = "@id", Value = 999 } }
        );

        Assert.Null(result);
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_WithAnonymousObject_NoRows_ReturnsNull()
    {
        using TestDatabase db = SetupDatabase();

        QueryItem? result = await db.QuerySingleOrDefaultAsync<QueryItem>(
            "SELECT * FROM QueryItems WHERE Id = @id",
            new { id = 999 }
        );

        Assert.Null(result);
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_WithAnonymousObject_MultipleRows_Throws()
    {
        using TestDatabase db = SetupDatabase();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await db.QuerySingleOrDefaultAsync<QueryItem>(
                "SELECT * FROM QueryItems WHERE AuthorId = @authorId",
                new { authorId = 1 })
        );
    }

    [Fact]
    public async Task ExecuteScalarAsync_WithParameterArray_ReturnsValue()
    {
        using TestDatabase db = SetupDatabase();

        int count = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM QueryItems",
            Array.Empty<SQLiteParameter>()
        );

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task ExecuteScalarAsync_WithAnonymousObject_ReturnsValue()
    {
        using TestDatabase db = SetupDatabase();

        double max = await db.ExecuteScalarAsync<double>(
            "SELECT MAX(Price) FROM QueryItems WHERE AuthorId = @authorId",
            new { authorId = 1 }
        );

        Assert.Equal(20d, max);
    }

    [Fact]
    public async Task ExecuteScalarAsync_NoRows_ReturnsDefault()
    {
        using TestDatabase db = SetupDatabase();

        string? result = await db.ExecuteScalarAsync<string>(
            "SELECT Name FROM QueryItems WHERE Id = @id",
            new { id = 999 }
        );

        Assert.Null(result);
    }

    [Fact]
    public async Task ExecuteAsync_WithParameterArray_InsertsRows()
    {
        using TestDatabase db = SetupDatabase();

        int affected = await db.ExecuteAsync(
            "INSERT INTO QueryItems (Id, Name, Price, AuthorId) VALUES (@id, @name, @price, @authorId)",
            new[]
            {
                new SQLiteParameter { Name = "@id", Value = 4 },
                new SQLiteParameter { Name = "@name", Value = "Delta" },
                new SQLiteParameter { Name = "@price", Value = 40d },
                new SQLiteParameter { Name = "@authorId", Value = 3 },
            }
        );

        Assert.Equal(1, affected);
        Assert.Equal(4, db.Table<QueryItem>().Count());
    }

    [Fact]
    public async Task ExecuteAsync_WithAnonymousObject_DeletesRows()
    {
        using TestDatabase db = SetupDatabase();

        int affected = await db.ExecuteAsync(
            "DELETE FROM QueryItems WHERE AuthorId = @authorId",
            new { authorId = 1 }
        );

        Assert.Equal(2, affected);
        Assert.Equal(1, db.Table<QueryItem>().Count());
    }

    [Fact]
    public async Task ExecuteAsync_CancelledToken_Throws()
    {
        using TestDatabase db = SetupDatabase();

        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await db.ExecuteAsync("DELETE FROM QueryItems", Array.Empty<SQLiteParameter>(), cts.Token));
    }

    [Fact]
    public async Task QueryAsync_CancelledToken_Throws()
    {
        using TestDatabase db = SetupDatabase();

        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await db.QueryAsync<QueryItem>("SELECT * FROM QueryItems", Array.Empty<SQLiteParameter>(), cts.Token));
    }
}
