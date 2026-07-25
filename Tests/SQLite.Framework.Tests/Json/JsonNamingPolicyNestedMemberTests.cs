using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class JnpnIgnoredInner
{
    public string Name { get; set; } = "";

    [JsonPropertyName("nm")]
    public string Tag { get; set; } = "";
}

public class JnpnInner
{
    public string DeepName { get; set; } = "";

    public int DeepScore { get; set; }
}

public class JnpnPayload
{
    public string SomeName { get; set; } = "";

    [JsonPropertyName("forced")]
    public int Score { get; set; }

    public JnpnInner Inner { get; set; } = new();

    [JsonIgnore]
    public JnpnIgnoredInner Extra { get; set; } = new();

    public Dictionary<string, int> Map { get; set; } = new();
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(JnpnPayload))]
public partial class JnpnContext : JsonSerializerContext;

[Table("JnpnDocs")]
public class JnpnDoc
{
    [Key]
    public int Id { get; set; }

    public JnpnPayload Data { get; set; } = new();
}

public class JsonNamingPolicyNestedMemberTests
{
    private static TestDatabase Create()
    {
        TestDatabase db = new(b =>
        {
            b.AddTypeConverter<JnpnPayload>(new SQLiteJsonConverter<JnpnPayload>(JnpnContext.Default.JnpnPayload));
            b.AddTypeConverter<Dictionary<string, int>>(new SQLiteJsonConverter<Dictionary<string, int>>(JnpnContext.Default.DictionaryStringInt32));
        });
        db.Table<JnpnDoc>().Schema.CreateTable();
        db.Table<JnpnDoc>().Add(new JnpnDoc
        {
            Id = 1,
            Data = new JnpnPayload
            {
                SomeName = "root",
                Score = 7,
                Inner = new JnpnInner { DeepName = "deep", DeepScore = 3 },
                Map = new Dictionary<string, int> { ["MyKey"] = 11 },
            },
        });
        db.Table<JnpnDoc>().Add(new JnpnDoc
        {
            Id = 2,
            Data = new JnpnPayload
            {
                SomeName = "other",
                Score = 8,
                Inner = new JnpnInner { DeepName = "shallow", DeepScore = 4 },
                Map = new Dictionary<string, int> { ["MyKey"] = 22 },
            },
        });
        return db;
    }

    [Fact]
    public void NestedTwoLevelPathUsesPolicyNames()
    {
        using TestDatabase db = Create();

        List<int> ids = db.Table<JnpnDoc>().Where(r => r.Data.Inner.DeepName == "deep").Select(r => r.Id).ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void NestedTwoLevelSelectReadsStoredValue()
    {
        using TestDatabase db = Create();

        List<int> scores = db.Table<JnpnDoc>().OrderBy(r => r.Id).Select(r => r.Data.Inner.DeepScore).ToList();

        Assert.Equal([3, 4], scores);
    }

    [Fact]
    public void JsonPropertyNameOverridesPolicy()
    {
        using TestDatabase db = Create();

        List<int> ids = db.Table<JnpnDoc>().Where(r => r.Data.Score == 8).Select(r => r.Id).ToList();

        Assert.Equal([2], ids);
    }

    [Fact]
    public void OrderByPolicyNamedMemberSorts()
    {
        using TestDatabase db = Create();

        List<int> ids = db.Table<JnpnDoc>().OrderBy(r => r.Data.SomeName).Select(r => r.Id).ToList();

        Assert.Equal([2, 1], ids);
    }

    [Fact]
    public void DictionaryKeyIsNotRenamedByPolicy()
    {
        using TestDatabase db = Create();

        List<int> values = db.Table<JnpnDoc>().OrderBy(r => r.Id).Select(r => r.Data.Map["MyKey"]).ToList();

        Assert.Equal([11, 22], values);
    }

    [Fact]
    public void IgnoredNestedTypeMemberFallsBackToRawName()
    {
        using TestDatabase db = Create();

        List<int> ids = db.Table<JnpnDoc>().Where(r => r.Data.Extra.Name == "x").Select(r => r.Id).ToList();

        Assert.Empty(ids);
    }

    [Fact]
    public void IgnoredNestedTypeMemberFallsBackToAttributeName()
    {
        using TestDatabase db = Create();

        List<int> ids = db.Table<JnpnDoc>().Where(r => r.Data.Extra.Tag == "x").Select(r => r.Id).ToList();

        Assert.Empty(ids);
    }
}
