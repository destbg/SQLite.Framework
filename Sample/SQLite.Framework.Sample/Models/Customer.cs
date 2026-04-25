using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;

namespace SQLite.Framework.Sample.Models;

[Table("Customers")]
public class Customer
{
    [Key]
    [AutoIncrement]
    [Column("CustomerId")]
    public int Id { get; set; }

    [Column("CustomerFirstName")]
    [Required]
    public required string FirstName { get; set; }

    [Column("CustomerLastName")]
    [Required]
    public required string LastName { get; set; }

    [Column("CustomerEmail")]
    [Required]
    [Indexed(IsUnique = true)]
    public required string Email { get; set; }

    [Column("CustomerPhone")]
    public string? Phone { get; set; }

    [Column("CustomerBirthDate")]
    public DateTime? BirthDate { get; set; }

    [Column("CustomerRegisteredAt")]
    [Required]
    public required DateTime RegisteredAt { get; set; }

    [Column("CustomerLastLoginAt")]
    public DateTime? LastLoginAt { get; set; }
}
