using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Attributes;

namespace SQLite.Framework.Tests.Entities;

public class Article
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Body { get; set; }
    public DateTime PublishedAt { get; set; }
}
