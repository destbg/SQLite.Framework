using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum H22gJsonGrade
{
    Low = 1,
    High = 2,
}

public class H22gJsonGradePayload
{
    public string Name { get; set; } = "";

    public H22gJsonGrade? Grade { get; set; }
}

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(H22gJsonGradePayload))]
public partial class H22gJsonGradeContext : JsonSerializerContext;

[Table("H22gJsonGradeRows")]
public class H22gJsonGradeRow
{
    [Key]
    public int Id { get; set; }

    public H22gJsonGradePayload Data { get; set; } = new();
}

public class NullableJsonStringEnumNumericCastTests
{
    [Fact]
    public void NumericCastOfANullableMemberMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H22gJsonGradeRow> rows);

        List<int?> expected = rows.OrderBy(r => r.Id).Select(r => (int?)r.Data.Grade).ToList();

        List<int?> actual = db.Table<H22gJsonGradeRow>()
            .OrderBy(r => r.Id)
            .Select(r => (int?)r.Data.Grade)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FilterOnTheNumericCastOfANullableMemberMatchesLinq()
    {
        using TestDatabase db = Seed(out List<H22gJsonGradeRow> rows);

        List<int> expected = rows
            .Where(r => (int?)r.Data.Grade == 2)
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> actual = db.Table<H22gJsonGradeRow>()
            .Where(r => (int?)r.Data.Grade == 2)
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static TestDatabase Seed(out List<H22gJsonGradeRow> rows)
    {
        TestDatabase db = new(b =>
            b.AddTypeConverter<H22gJsonGradePayload>(
                new SQLiteJsonConverter<H22gJsonGradePayload>(H22gJsonGradeContext.Default.H22gJsonGradePayload)));
        db.Table<H22gJsonGradeRow>().Schema.CreateTable();
        rows =
        [
            new H22gJsonGradeRow { Id = 1, Data = new H22gJsonGradePayload { Name = "a", Grade = H22gJsonGrade.Low } },
            new H22gJsonGradeRow { Id = 2, Data = new H22gJsonGradePayload { Name = "b", Grade = H22gJsonGrade.High } },
            new H22gJsonGradeRow { Id = 3, Data = new H22gJsonGradePayload { Name = "c", Grade = null } }
        ];
        db.Table<H22gJsonGradeRow>().AddRange(rows);
        return db;
    }
}
