using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using CommunityToolkit.Maui.Core.Extensions;
using SQLite.Framework.Attributes;

namespace SQLite.Framework.Maui.Models;

[Table("Tag")]
public class Tag
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Color { get; set; }

    [NotMapped]
    [JsonIgnore]
    public Brush ColorBrush
    {
        get
        {
            return new SolidColorBrush(Microsoft.Maui.Graphics.Color.FromArgb(Color));
        }
    }

    [NotMapped]
    [JsonIgnore]
    public Color DisplayColor
    {
        get
        {
            return Microsoft.Maui.Graphics.Color.FromArgb(Color);
        }
    }

    [NotMapped]
    [JsonIgnore]
    public Color DisplayDarkColor
    {
        get
        {
            return DisplayColor.WithBlackKey(0.8);
        }
    }

    [NotMapped]
    [JsonIgnore]
    public Color DisplayLightColor
    {
        get
        {
            return DisplayColor.WithBlackKey(0.2);
        }
    }
}
