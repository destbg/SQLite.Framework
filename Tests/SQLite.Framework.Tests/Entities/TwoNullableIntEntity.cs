using System.ComponentModel.DataAnnotations;

namespace SQLite.Framework.Tests.Entities;

public class TwoNullableIntEntity
{
    [Key]
    public required int Id { get; set; }

    public int? A { get; set; }

    public int? B { get; set; }
}
