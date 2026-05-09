using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;

namespace SQLite.Framework.Blazor.Models;

[Table("Todos")]
public class Todo
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public required string Title { get; set; }

    public DateOnly? DueBy { get; set; }

    public bool IsComplete { get; set; }

    public DateTime CreatedAt { get; set; }
}
