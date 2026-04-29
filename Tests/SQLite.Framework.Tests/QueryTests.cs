using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class QueryTests
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
    public void Query_WithNoParameters_ReturnsAllRows()
    {
        using TestDatabase db = SetupDatabase();

        List<QueryItem> result = db.Query<QueryItem>("SELECT * FROM QueryItems");

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Query_WithAnonymousObject_FiltersRows()
    {
        using TestDatabase db = SetupDatabase();

        List<QueryItem> result = db.Query<QueryItem>(
            "SELECT * FROM QueryItems WHERE Price < @price",
            new { price = 25 }
        );

        Assert.Equal(2, result.Count);
        Assert.All(result, b => Assert.True(b.Price < 25));
    }

    [Fact]
    public void Query_WithMultipleAnonymousParameters_FiltersRows()
    {
        using TestDatabase db = SetupDatabase();

        List<QueryItem> result = db.Query<QueryItem>(
            "SELECT * FROM QueryItems WHERE AuthorId = @authorId AND Price < @price",
            new { authorId = 1, price = 20 }
        );

        Assert.Single(result);
        Assert.Equal("Alpha", result[0].Name);
    }

    [Fact]
    public void Query_WithExplicitParameterList_FiltersRows()
    {
        using TestDatabase db = SetupDatabase();

        List<QueryItem> result = db.Query<QueryItem>(
            "SELECT * FROM QueryItems WHERE Price = @price",
            new List<SQLiteParameter> { new() { Name = "@price", Value = 20d } }
        );

        Assert.Single(result);
        Assert.Equal("Beta", result[0].Name);
    }

    [Fact]
    public void Query_WithSingleExplicitParameter_FiltersRows()
    {
        using TestDatabase db = SetupDatabase();

        List<QueryItem> result = db.Query<QueryItem>(
            "SELECT * FROM QueryItems WHERE Id = @id",
            new SQLiteParameter { Name = "@id", Value = 2 }
        );

        Assert.Single(result);
        Assert.Equal("Beta", result[0].Name);
    }

    [Fact]
    public void QueryFirst_ReturnsFirstRow()
    {
        using TestDatabase db = SetupDatabase();

        QueryItem result = db.QueryFirst<QueryItem>(
            "SELECT * FROM QueryItems WHERE AuthorId = @authorId ORDER BY Price",
            new { authorId = 1 }
        );

        Assert.Equal("Alpha", result.Name);
    }

    [Fact]
    public void QueryFirst_WhenNoRows_Throws()
    {
        using TestDatabase db = SetupDatabase();

        Assert.Throws<InvalidOperationException>(() =>
            db.QueryFirst<QueryItem>("SELECT * FROM QueryItems WHERE Id = @id", new { id = 999 })
        );
    }

    [Fact]
    public void QueryFirstOrDefault_ReturnsFirstRow()
    {
        using TestDatabase db = SetupDatabase();

        QueryItem? result = db.QueryFirstOrDefault<QueryItem>(
            "SELECT * FROM QueryItems WHERE Id = @id",
            new { id = 2 }
        );

        Assert.NotNull(result);
        Assert.Equal("Beta", result.Name);
    }

    [Fact]
    public void QueryFirstOrDefault_WhenNoRows_ReturnsNull()
    {
        using TestDatabase db = SetupDatabase();

        QueryItem? result = db.QueryFirstOrDefault<QueryItem>(
            "SELECT * FROM QueryItems WHERE Id = @id",
            new { id = 999 }
        );

        Assert.Null(result);
    }

    [Fact]
    public void QuerySingle_ReturnsSingleRow()
    {
        using TestDatabase db = SetupDatabase();

        QueryItem result = db.QuerySingle<QueryItem>(
            "SELECT * FROM QueryItems WHERE Id = @id",
            new { id = 1 }
        );

        Assert.Equal("Alpha", result.Name);
    }

    [Fact]
    public void QuerySingle_WhenNoRows_Throws()
    {
        using TestDatabase db = SetupDatabase();

        Assert.Throws<InvalidOperationException>(() =>
            db.QuerySingle<QueryItem>("SELECT * FROM QueryItems WHERE Id = @id", new { id = 999 })
        );
    }

    [Fact]
    public void QuerySingle_WhenMultipleRows_Throws()
    {
        using TestDatabase db = SetupDatabase();

        Assert.Throws<InvalidOperationException>(() =>
            db.QuerySingle<QueryItem>("SELECT * FROM QueryItems")
        );
    }

    [Fact]
    public void QuerySingleOrDefault_ReturnsSingleRow()
    {
        using TestDatabase db = SetupDatabase();

        QueryItem? result = db.QuerySingleOrDefault<QueryItem>(
            "SELECT * FROM QueryItems WHERE Id = @id",
            new { id = 3 }
        );

        Assert.NotNull(result);
        Assert.Equal("Gamma", result.Name);
    }

    [Fact]
    public void QuerySingleOrDefault_WhenNoRows_ReturnsNull()
    {
        using TestDatabase db = SetupDatabase();

        QueryItem? result = db.QuerySingleOrDefault<QueryItem>(
            "SELECT * FROM QueryItems WHERE Id = @id",
            new { id = 999 }
        );

        Assert.Null(result);
    }

    [Fact]
    public void QuerySingleOrDefault_WhenMultipleRows_Throws()
    {
        using TestDatabase db = SetupDatabase();

        Assert.Throws<InvalidOperationException>(() =>
            db.QuerySingleOrDefault<QueryItem>("SELECT * FROM QueryItems")
        );
    }

    [Fact]
    public void ExecuteScalar_ReturnsScalarValue()
    {
        using TestDatabase db = SetupDatabase();

        int count = db.ExecuteScalar<int>("SELECT COUNT(*) FROM QueryItems");

        Assert.Equal(3, count);
    }

    [Fact]
    public void ExecuteScalar_WithParameter_ReturnsFilteredScalar()
    {
        using TestDatabase db = SetupDatabase();

        double max = db.ExecuteScalar<double>(
            "SELECT MAX(Price) FROM QueryItems WHERE AuthorId = @authorId",
            new { authorId = 1 }
        );

        Assert.Equal(20, max);
    }

    [Fact]
    public void ExecuteScalar_WhenNoRows_ReturnsNull()
    {
        using TestDatabase db = SetupDatabase();

        string? result = db.ExecuteScalar<string>(
            "SELECT Name FROM QueryItems WHERE Id = @id",
            new { id = 999 }
        );

        Assert.Null(result);
    }

    [Fact]
    public void Execute_InsertsRow()
    {
        using TestDatabase db = SetupDatabase();

        int affected = db.Execute(
            "INSERT INTO QueryItems (Id, Name, AuthorId, Price) VALUES (@id, @name, @authorId, @price)",
            new { id = 4, name = "Delta", authorId = 2, price = 40.0 }
        );

        Assert.Equal(1, affected);
        Assert.Equal(4, db.Query<QueryItem>("SELECT * FROM QueryItems").Count);
    }

    [Fact]
    public void Execute_DeletesRows()
    {
        using TestDatabase db = SetupDatabase();

        int affected = db.Execute(
            "DELETE FROM QueryItems WHERE AuthorId = @authorId",
            new { authorId = 1 }
        );

        Assert.Equal(2, affected);
        Assert.Single(db.Query<QueryItem>("SELECT * FROM QueryItems"));
    }

    [Fact]
    public void Execute_UpdatesRow()
    {
        using TestDatabase db = SetupDatabase();

        int affected = db.Execute(
            "UPDATE QueryItems SET Name = @name WHERE Id = @id",
            new { name = "Updated", id = 1 }
        );

        Assert.Equal(1, affected);

        QueryItem? updated = db.QueryFirstOrDefault<QueryItem>(
            "SELECT * FROM QueryItems WHERE Id = @id",
            new { id = 1 }
        );

        Assert.Equal("Updated", updated!.Name);
    }

    [Fact]
    public void Execute_WithExplicitParameters_UpdatesRow()
    {
        using TestDatabase db = SetupDatabase();

        int affected = db.Execute(
            "UPDATE QueryItems SET Name = @name WHERE Id = @id",
            new SQLiteParameter { Name = "@name", Value = "Changed" },
            new SQLiteParameter { Name = "@id", Value = 2 }
        );

        Assert.Equal(1, affected);

        QueryItem? updated = db.QueryFirstOrDefault<QueryItem>(
            "SELECT * FROM QueryItems WHERE Id = @id",
            new { id = 2 }
        );

        Assert.Equal("Changed", updated!.Name);
    }

    [Fact]
    public void Query_WithPartialSelect_MappedColumnsAreSet()
    {
        using TestDatabase db = SetupDatabase();

        List<QueryItem> result = db.Query<QueryItem>(
            "SELECT Id, Name FROM QueryItems ORDER BY Id"
        );

        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("Alpha", result[0].Name);
    }

    [Fact]
    public void Query_WithPartialSelect_UnmappedPropertiesAreDefault()
    {
        using TestDatabase db = SetupDatabase();

        List<QueryItem> result = db.Query<QueryItem>(
            "SELECT Id, Name FROM QueryItems ORDER BY Id"
        );

        Assert.All(result, item =>
        {
            Assert.Equal(0, item.Price);
            Assert.Equal(0, item.AuthorId);
        });
    }

    [Fact]
    public void QueryFirst_WithPartialSelect_MappedColumnsAreSet()
    {
        using TestDatabase db = SetupDatabase();

        QueryItem result = db.QueryFirst<QueryItem>(
            "SELECT Name, Price FROM QueryItems WHERE AuthorId = @authorId ORDER BY Price",
            new { authorId = 1 }
        );

        Assert.Equal("Alpha", result.Name);
        Assert.Equal(10, result.Price);
    }

    [Fact]
    public void QueryFirst_WithPartialSelect_UnmappedPropertiesAreDefault()
    {
        using TestDatabase db = SetupDatabase();

        QueryItem result = db.QueryFirst<QueryItem>(
            "SELECT Name, Price FROM QueryItems WHERE AuthorId = @authorId ORDER BY Price",
            new { authorId = 1 }
        );

        Assert.Equal(0, result.Id);
        Assert.Equal(0, result.AuthorId);
    }

    [Fact]
    public void QuerySingleOrDefault_WithPartialSelect_MappedColumnsAreSet()
    {
        using TestDatabase db = SetupDatabase();

        QueryItem? result = db.QuerySingleOrDefault<QueryItem>(
            "SELECT Id FROM QueryItems WHERE Id = @id",
            new { id = 3 }
        );

        Assert.NotNull(result);
        Assert.Equal(3, result.Id);
    }

    [Fact]
    public void QuerySingleOrDefault_WithPartialSelect_UnmappedPropertiesAreDefault()
    {
        using TestDatabase db = SetupDatabase();

        QueryItem? result = db.QuerySingleOrDefault<QueryItem>(
            "SELECT Id FROM QueryItems WHERE Id = @id",
            new { id = 3 }
        );

        Assert.NotNull(result);
        Assert.Null(result.Name);
        Assert.Equal(0, result.Price);
        Assert.Equal(0, result.AuthorId);
    }

    [Fact]
    public void Linq_Single_WhenMultipleRows_Throws()
    {
        using TestDatabase db = SetupDatabase();

        Assert.Throws<InvalidOperationException>(() =>
            db.Table<QueryItem>().Single()
        );
    }

    [Fact]
    public void Linq_Single_WhenNoRows_Throws()
    {
        using TestDatabase db = SetupDatabase();

        Assert.Throws<InvalidOperationException>(() =>
            db.Table<QueryItem>().Where(q => q.Id == 999).Single()
        );
    }

    [Fact]
    public void Linq_First_WhenNoRows_Throws()
    {
        using TestDatabase db = SetupDatabase();

        Assert.Throws<InvalidOperationException>(() =>
            db.Table<QueryItem>().Where(q => q.Id == 999).First()
        );
    }

    [Fact]
    public void Linq_FirstOrDefault_WhenNoRows_ReturnsNull()
    {
        using TestDatabase db = SetupDatabase();

        QueryItem? result = db.Table<QueryItem>()
            .Where(q => q.Id == 999)
            .FirstOrDefault();

        Assert.Null(result);
    }

    [Fact]
    public void Linq_SingleOrDefault_WhenMultipleRows_Throws()
    {
        using TestDatabase db = SetupDatabase();

        Assert.Throws<InvalidOperationException>(() =>
            db.Table<QueryItem>().SingleOrDefault()
        );
    }

    [Fact]
    public void Linq_SingleOrDefault_WhenNoRows_ReturnsNull()
    {
        using TestDatabase db = SetupDatabase();

        QueryItem? result = db.Table<QueryItem>()
            .Where(q => q.Id == 999)
            .SingleOrDefault();

        Assert.Null(result);
    }

    [Fact]
    public void FromSql_WithNullSql_ThrowsArgumentException()
    {
        using TestDatabase db = SetupDatabase();

        Assert.Throws<ArgumentException>(() =>
            db.FromSql<QueryItem>(null!)
        );
    }

    [Fact]
    public void FromSql_WithEmptySql_ThrowsArgumentException()
    {
        using TestDatabase db = SetupDatabase();

        Assert.Throws<ArgumentException>(() =>
            db.FromSql<QueryItem>("")
        );
    }

    [Fact]
    public void FromSql_WithWhitespaceSql_ThrowsArgumentException()
    {
        using TestDatabase db = SetupDatabase();

        Assert.Throws<ArgumentException>(() =>
            db.FromSql<QueryItem>("   ")
        );
    }

    [Fact]
    public void CommandCreated_EventFires()
    {
        using TestDatabase db = SetupDatabase();
        SQLiteCommand? captured = null;
        db.CommandCreated += cmd => captured = cmd;

        db.Query<QueryItem>("SELECT * FROM QueryItems");

        Assert.NotNull(captured);
        Assert.Contains("SELECT", captured.CommandText);
    }

    [Fact]
    public void ExecuteScalar_WithExplicitParameters_ReturnsValue()
    {
        using TestDatabase db = SetupDatabase();

        double result = db.ExecuteScalar<double>(
            "SELECT Price FROM QueryItems WHERE Id = @id",
            new SQLiteParameter { Name = "@id", Value = 1 }
        );

        Assert.Equal(10.0, result);
    }

    [Fact]
    public void ExecuteScalar_WhenNoRows_ReturnsDefault()
    {
        using TestDatabase db = SetupDatabase();

        int result = db.ExecuteScalar<int>(
            "SELECT Id FROM QueryItems WHERE Id = @id",
            new SQLiteParameter { Name = "@id", Value = 999 }
        );

        Assert.Equal(0, result);
    }

    [Fact]
    public void Query_ReturnsEmptyListWhenNoMatch()
    {
        using TestDatabase db = SetupDatabase();

        List<QueryItem> result = db.Query<QueryItem>(
            "SELECT * FROM QueryItems WHERE Id = @id",
            new { id = 999 }
        );

        Assert.Empty(result);
    }

    [Fact]
    public void QueryFirst_WithExplicitParams_ReturnsFirstRow()
    {
        using TestDatabase db = SetupDatabase();

        QueryItem result = db.QueryFirst<QueryItem>(
            "SELECT * FROM QueryItems WHERE Id = @id",
            new SQLiteParameter { Name = "@id", Value = 1 }
        );

        Assert.Equal("Alpha", result.Name);
    }

    [Fact]
    public void QueryFirst_WithExplicitParams_WhenNoRows_Throws()
    {
        using TestDatabase db = SetupDatabase();

        Assert.Throws<InvalidOperationException>(() =>
            db.QueryFirst<QueryItem>(
                "SELECT * FROM QueryItems WHERE Id = @id",
                new SQLiteParameter { Name = "@id", Value = 999 }
            )
        );
    }

    [Fact]
    public void QueryFirstOrDefault_WithExplicitParams_ReturnsFirstRow()
    {
        using TestDatabase db = SetupDatabase();

        QueryItem? result = db.QueryFirstOrDefault<QueryItem>(
            "SELECT * FROM QueryItems WHERE Id = @id",
            new SQLiteParameter { Name = "@id", Value = 2 }
        );

        Assert.NotNull(result);
        Assert.Equal("Beta", result.Name);
    }

    [Fact]
    public void QueryFirstOrDefault_WithExplicitParams_WhenNoRows_ReturnsNull()
    {
        using TestDatabase db = SetupDatabase();

        QueryItem? result = db.QueryFirstOrDefault<QueryItem>(
            "SELECT * FROM QueryItems WHERE Id = @id",
            new SQLiteParameter { Name = "@id", Value = 999 }
        );

        Assert.Null(result);
    }

    [Fact]
    public void QuerySingle_WithExplicitParams_ReturnsSingleRow()
    {
        using TestDatabase db = SetupDatabase();

        QueryItem result = db.QuerySingle<QueryItem>(
            "SELECT * FROM QueryItems WHERE Id = @id",
            new SQLiteParameter { Name = "@id", Value = 3 }
        );

        Assert.Equal("Gamma", result.Name);
    }

    [Fact]
    public void QuerySingle_WithExplicitParams_WhenNoRows_Throws()
    {
        using TestDatabase db = SetupDatabase();

        Assert.Throws<InvalidOperationException>(() =>
            db.QuerySingle<QueryItem>(
                "SELECT * FROM QueryItems WHERE Id = @id",
                new SQLiteParameter { Name = "@id", Value = 999 }
            )
        );
    }

    [Fact]
    public void QuerySingle_WithExplicitParams_WhenMultipleRows_Throws()
    {
        using TestDatabase db = SetupDatabase();

        Assert.Throws<InvalidOperationException>(() =>
            db.QuerySingle<QueryItem>(
                "SELECT * FROM QueryItems WHERE AuthorId = @authorId",
                new SQLiteParameter { Name = "@authorId", Value = 1 }
            )
        );
    }

    [Fact]
    public void QuerySingleOrDefault_WithExplicitParams_ReturnsSingleRow()
    {
        using TestDatabase db = SetupDatabase();

        QueryItem? result = db.QuerySingleOrDefault<QueryItem>(
            "SELECT * FROM QueryItems WHERE Id = @id",
            new SQLiteParameter { Name = "@id", Value = 1 }
        );

        Assert.NotNull(result);
        Assert.Equal("Alpha", result.Name);
    }

    [Fact]
    public void QuerySingleOrDefault_WithExplicitParams_WhenNoRows_ReturnsNull()
    {
        using TestDatabase db = SetupDatabase();

        QueryItem? result = db.QuerySingleOrDefault<QueryItem>(
            "SELECT * FROM QueryItems WHERE Id = @id",
            new SQLiteParameter { Name = "@id", Value = 999 }
        );

        Assert.Null(result);
    }

    [Fact]
    public void QuerySingleOrDefault_WithExplicitParams_WhenMultipleRows_Throws()
    {
        using TestDatabase db = SetupDatabase();

        Assert.Throws<InvalidOperationException>(() =>
            db.QuerySingleOrDefault<QueryItem>(
                "SELECT * FROM QueryItems WHERE AuthorId = @authorId",
                new SQLiteParameter { Name = "@authorId", Value = 1 }
            )
        );
    }

    [Fact]
    public void Query_WithExplicitParamsArray_ReturnsResults()
    {
        using TestDatabase db = SetupDatabase();

        List<QueryItem> result = db.Query<QueryItem>(
            "SELECT * FROM QueryItems WHERE AuthorId = @authorId AND Price > @price",
            new SQLiteParameter { Name = "@authorId", Value = 1 },
            new SQLiteParameter { Name = "@price", Value = 15.0 }
        );

        Assert.Single(result);
        Assert.Equal("Beta", result[0].Name);
    }

    [Fact]
    public void ExecuteScalar_WithAnonymousObject_ReturnsValue()
    {
        using TestDatabase db = SetupDatabase();

        int count = db.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM QueryItems WHERE AuthorId = @authorId",
            new { authorId = 2 }
        );

        Assert.Equal(1, count);
    }

    [Fact]
    public void Linq_Select_Scalar_ReturnsCorrectValues()
    {
        using TestDatabase db = SetupDatabase();

        List<double> prices = db.Table<QueryItem>()
            .OrderBy(q => q.Id)
            .Select(q => q.Price)
            .ToList();

        Assert.Equal([10, 20, 30], prices);
    }

    [Fact]
    public void Linq_Count_ReturnsCorrectCount()
    {
        using TestDatabase db = SetupDatabase();

        int count = db.Table<QueryItem>().Count();

        Assert.Equal(3, count);
    }

    [Fact]
    public void Linq_Any_WithMatch_ReturnsTrue()
    {
        using TestDatabase db = SetupDatabase();

        bool exists = db.Table<QueryItem>()
            .Where(q => q.Name == "Alpha")
            .Any();

        Assert.True(exists);
    }

    [Fact]
    public void Linq_Any_WithNoMatch_ReturnsFalse()
    {
        using TestDatabase db = SetupDatabase();

        bool exists = db.Table<QueryItem>()
            .Where(q => q.Name == "Nobody")
            .Any();

        Assert.False(exists);
    }
}
