using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("NullObjectMethodRows")]
public class NullObjectMethodRow
{
    [Key]
    public int Id { get; set; }

    public bool? Flag { get; set; }

    public Guid? Token { get; set; }
}

public class NullableColumnObjectMethodTests
{
    private static List<NullObjectMethodRow> Rows()
    {
        return
        [
            new NullObjectMethodRow
            {
                Id = 1,
                Flag = true,
                Token = new Guid("11111111-2222-3333-4444-555555555555"),
            },
            new NullObjectMethodRow { Id = 2 },
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<NullObjectMethodRow>().Schema.CreateTable();
        db.Table<NullObjectMethodRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void NullableBoolHashCodeMatchesLinq()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows().OrderBy(r => r.Id).Select(r => r.Flag.GetHashCode()).ToList();
        List<int> actual = db.Table<NullObjectMethodRow>().OrderBy(r => r.Id).Select(r => r.Flag.GetHashCode()).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableGuidHashCodeMatchesLinq()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows().OrderBy(r => r.Id).Select(r => r.Token.GetHashCode()).ToList();
        List<int> actual = db.Table<NullObjectMethodRow>().OrderBy(r => r.Id).Select(r => r.Token.GetHashCode()).ToList();

        Assert.Equal(expected, actual);
    }
}
