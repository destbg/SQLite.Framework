using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using SQLite.Framework;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24tFilterNotes")]
public class H24tFilterNote
{
    [Key]
    public int Id { get; set; }

    public int OwnerId { get; set; }

    public bool Archived { get; set; }
}

[Table("H24tFilterOwners")]
public class H24tFilterOwner
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class H24tFilterNoteTable : SQLiteTable<H24tFilterNote>
{
    public H24tFilterNoteTable(SQLiteDatabase database, TableMapping table)
        : base(database, table)
    {
    }
}

public class H24tFilterDatabase : TestDatabase
{
    private H24tFilterNoteTable? notes;

    public H24tFilterDatabase([CallerMemberName] string? methodName = null)
        : base(b => b.AddQueryFilter<H24tFilterNote>(n => !n.Archived), methodName)
    {
    }

    public H24tFilterNoteTable Notes => notes ??= new H24tFilterNoteTable(this, TableMapping(typeof(H24tFilterNote)));

    public H24tFilterNoteTable NoteTable()
    {
        return Notes;
    }
}

public class TableReturningMethodFilterInjectionTests
{
    [Fact]
    public void SubqueryOverAMethodReturnedTableAppliesTheFilter()
    {
        using H24tFilterDatabase db = new();
        db.Table<H24tFilterOwner>().Schema.CreateTable();
        db.Table<H24tFilterNote>().Schema.CreateTable();
        db.Table<H24tFilterOwner>().AddRange(Owners());
        db.Table<H24tFilterNote>().AddRange(Notes());

        List<int> expected = Owners()
            .Where(o => Notes().Where(n => !n.Archived).Any(n => n.OwnerId == o.Id))
            .Select(o => o.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> actual = db.Table<H24tFilterOwner>()
            .Where(o => db.NoteTable().Any(n => n.OwnerId == o.Id))
            .Select(o => o.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SubqueryOverTheGenericTableAccessorAppliesTheFilter()
    {
        using H24tFilterDatabase db = new();
        db.Table<H24tFilterOwner>().Schema.CreateTable();
        db.Table<H24tFilterNote>().Schema.CreateTable();
        db.Table<H24tFilterOwner>().AddRange(Owners());
        db.Table<H24tFilterNote>().AddRange(Notes());

        List<int> expected = Owners()
            .Where(o => Notes().Where(n => !n.Archived).Any(n => n.OwnerId == o.Id))
            .Select(o => o.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> actual = db.Table<H24tFilterOwner>()
            .Where(o => db.Table<H24tFilterNote>().Any(n => n.OwnerId == o.Id))
            .Select(o => o.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24tFilterOwner> Owners()
    {
        return
        [
            new H24tFilterOwner { Id = 1, Name = "a" },
            new H24tFilterOwner { Id = 2, Name = "b" },
            new H24tFilterOwner { Id = 3, Name = "c" },
        ];
    }

    private static List<H24tFilterNote> Notes()
    {
        return
        [
            new H24tFilterNote { Id = 1, OwnerId = 1, Archived = false },
            new H24tFilterNote { Id = 2, OwnerId = 2, Archived = true },
            new H24tFilterNote { Id = 3, OwnerId = 3, Archived = true },
        ];
    }
}
