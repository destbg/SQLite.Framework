using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Internals.Models;
using SQLite.Framework.Models;

namespace SQLite.Framework.Tests;

public class McfObjDto
{
    public object? Tag { get; set; }
}

public class McfIfaceDto
{
    public IComparable? Rank { get; set; }
}

public class McfCtorDto
{
    public McfCtorDto(int id, EsfPart? part)
    {
        Id = id;
        Part = part;
    }

    public int Id { get; set; }

    public EsfPart? Part { get; set; }
}

public class MaterializerColumnFallbackTests
{
    private static SQLQuery MakeQuery(IReadOnlyDictionary<string, Type>? selectValueTypes)
    {
        return new SQLQuery
        {
            Sql = "",
            Parameters = [],
            CreateObject = null,
            Reverse = false,
            ThrowOnEmpty = false,
            ElementAtSemantic = false,
            ThrowOnMoreThanOne = false,
            SelectValueTypes = selectValueTypes,
        };
    }

    private static object? Materialize([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] Type elementType, string sql, Dictionary<string, int> columns, IReadOnlyDictionary<string, Type>? selectValueTypes)
    {
        using SQLiteDatabase db = new(new SQLiteOptionsBuilder(":memory:").DisableReflectionFallback(false).Build());
        SQLQuery query = MakeQuery(selectValueTypes);
        using SQLiteDataReader reader = db.CreateCommand(sql, []).ExecuteReader();
        Assert.True(reader.Read());
        SQLiteQueryContext context = BuildQueryObject.BuildContext(reader, columns, query);
        return BuildQueryObject.BuildMaterializer(reader, columns, query, elementType)(context);
    }

    [Fact]
    public void ObjectSlotWithoutRecordedTypeReadsRawValue()
    {
        McfObjDto dto = (McfObjDto)Materialize(
            typeof(McfObjDto),
            "SELECT 7 AS \"Tag\"",
            new Dictionary<string, int> { ["Tag"] = 0 },
            new Dictionary<string, Type> { ["Other"] = typeof(int) })!;

        Assert.Equal(7L, dto.Tag);
    }

    [Fact]
    public void ObjectSlotWithObjectRecordedTypeReadsRawValue()
    {
        McfObjDto dto = (McfObjDto)Materialize(
            typeof(McfObjDto),
            "SELECT 7 AS \"Tag\"",
            new Dictionary<string, int> { ["Tag"] = 0 },
            new Dictionary<string, Type> { ["Tag"] = typeof(object) })!;

        Assert.Equal(7L, dto.Tag);
    }

    [Fact]
    public void ObjectSlotWithoutAnyRecordedTypesReadsRawValue()
    {
        McfObjDto dto = (McfObjDto)Materialize(
            typeof(McfObjDto),
            "SELECT 7 AS \"Tag\"",
            new Dictionary<string, int> { ["Tag"] = 0 },
            null)!;

        Assert.Equal(7L, dto.Tag);
    }

    [Fact]
    public void InterfaceSlotWithoutRecordedTypeReadsRawValue()
    {
        McfIfaceDto dto = (McfIfaceDto)Materialize(
            typeof(McfIfaceDto),
            "SELECT 7 AS \"Rank\"",
            new Dictionary<string, int> { ["Rank"] = 0 },
            new Dictionary<string, Type> { ["Other"] = typeof(int) })!;

        Assert.Equal(7L, dto.Rank);
    }

    [Fact]
    public void PositionalEntityParameterWithoutColumnsReadsNull()
    {
        McfCtorDto dto = (McfCtorDto)Materialize(
            typeof(McfCtorDto),
            "SELECT 4 AS \"id\"",
            new Dictionary<string, int> { ["id"] = 0 },
            null)!;

        Assert.Equal(4, dto.Id);
        Assert.Null(dto.Part);
    }
}
