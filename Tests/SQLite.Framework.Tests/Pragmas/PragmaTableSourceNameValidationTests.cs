using SQLite.Framework;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class PragmaTableSourceNameValidationTests
{
    [Fact]
    public void PragmaNameWithAClosingParenthesisIsRejected()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"SecLPragmaTarget\" (\"Id\" INTEGER PRIMARY KEY)");

        Assert.Throws<ArgumentException>(() =>
            new SQLitePragmaTable<PragmaTableInfo>(db, "pragma_table_info) --", "SecLPragmaTarget").ToList());
    }

    [Fact]
    public void AnEmptyPragmaNameIsRejected()
    {
        using TestDatabase db = new();

        Assert.Throws<ArgumentException>(() =>
            new SQLitePragmaTable<PragmaTableInfo>(db, "", "SecLPragmaTarget").ToList());
    }

    [Fact]
    public void PragmaNameWithAnInjectedTableReferenceIsRejected()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"SecLPragmaAsked\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Execute("CREATE TABLE \"SecLPragmaOther\" (\"Id\" INTEGER PRIMARY KEY, \"Note\" TEXT)");

        Assert.Throws<ArgumentException>(() =>
            new SQLitePragmaTable<PragmaTableInfo>(db, "pragma_table_info('SecLPragmaOther') --", "SecLPragmaAsked").ToList());
    }
}
