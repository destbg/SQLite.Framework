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

[Flags]
file enum NoZeroAccess
{
    Read = 1,
    Write = 2
}

[Flags]
file enum CompositeOnlyAccess
{
    All = 3
}

[Table("AccessDocs")]
file sealed class AccessDoc
{
    [Key]
    public int Id { get; set; }

    public Access Perms { get; set; }

    public NoZeroAccess NoZeroPerms { get; set; }

    public CompositeOnlyAccess CompositePerms { get; set; }
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

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public void FlagsToStringMatchesDotNet(int raw)
    {
        Access value = (Access)raw;
        using TestDatabase db = new();
        db.Table<AccessDoc>().Schema.CreateTable();
        db.Table<AccessDoc>().Add(new AccessDoc { Id = 1, Perms = value });

        string actual = db.Table<AccessDoc>().Select(x => x.Perms.ToString()).First();

        Assert.Equal(value.ToString(), actual);
    }

    [Fact]
    public void FlagsToStringWithoutZeroMemberMatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<AccessDoc>().Schema.CreateTable();
        db.Table<AccessDoc>().Add(new AccessDoc { Id = 1, NoZeroPerms = 0 });
        db.Table<AccessDoc>().Add(new AccessDoc { Id = 2, NoZeroPerms = NoZeroAccess.Read | NoZeroAccess.Write });

        List<string> actual = db.Table<AccessDoc>().OrderBy(x => x.Id).Select(x => x.NoZeroPerms.ToString()).ToList();
        List<string> oracle = [((NoZeroAccess)0).ToString(), (NoZeroAccess.Read | NoZeroAccess.Write).ToString()];

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void CompositeOnlyFlagsToStringMatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<AccessDoc>().Schema.CreateTable();
        db.Table<AccessDoc>().Add(new AccessDoc { Id = 1, CompositePerms = CompositeOnlyAccess.All });

        string actual = db.Table<AccessDoc>().Select(x => x.CompositePerms.ToString()).First();

        Assert.Equal(CompositeOnlyAccess.All.ToString(), actual);
    }

    [Fact]
    public void FlagsToStringOnComputedValueMatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<AccessDoc>().Schema.CreateTable();
        db.Table<AccessDoc>().Add(new AccessDoc { Id = 1, Perms = Access.Write });

        string actual = db.Table<AccessDoc>().Select(x => (x.Perms | Access.Read).ToString()).First();

        Assert.Equal((Access.Write | Access.Read).ToString(), actual);
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
