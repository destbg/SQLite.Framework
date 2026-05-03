using System.ComponentModel.DataAnnotations;

namespace SQLite.Framework.Tests.Entities;

public class PrivateCtorEntity
{
    private PrivateCtorEntity() { }

    public PrivateCtorEntity(int id, string name)
    {
        Id = id;
        Name = name;
    }

    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
