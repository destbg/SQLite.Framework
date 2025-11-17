using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;

namespace SQLite.Framework.Sample.Models;

[Table("Orders")]
public class Order
{
    [Key]
    [AutoIncrement]
    [Column("OrderId")]
    public int Id { get; set; }

    [Column("OrderCustomerId")]
    [Required]
    [Indexed(Name = "IX_Order_CustomerId")]
    public required int CustomerId { get; set; }

    [Column("OrderDate")]
    [Required]
    [Indexed(Name = "IX_Order_OrderDate")]
    public required DateTime OrderDate { get; set; }

    [Column("OrderTotalAmount")]
    [Required]
    public required decimal TotalAmount { get; set; }

    [Column("OrderStatus")]
    [Required]
    public required OrderStatus Status { get; set; }

    [Column("OrderShippingAddress")]
    public string? ShippingAddress { get; set; }

    [Column("OrderNotes")]
    public string? Notes { get; set; }
}

public enum OrderStatus
{
    Pending = 0,
    Processing = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}
