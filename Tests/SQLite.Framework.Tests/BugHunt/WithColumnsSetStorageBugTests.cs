using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

public class WithColumnsSetStorageBugTests
{
    [Fact]
    public void WithColumns_EnumText_StoresMemberName()
    {
        using TestDatabase db = new(b => b.EnumStorage = EnumStorageMode.Text);
        db.Table<ZWcRow>().Schema.CreateTable();

        db.Table<ZWcRow>().Add(new ZWcRow { Id = 1, Status = ZWcStatus.Active });

        db.Table<ZWcRow>()
            .WithColumns(c => c.Set(x => x.Status, ZWcStatus.Active))
            .Add(new ZWcRow { Id = 2, Status = ZWcStatus.Inactive });

        string? normalStored = db.ExecuteScalar<string>("SELECT \"Status\" FROM \"ZWcRow\" WHERE \"Id\" = 1");
        string? withColumnsStored = db.ExecuteScalar<string>("SELECT \"Status\" FROM \"ZWcRow\" WHERE \"Id\" = 2");

        Assert.Equal(normalStored, withColumnsStored);
    }

    [Fact]
    public void WithColumns_DateTimeLiteral_RoundTrips()
    {
        using TestDatabase db = new();
        db.Table<ZWcDateRow>().Schema.CreateTable();

        DateTime when = new(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        db.Table<ZWcDateRow>()
            .WithColumns(c => c.Set(x => x.When, when))
            .Add(new ZWcDateRow { Id = 1, When = default });

        DateTime stored = db.Table<ZWcDateRow>().Single().When;

        Assert.Equal(when, stored);
    }
}

public enum ZWcStatus
{
    Active = 1,
    Inactive = 2
}

[Table("ZWcRow")]
public class ZWcRow
{
    [Key]
    public int Id { get; set; }

    public ZWcStatus Status { get; set; }
}

[Table("ZWcDateRow")]
public class ZWcDateRow
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }
}
