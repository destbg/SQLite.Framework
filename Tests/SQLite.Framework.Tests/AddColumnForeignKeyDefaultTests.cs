using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("FkStampParent")]
public class FkStampParentRow
{
    [Key]
    public int Id { get; set; }
}

[Table("FkStampChild")]
public class FkStampChildRow
{
    [Key]
    public int Id { get; set; }

    public int OwnerId { get; set; }
}

file sealed class FkStampDb : TestDatabase
{
    public FkStampDb()
        : base(useFile: true)
    {
    }

    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        builder.Entity<FkStampChildRow>()
            .ForeignKey<FkStampParentRow>(c => c.OwnerId)
            .Default(c => c.OwnerId, 1);
    }
}

public class AddColumnForeignKeyDefaultTests
{
    [Fact]
    public void AddColumnWorksForAForeignKeyColumnWithADefault()
    {
        using FkStampDb db = new();
        db.Schema.CreateTable<FkStampParentRow>();
        db.Execute("CREATE TABLE \"FkStampChild\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Table<FkStampParentRow>().Add(new FkStampParentRow { Id = 1 });

        db.Schema.AddColumn<FkStampChildRow>("OwnerId");

        List<string> columns = db.Pragmas.TableInfo("FkStampChild").Select(c => c.Name).ToList();
        Assert.Equal(["Id", "OwnerId"], columns);
    }
}
