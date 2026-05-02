using System.ComponentModel.DataAnnotations;

namespace SQLite.Framework.Tests.Entities;

public class NullableStringEntity
{
    [Key]
    public required int Id { get; set; }

    public string? Name { get; set; }
}
