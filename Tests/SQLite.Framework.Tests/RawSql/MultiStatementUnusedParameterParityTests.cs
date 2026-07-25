using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class UnusedParamRow
{
    [Key]
    public int Id { get; set; }

    public int Value { get; set; }
}

public class MultiStatementUnusedParameterParityTests
{
    [Fact]
    public void UnusedParameter_BehavesConsistentlyAcrossStatementCount()
    {
        using TestDatabase db = new();
        db.Table<UnusedParamRow>().Schema.CreateTable();
        db.Table<UnusedParamRow>().Add(new UnusedParamRow { Id = 1, Value = 0 });

        bool singleThrew = false;
        try
        {
            db.Execute("UPDATE \"UnusedParamRow\" SET \"Value\" = 1", new SQLiteParameter { Name = "@x", Value = 5 });
        }
        catch
        {
            singleThrew = true;
        }

        bool multiThrew = false;
        try
        {
            db.Execute("UPDATE \"UnusedParamRow\" SET \"Value\" = 1; UPDATE \"UnusedParamRow\" SET \"Value\" = 2",
                new SQLiteParameter { Name = "@x", Value = 5 });
        }
        catch
        {
            multiThrew = true;
        }

        Assert.Equal(singleThrew, multiThrew);
    }
}
