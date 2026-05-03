using System.ComponentModel.DataAnnotations;

namespace SQLite.Framework.Tests.Entities;

public class NullableBoolEntity
{
    [Key]
    public required int Id { get; set; }

    public bool? Flag { get; set; }
}
