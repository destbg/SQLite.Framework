using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Interfaces;

namespace SQLite.Framework.Tests.Entities;

public class AuditedEntity : ISoftDelete
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public required string Name { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }
}
