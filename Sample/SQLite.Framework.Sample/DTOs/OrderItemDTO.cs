namespace SQLite.Framework.Sample.DTOs;

public class OrderItemDTO
{
    public required int Id { get; init; }
    public required ProductDTO Product { get; init; }
    public required int Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
    public decimal? Discount { get; init; }
    public required decimal Total { get; init; }
}
