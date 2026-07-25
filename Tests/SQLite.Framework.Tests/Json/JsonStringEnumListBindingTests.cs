using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum JselState
{
    Draft = 0,
    Active = 1,
    Closed = 2,
}

public class JselPayload
{
    public JselState State { get; set; }

    public List<JselState> States { get; set; } = [];
}

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(JselPayload))]
public partial class JselContext : JsonSerializerContext;

public class JselNumericPayload
{
    public JselState State { get; set; }
}

[JsonSerializable(typeof(JselNumericPayload))]
public partial class JselNumericContext : JsonSerializerContext;

[JsonSerializable(typeof(JselPayload))]
public partial class JselNumericPayloadContext : JsonSerializerContext;

[Table("JselNumPayloadDocs")]
public class JselNumPayloadDoc
{
    [Key]
    public int Id { get; set; }

    public JselPayload Data { get; set; } = new();
}

[Table("JselDocs")]
public class JselDoc
{
    [Key]
    public int Id { get; set; }

    public JselPayload Data { get; set; } = new();
}

[Table("JselNumericDocs")]
public class JselNumericDoc
{
    [Key]
    public int Id { get; set; }

    public JselNumericPayload Data { get; set; } = new();
}

public class JsonStringEnumListBindingTests
{
    private static TestDatabase Create(out List<JselDoc> docs)
    {
        TestDatabase db = new(b =>
        {
            b.AddJsonContext(JselContext.Default);
        });
        db.Table<JselDoc>().Schema.CreateTable();
        docs =
        [
            new JselDoc { Id = 1, Data = new JselPayload { State = JselState.Active, States = [JselState.Draft, JselState.Active] } },
            new JselDoc { Id = 2, Data = new JselPayload { State = JselState.Draft, States = [JselState.Closed] } },
            new JselDoc { Id = 3, Data = new JselPayload { State = JselState.Closed, States = [] } },
        ];
        db.Table<JselDoc>().AddRange(docs);
        return db;
    }

    [Fact]
    public void CapturedListContainsEnumMemberMatchesRows()
    {
        using TestDatabase db = Create(out List<JselDoc> docs);
        List<JselState> wanted = [JselState.Active, JselState.Closed];

        List<int> ids = db.Table<JselDoc>().Where(r => wanted.Contains(r.Data.State)).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal(docs.Where(d => wanted.Contains(d.Data.State)).Select(d => d.Id).OrderBy(i => i).ToList(), ids);
    }

    [Fact]
    public void CapturedListContainsNumericJsonEnumMemberMatchesRows()
    {
        using TestDatabase db = new(b =>
        {
            b.AddTypeConverter<JselNumericPayload>(new SQLiteJsonConverter<JselNumericPayload>(JselNumericContext.Default.JselNumericPayload));
        });
        db.Table<JselNumericDoc>().Schema.CreateTable();
        db.Table<JselNumericDoc>().Add(new JselNumericDoc { Id = 1, Data = new JselNumericPayload { State = JselState.Active } });
        db.Table<JselNumericDoc>().Add(new JselNumericDoc { Id = 2, Data = new JselNumericPayload { State = JselState.Draft } });
        List<JselState> wanted = [JselState.Active];

        List<int> ids = db.Table<JselNumericDoc>().Where(r => wanted.Contains(r.Data.State)).Select(r => r.Id).ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void NumericContextCapturedListContainsEnumMemberMatchesRows()
    {
        using TestDatabase db = new(b => b.AddJsonContext(JselNumericPayloadContext.Default));
        db.Table<JselNumPayloadDoc>().Schema.CreateTable();
        List<JselNumPayloadDoc> docs =
        [
            new JselNumPayloadDoc { Id = 1, Data = new JselPayload { State = JselState.Active } },
            new JselNumPayloadDoc { Id = 2, Data = new JselPayload { State = JselState.Draft } },
            new JselNumPayloadDoc { Id = 3, Data = new JselPayload { State = JselState.Closed } },
        ];
        db.Table<JselNumPayloadDoc>().AddRange(docs);
        List<JselState> wanted = [JselState.Active, JselState.Closed];

        List<int> ids = db.Table<JselNumPayloadDoc>().Where(r => wanted.Contains(r.Data.State)).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal(docs.Where(d => wanted.Contains(d.Data.State)).Select(d => d.Id).OrderBy(i => i).ToList(), ids);
    }

    [Fact]
    public void JsonListContainsEnumConstantMatchesRows()
    {
        using TestDatabase db = Create(out List<JselDoc> docs);

        List<int> ids = db.Table<JselDoc>().Where(r => r.Data.States.Contains(JselState.Active)).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal(docs.Where(d => d.Data.States.Contains(JselState.Active)).Select(d => d.Id).OrderBy(i => i).ToList(), ids);
    }

    [Fact]
    public void JsonListIndexOfEnumConstantReadsPosition()
    {
        using TestDatabase db = Create(out List<JselDoc> docs);

        List<int> positions = db.Table<JselDoc>().OrderBy(r => r.Id).Select(r => r.Data.States.IndexOf(JselState.Active)).ToList();

        Assert.Equal(docs.OrderBy(d => d.Id).Select(d => d.Data.States.IndexOf(JselState.Active)).ToList(), positions);
    }
}
