using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class InstanceMethodRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

internal sealed class InstanceMethodDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool Running { get; set; }
}

public class InstanceMethodClientEvalProjectionTests
{
    private readonly HashSet<int> runningIds = [2];

    private bool IsRunning(int id) => runningIds.Contains(id);

    [Fact]
    public void InstanceMethodCallInPostAsEnumerableProjectionMaterializes()
    {
        using TestDatabase db = new();
        db.Table<InstanceMethodRow>().Schema.CreateTable();
        db.Table<InstanceMethodRow>().AddRange(
        [
            new InstanceMethodRow { Id = 1, Name = "a" },
            new InstanceMethodRow { Id = 2, Name = "b" },
        ]);

        List<InstanceMethodDto> actual = db.Table<InstanceMethodRow>()
            .OrderBy(r => r.Id)
            .Select(r => new InstanceMethodDto
            {
                Id = r.Id,
                Name = r.Name,
                Running = IsRunning(r.Id),
            }).ToList();

        Assert.Equal(
        [
            new InstanceMethodDto { Id = 1, Name = "a", Running = false },
            new InstanceMethodDto { Id = 2, Name = "b", Running = true },
        ], actual);
    }
}
