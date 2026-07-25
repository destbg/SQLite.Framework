using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("RollbackNote")]
public class RollbackNoteRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class ConflictRollbackRangeTests
{
    [Fact]
    public void AddOrUpdateRangeWithRollbackConflictSurfacesTheConstraintError()
    {
        using TestDatabase db = new();
        db.Table<RollbackNoteRow>().Schema.CreateTable();
        db.Execute("CREATE UNIQUE INDEX \"IX_RollbackNote_Name\" ON \"RollbackNote\" (\"Name\")");
        db.Table<RollbackNoteRow>().Add(new RollbackNoteRow { Id = 1, Name = "taken" });

        SQLiteException ex = Assert.Throws<SQLiteException>(() => db.Table<RollbackNoteRow>()
            .AddOrUpdateRange([new RollbackNoteRow { Id = 2, Name = "taken" }], conflict: SQLiteConflict.Rollback));

        Assert.Equal(SQLiteResult.Constraint, ex.Result);
    }
}
