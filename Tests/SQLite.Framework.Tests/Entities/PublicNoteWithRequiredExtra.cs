using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;

namespace SQLite.Framework.Tests.Entities;

[Table("PublicNotesWithRequiredExtras")]
public class PublicNoteWithRequiredExtra
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public required string Title { get; set; }

    [NotMapped]
    public required string SessionId { get; set; }
}
