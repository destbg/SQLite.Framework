using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class JcwapPayload
{
    public string SomeName { get; set; } = "";

    public int Score { get; set; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(JcwapPayload))]
public partial class JcwapContext : JsonSerializerContext;

[Table("JcwapDocs")]
public class JcwapDoc
{
    [Key]
    public int Id { get; set; }

    public JcwapPayload Data { get; set; } = new();
}

public class JcwapPlainAttributeProvider : ICustomAttributeProvider
{
    public object[] GetCustomAttributes(bool inherit)
    {
        return [];
    }

    public object[] GetCustomAttributes(Type attributeType, bool inherit)
    {
        return [];
    }

    public bool IsDefined(Type attributeType, bool inherit)
    {
        return false;
    }
}

public class JsonContractWithoutAttributeProviderTests
{
    [Fact]
    public void MemberWithoutReflectionAttributeProviderFallsBackToMemberName()
    {
        JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = JcwapContext.Default.WithAddedModifier(info =>
            {
                foreach (JsonPropertyInfo property in info.Properties)
                {
                    property.AttributeProvider = new JcwapPlainAttributeProvider();
                }
            }),
        };
        JsonTypeInfo<JcwapPayload> typeInfo = (JsonTypeInfo<JcwapPayload>)options.GetTypeInfo(typeof(JcwapPayload));

        using TestDatabase db = new(b =>
        {
            b.AddTypeConverter<JcwapPayload>(new SQLiteJsonConverter<JcwapPayload>(typeInfo));
        });
        db.Table<JcwapDoc>().Schema.CreateTable();
        db.Table<JcwapDoc>().Add(new JcwapDoc { Id = 1, Data = new JcwapPayload { SomeName = "abc", Score = 7 } });

        List<int> ids = db.Table<JcwapDoc>().Where(r => r.Data.SomeName == "abc").Select(r => r.Id).ToList();

        Assert.Empty(ids);
    }
}
