using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class GuidCaseEntity
{
    [Key]
    public int Id { get; set; }

    public Guid Gid { get; set; }
}

public class GuidEqualityCaseSensitivityTests
{
    [Fact]
    public void EqualityMatchesOnlyCanonicalLowercaseGuidText()
    {
        using TestDatabase db = new();
        db.Table<GuidCaseEntity>().Schema.CreateTable();

        Guid target = new("abcdef01-2345-6789-abcd-ef0123456789");
        db.Table<GuidCaseEntity>().Add(new GuidCaseEntity { Id = 1, Gid = target });
        db.Execute("INSERT INTO GuidCaseEntity (Id, Gid) VALUES (2, 'ABCDEF01-2345-6789-ABCD-EF0123456789')");

        List<int> actual = db.Table<GuidCaseEntity>().Where(x => x.Gid == target).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal([1], actual);
    }
}
