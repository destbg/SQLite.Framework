using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file enum LogParamColor
{
    Red = 0,
    Green = 1,
    Blue = 2,
}

file sealed class LogParamRow
{
    [Key]
    public int Id { get; set; }

    public LogParamColor Color { get; set; }
}

public class LoggingParameterValueParityTests
{
    [Fact]
    public void EnumParameter_LoggedValueMatchesStoredInteger()
    {
        List<string> lines = new();
        using TestDatabase db = new(b => b.LogCommands(lines.Add).EnableSensitiveParameterLogging());
        db.Table<LogParamRow>().Schema.CreateTable();
        db.Table<LogParamRow>().Add(new LogParamRow { Id = 1, Color = LogParamColor.Blue });

        string insertLine = lines.Last(l => l.Contains("INSERT INTO"));
        string paramSection = insertLine[(insertLine.IndexOf(" | ") + 3)..];

        Assert.Equal("@p0=1 @p1=2", paramSection);
    }

    [Fact]
    public void UnsupportedParameter_FailedCommand_LogsWithoutThrowingFromLogger()
    {
        List<string> lines = new();
        using TestDatabase db = new(b => b.LogCommands(lines.Add).EnableSensitiveParameterLogging());

        Assert.ThrowsAny<Exception>(() =>
            db.ExecuteScalar<int>("SELECT @p", new SQLiteParameter { Name = "@p", Value = new object() }));

        Assert.NotEmpty(lines);
    }
}
