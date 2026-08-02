using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SecDViewFormItems")]
public class SecDViewFormItem
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("SecDViewFormNames")]
public class SecDViewFormName
{
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class SecDViewParameterNameFormTests
{
    [Fact]
    public void CreateViewInlinesPrefixlessFromSqlParameter()
    {
        using TestDatabase db = new();
        db.Table<SecDViewFormItem>().Schema.CreateTable();
        db.Table<SecDViewFormItem>().Add(new SecDViewFormItem { Id = 1, Name = "keep" });
        db.Table<SecDViewFormItem>().Add(new SecDViewFormItem { Id = 2, Name = "drop" });

        List<SecDViewFormName> expected = db.FromSql<SecDViewFormName>(
            "SELECT \"Id\", \"Name\" FROM \"SecDViewFormItems\" WHERE \"Name\" = @name",
            new SQLiteParameter { Name = "name", Value = "keep" }).ToList();
        Assert.Single(expected);

        db.Schema.CreateView<SecDViewFormName>(() => db.FromSql<SecDViewFormName>(
            "SELECT \"Id\", \"Name\" FROM \"SecDViewFormItems\" WHERE \"Name\" = @name",
            new SQLiteParameter { Name = "name", Value = "keep" }));

        List<SecDViewFormName> actual = db.ReadOnlyTable<SecDViewFormName>().ToList();
        Assert.Equal(expected.Select(e => (e.Id, e.Name)), actual.Select(a => (a.Id, a.Name)));
    }

    [Fact]
    public void CreateViewInlinesColonNamedFromSqlParameterOverAtSlot()
    {
        using TestDatabase db = new();
        db.Table<SecDViewFormItem>().Schema.CreateTable();
        db.Table<SecDViewFormItem>().Add(new SecDViewFormItem { Id = 1, Name = "keep" });
        db.Table<SecDViewFormItem>().Add(new SecDViewFormItem { Id = 2, Name = "drop" });

        List<SecDViewFormName> expected = db.FromSql<SecDViewFormName>(
            "SELECT \"Id\", \"Name\" FROM \"SecDViewFormItems\" WHERE \"Name\" = @name",
            new SQLiteParameter { Name = ":name", Value = "keep" }).ToList();
        Assert.Single(expected);

        db.Schema.CreateView<SecDViewFormName>(() => db.FromSql<SecDViewFormName>(
            "SELECT \"Id\", \"Name\" FROM \"SecDViewFormItems\" WHERE \"Name\" = @name",
            new SQLiteParameter { Name = ":name", Value = "keep" }));

        List<SecDViewFormName> actual = db.ReadOnlyTable<SecDViewFormName>().ToList();
        Assert.Equal(expected.Select(e => (e.Id, e.Name)), actual.Select(a => (a.Id, a.Name)));
    }
}
