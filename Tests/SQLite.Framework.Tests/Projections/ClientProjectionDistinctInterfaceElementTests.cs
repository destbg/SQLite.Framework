using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public interface IDistinctLabel
{
    string Label { get; }
}

public class DistinctLabel : IDistinctLabel
{
    public DistinctLabel(string label)
    {
        Label = label;
    }

    public string Label { get; }
}

[Table("ClientDistinctInterfaceRows")]
public class ClientDistinctInterfaceRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class ClientProjectionDistinctInterfaceElementTests
{
    [Fact]
    public void DistinctOverAnInterfaceClientProjectionDedupesTheProjectedValues()
    {
        using TestDatabase db = Setup(nameof(DistinctOverAnInterfaceClientProjectionDedupesTheProjectedValues));

        List<string> actual = db.Table<ClientDistinctInterfaceRow>()
            .Select(r => Label(r.Name))
            .Distinct()
            .AsEnumerable()
            .Select(v => v.Label)
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(["a", "b"], actual);
    }

    private static IDistinctLabel Label(string name)
    {
        return new DistinctLabel(name);
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<ClientDistinctInterfaceRow>().Schema.CreateTable();
        db.Table<ClientDistinctInterfaceRow>().AddRange(
        [
            new ClientDistinctInterfaceRow { Id = 1, Name = "a" },
            new ClientDistinctInterfaceRow { Id = 2, Name = "b" },
            new ClientDistinctInterfaceRow { Id = 3, Name = "a" }
        ]);
        return db;
    }
}
