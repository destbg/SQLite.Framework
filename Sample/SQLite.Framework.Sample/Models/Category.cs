using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;

namespace SQLite.Framework.Sample.Models;

[Table("Categories")]
public class Category
{
    [Key]
    [AutoIncrement]
    [Column("CategoryId")]
    public int Id { get; set; }

    [Column("CategoryName")]
    [Required]
    [Indexed(IsUnique = true)]
    public required string Name { get; set; }

    [Column("CategoryDescription")]
    public string? Description { get; set; }

    [Column("CategoryParentId")]
    [ReferencesTable(typeof(Category), OnDelete = SQLiteForeignKeyAction.SetNull)]
    public int? ParentId { get; set; }
}
