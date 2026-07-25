using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class PrivateSetterAccount
{
    [Key]
    public int Id { get; set; }

    public string Name { get; private set; } = "";

    public static PrivateSetterAccount Create(int id, string name) => new() { Id = id, Name = name };
}

public class ProtectedSetterBadge
{
    [Key]
    public int Id { get; set; }

    public int Level { get; protected set; }

    public static ProtectedSetterBadge Create(int id, int level) => new() { Id = id, Level = level };
}

public class NonPublicSetterEntityMaterializationTests
{
    [Fact]
    public void PrivateSetterPropertyIsMaterialized()
    {
        using TestDatabase db = SetupDatabase();

        List<string> actual = db.Table<PrivateSetterAccount>()
            .OrderBy(a => a.Id)
            .ToList()
            .Select(a => a.Id + "|" + a.Name)
            .ToList();

        Assert.Equal(["1|alpha", "2|beta"], actual);
    }

    [Fact]
    public void ProtectedSetterPropertyIsMaterialized()
    {
        using TestDatabase db = SetupDatabase();

        List<string> actual = db.Table<ProtectedSetterBadge>()
            .OrderBy(b => b.Id)
            .ToList()
            .Select(b => b.Id + "|" + b.Level)
            .ToList();

        Assert.Equal(["1|7", "2|9"], actual);
    }

    [Fact]
    public void QuerySyntaxJoinProjectsWholeEntityWithPrivateSetter()
    {
        using TestDatabase db = SetupDatabase();

        List<string> expected =
            (from a in db.Table<ProtectedSetterBadge>().AsEnumerable()
             join b in db.Table<PrivateSetterAccount>().AsEnumerable() on a.Id equals b.Id
             orderby a.Id
             select a.Level + "|" + b.Name).ToList();

        Assert.Equal(["7|alpha", "9|beta"], expected);

        List<string> actual =
            (from a in db.Table<ProtectedSetterBadge>()
             join b in db.Table<PrivateSetterAccount>() on a.Id equals b.Id
             orderby a.Id
             select new { a.Level, Inner = b })
            .AsEnumerable()
            .Select(x => x.Level + "|" + x.Inner.Name)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<PrivateSetterAccount>().Schema.CreateTable();
        db.Table<PrivateSetterAccount>().Add(PrivateSetterAccount.Create(1, "alpha"));
        db.Table<PrivateSetterAccount>().Add(PrivateSetterAccount.Create(2, "beta"));
        db.Table<ProtectedSetterBadge>().Schema.CreateTable();
        db.Table<ProtectedSetterBadge>().Add(ProtectedSetterBadge.Create(1, 7));
        db.Table<ProtectedSetterBadge>().Add(ProtectedSetterBadge.Create(2, 9));
        return db;
    }
}
