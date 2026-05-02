using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;

namespace SQLite.Framework.Avalonia.Models;

[Table("Tag")]
public class Tag
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public required string Title { get; set; }

    public required string Color { get; set; }

    public override string ToString() => Title;
}
