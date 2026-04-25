using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;

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
    public int? ParentId { get; set; }
}
