using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Flags]
file enum Access
{
    None = 0,
    Read = 1,
    Write = 2
}

file enum BigEnum : ulong
{
    Zero = 0,
    Max = ulong.MaxValue
}

[Table("AccessDocs")]
file sealed class AccessDoc
{
    [Key]
    public int Id { get; set; }

    public Access Perms { get; set; }
}

[Table("BigEnumRows")]
file sealed class BigEnumRow
{
    [Key]
    public int Id { get; set; }

    public BigEnum Value { get; set; }
}

public class EnumFlagsProbeBugTests
{
    [Fact]
    public void CombinedFlagsValueRoundTrips()
    {
        using TestDatabase db = new();
        db.Table<AccessDoc>().Schema.CreateTable();
        db.Table<AccessDoc>().Add(new AccessDoc { Id = 1, Perms = Access.Read | Access.Write });

        AccessDoc back = db.Table<AccessDoc>().First();

        Assert.Equal(Access.Read | Access.Write, back.Perms);
    }

    [Fact]
    public void UlongBackedEnumToStringDoesNotOverflow()
    {
        using TestDatabase db = new();
        db.Table<BigEnumRow>().Schema.CreateTable();
        db.Table<BigEnumRow>().Add(new BigEnumRow { Id = 1, Value = BigEnum.Max });

        string s = db.Table<BigEnumRow>().Select(x => x.Value.ToString()).First();

        Assert.Equal("Max", s);
    }

    [Fact]
    public void EnumParseComparisonWorksUnderTextStorage()
    {
        using TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Table<AccessDoc>().Schema.CreateTable();
        db.Table<AccessDoc>().Add(new AccessDoc { Id = 1, Perms = Access.Read });

        List<int> ids = db.Table<AccessDoc>()
            .Where(d => d.Perms == Enum.Parse<Access>("Read"))
            .Select(d => d.Id)
            .ToList();

        Assert.Equal(new[] { 1 }, ids);
    }
}
