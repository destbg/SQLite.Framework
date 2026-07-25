using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum JsevState
{
    Draft = 0,
    Active = 1,
}

public enum JsevForeign
{
    One = 1,
    Two = 2,
}

[Flags]
public enum JsevFlags
{
    None = 0,
    Read = 1,
    Write = 2,
}

public class JsevPayload
{
    public JsevState State { get; set; }

    public JsevFlags Flags { get; set; }

    public int Level { get; set; }
}

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(JsevPayload))]
public partial class JsevContext : JsonSerializerContext;

public class JsevNumericPayload
{
    public JsevState State { get; set; }
}

[JsonSerializable(typeof(JsevNumericPayload))]
public partial class JsevNumericContext : JsonSerializerContext;

[Table("JsevDocs")]
public class JsevDoc
{
    [Key]
    public int Id { get; set; }

    public JsevPayload Data { get; set; } = new();
}

[Table("JsevNumericDocs")]
public class JsevNumericDoc
{
    [Key]
    public int Id { get; set; }

    public JsevNumericPayload Data { get; set; } = new();
}

public class JsevStateTextConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Text;

    public object? ToDatabase(object? value)
    {
        return value?.ToString();
    }

    public object? FromDatabase(object? value)
    {
        return value is string s ? Enum.Parse<JsevState>(s) : null;
    }
}

public class JsonStringEnumBindingVariantTests
{
    private static TestDatabase Create(Action<SQLiteOptionsBuilder>? extra = null)
    {
        TestDatabase db = new(b =>
        {
            extra?.Invoke(b);
            b.AddTypeConverter<JsevPayload>(new SQLiteJsonConverter<JsevPayload>(JsevContext.Default.JsevPayload));
        });
        db.Table<JsevDoc>().Schema.CreateTable();
        db.Table<JsevDoc>().Add(new JsevDoc
        {
            Id = 1,
            Data = new JsevPayload { State = JsevState.Active, Flags = JsevFlags.Read | JsevFlags.Write, Level = 2 },
        });
        db.Table<JsevDoc>().Add(new JsevDoc
        {
            Id = 2,
            Data = new JsevPayload { State = JsevState.Draft, Flags = (JsevFlags)8, Level = 1 },
        });
        return db;
    }

    [Fact]
    public void TextEnumStorageStillMatchesStoredName()
    {
        using TestDatabase db = Create(b => b.UseEnumStorage(EnumStorageMode.Text));

        List<int> ids = db.Table<JsevDoc>().Where(r => r.Data.State == JsevState.Active).Select(r => r.Id).ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void DefinedFlagsCombinationMatchesStoredNames()
    {
        using TestDatabase db = Create();

        List<int> ids = db.Table<JsevDoc>().Where(r => r.Data.Flags == (JsevFlags.Read | JsevFlags.Write)).Select(r => r.Id).ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void UndefinedFlagsCombinationMatchesNumericForm()
    {
        using TestDatabase db = Create();

        List<int> ids = db.Table<JsevDoc>().Where(r => r.Data.Flags == (JsevFlags)8).Select(r => r.Id).ToList();

        Assert.Equal([2], ids);
    }

    [Fact]
    public void ForeignEnumCastComparisonMatchesNumericJson()
    {
        using TestDatabase db = Create();

        List<int> ids = db.Table<JsevDoc>().Where(r => (JsevForeign)r.Data.Level == JsevForeign.Two).Select(r => r.Id).ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void EnumWithOwnConverterStillBindsSerializedName()
    {
        using TestDatabase db = Create(b => b.AddTypeConverter<JsevState>(new JsevStateTextConverter()));

        List<int> ids = db.Table<JsevDoc>().Where(r => r.Data.State == JsevState.Active).Select(r => r.Id).ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void NumericJsonEnumStillMatchesUnderDefaultStorage()
    {
        using TestDatabase db = new(b =>
        {
            b.AddTypeConverter<JsevNumericPayload>(new SQLiteJsonConverter<JsevNumericPayload>(JsevNumericContext.Default.JsevNumericPayload));
        });
        db.Table<JsevNumericDoc>().Schema.CreateTable();
        db.Table<JsevNumericDoc>().Add(new JsevNumericDoc { Id = 1, Data = new JsevNumericPayload { State = JsevState.Active } });
        db.Table<JsevNumericDoc>().Add(new JsevNumericDoc { Id = 2, Data = new JsevNumericPayload { State = JsevState.Draft } });

        List<int> ids = db.Table<JsevNumericDoc>().Where(r => r.Data.State == JsevState.Active).Select(r => r.Id).ToList();

        Assert.Equal([1], ids);
    }
}
