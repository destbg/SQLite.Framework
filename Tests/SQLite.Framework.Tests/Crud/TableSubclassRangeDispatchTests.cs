using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("GuardedNote")]
public class GuardedNoteRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    [NotMapped]
    public bool ReadOnlyRow { get; set; }
}

public class GuardedNoteTable : SQLiteTable<GuardedNoteRow>
{
    public GuardedNoteTable(SQLiteDatabase database, TableMapping table)
        : base(database, table)
    {
    }

    protected internal override SQLiteAction RunActionHooks(GuardedNoteRow item, SQLiteAction startingAction)
    {
        return item.ReadOnlyRow ? SQLiteAction.Skip : base.RunActionHooks(item, startingAction);
    }
}

public class GuardedNoteDatabase : TestDatabase
{
    private GuardedNoteTable? items;

    public GuardedNoteDatabase([CallerMemberName] string? methodName = null)
        : base(null, methodName)
    {
    }

    public GuardedNoteTable Items => items ??= new GuardedNoteTable(this, TableMapping(typeof(GuardedNoteRow)));
}

public class TableSubclassRangeDispatchTests
{
    [Fact]
    public void AddRangeHonorsTheActionHookOverride()
    {
        using GuardedNoteDatabase db = new();
        db.Items.Schema.CreateTable();

        db.Items.Add(new GuardedNoteRow { Name = "single", ReadOnlyRow = true });
        Assert.Empty(db.Items.ToList());

        db.Items.AddRange([new GuardedNoteRow { Name = "batch", ReadOnlyRow = true }]);
        Assert.Empty(db.Items.ToList());
    }
}
