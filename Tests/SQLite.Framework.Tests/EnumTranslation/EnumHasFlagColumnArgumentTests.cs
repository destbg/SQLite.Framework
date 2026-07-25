using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Flags]
internal enum AccessFlag
{
    None = 0,
    Read = 1,
    Write = 2
}

internal sealed class FlaggedAccessRow
{
    [Key]
    public int Id { get; set; }

    public AccessFlag Granted { get; set; }

    public AccessFlag Requested { get; set; }
}

public class EnumHasFlagColumnArgumentTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<FlaggedAccessRow>().Schema.CreateTable();
        db.Table<FlaggedAccessRow>().Add(new FlaggedAccessRow { Id = 1, Granted = AccessFlag.Read | AccessFlag.Write, Requested = AccessFlag.Read });
        db.Table<FlaggedAccessRow>().Add(new FlaggedAccessRow { Id = 2, Granted = AccessFlag.Write, Requested = AccessFlag.Read });
        return db;
    }

    [Fact]
    public void HasFlagWithColumnArgumentInSelect()
    {
        using TestDatabase db = SetupDatabase();

        List<bool> expected = db.Table<FlaggedAccessRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => r.Granted.HasFlag(r.Requested))
            .ToList();

        Assert.Equal([true, false], expected);

        List<bool> actual = db.Table<FlaggedAccessRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Granted.HasFlag(r.Requested))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void HasFlagWithColumnArgumentInWhere()
    {
        using TestDatabase db = SetupDatabase();

        List<int> expected = db.Table<FlaggedAccessRow>().AsEnumerable()
            .Where(r => r.Granted.HasFlag(r.Requested))
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([1], expected);

        List<int> actual = db.Table<FlaggedAccessRow>()
            .Where(r => r.Granted.HasFlag(r.Requested))
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
