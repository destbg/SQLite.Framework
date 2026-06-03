using System.ComponentModel.DataAnnotations;

namespace SQLite.Framework.Tests.Entities;

public class TwoNullableBoolEntity
{
    [Key]
    public required int Id { get; set; }

    public bool? A { get; set; }

    public bool? B { get; set; }
}
