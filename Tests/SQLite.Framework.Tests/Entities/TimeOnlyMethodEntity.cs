using System.ComponentModel.DataAnnotations;

namespace SQLite.Framework.Tests.Entities;

public class TimeOnlyMethodEntity
{
    [Key]
    public int Id { get; set; }

    public TimeOnly Time { get; set; }
}
