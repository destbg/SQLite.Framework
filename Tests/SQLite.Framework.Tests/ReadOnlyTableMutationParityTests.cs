using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class ReadOnlyMutationRow
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }
}

public class ReadOnlyTableMutationParityTests
{
    [Fact]
    public void ExecuteDelete_OnReadOnlyTable_DoesNotMutate()
    {
        using TestDatabase db = new();
        db.Table<ReadOnlyMutationRow>().Schema.CreateTable();
        db.Table<ReadOnlyMutationRow>().Add(new ReadOnlyMutationRow { Id = 1, Name = "a" });
        db.Table<ReadOnlyMutationRow>().Add(new ReadOnlyMutationRow { Id = 2, Name = "b" });

        db.ReadOnlyTable<ReadOnlyMutationRow>().ExecuteDelete();

        Assert.Equal(2, db.Table<ReadOnlyMutationRow>().Count());
    }

    [Fact]
    public void ExecuteUpdate_OnReadOnlyTable_DoesNotMutate()
    {
        using TestDatabase db = new();
        db.Table<ReadOnlyMutationRow>().Schema.CreateTable();
        db.Table<ReadOnlyMutationRow>().Add(new ReadOnlyMutationRow { Id = 1, Name = "a" });

        db.ReadOnlyTable<ReadOnlyMutationRow>().ExecuteUpdate(s => s.Set(x => x.Name, "changed"));

        Assert.Equal("a", db.Table<ReadOnlyMutationRow>().Single().Name);
    }
}
