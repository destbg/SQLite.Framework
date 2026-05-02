using System.ComponentModel.DataAnnotations;

namespace SQLite.Framework.Tests.Entities;

public class NullableEntity
{
    [Key]
    public required int Id { get; set; }

    public int? Value { get; set; }
}
