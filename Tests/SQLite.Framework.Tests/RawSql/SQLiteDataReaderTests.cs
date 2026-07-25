using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SQLiteDataReaderTests
{
    [Fact]
    public void FieldCount_ReturnsNumberOfColumnsInSelect()
    {
        using TestDatabase db = new();

        SQLiteCommand cmd = db.CreateCommand("SELECT 1, 2, 3", []);
        using SQLiteDataReader reader = cmd.ExecuteReader();

        Assert.Equal(3, reader.FieldCount);
    }

    [Fact]
    public void FieldCount_AfterRead_StaysTheSame()
    {
        using TestDatabase db = new();

        SQLiteCommand cmd = db.CreateCommand("SELECT 'a' AS one, 'b' AS two", []);
        using SQLiteDataReader reader = cmd.ExecuteReader();
        Assert.True(reader.Read());

        Assert.Equal(2, reader.FieldCount);
    }

    [Fact]
    public void FieldCount_AfterReadingPastLastRow_StaysTheSame()
    {
        using TestDatabase db = new();

        SQLiteCommand cmd = db.CreateCommand("SELECT 1, 2", []);
        using SQLiteDataReader reader = cmd.ExecuteReader();
        Assert.True(reader.Read());
        Assert.False(reader.Read());

        Assert.Equal(2, reader.FieldCount);
    }

    [Fact]
    public void FieldCount_NoRowsReturned_StillReturnsColumnCount()
    {
        using TestDatabase db = new();
        db.CreateCommand("CREATE TABLE empty_t (a INTEGER, b TEXT)", []).ExecuteNonQuery();

        SQLiteCommand cmd = db.CreateCommand("SELECT a, b FROM empty_t", []);
        using SQLiteDataReader reader = cmd.ExecuteReader();

        Assert.Equal(2, reader.FieldCount);
        Assert.False(reader.Read());
        Assert.Equal(2, reader.FieldCount);
    }

    [Fact]
    public void GetName_ReturnsLiteralColumnNameForBareSelect()
    {
        using TestDatabase db = new();
        db.CreateCommand("CREATE TABLE r (Id INTEGER, Title TEXT)", []).ExecuteNonQuery();

        SQLiteCommand cmd = db.CreateCommand("SELECT Id, Title FROM r", []);
        using SQLiteDataReader reader = cmd.ExecuteReader();

        Assert.Equal("Id", reader.GetName(0));
        Assert.Equal("Title", reader.GetName(1));
    }

    [Fact]
    public void GetName_ReturnsAliasWhenColumnIsAliased()
    {
        using TestDatabase db = new();

        SQLiteCommand cmd = db.CreateCommand("SELECT 1 AS \"first\", 2 AS \"second\"", []);
        using SQLiteDataReader reader = cmd.ExecuteReader();

        Assert.Equal("first", reader.GetName(0));
        Assert.Equal("second", reader.GetName(1));
    }

    [Fact]
    public void GetName_WorksBeforeFirstRead()
    {
        using TestDatabase db = new();

        SQLiteCommand cmd = db.CreateCommand("SELECT 1 AS only_column", []);
        using SQLiteDataReader reader = cmd.ExecuteReader();

        Assert.Equal("only_column", reader.GetName(0));
    }

    [Fact]
    public void GetName_QualifiedColumnName_StripsQualifier()
    {
        using TestDatabase db = new();
        db.CreateCommand("CREATE TABLE qt (col INTEGER)", []).ExecuteNonQuery();

        SQLiteCommand cmd = db.CreateCommand("SELECT qt.col FROM qt", []);
        using SQLiteDataReader reader = cmd.ExecuteReader();

        Assert.Equal("col", reader.GetName(0));
    }

    [Fact]
    public void GetName_ExpressionWithoutAlias_ReturnsExpressionText()
    {
        using TestDatabase db = new();

        SQLiteCommand cmd = db.CreateCommand("SELECT 1 + 2", []);
        using SQLiteDataReader reader = cmd.ExecuteReader();

        Assert.Equal("1 + 2", reader.GetName(0));
    }

    [Fact]
    public void FieldCountAndGetName_Together_DescribeRowShape()
    {
        using TestDatabase db = new();
        db.CreateCommand("CREATE TABLE shape_t (a INTEGER, b TEXT, c REAL)", []).ExecuteNonQuery();
        db.CreateCommand("INSERT INTO shape_t VALUES (1, 'two', 3.0)", []).ExecuteNonQuery();

        SQLiteCommand cmd = db.CreateCommand("SELECT a, b, c FROM shape_t", []);
        using SQLiteDataReader reader = cmd.ExecuteReader();
        Assert.True(reader.Read());

        string[] names = new string[reader.FieldCount];
        for (int i = 0; i < reader.FieldCount; i++)
        {
            names[i] = reader.GetName(i);
        }

        Assert.Equal(3, reader.FieldCount);
        Assert.Equal(["a", "b", "c"], names);
    }

    [Fact]
    public void FieldCount_NonQueryStatement_IsZero()
    {
        using TestDatabase db = new();

        SQLiteCommand cmd = db.CreateCommand("CREATE TABLE noop (id INTEGER)", []);
        using SQLiteDataReader reader = cmd.ExecuteReader();

        Assert.Equal(0, reader.FieldCount);
        Assert.False(reader.Read());
    }
}
