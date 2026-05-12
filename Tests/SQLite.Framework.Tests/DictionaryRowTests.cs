using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DictionaryRowTests
{
    [Table("DictRow")]
    private class DictRow
    {
        [Key]
        public int Id { get; set; }
        public required string Name { get; set; }
        public required double Price { get; set; }
    }

    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<DictRow>().Schema.CreateTable();
        db.Table<DictRow>().AddRange([
            new DictRow { Id = 1, Name = "alpha", Price = 1.5 },
            new DictRow { Id = 2, Name = "beta", Price = 2.5 },
        ]);
        return db;
    }

    [Fact]
    public void Query_AsDictionary_ReturnsColumnNameValuePairs()
    {
        using TestDatabase db = SetupDatabase();

        List<Dictionary<string, object?>> rows = db.Query<Dictionary<string, object?>>(
            "SELECT Id, Name, Price FROM DictRow ORDER BY Id");

        Assert.Equal(2, rows.Count);
        Assert.Equal(1L, rows[0]["Id"]);
        Assert.Equal("alpha", rows[0]["Name"]);
        Assert.Equal(1.5, rows[0]["Price"]);
        Assert.Equal(2L, rows[1]["Id"]);
        Assert.Equal("beta", rows[1]["Name"]);
    }

    [Fact]
    public void Query_AsDictionary_NonNullableObject_AlsoWorks()
    {
        using TestDatabase db = SetupDatabase();

        List<Dictionary<string, object>> rows = db.Query<Dictionary<string, object>>(
            "SELECT Id, Name FROM DictRow ORDER BY Id");

        Assert.Equal(2, rows.Count);
        Assert.Equal("alpha", rows[0]["Name"]);
    }

    [Fact]
    public void Query_AsDictionary_NullColumnYieldsNullValue()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE Optional (Id INTEGER PRIMARY KEY, Name TEXT)");
        db.Execute("INSERT INTO Optional (Id, Name) VALUES (1, NULL)");

        Dictionary<string, object?> row = db.Query<Dictionary<string, object?>>(
            "SELECT Id, Name FROM Optional WHERE Id = 1")[0];

        Assert.Null(row["Name"]);
        Assert.Equal(1L, row["Id"]);
    }

    [Fact]
    public void Query_AsDictionary_BlobRoundTripsAsByteArray()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE Blobs (Id INTEGER PRIMARY KEY, Data BLOB)");
        db.Execute("INSERT INTO Blobs (Id, Data) VALUES (1, x'01020304')");

        Dictionary<string, object?> row = db.Query<Dictionary<string, object?>>(
            "SELECT Data FROM Blobs WHERE Id = 1")[0];

        Assert.Equal(new byte[] { 1, 2, 3, 4 }, row["Data"]);
    }

    [Fact]
    public void Command_ExecuteQuery_AsDictionary_StreamsRows()
    {
        using TestDatabase db = SetupDatabase();

        List<Dictionary<string, object?>> rows = db
            .CreateCommand("SELECT Id, Name FROM DictRow ORDER BY Id", [])
            .ExecuteQuery<Dictionary<string, object?>>()
            .ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal("alpha", rows[0]["Name"]);
        Assert.Equal("beta", rows[1]["Name"]);
    }

    [Fact]
    public void Query_AsDictionary_SingleColumn_WithConverter_UsesConverter()
    {
        using TestDatabase db = new(b => b.AddTypeConverter<Dictionary<string, object>>(
            new RecordingDictConverter()));
        db.Execute("CREATE TABLE Docs (Id INTEGER PRIMARY KEY, Payload TEXT)");
        db.Execute("INSERT INTO Docs (Id, Payload) VALUES (1, 'fake-payload')");

        Dictionary<string, object> result = db.Query<Dictionary<string, object>>(
            "SELECT Payload FROM Docs WHERE Id = 1")[0];

        Assert.Equal("converter-was-called", result["marker"]);
    }

    [Fact]
    public void Query_AsDictionary_MultiColumn_WithConverter_StillReturnsRow()
    {
        using TestDatabase db = new(b => b.AddTypeConverter<Dictionary<string, object>>(
            new RecordingDictConverter()));
        db.Execute("CREATE TABLE Docs (Id INTEGER PRIMARY KEY, Payload TEXT)");
        db.Execute("INSERT INTO Docs (Id, Payload) VALUES (1, 'fake-payload')");

        Dictionary<string, object> row = db.Query<Dictionary<string, object>>(
            "SELECT Id, Payload FROM Docs WHERE Id = 1")[0];

        Assert.Equal(1L, row["Id"]);
        Assert.Equal("fake-payload", row["Payload"]);
        Assert.False(row.ContainsKey("marker"));
    }

    private sealed class RecordingDictConverter : ISQLiteTypeConverter
    {
        public SQLiteColumnType ColumnType => SQLiteColumnType.Text;
        public object? ToDatabase(object? value) => value;
        public object? FromDatabase(object? value)
        {
            return new Dictionary<string, object> { ["marker"] = "converter-was-called" };
        }
    }
}
