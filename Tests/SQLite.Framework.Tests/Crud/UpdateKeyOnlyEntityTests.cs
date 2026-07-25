using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class UpdateKeyOnlyEntityTests
{
    [Fact]
    public void UpdateOfAutoKeyOnlyEntityIsNoOp()
    {
        using TestDatabase db = new();
        db.Table<AutoKeyOnlyRow>().Schema.CreateTable();
        db.Table<AutoKeyOnlyRow>().Add(new AutoKeyOnlyRow());

        int changes = db.Table<AutoKeyOnlyRow>().Update(new AutoKeyOnlyRow { Id = 1 });

        Assert.Equal(1, changes);
        Assert.Equal(1, db.Table<AutoKeyOnlyRow>().Count());
    }
}

public class AutoKeyOnlyRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }
}
