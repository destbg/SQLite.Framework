using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("TwoConstructorKey")]
public class TwoConstructorKeyRow
{
    public TwoConstructorKeyRow()
    {
    }

    public TwoConstructorKeyRow(int id, string name)
    {
        Id = id;
        Name = name;
    }

    [Key]
    public int Id { get; }

    public string Name { get; set; } = "";
}

public class NestedMemberValue
{
    public int X { get; set; }
}

public class NestedMemberDto
{
    public int A { get; set; }

    public NestedMemberValue Nested { get; set; } = new();
}

[Table("NestedMemberSource")]
public class NestedMemberSourceRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }

    public int X { get; set; }
}

public class ProjectionMaterializationDivergenceTests
{
    [Fact]
    public void GetOnlyKeyWithParameterlessAndParameterizedConstructorRoundTrips()
    {
        using TestDatabase db = new();
        db.Table<TwoConstructorKeyRow>().Schema.CreateTable();
        db.Table<TwoConstructorKeyRow>().Add(new TwoConstructorKeyRow(5, "x"));

        Assert.Equal(5, db.Table<TwoConstructorKeyRow>().Single().Id);
    }

    [Fact]
    public void SetOperationMemberMemberBindingKeepsColumnOrder()
    {
        using TestDatabase db = new();
        db.Table<NestedMemberSourceRow>().Schema.CreateTable();
        db.Table<NestedMemberSourceRow>().Add(new NestedMemberSourceRow { Id = 1, A = 5, X = 9 });

        List<NestedMemberDto> rows = db.Table<NestedMemberSourceRow>().Select(x => new NestedMemberDto { A = x.A, Nested = { X = x.X } })
            .Union(db.Table<NestedMemberSourceRow>().Select(x => new NestedMemberDto { A = x.A, Nested = new NestedMemberValue { X = x.X } }))
            .ToList();

        Assert.All(rows, r => Assert.Equal(5, r.A));
        Assert.All(rows, r => Assert.Equal(9, r.Nested.X));
    }
}
