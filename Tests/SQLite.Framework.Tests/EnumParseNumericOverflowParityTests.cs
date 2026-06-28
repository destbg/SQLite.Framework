using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class EnumParseNumericOverflowParityTests
{
    public enum ByteBacked : byte { A = 1, B = 2 }

    public class EpRow
    {
        [Key]
        public int Id { get; set; }
        public ByteBacked Value { get; set; }
        public string Code { get; set; } = "";
    }

    private static TestDatabase Create()
    {
        TestDatabase db = new();
        db.Table<EpRow>().Schema.CreateTable();
        db.Table<EpRow>().Add(new EpRow { Id = 1, Code = "300" });
        return db;
    }

    [Fact]
    public void EnumParseNumericStringAboveUnderlyingRange_WrapsInsteadOfThrowing()
    {
        using TestDatabase db = Create();

        Assert.Throws<OverflowException>(() => Enum.Parse<ByteBacked>("300"));

        ByteBacked actual = db.Table<EpRow>().Select(r => Enum.Parse<ByteBacked>(r.Code)).First();
        Assert.Equal((ByteBacked)unchecked((byte)300), actual);
    }
}
