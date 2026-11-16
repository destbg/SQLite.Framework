#if NET11_0_OR_GREATER
using System.Text;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class Net11StringTests
{
    [Fact]
    public void RuneFromColumnUsesClientProjection()
    {
        using TestDatabase db = new();
        db.Table<Net11JoinRow>().Schema.CreateTable();
        db.Table<Net11JoinRow>().AddRange([
            new() { Id = 'A', Code = "Apple" }, new() { Id = 'B', Code = "Apple" }
        ]);

        bool[] actual = db.Table<Net11JoinRow>().OrderBy(x => x.Id)
            .Select(x => x.Code!.Contains(new Rune(x.Id))).ToArray();

        Assert.Equal([true, false], actual);
    }

    [Fact]
    public void RuneSearchReplaceAndTrimUseUnicodeScalars()
    {
        using TestDatabase db = new();
        db.Table<Net11JoinRow>().Schema.CreateTable();
        db.Table<Net11JoinRow>().AddRange([
            new() { Id = 1, Code = "😀hi😀" }, new() { Id = 2, Code = "hi" },
            new() { Id = 3, Code = "100%" }, new() { Id = 4, Code = "😀" }
        ]);
        Rune face = new(0x1F600);
        Rune replacement = new('!');
        var expected = db.Table<Net11JoinRow>().ToList().Select(x => new
        {
            x.Id, Contains = x.Code!.Contains(face), Starts = x.Code.StartsWith(face), Ends = x.Code.EndsWith(face),
            Trimmed = x.Code.Trim(face), Start = x.Code.TrimStart(face), End = x.Code.TrimEnd(face),
            Replaced = x.Code.Replace(face, replacement)
        }).OrderBy(x => x.Id);
        var actual = db.Table<Net11JoinRow>().Select(x => new
        {
            x.Id, Contains = x.Code!.Contains(face), Starts = x.Code.StartsWith(face), Ends = x.Code.EndsWith(face),
            Trimmed = x.Code.Trim(face), Start = x.Code.TrimStart(face), End = x.Code.TrimEnd(face),
            Replaced = x.Code.Replace(face, replacement)
        }).OrderBy(x => x.Id).ToList();
        Assert.Equal(expected, actual);
        Assert.Equal([1, 4], db.Table<Net11JoinRow>().Where(x => x.Code!.Contains(face, StringComparison.Ordinal)).Select(x => x.Id).OrderBy(x => x).ToList());
        Rune percent = new('%');
        Assert.Equal(3, db.Table<Net11JoinRow>().Where(x => x.Code!.Contains(percent)).Single().Id);
    }

    [Fact]
    public void OrdinalCaseConversionAndCharComparisonTranslate()
    {
        using TestDatabase db = new();
        db.Table<Net11JoinRow>().Schema.CreateTable();
        db.Table<Net11JoinRow>().Add(new() { Id = 1, Code = "aBc" });
        var actual = db.Table<Net11JoinRow>().Select(x => new
        {
            Upper = x.Code!.ToUpperOrdinal(), Lower = x.Code.ToLowerOrdinal(),
            UpperChar = char.ToUpperOrdinal(x.Code[0]), LowerChar = char.ToLowerOrdinal(x.Code[1]),
            Starts = x.Code.StartsWith('A', StringComparison.OrdinalIgnoreCase),
            Ends = x.Code.EndsWith('C', StringComparison.OrdinalIgnoreCase)
        }).Single();
        Assert.Equal(new { Upper = "ABC", Lower = "abc", UpperChar = 'A', LowerChar = 'b', Starts = true, Ends = true }, actual);
    }

    [Theory]
    [InlineData(CharStorageMode.Text)]
    [InlineData(CharStorageMode.Integer)]
    public void CharEqualsComparisonUsesTheConfiguredStorage(CharStorageMode storage)
    {
        using TestDatabase db = new(x => x.UseCharStorage(storage));
        db.Table<Net11JoinRow>().Schema.CreateTable();
        db.Table<Net11JoinRow>().Add(new() { Id = 1, Code = "aBc" });
        Assert.Single(db.Table<Net11JoinRow>().Where(x => x.Code![0].Equals('A', StringComparison.OrdinalIgnoreCase)).ToList());
        Assert.Empty(db.Table<Net11JoinRow>().Where(x => x.Code![0].Equals('A', StringComparison.Ordinal)).ToList());
    }
}
#endif
