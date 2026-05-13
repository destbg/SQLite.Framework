using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;

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
    [ReferencesTable(typeof(Project), OnDelete = SQLiteForeignKeyAction.Cascade)]
    public int ProjectId { get; set; }
}
