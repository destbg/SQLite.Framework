#if !SQLITECIPHER
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class JbnpPayload
{
    public string SomeName { get; set; } = "";

    public int Score { get; set; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(JbnpPayload))]
public partial class JbnpContext : JsonSerializerContext;

[Table("JbnpDocs")]
public class JbnpDoc
{
    [Key]
    public int Id { get; set; }

    public JbnpPayload Data { get; set; } = new();
}

public class JsonbNamingPolicyMemberTests
{
    private static TestDatabase Create()
    {
        TestDatabase db = new(b =>
        {
            b.AddTypeConverter<JbnpPayload>(new SQLiteJsonbConverter<JbnpPayload>(JbnpContext.Default.JbnpPayload));
        });
        db.Table<JbnpDoc>().Schema.CreateTable();
        db.Table<JbnpDoc>().Add(new JbnpDoc { Id = 1, Data = new JbnpPayload { SomeName = "abc", Score = 7 } });
        db.Table<JbnpDoc>().Add(new JbnpDoc { Id = 2, Data = new JbnpPayload { SomeName = "def", Score = 8 } });
        return db;
    }

    [Fact]
    public void WhereOnPolicyNamedMemberMatchesRow()
    {
        using TestDatabase db = Create();

        List<int> ids = db.Table<JbnpDoc>().Where(r => r.Data.SomeName == "abc").Select(r => r.Id).ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void SelectPolicyNamedMemberReadsStoredValue()
    {
        using TestDatabase db = Create();

        List<int> scores = db.Table<JbnpDoc>().OrderBy(r => r.Id).Select(r => r.Data.Score).ToList();

        Assert.Equal([7, 8], scores);
    }
}
#endif
