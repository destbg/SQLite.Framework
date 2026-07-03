using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ComputedColumnPrecedenceTests
{
    [Table("CcRows")]
    internal sealed class CcRow
    {
        [Key]
        public int Id { get; set; }

        public required string Str { get; set; }

        public int Idx { get; set; }
    }

    [Fact]
    public void ComputedColumnWithIndexOfArithmeticKeepsPrecedence()
    {
        using ModelTestDatabase db = new(model => model.Entity<CcRow>()
            .Computed(r => r.Idx, r => r.Str.IndexOf('b') * 2));
        db.Schema.CreateTable<CcRow>();

        db.Execute("INSERT INTO CcRows (\"Id\", \"Str\") VALUES (1, 'abc'), (2, 'abz'), (3, 'xyz')");

        List<(string Str, int Idx)> rows = new()
        {
            ("abc", 0), ("abz", 0), ("xyz", 0),
        };
        List<int> expected = rows.Select(r => r.Str.IndexOf('b') * 2).ToList();
        Assert.Equal([2, 2, -2], expected);

        List<int> actual = db.Table<CcRow>().OrderBy(r => r.Id).Select(r => r.Idx).ToList();

        Assert.Equal(expected, actual);
    }
}
