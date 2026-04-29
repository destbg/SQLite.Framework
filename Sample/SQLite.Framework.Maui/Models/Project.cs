using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Attributes;

namespace SQLite.Framework.Maui.Models;

[Table("Project")]
public class Project
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;

    [JsonIgnore]
    public int CategoryId { get; set; }

    [NotMapped]
    public Category? Category { get; set; }

    [NotMapped]
    public List<ProjectTask> Tasks { get; set; } = [];

    [NotMapped]
    public List<Tag> Tags { get; set; } = [];

    [NotMapped]
    public string AccessibilityDescription
    {
        get { return $"{Name} Project. {Description}"; }
    }

    public override string ToString() => $"{Name}";
}

public class ProjectsJson
{
    public List<Project> Projects { get; set; } = [];
}
