using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class InterpolationProjectionRow
{
    [Key]
    public int Id { get; set; }

    public int Value { get; set; }

    public string Name { get; set; } = "";
}

public class InterpolatedStringProjectionParityTests
{
    private static TestDatabase CreateDb()
    {
        TestDatabase db = new();
        db.Table<InterpolationProjectionRow>().Schema.CreateTable();
        db.Table<InterpolationProjectionRow>().Add(new InterpolationProjectionRow { Id = 1, Value = 7, Name = "abc" });
        return db;
    }

    [Fact]
    public void Interpolation_WithComputedAlignment_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        string expected = $"{7,1 + 4}-abc";
        string actual = db.Table<InterpolationProjectionRow>()
            .Select(x => $"{x.Value,1 + 4}-{x.Name}")
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Interpolation_NoLiteralTextWithClientEvalHole_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        string expected = $"{"abc".Normalize(NormalizationForm.FormD)}{7}";
        string actual = db.Table<InterpolationProjectionRow>()
            .Select(x => $"{x.Name.Normalize(NormalizationForm.FormD)}{x.Value}")
            .First();

        Assert.Equal(expected, actual);
    }
}
