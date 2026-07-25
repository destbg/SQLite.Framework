using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum JseqState
{
    Draft = 0,
    Active = 1,
    Closed = 2,
}

public class JseqPayload
{
    public string Name { get; set; } = "";

    public JseqState State { get; set; }

    public JseqState OtherState { get; set; }

    public JseqState? MaybeState { get; set; }
}

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(JseqPayload))]
public partial class JseqContext : JsonSerializerContext;

[Table("JseqDocs")]
public class JseqDoc
{
    [Key]
    public int Id { get; set; }

    public JseqPayload Data { get; set; } = new();
}

public class JsonStringEnumMemberQueryShapeTests
{
    private static TestDatabase Create(out List<JseqDoc> docs)
    {
        TestDatabase db = new(b =>
        {
            b.AddTypeConverter<JseqPayload>(new SQLiteJsonConverter<JseqPayload>(JseqContext.Default.JseqPayload));
        });
        db.Table<JseqDoc>().Schema.CreateTable();
        docs =
        [
            new JseqDoc { Id = 1, Data = new JseqPayload { Name = "a", State = JseqState.Active, OtherState = JseqState.Active, MaybeState = JseqState.Closed } },
            new JseqDoc { Id = 2, Data = new JseqPayload { Name = "b", State = JseqState.Draft, OtherState = JseqState.Closed, MaybeState = null } },
            new JseqDoc { Id = 3, Data = new JseqPayload { Name = "c", State = JseqState.Closed, OtherState = JseqState.Closed, MaybeState = JseqState.Active } },
        ];
        db.Table<JseqDoc>().AddRange(docs);
        return db;
    }

    [Fact]
    public void NotEqualOnEnumMemberFiltersRows()
    {
        using TestDatabase db = Create(out List<JseqDoc> docs);

        List<int> ids = db.Table<JseqDoc>().Where(r => r.Data.State != JseqState.Active).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal(docs.Where(d => d.Data.State != JseqState.Active).Select(d => d.Id).OrderBy(i => i).ToList(), ids);
    }

    [Fact]
    public void EqualsMethodOnEnumMemberMatchesRow()
    {
        using TestDatabase db = Create(out List<JseqDoc> docs);

        List<int> ids = db.Table<JseqDoc>().Where(r => r.Data.State.Equals(JseqState.Closed)).Select(r => r.Id).ToList();

        Assert.Equal(docs.Where(d => d.Data.State.Equals(JseqState.Closed)).Select(d => d.Id).ToList(), ids);
    }

    [Fact]
    public void ReversedOperandsMatchRow()
    {
        using TestDatabase db = Create(out List<JseqDoc> docs);

        List<int> ids = db.Table<JseqDoc>().Where(r => JseqState.Draft == r.Data.State).Select(r => r.Id).ToList();

        Assert.Equal(docs.Where(d => JseqState.Draft == d.Data.State).Select(d => d.Id).ToList(), ids);
    }

    [Fact]
    public void CapturedEnumVariableMatchesRow()
    {
        using TestDatabase db = Create(out List<JseqDoc> docs);
        JseqState wanted = JseqState.Active;

        List<int> ids = db.Table<JseqDoc>().Where(r => r.Data.State == wanted).Select(r => r.Id).ToList();

        Assert.Equal(docs.Where(d => d.Data.State == wanted).Select(d => d.Id).ToList(), ids);
    }

    [Fact]
    public void EnumMemberComparedToOtherMemberTranslates()
    {
        using TestDatabase db = Create(out List<JseqDoc> docs);

        List<int> ids = db.Table<JseqDoc>().Where(r => r.Data.State == r.Data.OtherState).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal(docs.Where(d => d.Data.State == d.Data.OtherState).Select(d => d.Id).OrderBy(i => i).ToList(), ids);
    }

    [Fact]
    public void NullableEnumMemberMatchesRow()
    {
        using TestDatabase db = Create(out List<JseqDoc> docs);

        List<int> ids = db.Table<JseqDoc>().Where(r => r.Data.MaybeState == JseqState.Active).Select(r => r.Id).ToList();

        Assert.Equal(docs.Where(d => d.Data.MaybeState == JseqState.Active).Select(d => d.Id).ToList(), ids);
    }

    [Fact]
    public void NullableEnumMemberNullComparisonMatchesRow()
    {
        using TestDatabase db = Create(out List<JseqDoc> docs);

        List<int> ids = db.Table<JseqDoc>().Where(r => r.Data.MaybeState == null).Select(r => r.Id).ToList();

        Assert.Equal(docs.Where(d => d.Data.MaybeState == null).Select(d => d.Id).ToList(), ids);
    }

    [Fact]
    public void SelectEnumMemberReadsStoredName()
    {
        using TestDatabase db = Create(out List<JseqDoc> docs);

        List<JseqState> states = db.Table<JseqDoc>().OrderBy(r => r.Id).Select(r => r.Data.State).ToList();

        Assert.Equal(docs.OrderBy(d => d.Id).Select(d => d.Data.State).ToList(), states);
    }

    [Fact]
    public void OrderByEnumMemberSortsByStoredName()
    {
        using TestDatabase db = Create(out List<JseqDoc> docs);

        List<int> ids = db.Table<JseqDoc>().OrderBy(r => r.Data.State).ThenBy(r => r.Id).Select(r => r.Id).ToList();

        Assert.Equal([1, 3, 2], ids);
    }
}
