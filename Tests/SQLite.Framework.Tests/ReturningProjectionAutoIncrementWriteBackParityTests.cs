using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class ReturningProjRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public required string Name { get; set; }
}

public class ReturningProjectionAutoIncrementWriteBackParityTests
{
    [Fact]
    public void ReturningSubsetProjectionAdd_KeepsAutoIncrementWriteBack()
    {
        using TestDatabase db = new();
        db.Table<ReturningProjRow>().Schema.CreateTable();

        ReturningProjRow plain = new() { Name = "plain" };
        db.Table<ReturningProjRow>().Add(plain);
        int plainKey = plain.Id;

        ReturningProjRow projected = new() { Name = "projected" };
        db.Table<ReturningProjRow>().Returning(r => new ReturningProjRow { Name = r.Name }).Add(projected);

        Assert.True(plainKey > 0);
        Assert.True(projected.Id > 0);
    }
}
