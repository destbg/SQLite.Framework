using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Attributes;

namespace SQLite.Framework.Tests.Entities;

[StrictTable]
public class StrictTableEntity
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }
}
