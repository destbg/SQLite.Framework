using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Interfaces;

namespace SQLite.Framework.Tests.Entities;

[Table("Authors")]
public class Author : IEntity
{
    [Key]
    [Column("AuthorId")]
    public int Id { get; set; }

    [Column("AuthorName")]
    public required string Name { get; set; }

    [Column("AuthorEmail")]
    public required string Email { get; set; }

    [Column("AuthorBirthDate")]
    public required DateTime BirthDate { get; set; }
}