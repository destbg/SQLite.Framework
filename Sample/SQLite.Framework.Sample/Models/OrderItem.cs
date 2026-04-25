using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;

namespace SQLite.Framework.Sample.Models;

[Table("OrderItems")]
public class OrderItem
{
    [Key]
    [AutoIncrement]
    [Column("OrderItemId")]
    public int Id { get; set; }

    [Column("OrderItemOrderId")]
    [Required]
    [Indexed(Name = "IX_OrderItem_OrderId")]
    public required int OrderId { get; set; }

    [Column("OrderItemProductId")]
    [Required]
    [Indexed(Name = "IX_OrderItem_ProductId")]
    public required int ProductId { get; set; }

    [Column("OrderItemQuantity")]
    [Required]
    public required int Quantity { get; set; }

    [Column("OrderItemUnitPrice")]
    [Required]
    public required decimal UnitPrice { get; set; }

    [Column("OrderItemDiscount")]
    public decimal? Discount { get; set; }
}
