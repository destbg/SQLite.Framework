using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class NotMappedMaterializationTests
{
    [Fact]
    public void Materializer_PreservesConstructorDefault_OnNotMappedCollection()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NoteWithExtras>();

        db.Table<NoteWithExtras>().Add(new NoteWithExtras { Title = "first" });

        NoteWithExtras row = db.Table<NoteWithExtras>().Single();
        Assert.Equal("first", row.Title);
        Assert.NotNull(row.Tags);
        Assert.Empty(row.Tags);
        Assert.Equal("default", row.Computed);
    }

    [Fact]
    public void Materializer_RequiredNotMappedProperty_CompilesAndMaterializes()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NoteWithRequiredExtra>();

        db.Table<NoteWithRequiredExtra>().Add(new NoteWithRequiredExtra { Title = "first", SessionId = "ignored-on-write" });

        NoteWithRequiredExtra row = db.Table<NoteWithRequiredExtra>().Single();
        Assert.Equal("first", row.Title);
    }

    [Fact]
    public void Materializer_PublicRequiredNotMappedProperty_CompilesAndMaterializes()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<PublicNoteWithRequiredExtra>();

        db.Table<PublicNoteWithRequiredExtra>().Add(new PublicNoteWithRequiredExtra { Title = "first", SessionId = "ignored-on-write" });

        PublicNoteWithRequiredExtra row = db.Table<PublicNoteWithRequiredExtra>().Single();
        Assert.Equal("first", row.Title);
    }

    [Fact]
    public void Materializer_DoesNotIncludeNotMappedColumns_InWriters()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NoteWithExtras>();

        SQLiteCommand command = (
            from n in db.Table<NoteWithExtras>()
            select n
        ).ToSqlCommand();

        Assert.DoesNotContain("Tags", command.CommandText);
        Assert.DoesNotContain("Computed", command.CommandText);
    }
}

[Table("NotesWithExtras")]
file class NoteWithExtras
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public required string Title { get; set; }

    [NotMapped]
    public List<string> Tags { get; set; } = [];

    [NotMapped]
    public string Computed { get; set; } = "default";
}

[Table("NotesWithRequiredExtras")]
file class NoteWithRequiredExtra
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public required string Title { get; set; }

    [NotMapped]
    public required string SessionId { get; set; }
}
