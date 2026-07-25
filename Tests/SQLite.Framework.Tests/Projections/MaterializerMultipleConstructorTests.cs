using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("mat_two_ctor_writable")]
public class MatTwoCtorWritableEntity
{
    public MatTwoCtorWritableEntity(int id)
    {
        Id = id;
    }

    public MatTwoCtorWritableEntity(int id, string name)
    {
        Id = id;
        Name = name;
    }

    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("mat_narrow_ctor_first")]
public class MatNarrowCtorFirstEntity
{
    public MatNarrowCtorFirstEntity(int id)
    {
        Id = id;
    }

    public MatNarrowCtorFirstEntity(int id, string name)
    {
        Id = id;
        Name = name;
    }

    [Key]
    public int Id { get; }

    public string Name { get; } = "";
}

[Table("mat_wide_ctor_first")]
public class MatWideCtorFirstEntity
{
    public MatWideCtorFirstEntity(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public MatWideCtorFirstEntity(int id)
    {
        Id = id;
    }

    [Key]
    public int Id { get; }

    public string Name { get; } = "";
}

[Table("mat_unrelated_ctor_first")]
public class MatUnrelatedCtorFirstEntity
{
    public MatUnrelatedCtorFirstEntity(int seed, string prefix)
    {
        Id = seed * 100;
        Name = prefix + seed;
    }

    public MatUnrelatedCtorFirstEntity(int id, string name, bool flag)
    {
        Id = id;
        Name = name;
        Flag = flag;
    }

    [Key]
    public int Id { get; }

    public string Name { get; } = "";

    public bool Flag { get; }
}

public class MaterializerMultipleConstructorTests
{
    [Fact]
    public void WritablePropertiesTwoConstructorsRoundTrips()
    {
        using TestDatabase db = new();
        db.Table<MatTwoCtorWritableEntity>().Schema.CreateTable();
        db.Table<MatTwoCtorWritableEntity>().Add(new MatTwoCtorWritableEntity(5, "hello"));

        MatTwoCtorWritableEntity original = new(5, "hello");
        MatTwoCtorWritableEntity actual = db.Table<MatTwoCtorWritableEntity>().First();

        Assert.Equal(original.Id, actual.Id);
        Assert.Equal(original.Name, actual.Name);
    }

    [Fact]
    public void GetOnlyPropertiesNarrowConstructorFirstKeepsWiderProperty()
    {
        using TestDatabase db = new();
        db.Table<MatNarrowCtorFirstEntity>().Schema.CreateTable();
        db.Table<MatNarrowCtorFirstEntity>().Add(new MatNarrowCtorFirstEntity(1, "kept"));

        MatNarrowCtorFirstEntity original = new(1, "kept");
        MatNarrowCtorFirstEntity actual = db.Table<MatNarrowCtorFirstEntity>().First();

        Assert.Equal(original.Id, actual.Id);
        Assert.Equal(original.Name, actual.Name);
    }

    [Fact]
    public void GetOnlyPropertiesWiderConstructorFirstRoundTrips()
    {
        using TestDatabase db = new();
        db.Table<MatWideCtorFirstEntity>().Schema.CreateTable();
        db.Table<MatWideCtorFirstEntity>().Add(new MatWideCtorFirstEntity(1, "kept"));

        MatWideCtorFirstEntity original = new(1, "kept");
        MatWideCtorFirstEntity actual = db.Table<MatWideCtorFirstEntity>().First();

        Assert.Equal(original.Id, actual.Id);
        Assert.Equal(original.Name, actual.Name);
    }

    [Fact]
    public void ConstructorWithUnrelatedParameterNamesIsSkipped()
    {
        using TestDatabase db = new();
        db.Table<MatUnrelatedCtorFirstEntity>().Schema.CreateTable();
        db.Table<MatUnrelatedCtorFirstEntity>().Add(new MatUnrelatedCtorFirstEntity(3, "keep", true));

        MatUnrelatedCtorFirstEntity original = new(3, "keep", true);
        MatUnrelatedCtorFirstEntity actual = db.Table<MatUnrelatedCtorFirstEntity>().First();

        Assert.Equal(original.Id, actual.Id);
        Assert.Equal(original.Name, actual.Name);
        Assert.Equal(original.Flag, actual.Flag);
    }
}
