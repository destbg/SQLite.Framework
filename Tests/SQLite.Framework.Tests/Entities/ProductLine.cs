using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SQLite.Framework.Tests.Entities;

[Table("ProductLines")]
public class ProductLine
{
    [Key]
    public int Id { get; set; }

    public required decimal Price { get; set; }

    public required int Quantity { get; set; }

    public decimal Total { get; set; }
}
