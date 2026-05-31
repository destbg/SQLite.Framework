using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("MigSetRows")]
file sealed class MigSetRow
{
    [Key]
    public int Id { get; set; }

    public DateTime Dt { get; set; }

    public DateTimeOffset Dto { get; set; }

    public TimeSpan Ts { get; set; }

    public DateOnly Do { get; set; }

    public TimeOnly To { get; set; }

    public decimal Dec { get; set; }

    public PublisherType En { get; set; }

    public Guid Gu { get; set; }

    public char Ch { get; set; }

    public byte[] Blob { get; set; } = [];

    public int Num { get; set; }
}

public class MigrateSetStorageTests
{
    [Fact]
    public void MigrateSet_IntegerAndDefaultStorage_FormatsEveryType()
    {
        using TestDatabase db = new();
        db.Table<MigSetRow>().Schema.CreateTable();

        Exception? ex = Record.Exception(() => db.Schema.Migrate<MigSetRow>(m => m
            .Set(r => r.Dt, new DateTime(2000, 1, 2, 3, 4, 5))
            .Set(r => r.Dto, new DateTimeOffset(2000, 1, 2, 3, 4, 5, TimeSpan.Zero))
            .Set(r => r.Ts, TimeSpan.FromMinutes(90))
            .Set(r => r.Do, new DateOnly(2000, 1, 2))
            .Set(r => r.To, new TimeOnly(3, 4, 5))
            .Set(r => r.Dec, 12.5m)
            .Set(r => r.En, PublisherType.Magazine)
            .Set(r => r.Gu, Guid.Empty)
            .Set(r => r.Ch, 'x')
            .Set(r => r.Blob, new byte[] { 1, 2, 3 })
            .Set(r => r.Num, 7)));

        Assert.Null(ex);
    }

    [Fact]
    public void MigrateSet_TextStorage_FormatsTextModes()
    {
        using TestDatabase db = new(b => b
            .UseDateTimeStorage(DateTimeStorageMode.TextFormatted)
            .UseDateTimeOffsetStorage(DateTimeOffsetStorageMode.TextFormatted)
            .UseTimeSpanStorage(TimeSpanStorageMode.Text)
            .UseDateOnlyStorage(DateOnlyStorageMode.Text)
            .UseTimeOnlyStorage(TimeOnlyStorageMode.Text)
            .UseDecimalStorage(DecimalStorageMode.Text)
            .UseEnumStorage(EnumStorageMode.Text));
        db.Table<MigSetRow>().Schema.CreateTable();

        Exception? ex = Record.Exception(() => db.Schema.Migrate<MigSetRow>(m => m
            .Set(r => r.Dt, new DateTime(2000, 1, 2, 3, 4, 5))
            .Set(r => r.Dto, new DateTimeOffset(2000, 1, 2, 3, 4, 5, TimeSpan.Zero))
            .Set(r => r.Ts, TimeSpan.FromMinutes(90))
            .Set(r => r.Do, new DateOnly(2000, 1, 2))
            .Set(r => r.To, new TimeOnly(3, 4, 5))
            .Set(r => r.Dec, 12.5m)
            .Set(r => r.En, PublisherType.Magazine)));

        Assert.Null(ex);
    }

    [Fact]
    public void MigrateSet_TextTicksAndUtcTicks_Formats()
    {
        using TestDatabase db = new(b => b
            .UseDateTimeStorage(DateTimeStorageMode.TextTicks)
            .UseDateTimeOffsetStorage(DateTimeOffsetStorageMode.UtcTicks));
        db.Table<MigSetRow>().Schema.CreateTable();

        Exception? ex = Record.Exception(() => db.Schema.Migrate<MigSetRow>(m => m
            .Set(r => r.Dt, new DateTime(2000, 1, 2, 3, 4, 5))
            .Set(r => r.Dto, new DateTimeOffset(2000, 1, 2, 3, 4, 5, TimeSpan.Zero))));

        Assert.Null(ex);
    }
}
