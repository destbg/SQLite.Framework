using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum H25sNullableKind
{
    Alpha = 1,
    Beta = 2
}

[Table("H25sNullableEnumRows")]
public class H25sNullableEnumRow
{
    [Key]
    public int Id { get; set; }

    public H25sNullableKind? Kind { get; set; }
}

public static class H25sNullableEnumFunctions
{
    public static string Describe(bool matched)
    {
        return matched ? "yes" : "no";
    }
}

public class NullableEnumComparisonProjectionTests
{
    [Fact]
    public void NullableEnumComparedToAConstantInsideAClientCallMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(NullableEnumComparedToAConstantInsideAClientCallMatchesLinq));

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => H25sNullableEnumFunctions.Describe(r.Kind == H25sNullableKind.Beta))
            .ToList();

        List<string> actual = db.Table<H25sNullableEnumRow>()
            .OrderBy(r => r.Id)
            .Select(r => H25sNullableEnumFunctions.Describe(r.Kind == H25sNullableKind.Beta))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H25sNullableEnumRow> Rows()
    {
        return
        [
            new H25sNullableEnumRow { Id = 1, Kind = H25sNullableKind.Alpha },
            new H25sNullableEnumRow { Id = 2, Kind = H25sNullableKind.Beta }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H25sNullableEnumRow>().Schema.CreateTable();
        db.Table<H25sNullableEnumRow>().AddRange(Rows());
        return db;
    }
}
