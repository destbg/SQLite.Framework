using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum H22vEmptyInt
{
}

public enum H22vEmptyLong : ulong
{
}

[Flags]
public enum H22vTextFlags
{
    None = 0,
    One = 1,
    Two = 2
}

[Table("H22vMarkerRows")]
public class H22vMarkerRow
{
    [Key]
    public int Id { get; set; }

    public H22vEmptyInt Marker { get; set; }

    public H22vEmptyLong WideMarker { get; set; }
}

[Table("H22vFlagRows")]
public class H22vFlagRow
{
    [Key]
    public int Id { get; set; }

    public H22vTextFlags Flags { get; set; }
}

[Table("H22vPriceRows")]
public class H22vPriceRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public decimal Price { get; set; }

    public decimal? MaybePrice { get; set; }
}

[Table("H22vSourceRows")]
public class H22vSourceRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public DateTime Moment { get; set; }
}

public class H22vNamedDto
{
    public int K { get; set; }

    public string V { get; set; } = "";
}

public class H22vCtorDto
{
    public H22vCtorDto(int k, string v)
    {
        K = k;
        V = v;
    }

    public int K { get; }

    public string V { get; }
}

public readonly struct H22vBadConvertible
{
    public static implicit operator string(H22vBadConvertible value)
    {
        throw new FormatException("residual conversion failure");
    }
}

public static class H22vBoom
{
    public static int Calc(int seed)
    {
        throw new InvalidOperationException("residual calc failure");
    }

    public static List<int> Explode(List<int> source)
    {
        throw new InvalidOperationException("residual explode failure");
    }

    public static string Render(string value)
    {
        return value + "!";
    }
}

public class H22vRenderer
{
    private readonly string suffix;

    public H22vRenderer(string suffix)
    {
        this.suffix = suffix;
    }

    public string Decorate(string value)
    {
        return value + suffix;
    }
}

public class ResidualTranslationCoverageTests
{
    [Fact]
    public void MemberlessEnumHexFormatUsesPrintf()
    {
        using TestDatabase db = SetupMarkers(nameof(MemberlessEnumHexFormatUsesPrintf));

        List<string> actual = db.Table<H22vMarkerRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Marker.ToString("X"))
            .ToList();

