using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class FullTextMatchProjectionOverloadTests
{
    [Fact]
    public void AColumnMatchInAProjectionTranslatesToASubquery()
    {
        using TestDatabase db = new(null, nameof(AColumnMatchInAProjectionTranslatesToASubquery));

        SQLiteCommand command = db.Table<H26kFlagDoc>()
            .Select(d => SQLiteFTS5Functions.Match(d.Body, "apple"))
            .ToSqlCommand();

        Assert.Equal(
            "SELECT h0.\"rowid\" IN (SELECT \"rowid\" FROM \"H26kFlagDocs\" WHERE \"H26kFlagDocs\" MATCH @p0) AS \"4\"\nFROM \"H26kFlagDocs\" AS h0",
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void AnEntityMatchInAProjectionTranslatesToASubquery()
    {
        using TestDatabase db = new(null, nameof(AnEntityMatchInAProjectionTranslatesToASubquery));

        SQLiteCommand command = db.Table<H26kFlagDoc>()
            .Select(d => SQLiteFTS5Functions.Match(d, "apple"))
            .ToSqlCommand();

        Assert.Equal(
            "SELECT h0.\"rowid\" IN (SELECT \"rowid\" FROM \"H26kFlagDocs\" WHERE \"H26kFlagDocs\" MATCH @p0) AS \"4\"\nFROM \"H26kFlagDocs\" AS h0",
            command.CommandText.Replace("\r\n", "\n"));
    }
}
