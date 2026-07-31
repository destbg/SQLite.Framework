using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26qDerivedTexts")]
public class H26qDerivedText
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string Derived { get; set; } = "";
}

[Table("H26qPositionTexts")]
public class H26qPositionText
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Position { get; set; }
}

[Table("H26qCheckedTexts")]
public class H26qCheckedText
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("H26qIndexedTexts")]
public class H26qIndexedText
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("H26qDefaultedTexts")]
public class H26qDefaultedText
{
    [Key]
    public int Id { get; set; }

    public string? Code { get; set; }
}

public class StringMethodsInDeclarationSiteExpressionTests
{
    [Fact]
    public void PadLeftInAComputedColumnKeepsThePaddedValue()
    {
        using ModelTestDatabase db = new(mb => mb.Entity<H26qDerivedText>()
            .Computed(r => r.Derived, r => r.Name.PadLeft(8)));
        db.Schema.CreateTable<H26qDerivedText>();
        db.Table<H26qDerivedText>().AddRange(DerivedRows());

        List<string> expected = DerivedRows().OrderBy(r => r.Id).Select(r => r.Name.PadLeft(8)).ToList();
        List<string> actual = db.Table<H26qDerivedText>().OrderBy(r => r.Id).Select(r => r.Derived).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InsertInAComputedColumnKeepsTheInsertedValue()
    {
        using ModelTestDatabase db = new(mb => mb.Entity<H26qDerivedText>()
            .Computed(r => r.Derived, r => r.Name.Insert(2, "--")));
        db.Schema.CreateTable<H26qDerivedText>();
        db.Table<H26qDerivedText>().AddRange(DerivedRows());

        List<string> expected = DerivedRows().OrderBy(r => r.Id).Select(r => r.Name.Insert(2, "--")).ToList();
        List<string> actual = db.Table<H26qDerivedText>().OrderBy(r => r.Id).Select(r => r.Derived).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RemoveWithACountInAComputedColumnKeepsTheShortenedValue()
    {
        using ModelTestDatabase db = new(mb => mb.Entity<H26qDerivedText>()
            .Computed(r => r.Derived, r => r.Name.Remove(1, 1)));
        db.Schema.CreateTable<H26qDerivedText>();
        db.Table<H26qDerivedText>().AddRange(DerivedRows());

        List<string> expected = DerivedRows().OrderBy(r => r.Id).Select(r => r.Name.Remove(1, 1)).ToList();
        List<string> actual = db.Table<H26qDerivedText>().OrderBy(r => r.Id).Select(r => r.Derived).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IndexOfWithAStartIndexInAComputedColumnKeepsThePosition()
    {
        using ModelTestDatabase db = new(mb => mb.Entity<H26qPositionText>()
            .Computed(r => r.Position, r => r.Name.IndexOf("a", 2)));
        db.Schema.CreateTable<H26qPositionText>();
        db.Table<H26qPositionText>().AddRange(PositionRows());

        List<int> expected = PositionRows().OrderBy(r => r.Id).Select(r => r.Name.IndexOf("a", 2)).ToList();
        List<int> actual = db.Table<H26qPositionText>().OrderBy(r => r.Id).Select(r => r.Position).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CompareToInACheckConstraintAcceptsTheSameRows()
    {
        using ModelTestDatabase db = new(mb => mb.Entity<H26qCheckedText>()
            .Check(r => r.Name.CompareTo("b") < 0, name: "CK_H26qCheckedTexts_Name"));
        db.Schema.CreateTable<H26qCheckedText>();

        List<H26qCheckedText> allowed = CheckedRows().Where(r => r.Name.CompareTo("b") < 0).ToList();
        db.Table<H26qCheckedText>().AddRange(allowed);

        Assert.Equal(allowed.Count, db.Table<H26qCheckedText>().Count());
        Assert.ThrowsAny<Exception>(() =>
            db.Table<H26qCheckedText>().Add(new H26qCheckedText { Id = 99, Name = "zeta" }));
    }

    [Fact]
    public void StringCompareInAPartialIndexFilterCoversTheSameRows()
    {
        using ModelTestDatabase db = new(mb => mb.Entity<H26qIndexedText>()
            .Index(r => r.Name, name: "h26q_partial_name_idx", unique: true, filter: r => string.Compare(r.Name, "b") < 0));
        db.Schema.CreateTable<H26qIndexedText>();

        db.Table<H26qIndexedText>().Add(new H26qIndexedText { Id = 1, Name = "alpha" });
        db.Table<H26qIndexedText>().Add(new H26qIndexedText { Id = 2, Name = "gamma" });
        db.Table<H26qIndexedText>().Add(new H26qIndexedText { Id = 3, Name = "gamma" });

        Assert.ThrowsAny<Exception>(() =>
            db.Table<H26qIndexedText>().Add(new H26qIndexedText { Id = 4, Name = "alpha" }));
        Assert.Equal(3, db.Table<H26qIndexedText>().Count());
    }

    [Fact]
    public void PadRightInAnIndexExpressionKeepsTheIndexedRowsReadable()
    {
        using ModelTestDatabase db = new(mb => mb.Entity<H26qIndexedText>()
            .Index(r => r.Name.PadRight(8), name: "h26q_padright_idx"));
        db.Schema.CreateTable<H26qIndexedText>();

        List<H26qIndexedText> rows =
        [
            new H26qIndexedText { Id = 1, Name = "alpha" },
            new H26qIndexedText { Id = 2, Name = "be" }
        ];
        db.Table<H26qIndexedText>().AddRange(rows);

        List<string> expected = rows.OrderBy(r => r.Id).Select(r => r.Name.PadRight(8)).ToList();
        List<string> actual = db.Table<H26qIndexedText>().OrderBy(r => r.Id).Select(r => r.Name.PadRight(8)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PadLeftInAColumnDefaultExpressionStoresThePaddedValue()
    {
        using ModelTestDatabase db = new(mb => mb.Entity<H26qDefaultedText>()
            .Default(r => r.Code, () => "7".PadLeft(4, '0')));
        db.Schema.CreateTable<H26qDefaultedText>();

        db.Table<H26qDefaultedText>().Add(new H26qDefaultedText { Id = 1 });

        Assert.Equal("7".PadLeft(4, '0'), db.Table<H26qDefaultedText>().Select(r => r.Code).Single());
    }

    private static List<H26qDerivedText> DerivedRows()
    {
        return
        [
            new H26qDerivedText { Id = 1, Name = "alpha" },
            new H26qDerivedText { Id = 2, Name = "be" },
            new H26qDerivedText { Id = 3, Name = "gamma-delta" }
        ];
    }

    private static List<H26qPositionText> PositionRows()
    {
        return
        [
            new H26qPositionText { Id = 1, Name = "alpha" },
            new H26qPositionText { Id = 2, Name = "be" },
            new H26qPositionText { Id = 3, Name = "gamma-delta" }
        ];
    }

    private static List<H26qCheckedText> CheckedRows()
    {
        return
        [
            new H26qCheckedText { Id = 1, Name = "alpha" },
            new H26qCheckedText { Id = 2, Name = "apple" },
            new H26qCheckedText { Id = 3, Name = "gamma" }
        ];
    }
}
