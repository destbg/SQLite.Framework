using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Enums;
using SQLite.Framework.Tests.Interfaces;

namespace SQLite.Framework.Tests.Entities;

public class Publisher : IEntity
{
    [Key]
    public required int Id { get; set; }

    public required string Name { get; set; }

    public required PublisherType Type { get; set; }
}
