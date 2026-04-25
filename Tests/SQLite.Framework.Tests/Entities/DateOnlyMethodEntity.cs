using System.ComponentModel.DataAnnotations;

namespace SQLite.Framework.Tests.Entities;

public class DateOnlyMethodEntity
{
    [Key]
    public int Id { get; set; }

    public DateOnly Date { get; set; }
}
