using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26fChainedRows")]
public class H26fChainedRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }

    public int B { get; set; }
}

public class ChainedWithColumnsTests
{
    [Fact]
    public void ChainingWithColumnsWritesEveryDeclaredColumnOnAdd()
    {
        using TestDatabase db = new(null, nameof(ChainingWithColumnsWritesEveryDeclaredColumnOnAdd));
        db.Table<H26fChainedRow>().Schema.CreateTable();

        db.Table<H26fChainedRow>()
            .WithColumns(c => c.Set(x => x.A, 11))
            .WithColumns(c => c.Set(x => x.B, 22))
            .Add(new H26fChainedRow { Id = 1, A = 1, B = 2 });

        Assert.Equal(11, db.ExecuteScalar<int>("SELECT \"A\" FROM \"H26fChainedRows\" WHERE \"Id\" = 1"));
        Assert.Equal(22, db.ExecuteScalar<int>("SELECT \"B\" FROM \"H26fChainedRows\" WHERE \"Id\" = 1"));
    }

    [Fact]
    public void ChainingWithColumnsWritesEveryDeclaredColumnOnUpdate()
    {
        using TestDatabase db = new(null, nameof(ChainingWithColumnsWritesEveryDeclaredColumnOnUpdate));
        db.Table<H26fChainedRow>().Schema.CreateTable();
        H26fChainedRow row = new() { Id = 1, A = 1, B = 2 };
        db.Table<H26fChainedRow>().Add(row);

        db.Table<H26fChainedRow>()
            .WithColumns(c => c.Set(x => x.A, 11))
            .WithColumns(c => c.Set(x => x.B, 22))
            .Update(row);

        Assert.Equal(11, db.ExecuteScalar<int>("SELECT \"A\" FROM \"H26fChainedRows\" WHERE \"Id\" = 1"));
        Assert.Equal(22, db.ExecuteScalar<int>("SELECT \"B\" FROM \"H26fChainedRows\" WHERE \"Id\" = 1"));
    }
}
