using System.ComponentModel.DataAnnotations.Schema;

namespace SQLite.Framework.Tests.Entities;

[Table("vBookSummary")]
public class BookView
{
    public required int Id { get; set; }
    public required string Title { get; set; }
    public required double Price { get; set; }
}
