using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Attributes;

namespace SQLite.Framework.Maui.Models;

[Table("Task")]
public class ProjectTask
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }
    public required string Title { get; set; }
    public bool IsCompleted { get; set; }

    [JsonIgnore]
    public int ProjectId { get; set; }
}