        Assert.Equal(["00000005", "0000000B"], actual);
    }

    [Fact]
    public void MemberlessEnumWideHexFormatUsesLongPrintf()
    {
        using TestDatabase db = SetupMarkers(nameof(MemberlessEnumWideHexFormatUsesLongPrintf));

        List<string> actual = db.Table<H22vMarkerRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.WideMarker.ToString("X"))
            .ToList();

        Assert.Equal(["0000000000000007", "0000000000000009"], actual);
    }

    [Fact]
    public void MemberlessEnumDecimalFormatCastsToText()
    {
        using TestDatabase db = SetupMarkers(nameof(MemberlessEnumDecimalFormatCastsToText));

        List<string> actual = db.Table<H22vMarkerRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Marker.ToString("D"))
            .ToList();

        Assert.Equal(["5", "11"], actual);
    }

    [Fact]
    public void TextStoredFlagsConversionCarriesOperandParameters()
    {
        using TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text), nameof(TextStoredFlagsConversionCarriesOperandParameters));
        db.Table<H22vFlagRow>().Schema.CreateTable();
        db.Table<H22vFlagRow>().AddRange(
        [
            new H22vFlagRow { Id = 1, Flags = H22vTextFlags.One },
            new H22vFlagRow { Id = 2, Flags = H22vTextFlags.Two }
        ]);
        H22vTextFlags captured = H22vTextFlags.One | H22vTextFlags.Two;
        int capturedId = 1;

        List<int> actual = db.Table<H22vFlagRow>()
            .OrderBy(r => r.Id)
            .Select(r => (int)(r.Id == capturedId ? r.Flags : captured))
            .ToList();

        Assert.Equal([1, 3], actual);
    }

    [Fact]
    public void TextStoredMemberlessEnumConversionCastsDirectly()
    {
        using TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text), nameof(TextStoredMemberlessEnumConversionCastsDirectly));
        db.Table<H22vMarkerRow>().Schema.CreateTable();
        db.Table<H22vMarkerRow>().AddRange(
        [
            new H22vMarkerRow { Id = 1, Marker = (H22vEmptyInt)5, WideMarker = (H22vEmptyLong)7 }
        ]);

        List<int> actual = db.Table<H22vMarkerRow>()
            .Select(r => (int)r.Marker)
            .ToList();

        Assert.Equal([5], actual);
    }

    [Fact]
    public void EnumerableFoldUnwrapsTargetInvocationExceptions()
    {
        using TestDatabase db = SetupSources(nameof(EnumerableFoldUnwrapsTargetInvocationExceptions));
        int[] captured = [];

        Assert.Throws<InvalidOperationException>(() => db.Table<H22vSourceRow>()
            .Where(r => r.Id == captured.First())
            .ToList());
    }

    [Fact]
    public void ConstantMethodFoldUnwrapsTargetInvocationExceptions()
    {
        using TestDatabase db = SetupSources(nameof(ConstantMethodFoldUnwrapsTargetInvocationExceptions));

        Assert.Throws<FormatException>(() => db.Table<H22vSourceRow>()
            .Where(r => r.Moment == DateTime.Parse("nope"))
            .ToList());
    }

    [Fact]
    public void InlineArraySystemElementFoldUnwrapsTargetInvocationExceptions()
    {
        using TestDatabase db = SetupSources(nameof(InlineArraySystemElementFoldUnwrapsTargetInvocationExceptions));

        Assert.Throws<ArgumentOutOfRangeException>(() => db.Table<H22vSourceRow>()
            .Select(r => string.Join(",", new[] { "x".Substring(5), r.Name }))
            .ToList());
    }

    [Fact]
    public void InlineArrayCapturedCallElementEvaluatesOnTheClient()
    {
        using TestDatabase db = SetupSources(nameof(InlineArrayCapturedCallElementEvaluatesOnTheClient));
        List<H22vSourceRow> local = SourceRows();
        string captured = "a";

        List<string> expected = local
            .OrderBy(r => r.Id)
            .Select(r => string.Join(",", new[] { H22vBoom.Render(captured), r.Name }))
            .ToList();

        List<string> actual = db.Table<H22vSourceRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Join(",", new[] { H22vBoom.Render(captured), r.Name }))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InlineArrayCapturedInstanceCallElementEvaluatesOnTheClient()
    {
        using TestDatabase db = SetupSources(nameof(InlineArrayCapturedInstanceCallElementEvaluatesOnTheClient));
        List<H22vSourceRow> local = SourceRows();
        H22vRenderer renderer = new("!");

        List<string> expected = local
            .OrderBy(r => r.Id)
            .Select(r => string.Join(",", new[] { renderer.Decorate("a"), r.Name }))
            .ToList();

        List<string> actual = db.Table<H22vSourceRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Join(",", new[] { renderer.Decorate("a"), r.Name }))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InlineArrayGlobalNamespaceCallElementEvaluatesOnTheClient()
    {
        using TestDatabase db = SetupSources(nameof(InlineArrayGlobalNamespaceCallElementEvaluatesOnTheClient));
        List<H22vSourceRow> local = SourceRows();
        string captured = "a";

        List<string> expected = local
            .OrderBy(r => r.Id)
            .Select(r => string.Join(",", new[] { GlobalResidualHelper.Fragment(captured), r.Name }))
            .ToList();

        List<string> actual = db.Table<H22vSourceRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Join(",", new[] { GlobalResidualHelper.Fragment(captured), r.Name }))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TypeAsConstantKeepsMatchingValues()
    {
        using TestDatabase db = SetupSources(nameof(TypeAsConstantKeepsMatchingValues));
        object boxed = "a!";

        List<int> actual = db.Table<H22vSourceRow>()
            .Where(r => r.Name == boxed as string)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([1], actual);
    }

    [Fact]
    public void TypeAsConstantYieldsNullForMismatchedValues()
    {
        using TestDatabase db = SetupSources(nameof(TypeAsConstantYieldsNullForMismatchedValues));
        object boxed = 42;

        List<int> actual = db.Table<H22vSourceRow>()
            .Where(r => (boxed as string) == null)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([1, 2], actual);
    }

    [Fact]
    public void ThrowingConversionOperatorSurfacesTheOriginalException()
    {
        using TestDatabase db = SetupSources(nameof(ThrowingConversionOperatorSurfacesTheOriginalException));
        H22vBadConvertible captured = default;

        FormatException ex = Assert.Throws<FormatException>(() => db.Table<H22vSourceRow>()
            .Where(r => r.Name == (string)captured)
            .ToList());

        Assert.Equal("residual conversion failure", ex.Message);
    }

    [Fact]
    public void TextDecimalThenByPropagatesToChainedThenBy()
    {
        using TestDatabase db = SetupPrices(nameof(TextDecimalThenByPropagatesToChainedThenBy));
        List<H22vPriceRow> local = PriceRows();

        List<int> expected = local
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .ThenBy(r => r.Price)
            .ThenBy(r => r.Id)
            .Select(r => r.Id)
            .ToList();

        List<int> actual = db.Table<H22vPriceRow>()
            .OrderBy(r => r.Name)
            .ThenBy(r => r.Price)
            .ThenBy(r => r.Id)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TextDecimalThenByPropagatesToLaterOrderBy()
    {
        using TestDatabase db = SetupPrices(nameof(TextDecimalThenByPropagatesToLaterOrderBy));
        List<H22vPriceRow> local = PriceRows();

        List<int> expected = local
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .ThenBy(r => r.Price)
            .OrderBy(r => r.Price)
            .Select(r => r.Id)
            .ToList();

        List<int> actual = db.Table<H22vPriceRow>()
            .OrderBy(r => r.Name)
            .ThenBy(r => r.Price)
            .OrderBy(r => r.Price)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableTextDecimalMaxWithoutSelectorCastsTheAggregate()
    {
        using TestDatabase db = SetupPrices(nameof(NullableTextDecimalMaxWithoutSelectorCastsTheAggregate));

        decimal? actual = db.Table<H22vPriceRow>()
            .Select(r => r.MaybePrice)
            .Max();

        Assert.Equal(30.5m, actual);
    }

    [Fact]
    public void TextDecimalMinWithoutSelectorCastsTheAggregate()
    {
        using TestDatabase db = SetupPrices(nameof(TextDecimalMinWithoutSelectorCastsTheAggregate));

        decimal actual = db.Table<H22vPriceRow>()
            .Select(r => r.Price)
            .Min();

        Assert.Equal(1.5m, actual);
    }

    [Fact]
    public void TextDecimalDatabaseStringMinStaysUncast()
    {
        using TestDatabase db = SetupPrices(nameof(TextDecimalDatabaseStringMinStaysUncast));

        string? actual = db.Table<H22vPriceRow>()
            .Select(r => r.Name)
            .Min();

        Assert.Equal("a", actual);
    }

    [Fact]
    public void InlineArraySystemNamespaceCallElementFoldsToALiteral()
    {
        using TestDatabase db = SetupSources(nameof(InlineArraySystemNamespaceCallElementFoldsToALiteral));
        List<H22vSourceRow> local = SourceRows();
        int captured = 42;

        List<string> expected = local
            .OrderBy(r => r.Id)
            .Select(r => string.Join(",", new[] { Convert.ToString(captured), r.Name }))
            .ToList();

        List<string> actual = db.Table<H22vSourceRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Join(",", new[] { Convert.ToString(captured), r.Name }))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedScalarDayOfWeekQueryableKeepsTheDayMapping()
    {
        using TestDatabase db = SetupSources(nameof(CapturedScalarDayOfWeekQueryableKeepsTheDayMapping));
        List<H22vSourceRow> local = SourceRows();
        IQueryable<DayOfWeek> inner = db.Table<H22vSourceRow>().Select(r => r.Moment.DayOfWeek);

        List<int> expected = local
            .Join(
                local.Select(x => x.Moment.DayOfWeek),
                r => r.Moment.DayOfWeek,
                d => d,
                (r, d) => r.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> actual = db.Table<H22vSourceRow>()
            .Join(inner, r => r.Moment.DayOfWeek, d => d, (r, d) => r.Id)
            .ToList()
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedFilteredEntityQueryableFallsBackToEntityColumns()
    {
        using TestDatabase db = SetupSources(nameof(CapturedFilteredEntityQueryableFallsBackToEntityColumns));
        List<H22vSourceRow> local = SourceRows();
        IQueryable<H22vSourceRow> inner = db.Table<H22vSourceRow>().Where(r => r.Id > 0);

        List<string> expected = local
            .Join(local.Where(r => r.Id > 0), r => r.Id, d => d.Id, (r, d) => d.Name)
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToList();

        List<string> actual = db.Table<H22vSourceRow>()
            .Join(inner, r => r.Id, d => d.Id, (r, d) => d.Name)
            .ToList()
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WrappedConditionalConstructedMemberResolvesThroughTheBranch()
    {
        using TestDatabase db = SetupSources(nameof(WrappedConditionalConstructedMemberResolvesThroughTheBranch));
        List<H22vSourceRow> local = SourceRows();

        List<int> expected = local
            .OrderBy(r => r.Id)
            .Select(r => new { Inner = r.Id == 1 ? new H22vNamedDto { K = r.Id, V = r.Name } : null })
            .Select(x => x.Inner == null ? -1 : x.Inner.K)
            .ToList();

        List<int> actual = db.Table<H22vSourceRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { Inner = r.Id == 1 ? new H22vNamedDto { K = r.Id, V = r.Name } : null })
            .Select(x => x.Inner == null ? -1 : x.Inner.K)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InlineArrayLinqNamespaceCallElementFoldsToTheFirstValue()
    {
        using TestDatabase db = SetupSources(nameof(InlineArrayLinqNamespaceCallElementFoldsToTheFirstValue));
        List<H22vSourceRow> local = SourceRows();
        List<string> captured = ["z"];

        List<string> expected = local
            .OrderBy(r => r.Id)
            .Select(r => string.Join(",", new[] { Enumerable.First(captured), r.Name }))
            .ToList();

        List<string> actual = db.Table<H22vSourceRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Join(",", new[] { Enumerable.First(captured), r.Name }))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TwoLevelConstructedMemberChainFoldsToTheLeaf()
    {
        using TestDatabase db = SetupSources(nameof(TwoLevelConstructedMemberChainFoldsToTheLeaf));
        List<H22vSourceRow> local = SourceRows();

        List<int> expected = local
            .OrderBy(r => r.Id)
            .Select(r => new { Inner = new H22vNamedDto { K = r.Id, V = r.Name } })
            .Select(x => x.Inner.K)
            .ToList();

        List<int> actual = db.Table<H22vSourceRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { Inner = new H22vNamedDto { K = r.Id, V = r.Name } })
            .Select(x => x.Inner.K)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TextDecimalThenByDescendingPropagatesThroughTheChain()
    {
        using TestDatabase db = SetupPrices(nameof(TextDecimalThenByDescendingPropagatesThroughTheChain));
        List<H22vPriceRow> local = PriceRows();

        List<int> expected = local
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .ThenBy(r => r.Id)
            .ThenByDescending(r => r.Price)
            .Select(r => r.Id)
            .ToList();

        List<int> actual = db.Table<H22vPriceRow>()
            .OrderBy(r => r.Name)
            .ThenBy(r => r.Id)
            .ThenByDescending(r => r.Price)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TextDecimalThenByPropagatesToLaterOrderByDescending()
    {
        using TestDatabase db = SetupPrices(nameof(TextDecimalThenByPropagatesToLaterOrderByDescending));
        List<H22vPriceRow> local = PriceRows();

        List<int> expected = local
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .ThenBy(r => r.Price)
            .OrderByDescending(r => r.Price)
            .Select(r => r.Id)
            .ToList();

        List<int> actual = db.Table<H22vPriceRow>()
            .OrderBy(r => r.Name)
            .ThenBy(r => r.Price)
            .OrderByDescending(r => r.Price)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedComputedProjectionQueryableJoinsWithoutRecordedTypes()
    {
        using TestDatabase db = SetupSources(nameof(CapturedComputedProjectionQueryableJoinsWithoutRecordedTypes));
        List<H22vSourceRow> local = SourceRows();
        IQueryable<H22vNamedDto> inner = db.Table<H22vSourceRow>()
            .Select(r => new H22vNamedDto { K = r.Id * 2, V = r.Name + "x" });

        List<string> expected = local
            .Join(
                local.Select(r => new H22vNamedDto { K = r.Id * 2, V = r.Name + "x" }),
                r => r.Id * 2,
                d => d.K,
                (r, d) => d.V)
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToList();

        List<string> actual = db.Table<H22vSourceRow>()
            .Join(inner, r => r.Id * 2, d => d.K, (r, d) => d.V)
            .ToList()
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UnionTextDecimalThenByChainsShareTheWrapBoundary()
    {
        using TestDatabase db = SetupPrices(nameof(UnionTextDecimalThenByChainsShareTheWrapBoundary));
        List<H22vPriceRow> local = PriceRows();

        List<int> expected = local.Where(r => r.Id != 3)
            .Union(local.Where(r => r.Id == 3))
            .OrderBy(r => r.Id)
            .ThenBy(r => r.Price)
            .ThenBy(r => r.Name, StringComparer.Ordinal)
            .Select(r => r.Id)
            .ToList();

        List<int> actual = db.Table<H22vPriceRow>().Where(r => r.Id != 3)
            .Union(db.Table<H22vPriceRow>().Where(r => r.Id == 3))
            .OrderBy(r => r.Id)
            .ThenBy(r => r.Price)
            .ThenBy(r => r.Name)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UnionTextDecimalThenByDescendingChainsShareTheWrapBoundary()
    {
        using TestDatabase db = SetupPrices(nameof(UnionTextDecimalThenByDescendingChainsShareTheWrapBoundary));
        List<H22vPriceRow> local = PriceRows();

        List<int> expected = local.Where(r => r.Id != 3)
            .Union(local.Where(r => r.Id == 3))
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .ThenByDescending(r => r.Price)
            .ThenBy(r => r.Id)
            .Select(r => r.Id)
            .ToList();

        List<int> actual = db.Table<H22vPriceRow>().Where(r => r.Id != 3)
            .Union(db.Table<H22vPriceRow>().Where(r => r.Id == 3))
            .OrderBy(r => r.Name)
            .ThenByDescending(r => r.Price)
            .ThenBy(r => r.Id)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TextDecimalThenByPropagatesThroughDescendingSiblings()
    {
        using TestDatabase db = SetupPrices(nameof(TextDecimalThenByPropagatesThroughDescendingSiblings));
        List<H22vPriceRow> local = PriceRows();

        List<int> expected = local
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .ThenByDescending(r => r.Id)
            .ThenBy(r => r.Price)
            .Select(r => r.Id)
            .ToList();

        List<int> actual = db.Table<H22vPriceRow>()
            .OrderBy(r => r.Name)
            .ThenByDescending(r => r.Id)
            .ThenBy(r => r.Price)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TextDecimalThenByPropagatesToTheDescendingRoot()
    {
        using TestDatabase db = SetupPrices(nameof(TextDecimalThenByPropagatesToTheDescendingRoot));
        List<H22vPriceRow> local = PriceRows();

        List<int> expected = local
            .OrderByDescending(r => r.Name, StringComparer.Ordinal)
            .ThenBy(r => r.Price)
            .Select(r => r.Id)
            .ToList();

        List<int> actual = db.Table<H22vPriceRow>()
            .OrderByDescending(r => r.Name)
            .ThenBy(r => r.Price)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TextDecimalThenByAtTheChainTailPropagatesBackwards()
    {
        using TestDatabase db = SetupPrices(nameof(TextDecimalThenByAtTheChainTailPropagatesBackwards));
        List<H22vPriceRow> local = PriceRows();

        List<int> expected = local
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .ThenBy(r => r.Id)
            .ThenBy(r => r.Price)
            .Select(r => r.Id)
            .ToList();

        List<int> actual = db.Table<H22vPriceRow>()
            .OrderBy(r => r.Name)
            .ThenBy(r => r.Id)
            .ThenBy(r => r.Price)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TextStoredMemberlessEnumFormatsUsePrintfAndCast()
    {
        using TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text), nameof(TextStoredMemberlessEnumFormatsUsePrintfAndCast));
        db.Table<H22vMarkerRow>().Schema.CreateTable();
        db.Table<H22vMarkerRow>().AddRange(
        [
            new H22vMarkerRow { Id = 1, Marker = (H22vEmptyInt)5, WideMarker = (H22vEmptyLong)7 }
        ]);

        List<string> decimals = db.Table<H22vMarkerRow>().Select(r => r.Marker.ToString("D")).ToList();
        List<string> hexes = db.Table<H22vMarkerRow>().Select(r => r.Marker.ToString("X")).ToList();
        List<string> wideHexes = db.Table<H22vMarkerRow>().Select(r => r.WideMarker.ToString("X")).ToList();

        Assert.Equal(["5"], decimals);
        Assert.Equal(["00000005"], hexes);
        Assert.Equal(["0000000000000007"], wideHexes);
    }

    [Fact]
    public void CapturedProjectedQueryableJoinsByItsColumns()
    {
        using TestDatabase db = SetupSources(nameof(CapturedProjectedQueryableJoinsByItsColumns));
        List<H22vSourceRow> local = SourceRows();
        IQueryable<H22vNamedDto> inner = db.Table<H22vSourceRow>()
            .Select(r => new H22vNamedDto { K = r.Id, V = r.Name });

        List<string> expected = local
            .Join(
                local.Select(r => new H22vNamedDto { K = r.Id, V = r.Name }),
                r => r.Id,
                d => d.K,
                (r, d) => d.V)
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToList();

        List<string> actual = db.Table<H22vSourceRow>()
            .Join(inner, r => r.Id, d => d.K, (r, d) => d.V)
            .ToList()
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConditionalConstructedRootMemberFoldsToTheBranchValue()
    {
        using TestDatabase db = SetupSources(nameof(ConditionalConstructedRootMemberFoldsToTheBranchValue));
        List<H22vSourceRow> local = SourceRows();

        List<int> expected = local
            .OrderBy(r => r.Id)
            .Select(r => r.Id == 1 ? new H22vNamedDto { K = r.Id, V = r.Name } : null)
            .Select(d => d == null ? -1 : d.K)
            .ToList();

        List<int> actual = db.Table<H22vSourceRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Id == 1 ? new H22vNamedDto { K = r.Id, V = r.Name } : null)
            .Select(d => d == null ? -1 : d.K)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PositionalConstructedRootMemberFoldsToTheArgument()
    {
        using TestDatabase db = SetupSources(nameof(PositionalConstructedRootMemberFoldsToTheArgument));
        List<H22vSourceRow> local = SourceRows();

        List<int> expected = local
            .OrderBy(r => r.Id)
            .Select(r => new H22vCtorDto(r.Id, r.Name))
            .Select(d => d.K)
            .ToList();

        List<int> actual = db.Table<H22vSourceRow>()
            .OrderBy(r => r.Id)
            .Select(r => new H22vCtorDto(r.Id, r.Name))
            .Select(d => d.K)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedPositionalProjectionQueryableFallsBackToPropertyColumns()
    {
        using TestDatabase db = SetupSources(nameof(CapturedPositionalProjectionQueryableFallsBackToPropertyColumns));
        List<H22vSourceRow> local = SourceRows();
        IQueryable<H22vCtorDto> inner = db.Table<H22vSourceRow>()
            .Select(r => new H22vCtorDto(r.Id, r.Name));

        List<string> expected = local
            .Join(
                local.Select(r => new H22vCtorDto(r.Id, r.Name)),
                r => r.Id,
                d => d.K,
                (r, d) => d.V)
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToList();

        List<string> actual = db.Table<H22vSourceRow>()
            .Join(inner, r => r.Id, d => d.K, (r, d) => d.V)
            .ToList()
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22vSourceRow> SourceRows()
    {
        return
        [
            new H22vSourceRow { Id = 1, Name = "a!", Moment = new DateTime(2026, 7, 20) },
            new H22vSourceRow { Id = 2, Name = "b!", Moment = new DateTime(2026, 7, 21) }
        ];
    }

    private static List<H22vPriceRow> PriceRows()
    {
        return
        [
            new H22vPriceRow { Id = 1, Name = "a", Price = 10.5m, MaybePrice = 30.5m },
            new H22vPriceRow { Id = 2, Name = "a", Price = 1.5m, MaybePrice = null },
            new H22vPriceRow { Id = 3, Name = "b", Price = 20.5m, MaybePrice = 2.5m }
        ];
    }

    private static TestDatabase SetupMarkers(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H22vMarkerRow>().Schema.CreateTable();
        db.Table<H22vMarkerRow>().AddRange(
        [
            new H22vMarkerRow { Id = 1, Marker = (H22vEmptyInt)5, WideMarker = (H22vEmptyLong)7 },
            new H22vMarkerRow { Id = 2, Marker = (H22vEmptyInt)11, WideMarker = (H22vEmptyLong)9 }
        ]);
        return db;
    }

    private static TestDatabase SetupSources(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H22vSourceRow>().Schema.CreateTable();
        db.Table<H22vSourceRow>().AddRange(SourceRows());
        return db;
    }

    private static TestDatabase SetupPrices(string methodName)
    {
        TestDatabase db = new(o => o.UseDecimalStorage(DecimalStorageMode.Text), methodName);
        db.Table<H22vPriceRow>().Schema.CreateTable();
        db.Table<H22vPriceRow>().AddRange(PriceRows());
        return db;
    }
}
