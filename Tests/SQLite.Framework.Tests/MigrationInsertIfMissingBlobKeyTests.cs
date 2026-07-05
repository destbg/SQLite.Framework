using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("BlobSeed")]
public class BlobSeedRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public byte[] Token { get; set; } = [];
}

public class MigrationInsertIfMissingBlobKeyTests
{
    [Fact]
    public void SkipsRowWhoseBlobKeyIsAlreadyInTheTable()
    {
        using TestDatabase db = new(useFile: true);

        db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<BlobSeedRow>()
                .Insert(new BlobSeedRow { Token = [1, 2, 3] }))
            .Migrate();

        db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<BlobSeedRow>()
                .Insert(new BlobSeedRow { Token = [1, 2, 3] }))
            .Version(2, m => m.InsertIfMissing(x => x.Token, new BlobSeedRow { Token = [1, 2, 3] }))
            .Migrate();

        Assert.Equal(1, db.Table<BlobSeedRow>().Count());
    }
}
