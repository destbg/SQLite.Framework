using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Attributes;

namespace SQLite.Framework.Tests.Entities;

[StrictTable]
[WithoutRowId]
public class StrictWithoutRowIdEntity
{
    [Key]
    public required string Code { get; set; }

    public required string Name { get; set; }
}
