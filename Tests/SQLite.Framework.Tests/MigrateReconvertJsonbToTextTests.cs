#if !SQLITECIPHER
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("reconvert_jsonb_to_text_rows")]
public class ReconvertJsonbToTextRow
{
    [Key]
    public int Id { get; set; }

    public Address Data { get; set; } = new();
}

public class MigrateReconvertJsonbToTextTests
{
    private static TestDatabase Db()
    {
        return new TestDatabase(b =>
            b.TypeConverters[typeof(Address)] = new SQLiteJsonConverter<Address>(TestJsonContext.Default.Address));
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void ReconvertJsonbColumnToJsonTextKeepsValue(MigrateMode mode)
    {
        using TestDatabase db = Db();
        db.Execute("CREATE TABLE \"reconvert_jsonb_to_text_rows\" (\"Id\" INTEGER PRIMARY KEY, \"Data\" BLOB NOT NULL)");
        db.Execute("INSERT INTO \"reconvert_jsonb_to_text_rows\" (\"Id\", \"Data\") VALUES (1, jsonb('{\"Street\":\"1\",\"City\":\"A\"}'))");

        db.Table<ReconvertJsonbToTextRow>().Schema.Migrate(mode, m => m.Reconvert(x => x.Data));

        Address? data = db.Table<ReconvertJsonbToTextRow>().Select(r => r.Data).Single();

        Assert.Equal("1", data?.Street);
        Assert.Equal("A", data?.City);
    }
}
#endif
