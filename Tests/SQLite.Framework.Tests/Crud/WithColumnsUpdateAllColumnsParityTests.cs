using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class WcAllOverrideRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public int Value { get; set; }
}

public class WithColumnsUpdateAllColumnsParityTests
{
    [Fact]
    public void WithColumns_Update_OverridesEveryNonKeyColumn_UpdatesRow()
    {
        using TestDatabase db = new();
        db.Table<WcAllOverrideRow>().Schema.CreateTable();
        WcAllOverrideRow item = new() { Value = 1 };
        db.Table<WcAllOverrideRow>().Add(item);

        db.Table<WcAllOverrideRow>()
            .WithColumns(c => c.Set(x => x.Value, 99))
            .Update(item);

        Assert.Equal(99, db.Table<WcAllOverrideRow>().Single().Value);
    }
}
