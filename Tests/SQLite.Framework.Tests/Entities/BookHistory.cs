using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;

namespace SQLite.Framework.Tests.Entities;

[Table("BookHistory")]
public class BookHistory
{
    [Key, AutoIncrement]
    public int Id { get; set; }
    public required int BookId { get; set; }
    public required double OldPrice { get; set; }
    public required double NewPrice { get; set; }
}
