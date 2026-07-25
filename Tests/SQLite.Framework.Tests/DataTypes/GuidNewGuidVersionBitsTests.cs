using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class GuidVersionSeedRow
{
    [Key]
    public int Id { get; set; }
}

public class GuidNewGuidVersionBitsTests
{
    [Fact]
    public void GeneratedGuidsHaveVersionFourAndRfcVariant()
    {
        using TestDatabase db = new();
        db.Table<GuidVersionSeedRow>().Schema.CreateTable();
        for (int i = 1; i <= 64; i++)
        {
            db.Table<GuidVersionSeedRow>().Add(new GuidVersionSeedRow { Id = i });
        }

        bool oracle = Enumerable.Range(1, 64)
            .Select(_ => Guid.NewGuid())
            .All(g => g.Version == 4 && g.ToString("D")[19] is '8' or '9' or 'a' or 'b');

        Assert.True(oracle);

        List<Guid> generated = db.Table<GuidVersionSeedRow>().Select(_ => Guid.NewGuid()).ToList();

        Assert.All(generated, g => Assert.Equal(4, g.Version));
        Assert.All(generated, g => Assert.True(g.ToString("D")[19] is '8' or '9' or 'a' or 'b'));
    }
}
