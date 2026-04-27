using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CommandHelpersCoverageTests
{
    [Fact]
    public void Read_DateTimeColumn_WithRealValue_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<DateTimeRow>();
        db.CreateCommand("INSERT INTO DateTimeRow (Id, Stamp) VALUES (1, 3.14)", []).ExecuteNonQuery();

        Assert.ThrowsAny<Exception>(() => db.Table<DateTimeRow>().First());
    }

    [Fact]
    public void Read_DateTimeOffsetColumn_WithRealValue_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<DateTimeOffsetRow>();
        db.CreateCommand("INSERT INTO DateTimeOffsetRow (Id, Stamp) VALUES (1, 3.14)", []).ExecuteNonQuery();

        Assert.ThrowsAny<Exception>(() => db.Table<DateTimeOffsetRow>().First());
    }

    [Fact]
    public void Read_TimeSpanColumn_WithRealValue_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<TimeSpanRow>();
        db.CreateCommand("INSERT INTO TimeSpanRow (Id, Span) VALUES (1, 3.14)", []).ExecuteNonQuery();

        Assert.ThrowsAny<Exception>(() => db.Table<TimeSpanRow>().First());
    }

    [Fact]
    public void Read_DateOnlyColumn_WithRealValue_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<DateOnlyRow>();
        db.CreateCommand("INSERT INTO DateOnlyRow (Id, D) VALUES (1, 3.14)", []).ExecuteNonQuery();

        Assert.ThrowsAny<Exception>(() => db.Table<DateOnlyRow>().First());
    }

    [Fact]
    public void Read_TimeOnlyColumn_WithRealValue_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<TimeOnlyRow>();
        db.CreateCommand("INSERT INTO TimeOnlyRow (Id, T) VALUES (1, 3.14)", []).ExecuteNonQuery();

        Assert.ThrowsAny<Exception>(() => db.Table<TimeOnlyRow>().First());
    }

    [Fact]
    public void Read_GuidColumn_WithIntegerValue_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<GuidRow>();
        db.CreateCommand("INSERT INTO GuidRow (Id, Token) VALUES (1, 42)", []).ExecuteNonQuery();

        Assert.ThrowsAny<Exception>(() => db.Table<GuidRow>().First());
    }

    [Fact]
    public void ExecuteQuery_GuidElementType_WithIntegerValue_Throws()
    {
        using TestDatabase db = new();

        Assert.ThrowsAny<Exception>(() =>
            db.CreateCommand("SELECT 42", []).ExecuteQuery<Guid>().ToList());
    }

    [Fact]
    public void BindParameter_UnsupportedType_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<DateTimeRow>();

        SQLiteCommand cmd = db.CreateCommand(
            "INSERT INTO DateTimeRow (Id, Stamp) VALUES (1, @p0)",
            [new SQLiteParameter { Name = "@p0", Value = new System.Numerics.Vector3(1, 2, 3) }]);

        Assert.Throws<NotSupportedException>(() => cmd.ExecuteNonQuery());
    }
}

public class DateTimeRow
{
    [Key]
    public int Id { get; set; }

    public DateTime Stamp { get; set; }
}

public class DateTimeOffsetRow
{
    [Key]
    public int Id { get; set; }

    public DateTimeOffset Stamp { get; set; }
}

public class TimeSpanRow
{
    [Key]
    public int Id { get; set; }

    public TimeSpan Span { get; set; }
}

public class DateOnlyRow
{
    [Key]
    public int Id { get; set; }

    public DateOnly D { get; set; }
}

public class TimeOnlyRow
{
    [Key]
    public int Id { get; set; }

    public TimeOnly T { get; set; }
}

public class GuidRow
{
    [Key]
    public int Id { get; set; }

    public Guid Token { get; set; }
}
