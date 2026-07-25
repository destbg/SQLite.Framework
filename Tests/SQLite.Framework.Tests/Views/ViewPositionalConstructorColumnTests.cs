using System;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("PositionalViewSource")]
internal sealed class PositionalViewSourceRow
{
    [Key]
    public int Id { get; set; }
}

[Table("PositionalView")]
internal sealed class PositionalViewRow
{
    public PositionalViewRow()
    {
    }

    public PositionalViewRow(int identifier)
    {
        Id = identifier;
    }

    [Column("SumId")]
    public int Id { get; set; }
}

public class ViewPositionalConstructorColumnTests
{
    [Fact]
    public void ViewWithMismatchedPositionalConstructorDoesNotReadBack()
    {
        using TestDatabase db = new(useFile: true);
        db.Table<PositionalViewSourceRow>().Schema.CreateTable();
        db.Table<PositionalViewSourceRow>().Add(new PositionalViewSourceRow { Id = 7 });

        db.Schema.CreateView<PositionalViewRow>(() => from s in db.Table<PositionalViewSourceRow>() select new PositionalViewRow(s.Id));

        Assert.ThrowsAny<Exception>(() => db.ReadOnlyTable<PositionalViewRow>().ToList());
    }

    [Fact]
    public void ViewWithMemberInitProjectionReadsBack()
    {
        using TestDatabase db = new(useFile: true);
        db.Table<PositionalViewSourceRow>().Schema.CreateTable();
        db.Table<PositionalViewSourceRow>().Add(new PositionalViewSourceRow { Id = 7 });

        db.Schema.CreateView<PositionalViewRow>(() => from s in db.Table<PositionalViewSourceRow>() select new PositionalViewRow { Id = s.Id });

        Assert.Equal(7, db.ReadOnlyTable<PositionalViewRow>().Single().Id);
    }
}
