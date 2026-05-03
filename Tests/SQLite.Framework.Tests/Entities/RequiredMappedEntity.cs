using System.ComponentModel.DataAnnotations;

namespace SQLite.Framework.Tests.Entities;

public class RequiredMappedEntity
{
    [Key]
    public required int Id { get; set; }

    public required string Name { get; set; }

    public required double Value { get; set; }
}
