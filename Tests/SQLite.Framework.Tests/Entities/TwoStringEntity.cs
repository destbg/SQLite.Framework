using System.ComponentModel.DataAnnotations;

namespace SQLite.Framework.Tests.Entities;

public class TwoStringEntity
{
    [Key]
    public required int Id { get; set; }

    public required string A { get; set; }

    public required string B { get; set; }
}
