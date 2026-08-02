using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SecK_RenameGenerated")]
public class SecKRenameGeneratedRow
{
    [Key]
    public int Id { get; set; }

    public int Base { get; set; }

    public int Tripled { get; set; }
}

public class MigrationRenameGeneratedColumnTests
{
    [Fact]
    public void RenameColumnStepRenamesALiveGeneratedColumn()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<SecKRenameGeneratedRow>().Computed(x => x.Tripled, x => x.Base * 2));
        db.Execute("CREATE TABLE \"SecK_RenameGenerated\" (\"Id\" INTEGER PRIMARY KEY, \"Base\" INTEGER NOT NULL, \"Doubled\" INTEGER GENERATED ALWAYS AS (\"Base\" * 2) VIRTUAL)");
        db.Execute("INSERT INTO \"SecK_RenameGenerated\" (\"Id\", \"Base\") VALUES (1, 21)");

        db.Schema.Migrations()
            .Version(1, m => m.RenameColumn<SecKRenameGeneratedRow>("Doubled", "Tripled"))
            .Migrate();

        Assert.True(db.Schema.ColumnExists<SecKRenameGeneratedRow>("Tripled"));
        Assert.Equal(42, db.Table<SecKRenameGeneratedRow>().Single().Tripled);
    }
}
