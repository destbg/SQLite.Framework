using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Attributes;

namespace SQLite.Framework.Maui.Models;

[Table("Category")]
public class Category
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Color { get; set; } = "#FF0000";

    [NotMapped]
    [JsonIgnore]
    public Brush ColorBrush
    {
        get
        {
            return new SolidColorBrush(Microsoft.Maui.Graphics.Color.FromArgb(Color));
        }
    }

    public override string ToString() => $"{Title}";
}
