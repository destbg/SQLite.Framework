using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class ReturningHookItem
{
    [Key]
    [SQLite.Framework.Attributes.AutoIncrement]
    public int Id { get; set; }

    public required string Name { get; set; }
}

public class ReturningColumnHookParityTests
{
    [Fact]
    public void ReturningAdd_RunsColumnHookLikePlainAdd()
    {
        using ModelTestDatabase plain = new(
            model => model.Entity<ReturningHookItem>().Column("CreatedAt", SQLiteColumnType.Integer, nullable: true),
            o => o.OnAdd<ReturningHookItem>((_, _, columns) =>
            {
                columns["CreatedAt"] = 123L;
                return true;
            }));
        plain.Schema.CreateTable<ReturningHookItem>();
        plain.Table<ReturningHookItem>().Add(new ReturningHookItem { Name = "a" });
        long plainCreatedAt = plain.ExecuteScalar<long>("SELECT \"CreatedAt\" FROM \"ReturningHookItem\"");

        using ModelTestDatabase db = new(
            model => model.Entity<ReturningHookItem>().Column("CreatedAt", SQLiteColumnType.Integer, nullable: true),
            o => o.OnAdd<ReturningHookItem>((_, _, columns) =>
            {
                columns["CreatedAt"] = 123L;
                return true;
            }));
        db.Schema.CreateTable<ReturningHookItem>();
        db.Table<ReturningHookItem>().Returning().Add(new ReturningHookItem { Name = "a" });
        long returningCreatedAt = db.ExecuteScalar<long>("SELECT \"CreatedAt\" FROM \"ReturningHookItem\"");

        Assert.Equal(plainCreatedAt, returningCreatedAt);
    }
}
