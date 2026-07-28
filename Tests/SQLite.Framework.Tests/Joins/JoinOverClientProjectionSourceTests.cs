using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23zJoinClientRows")]
public class H23zJoinClientRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public static class H23zJoinClientText
{
    public static string Tail(string value)
    {
        return value.Substring(value.Length - 1);
    }
}

public class JoinOverClientProjectionSourceTests
{
    [Fact]
    public void JoiningAClientProjectionEitherMatchesTheProjectedValuesOrReportsWhatIsUnsupported()
    {
        using TestDatabase db = Setup(nameof(JoiningAClientProjectionEitherMatchesTheProjectedValuesOrReportsWhatIsUnsupported));

        List<string> expected = Rows()
            .Select(r => H23zJoinClientText.Tail(r.Name))
            .Join(Rows(), v => v, r => H23zJoinClientText.Tail(r.Name), (v, r) => v + r.Id)
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToList();

        List<string> actual;
        try
        {
            actual = db.Table<H23zJoinClientRow>()
                .Select(r => H23zJoinClientText.Tail(r.Name))
                .Join(db.Table<H23zJoinClientRow>(), v => v, r => H23zJoinClientText.Tail(r.Name), (v, r) => v + r.Id)
                .AsEnumerable()
                .OrderBy(v => v, StringComparer.Ordinal)
                .ToList();
        }
        catch (NotSupportedException)
        {
            return;
        }

        Assert.Equal(expected, actual);
    }

    private static List<H23zJoinClientRow> Rows()
    {
        return
        [
            new H23zJoinClientRow { Id = 1, Name = "1a" },
            new H23zJoinClientRow { Id = 2, Name = "2a" },
            new H23zJoinClientRow { Id = 3, Name = "3b" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H23zJoinClientRow>().Schema.CreateTable();
        db.Table<H23zJoinClientRow>().AddRange(Rows());
        return db;
    }
}
