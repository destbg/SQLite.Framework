using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CompositeAutoIncRow")]
internal sealed class CompositeAutoIncRow
{
    [Key]
    [AutoIncrement]
    public int A { get; set; }

    [Key]
    public int B { get; set; }

    public string Note { get; set; } = "";
}

public class CompositeKeyAutoIncrementInsertParityTests
{
    [Fact]
    public void CreateTable_WithCompositeAutoIncrementKey_ThrowsClearError()
    {
        using TestDatabase db = new();

        Assert.Throws<InvalidOperationException>(() => db.Table<CompositeAutoIncRow>().Schema.CreateTable());
    }
}
