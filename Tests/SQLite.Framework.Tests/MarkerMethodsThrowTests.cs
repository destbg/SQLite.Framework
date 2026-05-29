namespace SQLite.Framework.Tests;

public class MarkerMethodsThrowTests
{
    [Fact]
    public void SQLiteFunctions_AllMarkers_Throw()
    {
        Assert.Throws<InvalidOperationException>(() => SQLiteFunctions.Random());
        Assert.Throws<InvalidOperationException>(() => SQLiteFunctions.RandomBlob(8));
        Assert.Throws<InvalidOperationException>(() => SQLiteFunctions.Glob("a*", "abc"));
        Assert.Throws<InvalidOperationException>(() => SQLiteFunctions.UnixEpoch());
        Assert.Throws<InvalidOperationException>(() => SQLiteFunctions.UnixEpoch("now"));
        Assert.Throws<InvalidOperationException>(() => SQLiteFunctions.Printf("%d", 1));
        Assert.Throws<InvalidOperationException>(() => SQLiteFunctions.Regexp("abc", "a.*"));
        Assert.Throws<InvalidOperationException>(() => SQLiteFunctions.Total(new[] { 1, 2 }));
        Assert.Throws<InvalidOperationException>(() => SQLiteFunctions.TotalChanges());
    }

    [Fact]
    public void SQLiteFTS5Functions_AllMarkers_Throw()
    {
        Assert.Throws<InvalidOperationException>(() => SQLiteFTS5Functions.Match(new object(), "q"));
        Assert.Throws<InvalidOperationException>(() => SQLiteFTS5Functions.Match("col", "q"));
        Assert.Throws<InvalidOperationException>(() => SQLiteFTS5Functions.Match(new object(), b => b.Term("x")));
        Assert.Throws<InvalidOperationException>(() => SQLiteFTS5Functions.Match("col", b => b.Term("x")));
        Assert.Throws<InvalidOperationException>(() => SQLiteFTS5Functions.Rank(new object()));
        Assert.Throws<InvalidOperationException>(() => SQLiteFTS5Functions.Snippet(new object(), "col", "<", ">", "...", 5));
        Assert.Throws<InvalidOperationException>(() => SQLiteFTS5Functions.Highlight(new object(), "col", "<", ">"));
    }

    [Fact]
    public void SQLiteFTS5Builder_AllMarkers_Throw()
    {
        SQLiteFTS5Builder builder = new();

        Assert.Throws<InvalidOperationException>(() => builder.Term("x"));
        Assert.Throws<InvalidOperationException>(() => builder.Phrase("x y"));
        Assert.Throws<InvalidOperationException>(() => builder.Prefix("x"));
        Assert.Throws<InvalidOperationException>(() => builder.Near(3, "a", "b"));
        Assert.Throws<InvalidOperationException>(() => builder.Column("col", true));
    }

    [Fact]
    public void SQLiteJsonFunctions_AllMarkers_Throw()
    {
        Assert.Throws<InvalidOperationException>(() => SQLiteJsonFunctions.Extract<int>("{}", "$.x"));
        Assert.Throws<InvalidOperationException>(() => SQLiteJsonFunctions.Set("{}", "$.x", 1));
        Assert.Throws<InvalidOperationException>(() => SQLiteJsonFunctions.Insert("{}", "$.x", 1));
        Assert.Throws<InvalidOperationException>(() => SQLiteJsonFunctions.Replace("{}", "$.x", 1));
        Assert.Throws<InvalidOperationException>(() => SQLiteJsonFunctions.Remove("{}", "$.x"));
        Assert.Throws<InvalidOperationException>(() => SQLiteJsonFunctions.Type("{}", "$.x"));
        Assert.Throws<InvalidOperationException>(() => SQLiteJsonFunctions.Valid("{}"));
        Assert.Throws<InvalidOperationException>(() => SQLiteJsonFunctions.Patch("{}", "{}"));
        Assert.Throws<InvalidOperationException>(() => SQLiteJsonFunctions.ArrayLength("[]"));
        Assert.Throws<InvalidOperationException>(() => SQLiteJsonFunctions.ArrayLength("[]", "$"));
        Assert.Throws<InvalidOperationException>(() => SQLiteJsonFunctions.Minify("{}"));
#if !SQLITECIPHER
        Assert.Throws<InvalidOperationException>(() => SQLiteJsonFunctions.ToJsonb("{}"));
        Assert.Throws<InvalidOperationException>(() => SQLiteJsonFunctions.ExtractJsonb<int>([], "$"));
#endif
    }

