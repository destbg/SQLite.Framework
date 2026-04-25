using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Attributes;

namespace SQLite.Framework.Tests.Entities;

public class SubclassedTableEntity
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public required string Name { get; set; }
}
