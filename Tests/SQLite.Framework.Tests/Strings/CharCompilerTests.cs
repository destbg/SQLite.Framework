using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CharSampleRows")]
file sealed class CharSampleRow
{
    [Key]
    public int Id { get; set; }

    public char C { get; set; }
}

public class CharCompilerTests
{
    [Fact]
    public void CharIsWhiteSpaceMatchesTabAndSpace()
    {
        using TestDatabase db = new();
        db.Table<CharSampleRow>().Schema.CreateTable();
        db.Table<CharSampleRow>().Add(new CharSampleRow { Id = 1, C = '\t' });
        db.Table<CharSampleRow>().Add(new CharSampleRow { Id = 2, C = ' ' });

        List<int> ids = db.Table<CharSampleRow>()
            .Where(r => char.IsWhiteSpace(r.C))
            .OrderBy(r => r.Id)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(new[] { 1, 2 }, ids);
    }

    [Fact]
    public void ClientCompiledCastToNullableLongWorks()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 3, Price = 1 });

        var rows = db.Table<Book>()
            .Select(b => new { b.Id, Cast = (long?)InterceptorHelpers.Double(b.AuthorId) })
            .ToList();

        Assert.Equal(6L, rows[0].Cast);
    }
}
