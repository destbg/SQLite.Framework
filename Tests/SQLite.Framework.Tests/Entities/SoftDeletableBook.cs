using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Interfaces;

namespace SQLite.Framework.Tests.Entities;

public class SoftDeletableBook : ISoftDelete
{
    [Key]
    public int Id { get; set; }

    public required string Title { get; set; }

    public bool IsDeleted { get; set; }
}
