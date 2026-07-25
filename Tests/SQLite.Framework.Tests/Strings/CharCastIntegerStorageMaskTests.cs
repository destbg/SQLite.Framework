using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CharCastIntegerStorageMaskTests
{
    internal sealed class CodeRow
    {
        [Key]
        public int Id { get; set; }

        public int Code { get; set; }
    }

    [Fact]
    public void CharCastMasksToSixteenBitsUnderIntegerStorage()
    {
        using TestDatabase db = new(b => b.CharStorage = CharStorageMode.Integer);
        db.Table<CodeRow>().Schema.CreateTable();

        List<CodeRow> rows =
        [
            new() { Id = 1, Code = 65 },
            new() { Id = 2, Code = 65 + 65536 },
        ];
        db.Table<CodeRow>().AddRange(rows);

        List<int> expected = rows
            .Where(x => (char)x.Code == 'A')
            .Select(x => x.Id).OrderBy(i => i).ToList();
        Assert.Equal([1, 2], expected);

        List<int> actual = db.Table<CodeRow>()
            .Where(x => (char)x.Code == 'A')
            .Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }
}
