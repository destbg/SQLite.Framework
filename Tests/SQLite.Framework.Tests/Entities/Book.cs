using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Interfaces;

namespace SQLite.Framework.Tests.Entities;

[Table("Books")]
public class Book : IEntity
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