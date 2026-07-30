using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25bRecordInitRows")]
public class H25bRecordInitRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public record H25bRecordInitDto(int Id, string Label);

public class RecordInitializerOverridingPositionalMemberTests
{
    [Fact]
    public void AnInitializerThatOverridesAPositionalMemberUsesTheInitializerValue()
    {
        using TestDatabase db = Setup(nameof(AnInitializerThatOverridesAPositionalMemberUsesTheInitializerValue));

        List<H25bRecordInitDto> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new H25bRecordInitDto(r.Id, r.Name) { Label = r.Name + "!" })
            .ToList();

        List<H25bRecordInitDto> actual = db.Table<H25bRecordInitRow>().OrderBy(r => r.Id)
            .Select(r => new H25bRecordInitDto(r.Id, r.Name) { Label = r.Name + "!" })
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H25bRecordInitRow> Rows()
    {
        return
        [
            new H25bRecordInitRow { Id = 1, Name = "a" },
            new H25bRecordInitRow { Id = 2, Name = "b" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H25bRecordInitRow>().Schema.CreateTable();
        db.Table<H25bRecordInitRow>().AddRange(Rows());
        return db;
    }
}
