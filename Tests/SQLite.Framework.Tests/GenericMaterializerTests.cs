using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GenericMaterializerTests
{
    [Fact]
    public void ExecuteQuery_via_generic_class_repo_returns_entities()
    {
        using TestDatabase db = new();
        db.Table<GenericRepoEntityA>().Schema.CreateTable();
        db.Table<GenericRepoEntityB>().Schema.CreateTable();
        db.CreateCommand("INSERT INTO GenericRepoEntityA (Id, Name) VALUES (1, 'alpha')", []).ExecuteNonQuery();
        db.CreateCommand("INSERT INTO GenericRepoEntityB (Id, Description) VALUES (1, 'beta')", []).ExecuteNonQuery();

        RepoA repoA = new(db);
        RepoB repoB = new(db);

        List<GenericRepoEntityA> resultA = repoA.Get("SELECT Id, Name FROM GenericRepoEntityA");
        List<GenericRepoEntityB> resultB = repoB.Get("SELECT Id, Description FROM GenericRepoEntityB");

        Assert.Single(resultA);
        Assert.Equal("alpha", resultA[0].Name);
        Assert.Single(resultB);
        Assert.Equal("beta", resultB[0].Description);
    }

    [Fact]
    public void Generic_select_projection_materializes_per_concrete_dto()
    {
        using TestDatabase db = new();
        db.Table<NomenclatureA>().Schema.CreateTable();
        db.Table<NomenclatureB>().Schema.CreateTable();
        db.Table<NomenclatureA>().Add(new NomenclatureA { Id = 1, Name = "alphaA" });
        db.Table<NomenclatureB>().Add(new NomenclatureB { Id = 2, Name = "alphaB" });

        DtoA aDto = ProjectFirst<NomenclatureA, DtoA>(db.Table<NomenclatureA>());
        DtoB bDto = ProjectFirst<NomenclatureB, DtoB>(db.Table<NomenclatureB>());

        Assert.Equal(1, aDto.Id);
        Assert.Equal("alphaA", aDto.Name);
        Assert.Equal(2, bDto.Id);
        Assert.Equal("alphaB", bDto.Name);
    }

    private static TResult ProjectFirst<T, TResult>(IQueryable<T> query)
        where T : INomenclature
        where TResult : NomenclatureDtoBase, new()
        => query.Select(f => new TResult { Id = f.Id, Name = f.Name }).First();

    [Fact]
    public void ExecuteQuery_via_generic_method_returns_entities()
    {
        using TestDatabase db = new();
        db.Table<GenericMethodEntityA>().Schema.CreateTable();
        db.Table<GenericMethodEntityB>().Schema.CreateTable();
        db.CreateCommand("INSERT INTO GenericMethodEntityA (Id, Tag) VALUES (1, 'one')", []).ExecuteNonQuery();
        db.CreateCommand("INSERT INTO GenericMethodEntityB (Id, Code) VALUES (2, 'two')", []).ExecuteNonQuery();

        List<GenericMethodEntityA> aRows = RunQuery<GenericMethodEntityA>(db, "SELECT Id, Tag FROM GenericMethodEntityA");
        List<GenericMethodEntityB> bRows = RunQuery<GenericMethodEntityB>(db, "SELECT Id, Code FROM GenericMethodEntityB");

        Assert.Single(aRows);
        Assert.Equal("one", aRows[0].Tag);
        Assert.Single(bRows);
        Assert.Equal("two", bRows[0].Code);
    }

    private static List<T> RunQuery<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(TestDatabase db, string sql)
    {
        return db.CreateCommand(sql, []).ExecuteQuery<T>().ToList();
    }

    private class RepoA : Repo<GenericRepoEntityA>
    {
        public RepoA(TestDatabase db) : base(db) { }
    }

    private class RepoB : Repo<GenericRepoEntityB>
    {
        public RepoB(TestDatabase db) : base(db) { }
    }

    private class Repo<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>
    {
        private readonly TestDatabase db;

        public Repo(TestDatabase db)
        {
            this.db = db;
        }

        public List<T> Get(string sql) => db.CreateCommand(sql, []).ExecuteQuery<T>().ToList();
    }
}

public class GenericRepoEntityA
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class GenericRepoEntityB
{
    [Key]
    public int Id { get; set; }

    public string Description { get; set; } = string.Empty;
}

public class GenericMethodEntityA
{
    [Key]
    public int Id { get; set; }

    public string Tag { get; set; } = string.Empty;
}

public class GenericMethodEntityB
{
    [Key]
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;
}

public interface INomenclature
{
    int Id { get; }
    string Name { get; }
}

public class NomenclatureDtoBase
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class DtoA : NomenclatureDtoBase
{
}

public class DtoB : NomenclatureDtoBase
{
}

public class NomenclatureA : INomenclature
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class NomenclatureB : INomenclature
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
