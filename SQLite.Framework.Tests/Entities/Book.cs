using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;

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
    [Indexed(Name = "IX_Book_AuthorId", Order = 1)]
    public required int AuthorId { get; set; }

    [Column("BookPrice")]
    [Indexed(IsUnique = true)]
    public required double Price { get; set; }
}