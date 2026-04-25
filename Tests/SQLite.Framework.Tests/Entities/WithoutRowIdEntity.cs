using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Attributes;

namespace SQLite.Framework.Tests.Entities;

[WithoutRowId]
public class WithoutRowIdEntity
{
    [Key]
    public required string Code { get; set; }

    public required string Name { get; set; }
}
