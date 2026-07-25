using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class RangeLogRow
{
    [Key]
    public int Id { get; set; }

    public int Value { get; set; }
}

public class RangeWriteLoggingParameterValueParityTests
{
    [Fact]
    public void AddRange_WithSensitiveLogging_LogsParameterValuesLikeSingleAdd()
    {
        List<string> singleLines = new();
        using (TestDatabase single = new(b => b.LogCommands(singleLines.Add).EnableSensitiveParameterLogging()))
        {
            single.Table<RangeLogRow>().Schema.CreateTable();
            single.Table<RangeLogRow>().Add(new RangeLogRow { Id = 1, Value = 11 });
        }

        string singleInsert = singleLines.Last(l => l.Contains("INSERT INTO"));
        bool singleHasParams = singleInsert.Contains(" | ") && singleInsert[(singleInsert.IndexOf(" | ") + 3)..].Trim().Length > 0;
        Assert.True(singleHasParams);

        List<string> rangeLines = new();
        using TestDatabase db = new(b => b.LogCommands(rangeLines.Add).EnableSensitiveParameterLogging());
        db.Table<RangeLogRow>().Schema.CreateTable();
        db.Table<RangeLogRow>().AddRange(new List<RangeLogRow>
        {
            new() { Id = 1, Value = 11 },
            new() { Id = 2, Value = 22 },
        });

        string rangeInsert = rangeLines.Last(l => l.Contains("INSERT INTO"));
        string rangeParams = rangeInsert.Contains(" | ") ? rangeInsert[(rangeInsert.IndexOf(" | ") + 3)..].Trim() : "";

        Assert.True(rangeParams.Length > 0);
    }
}
