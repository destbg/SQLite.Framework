using System.ComponentModel.DataAnnotations;

namespace SQLite.Framework.Tests.Entities;

public class InitOnlyEntity
{
    [Key]
    public int Id { get; init; }

    public required string Name { get; init; }

    public int Count { get; init; }
}
