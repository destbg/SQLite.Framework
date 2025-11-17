using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;

namespace SQLite.Framework.Sample.Models;

[Table("Products")]
public class Product
{
    [Key]
    [AutoIncrement]
    [Column("ProductId")]
    public int Id { get; set; }

    [Column("ProductName")]
    [Required]
    public required string Name { get; set; }

    [Column("ProductDescription")]
    public string? Description { get; set; }

    [Column("ProductPrice")]
    [Required]
    [Indexed(Name = "IX_Product_Price")]
    public required decimal Price { get; set; }

    [Column("ProductCategoryId")]
    [Required]
    [Indexed(Name = "IX_Product_CategoryId")]
    public required int CategoryId { get; set; }

    [Column("ProductStock")]
    [Required]
    public required int Stock { get; set; }

    [Column("ProductCreatedAt")]
    [Required]
    public required DateTime CreatedAt { get; set; }

    [Column("ProductUpdatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [Column("ProductIsActive")]
    [Required]
    public required bool IsActive { get; set; }
}