    [Fact]
    public void SQLiteFrameBoundary_AllMarkers_Throw()
    {
        Assert.Throws<InvalidOperationException>(() => SQLiteFrameBoundary.UnboundedPreceding());
        Assert.Throws<InvalidOperationException>(() => SQLiteFrameBoundary.CurrentRow());
        Assert.Throws<InvalidOperationException>(() => SQLiteFrameBoundary.UnboundedFollowing());
        Assert.Throws<InvalidOperationException>(() => SQLiteFrameBoundary.Preceding(1));
        Assert.Throws<InvalidOperationException>(() => SQLiteFrameBoundary.Following(1));

        System.Reflection.ConstructorInfo ctor = typeof(SQLiteFrameBoundary)
            .GetConstructor(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, [])!;
        Assert.NotNull(ctor.Invoke(null));
    }

    [Fact]
    public void SQLiteWindowFunctions_AllMarkers_Throw()
    {
        Assert.Throws<InvalidOperationException>(() => SQLiteWindowFunctions.Sum(1));
        Assert.Throws<InvalidOperationException>(() => SQLiteWindowFunctions.Avg(1));
        Assert.Throws<InvalidOperationException>(() => SQLiteWindowFunctions.Min(1));
        Assert.Throws<InvalidOperationException>(() => SQLiteWindowFunctions.Max(1));
        Assert.Throws<InvalidOperationException>(() => SQLiteWindowFunctions.Count());
        Assert.Throws<InvalidOperationException>(() => SQLiteWindowFunctions.Count(1));
        Assert.Throws<InvalidOperationException>(() => SQLiteWindowFunctions.RowNumber());
        Assert.Throws<InvalidOperationException>(() => SQLiteWindowFunctions.Rank());
        Assert.Throws<InvalidOperationException>(() => SQLiteWindowFunctions.DenseRank());
        Assert.Throws<InvalidOperationException>(() => SQLiteWindowFunctions.PercentRank());
        Assert.Throws<InvalidOperationException>(() => SQLiteWindowFunctions.CumeDist());
        Assert.Throws<InvalidOperationException>(() => SQLiteWindowFunctions.NTile(4));
        Assert.Throws<InvalidOperationException>(() => SQLiteWindowFunctions.Lag(1));
        Assert.Throws<InvalidOperationException>(() => SQLiteWindowFunctions.Lag(1, 2));
        Assert.Throws<InvalidOperationException>(() => SQLiteWindowFunctions.Lag(1, 2, 0));
        Assert.Throws<InvalidOperationException>(() => SQLiteWindowFunctions.Lead(1));
        Assert.Throws<InvalidOperationException>(() => SQLiteWindowFunctions.Lead(1, 2));
        Assert.Throws<InvalidOperationException>(() => SQLiteWindowFunctions.Lead(1, 2, 0));
        Assert.Throws<InvalidOperationException>(() => SQLiteWindowFunctions.FirstValue(1));
        Assert.Throws<InvalidOperationException>(() => SQLiteWindowFunctions.LastValue(1));
        Assert.Throws<InvalidOperationException>(() => SQLiteWindowFunctions.NthValue(1, 2));
    }

    [Fact]
    public void SQLiteWindow_AllMarkers_Throw()
    {
        SQLiteWindow<int> spec = default;

        Assert.Throws<InvalidOperationException>(() => spec.AsValue());
        Assert.Throws<InvalidOperationException>(() => spec.Over());
        Assert.Throws<InvalidOperationException>(() => spec.Filter(true));
        Assert.Throws<InvalidOperationException>(() => spec.PartitionBy(1));
        Assert.Throws<InvalidOperationException>(() => spec.ThenPartitionBy(1));
        Assert.Throws<InvalidOperationException>(() => spec.OrderBy(1));
        Assert.Throws<InvalidOperationException>(() => spec.OrderByDescending(1));
        Assert.Throws<InvalidOperationException>(() => spec.ThenOrderBy(1));
        Assert.Throws<InvalidOperationException>(() => spec.ThenOrderByDescending(1));
        Assert.Throws<InvalidOperationException>(() => spec.Rows(null!, null!));
        Assert.Throws<InvalidOperationException>(() => spec.Range(null!, null!));
        Assert.Throws<InvalidOperationException>(() => spec.Groups(null!, null!));
        Assert.Throws<InvalidOperationException>(() =>
        {
            int _ = spec;
        });
    }
}
