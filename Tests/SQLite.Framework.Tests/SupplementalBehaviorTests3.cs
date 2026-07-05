using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ComputedTotalNote")]
public class ComputedTotalNoteRow
{
    [Key]
    public int Id { get; set; }

    public int Price { get; set; }

    [NotMapped]
    public int Doubled => Price * 2;

    [NotMapped]
    public int this[int index] => index;
}

[Table("TitledTome")]
public class TitledTomeRow
{
    [Key]
    public int Id { get; set; }

    [Column("TomeTitle")]
    public string? Title { get; set; }
}

[Table("KeyOnlyEntry")]
public class KeyOnlyEntryRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }
}

[WithoutRowId]
[Table("HeldTagChild")]
public class HeldTagChildRow
{
    [Key]
    public string Tag { get; set; } = "";

    [ReferencesTable(typeof(HeldParentRow))]
    public int ParentId { get; set; }
}

public class SupplementalBehaviorTests3
{
    [Fact]
    public void ScriptKeepsParameterNamesInsideCommentsAndBracketQuotes()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"NotedRows\" (\"Id\" INTEGER PRIMARY KEY, [x@p0] TEXT, `y@p0` TEXT)");

        IReadOnlyList<string> statements = db.Schema.Migrations()
            .Version(1, m => m.Sql(
                "INSERT INTO \"NotedRows\" (\"Id\", [x@p0], `y@p0`) -- line @p0 note\n VALUES (1, /* block @p0 */ @p0, 'a''b')",
                new SQLiteParameter { Name = "@p0", Value = "v" }))
            .Script();

        Assert.Equal(
        [
            "INSERT INTO \"NotedRows\" (\"Id\", [x@p0], `y@p0`) -- line @p0 note\n VALUES (1, /* block @p0 */ 'v', 'a''b')",
            "PRAGMA user_version = 1",
        ], statements);
    }

    [Fact]
    public void AComputedGetOnlyPropertyDoesNotForceTheConstructorPath()
    {
        using TestDatabase db = new();
        db.Table<ComputedTotalNoteRow>().Schema.CreateTable();
        db.Table<ComputedTotalNoteRow>().Add(new ComputedTotalNoteRow { Id = 1, Price = 5 });

        ComputedTotalNoteRow row = db.Table<ComputedTotalNoteRow>().Single();

        Assert.Equal(5, row.Price);
        Assert.Equal(10, row.Doubled);
    }

    [Fact]
    public void JsonDistinctThenReverseMatchesLinq()
    {
        List<int> local = [5, 3, 5, 8];
        using TestDatabase db = new(b => b.AddJsonContext(PagedNumListContext.Default));
        db.Table<PagedNumListRow>().Schema.CreateTable();
        db.Table<PagedNumListRow>().Add(new PagedNumListRow { Id = 1, Nums = local });

        List<int> expected = local.Distinct().Reverse().ToList();
        List<int> actual = db.Table<PagedNumListRow>().Select(r => r.Nums.Distinct().Reverse().ToList()).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JsonGroupFilterThenTakeMatchesKeyOrder()
    {
        List<int> local = [1, 2, 3, 1];
        using TestDatabase db = new(b => b.AddJsonContext(PagedNumListContext.Default));
        db.Table<PagedNumListRow>().Schema.CreateTable();
        db.Table<PagedNumListRow>().Add(new PagedNumListRow { Id = 1, Nums = local });

        List<int> expected = local.GroupBy(x => x % 2).Where(g => g.Count() > 1).OrderBy(g => g.Key)
            .Select(g => g.Sum()).Take(1).ToList();
        List<int> actual = db.Table<PagedNumListRow>()
            .Select(r => r.Nums.GroupBy(x => x % 2).Where(g => g.Count() > 1).Select(g => g.Sum()).Take(1).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FlattenedComputedDayOfWeekScalarKeepsItsForm()
    {
        using TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Table<DowMixEntryRow>().Schema.CreateTable();
        List<DowMixEntryRow> rows =
        [
            new DowMixEntryRow { Id = 1, When = new DateTime(2026, 7, 6), Other = new DateTime(2026, 7, 6), Dow = DayOfWeek.Monday },
            new DowMixEntryRow { Id = 2, When = new DateTime(2026, 7, 6), Other = new DateTime(2026, 7, 6), Dow = DayOfWeek.Monday },
        ];
        db.Table<DowMixEntryRow>().AddRange(rows);

        int expected = rows.Select(m => m.When.DayOfWeek).Distinct().Count(d => d == DayOfWeek.Monday);
        int actual = db.Table<DowMixEntryRow>().Select(m => m.When.DayOfWeek).Distinct().Count(d => d == DayOfWeek.Monday);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SubqueryContainsAComputedDayOfWeekUnderIntegerStorageMatchesLinq()
    {
        using TestDatabase db = new();
        db.Table<DowMixEntryRow>().Schema.CreateTable();
        db.Table<DowSlotEntryRow>().Schema.CreateTable();
        List<DowMixEntryRow> rows = [new DowMixEntryRow { Id = 1, When = new DateTime(2026, 7, 6), Other = new DateTime(2026, 7, 6) }];
        List<DowSlotEntryRow> slots = [new DowSlotEntryRow { Id = 1, Dow = DayOfWeek.Monday }];
        db.Table<DowMixEntryRow>().AddRange(rows);
        db.Table<DowSlotEntryRow>().AddRange(slots);

        List<int> expected = rows.Where(m => slots.Select(s => s.Dow).Contains(m.When.DayOfWeek)).Select(m => m.Id).ToList();
        List<int> actual = db.Table<DowMixEntryRow>()
            .Where(m => db.Table<DowSlotEntryRow>().Select(s => s.Dow).Contains(m.When.DayOfWeek))
            .Select(m => m.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SubqueryContainsAStoredValueUnderTextStorageMatchesLinq()
    {
        using TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Table<DowMixEntryRow>().Schema.CreateTable();
        db.Table<DowSlotEntryRow>().Schema.CreateTable();
        List<DowMixEntryRow> rows = [new DowMixEntryRow { Id = 1, When = new DateTime(2026, 7, 6), Other = new DateTime(2026, 7, 6), Dow = DayOfWeek.Monday }];
        List<DowSlotEntryRow> slots = [new DowSlotEntryRow { Id = 1, Dow = DayOfWeek.Monday }];
        db.Table<DowMixEntryRow>().AddRange(rows);
        db.Table<DowSlotEntryRow>().AddRange(slots);

        List<int> expected = rows.Where(m => slots.Select(s => s.Dow).Contains(m.Dow)).Select(m => m.Id).ToList();
        List<int> actual = db.Table<DowMixEntryRow>()
            .Where(m => db.Table<DowSlotEntryRow>().Select(s => s.Dow).Contains(m.Dow))
            .Select(m => m.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ARootMarkerShortCircuitsTheCapturedQueryableScan()
    {
        using TestDatabase db = new(b => b.AddQueryFilter<CteFilteredNoteRow>(s => !s.IsDeleted));
        db.Table<CteFilteredNoteRow>().Schema.CreateTable();
        db.Table<CteFilteredNoteRow>().AddRange(
        [
            new CteFilteredNoteRow { Id = 1, IsDeleted = false },
            new CteFilteredNoteRow { Id = 2, IsDeleted = true },
        ]);
        IQueryable<CteFilteredNoteRow> captured = db.Table<CteFilteredNoteRow>();

        int count = db.Table<CteFilteredNoteRow>().IgnoreQueryFilters()
            .Count(x => captured.Any(c => c.Id == x.Id));

        Assert.Equal(2, count);
    }

    [Fact]
    public void RebuildKeepsRowsOfAWithoutRowIdReferencingTable()
    {
        using TestDatabase db = new(useFile: true);
        db.Table<HeldParentRow>().Schema.CreateTable();
        db.Table<HeldTagChildRow>().Schema.CreateTable();
        db.Table<HeldParentRow>().Add(new HeldParentRow { Id = 1 });
        db.Table<HeldTagChildRow>().Add(new HeldTagChildRow { Tag = "a", ParentId = 1 });

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<HeldParentRow>(s => s.Set(x => x.Note, "filled"), rebuild: true))
            .Migrate();

        Assert.Equal(1, db.Table<HeldTagChildRow>().Count());
    }

    [Fact]
    public void RebuildOfAnEmptyTableWithNoSharedColumnsProducesTheModelSchema()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"SkuItem\" (\"A\" TEXT)");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<SkuItemRow>(rebuild: true))
            .Migrate();

        List<string> names = db.Pragmas.TableInfo("SkuItem").Select(c => c.Name).ToList();
        Assert.Equal(["Sku", "Qty"], names);
    }

    [Fact]
    public void FillReadingANestedMemberRunsInTheDataPhase()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<SluggedBookRow>().Insert(new SluggedBookRow { Id = 1, Slug = "keep" }))
            .Migrate();

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<SluggedBookRow>().Insert(new SluggedBookRow { Id = 1, Slug = "keep" }))
            .Version(2, m => m.TableChanged<SluggedBookRow>(s => s.Set(x => x.Slug, x => x.Slug!.Length.ToString())))
            .Migrate();

        Assert.Equal("4", db.Table<SluggedBookRow>().Single().Slug);
    }

    [Fact]
    public void AnEmptyInsertHookOnAKeyOnlyEntityUsesDefaultValues()
    {
        using TestDatabase db = new(b => b
            .OnAdd<KeyOnlyEntryRow>((d, item, columns) => true));
        db.Table<KeyOnlyEntryRow>().Schema.CreateTable();

        db.Table<KeyOnlyEntryRow>().Add(new KeyOnlyEntryRow());

        Assert.Equal(1, db.Table<KeyOnlyEntryRow>().Count());
    }

    [Fact]
    public void DistinctThenProjectionOnAnEntityWithAComputedPropertyMatchesLinq()
    {
        using TestDatabase db = new();
        db.Table<ComputedTotalNoteRow>().Schema.CreateTable();
        List<ComputedTotalNoteRow> rows =
        [
            new ComputedTotalNoteRow { Id = 1, Price = 3 },
            new ComputedTotalNoteRow { Id = 2, Price = 4 },
        ];
        db.Table<ComputedTotalNoteRow>().AddRange(rows);

        List<int> expected = rows.Distinct().Select(t => t.Price).OrderBy(x => x).ToList();
        List<int> actual = db.Table<ComputedTotalNoteRow>().Distinct().Select(t => t.Price)
            .ToList().OrderBy(x => x).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DistinctThenFilterOnARenamedColumnMatchesLinq()
    {
        using TestDatabase db = new();
        db.Table<TitledTomeRow>().Schema.CreateTable();
        List<TitledTomeRow> rows =
        [
            new TitledTomeRow { Id = 1, Title = "x" },
            new TitledTomeRow { Id = 2, Title = "y" },
        ];
        db.Table<TitledTomeRow>().AddRange(rows);

        List<string?> expected = rows.Distinct().Select(t => t.Title).OrderBy(x => x).ToList();
        List<string?> actual = db.Table<TitledTomeRow>().Distinct().Select(t => t.Title)
            .ToList().OrderBy(x => x).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AMigrationSeedOfAKeyOnlyEntityInsertsDefaults()
    {
        using TestDatabase db = new(useFile: true);

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<KeyOnlyEntryRow>().Insert(new KeyOnlyEntryRow()))
            .Migrate();

        Assert.Equal(1, db.Table<KeyOnlyEntryRow>().Count());
    }
}
