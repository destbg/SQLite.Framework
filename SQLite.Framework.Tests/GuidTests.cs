using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GuidTests
{
    private static readonly Guid StaticGuid = Guid.NewGuid();

    private class TestEntity
    {
        [Key]
        public required Guid Id { get; set; }
    }

    [Fact]
    public void CheckGuidEquals()
    {
        using TestDatabase db = SetupDatabase();

        TestEntity author = (
            from a in db.Table<TestEntity>()
            where a.Id == StaticGuid
            select new TestEntity
            {
                Id = a.Id,
            }
        ).First();

        Assert.NotNull(author);
        Assert.Equal(StaticGuid, author.Id);
    }

    [Fact]
    public void CheckGuidEqualsNewGuid()
    {
        using TestDatabase db = SetupDatabase();

        TestEntity? author = (
            from a in db.Table<TestEntity>()
            where a.Id == Guid.NewGuid()
            select new TestEntity
            {
                Id = a.Id,
            }
        ).FirstOrDefault();

        Assert.Null(author);
    }

    private static TestDatabase SetupDatabase([CallerMemberName] string? methodName = null)
    {
        TestDatabase db = new(methodName);

        db.Table<TestEntity>().CreateTable();

        db.Table<TestEntity>().AddRange(new[]
        {
            new TestEntity
            {
                Id = StaticGuid,
            }
        });

        return db;
    }
}