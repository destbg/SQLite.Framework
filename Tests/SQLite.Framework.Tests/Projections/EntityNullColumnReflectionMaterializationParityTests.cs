using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class PositionalPrivateSetterEntity
{
    public PositionalPrivateSetterEntity(int id)
    {
        Id = id;
    }

    [Key] public int Id { get; }
    public string? Extra { get; private set; } = "ctor-default";
}

public class ParameterlessPrivateSetterEntity
{
    [Key] public int Id { get; set; }
    public string? Extra { get; private set; } = "ctor-default";
}

public class EntityNullColumnReflectionMaterializationParityTests
{
    [Fact]
    public void PositionalEntity_NullColumn_OverwritesConstructorDefaultWithNull()
    {
        using TestDatabase db = new();
        db.Table<PositionalPrivateSetterEntity>().Schema.CreateTable();
        db.Execute("INSERT INTO \"PositionalPrivateSetterEntity\" (\"Id\", \"Extra\") VALUES (1, NULL)");

        PositionalPrivateSetterEntity row = db.Table<PositionalPrivateSetterEntity>().First(r => r.Id == 1);

        Assert.Null(row.Extra);
    }

    [Fact]
    public void PositionalEntity_NonNullColumn_ReadsStoredValue()
    {
        using TestDatabase db = new();
        db.Table<PositionalPrivateSetterEntity>().Schema.CreateTable();
        db.Execute("INSERT INTO \"PositionalPrivateSetterEntity\" (\"Id\", \"Extra\") VALUES (1, 'stored')");

        PositionalPrivateSetterEntity row = db.Table<PositionalPrivateSetterEntity>().First(r => r.Id == 1);

        Assert.Equal("stored", row.Extra);
    }

    [Fact]
    public void ParameterlessEntity_NullColumn_OverwritesConstructorDefaultWithNull()
    {
        using TestDatabase db = new();
        db.Table<ParameterlessPrivateSetterEntity>().Schema.CreateTable();
        db.Execute("INSERT INTO \"ParameterlessPrivateSetterEntity\" (\"Id\", \"Extra\") VALUES (1, NULL)");

        ParameterlessPrivateSetterEntity row = db.Table<ParameterlessPrivateSetterEntity>().First(r => r.Id == 1);

        Assert.Null(row.Extra);
    }

    [Fact]
    public void ParameterlessEntity_NonNullColumn_ReadsStoredValue()
    {
        using TestDatabase db = new();
        db.Table<ParameterlessPrivateSetterEntity>().Schema.CreateTable();
        db.Execute("INSERT INTO \"ParameterlessPrivateSetterEntity\" (\"Id\", \"Extra\") VALUES (1, 'stored')");

        ParameterlessPrivateSetterEntity row = db.Table<ParameterlessPrivateSetterEntity>().First(r => r.Id == 1);

        Assert.Equal("stored", row.Extra);
    }
}
