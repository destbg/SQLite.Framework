using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SQLite.Framework.Tests.Entities;

[Table("Evolving")]
public class EvolvingTablePlusName
{
    [Key]
    public int Id { get; set; }

    public string? Name { get; set; }
}
