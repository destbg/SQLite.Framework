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
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }

    [JsonIgnore]
    public int ProjectId { get; set; }
}
