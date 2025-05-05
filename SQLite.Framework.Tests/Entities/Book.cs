using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SQLite.Framework.Tests.Entities;

[Table("Books")]
public class Book
{
    [Key]
    [Column("BookId")]
    public int Id { get; set; }

    [Column("BookTitle")]
    public required string Title { get; set; }

    [Column("BookAuthorId")]
    public required int AuthorId { get; set; }

    [Column("BookPrice")]
    public required double Price { get; set; }
}