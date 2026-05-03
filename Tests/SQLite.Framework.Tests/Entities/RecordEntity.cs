using System.ComponentModel.DataAnnotations;

namespace SQLite.Framework.Tests.Entities;

public record RecordEntity
{
    [Key]
    public int Id { get; init; }

    public required string Name { get; init; }
}
