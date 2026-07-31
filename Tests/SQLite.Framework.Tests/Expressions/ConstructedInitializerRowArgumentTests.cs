using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26mInitializerArgumentRows")]
public class H26mInitializerArgumentRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class H26mLabel
{
    public H26mLabel(string text)
    {
        Text = text;
    }

    public string Text { get; }

    public string Suffix { get; set; } = string.Empty;
}

public static class H26mLabelFormatter
{
    public static string Render(H26mLabel label)
    {
        return label.Text + label.Suffix;
    }
}

public class ConstructedInitializerRowArgumentTests
{
    [Fact]
    public void AnInitializerWhoseConstructorArgumentComesFromTheRowIsBuiltPerRow()
    {
        using TestDatabase db = Setup(nameof(AnInitializerWhoseConstructorArgumentComesFromTheRowIsBuiltPerRow));

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => H26mLabelFormatter.Render(new H26mLabel(r.Name) { Suffix = "!" }))
            .ToList();

        List<string> actual = db.Table<H26mInitializerArgumentRow>()
            .OrderBy(r => r.Id)
            .Select(r => H26mLabelFormatter.Render(new H26mLabel(r.Name) { Suffix = "!" }))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H26mInitializerArgumentRow> Rows()
    {
        return
        [
            new H26mInitializerArgumentRow { Id = 1, Name = "alpha" },
            new H26mInitializerArgumentRow { Id = 2, Name = "beta" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(o =>
        {
            o.SelectMaterializers.Clear();
            o.ReflectionFallbackDisabled = false;
        }, methodName);
        db.Table<H26mInitializerArgumentRow>().Schema.CreateTable();
        db.Table<H26mInitializerArgumentRow>().AddRange(Rows());
        return db;
    }
}
