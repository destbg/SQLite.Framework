using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class RequiredNotMappedEntity
{
    [Key] public int Id { get; set; }
    [NotMapped] public required string Note { get; set; } = "default_note";
    public string Name { get; set; } = "";
}

public class RequiredNotMappedInitializerParityTests
{
    [Fact]
    public void RequiredNotMappedProperty_KeepsInitializerAfterRead()
    {
        using TestDatabase db = new();
        db.Table<RequiredNotMappedEntity>().Schema.CreateTable();
        db.Table<RequiredNotMappedEntity>().Add(new RequiredNotMappedEntity { Id = 1, Note = "ignored_on_write", Name = "realname" });

        RequiredNotMappedEntity row = db.Table<RequiredNotMappedEntity>().OrderBy(r => r.Id).ToList().Single();
        Assert.Equal("default_note", row.Note);
        Assert.Equal("realname", row.Name);
    }
}
