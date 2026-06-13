using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("WhitespaceColumnTable")]
internal sealed class WhitespaceColumnV1
{
    [Key]
    public int Id { get; set; }

    [Column("A B")]
    public string? Note { get; set; }
}

[Table("WhitespaceColumnTable")]
internal sealed class WhitespaceColumnV2
{
    [Key]
    public int Id { get; set; }

    [Column("AB")]
    public string? Note { get; set; }
}

public class MigrateByRebuildQuotedIdentifierWhitespaceTests
{
    [Fact]
    public void RenameRemovingSpaceInsideQuotedColumnIsDetectedAsDrift()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<WhitespaceColumnV1>();
        db.Table<WhitespaceColumnV1>().Add(new WhitespaceColumnV1 { Id = 1, Note = "hello" });

        db.Schema.MigrateByRebuild<WhitespaceColumnV2>();

        string liveDdl = db.ExecuteScalar<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'WhitespaceColumnTable'")!;

        Assert.Contains("\"AB\"", liveDdl);

        List<string?> notes = db.Table<WhitespaceColumnV2>().Select(w => w.Note).ToList();

        Assert.Null(Assert.Single(notes));
    }
}
