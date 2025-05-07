using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Enums;

namespace SQLite.Framework.Tests.Entities;

public class Publisher
{
    [Key]
    public required int Id { get; set; }

    public required string Name { get; set; }

    public required PublisherType Type { get; set; }
}
