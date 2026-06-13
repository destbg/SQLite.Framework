using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class SurrogateTextRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class StringIndexerSurrogatePairTests
{
    private static readonly string AstralText = "x" + char.ConvertFromUtf32(0x1F600) + "y";

    private static TestDatabase SetupDatabase(CharStorageMode mode)
    {
        TestDatabase db = new(b => b.CharStorage = mode);
        db.Table<SurrogateTextRow>().Schema.CreateTable();
        db.Table<SurrogateTextRow>().Add(new SurrogateTextRow { Id = 1, Name = AstralText });
        return db;
    }

    [Fact]
    public void IndexerOnAstralCharacterTextStorageCannotReadHalfSurrogate()
    {
        using TestDatabase db = SetupDatabase(CharStorageMode.Text);

        char expected = AstralText[1];

        Assert.Equal('\uD83D', expected);

        Assert.Throws<FormatException>(() =>
            db.Table<SurrogateTextRow>().Select(r => r.Name[1]).First());
    }

    [Fact]
    public void IndexerOnAstralCharacterIntegerStorageCannotReadHalfSurrogate()
    {
        using TestDatabase db = SetupDatabase(CharStorageMode.Integer);

        char expected = AstralText[1];

        Assert.Equal('\uD83D', expected);

        Assert.Throws<OverflowException>(() =>
            db.Table<SurrogateTextRow>().Select(r => r.Name[1]).First());
    }
}
