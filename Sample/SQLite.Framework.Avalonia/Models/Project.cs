using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;

namespace SQLite.Framework.Avalonia.Models;

[Table("Project")]
public class Project
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public int CategoryId { get; set; }

    public override string ToString() => Name;
}
