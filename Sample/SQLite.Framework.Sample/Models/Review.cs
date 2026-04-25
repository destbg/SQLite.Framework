using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;

namespace SQLite.Framework.Sample.Models;

[Table("Reviews")]
public class Review
{
    [Key]
    [AutoIncrement]
    [Column("ReviewId")]
    public int Id { get; set; }

    [Column("ReviewProductId")]
    [Required]
    [Indexed(Name = "IX_Review_ProductId")]
    public required int ProductId { get; set; }

    [Column("ReviewCustomerId")]
    [Required]
    [Indexed(Name = "IX_Review_CustomerId")]
    public required int CustomerId { get; set; }

    [Column("ReviewRating")]
    [Required]
    public required int Rating { get; set; }

    [Column("ReviewComment")]
    public string? Comment { get; set; }

    [Column("ReviewCreatedAt")]
    [Required]
    public required DateTime CreatedAt { get; set; }
}
