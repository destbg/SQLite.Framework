using SQLite.Framework.Sample.Models;

namespace SQLite.Framework.Sample.DTOs;

public class OrderDTO
{
    public required int Id { get; init; }
    public required CustomerDTO Customer { get; init; }
    public required DateTime OrderDate { get; init; }
    public required decimal TotalAmount { get; init; }
    public required OrderStatus Status { get; init; }
}
