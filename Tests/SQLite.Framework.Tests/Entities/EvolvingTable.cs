using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SQLite.Framework.Tests.Entities;

[Table("Evolving")]
public class EvolvingTable
{
    [Key]
    public int Id { get; set; }
}
