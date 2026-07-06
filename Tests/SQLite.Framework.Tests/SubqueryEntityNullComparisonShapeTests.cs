using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SubqNullShapeItems")]
public class SubqNullShapeItem
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("SubqNullShapeChildren")]
public class SubqNullShapeChild
{
    [Key]
    public int Id { get; set; }

    public int ItemId { get; set; }

    public string? Note { get; set; }
}

public class SubqueryEntityNullComparisonShapeTests
{
    private static List<SubqNullShapeItem> Items() =>
    [
        new() { Id = 1, Name = "one" },
        new() { Id = 2, Name = "two" },
        new() { Id = 3, Name = "three" },
    ];

    private static List<SubqNullShapeChild> Children() =>
    [
        new() { Id = 1, ItemId = 1, Note = "a" },
        new() { Id = 2, ItemId = 3, Note = null },
    ];

    private static TestDatabase Seed(string methodName)
    {
        TestDatabase db = new(methodName);
        db.Table<SubqNullShapeItem>().Schema.CreateTable();
        db.Table<SubqNullShapeItem>().AddRange(Items());
        db.Table<SubqNullShapeChild>().Schema.CreateTable();
        db.Table<SubqNullShapeChild>().AddRange(Children());
        return db;
    }

    [Fact]
    public void SingleOrDefaultComparedToNullKeepsRowsWithoutMatch()
    {
        using TestDatabase db = Seed(nameof(SingleOrDefaultComparedToNullKeepsRowsWithoutMatch));
        List<int> expected = Items()
            .Where(i => Children().SingleOrDefault(c => c.ItemId == i.Id) == null)
            .Select(i => i.Id)
            .ToList();
        List<int> actual = db.Table<SubqNullShapeItem>()
            .Where(i => db.Table<SubqNullShapeChild>().SingleOrDefault(c => c.ItemId == i.Id) == null)
            .OrderBy(i => i.Id)
            .Select(i => i.Id)
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LastOrDefaultComparedToNotNullKeepsRowsWithMatch()
    {
        using TestDatabase db = Seed(nameof(LastOrDefaultComparedToNotNullKeepsRowsWithMatch));
        List<int> expected = Items()
            .Where(i => Children().LastOrDefault(c => c.ItemId == i.Id) != null)
            .Select(i => i.Id)
            .ToList();
        List<int> actual = db.Table<SubqNullShapeItem>()
            .Where(i => db.Table<SubqNullShapeChild>().LastOrDefault(c => c.ItemId == i.Id) != null)
            .OrderBy(i => i.Id)
            .Select(i => i.Id)
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullComparedToFirstOrDefaultKeepsRowsWithoutMatch()
    {
        using TestDatabase db = Seed(nameof(NullComparedToFirstOrDefaultKeepsRowsWithoutMatch));
        List<int> expected = Items()
            .Where(i => null == Children().FirstOrDefault(c => c.ItemId == i.Id))
            .Select(i => i.Id)
            .ToList();
        List<int> actual = db.Table<SubqNullShapeItem>()
            .Where(i => null == db.Table<SubqNullShapeChild>().FirstOrDefault(c => c.ItemId == i.Id))
            .OrderBy(i => i.Id)
            .Select(i => i.Id)
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FirstOrDefaultWithoutPredicateOverFilteredSourceMatches()
    {
        using TestDatabase db = Seed(nameof(FirstOrDefaultWithoutPredicateOverFilteredSourceMatches));
        List<int> expected = Items()
            .Where(i => Children().Where(c => c.ItemId == i.Id).FirstOrDefault() == null)
            .Select(i => i.Id)
            .ToList();
        List<int> actual = db.Table<SubqNullShapeItem>()
            .Where(i => db.Table<SubqNullShapeChild>().Where(c => c.ItemId == i.Id).FirstOrDefault() == null)
            .OrderBy(i => i.Id)
            .Select(i => i.Id)
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullCheckCombinedWithAndKeepsFiltering()
    {
        using TestDatabase db = Seed(nameof(NullCheckCombinedWithAndKeepsFiltering));
        List<int> expected = Items()
            .Where(i => Children().FirstOrDefault(c => c.ItemId == i.Id) == null && i.Id > 1)
            .Select(i => i.Id)
            .ToList();
        List<int> actual = db.Table<SubqNullShapeItem>()
            .Where(i => db.Table<SubqNullShapeChild>().FirstOrDefault(c => c.ItemId == i.Id) == null && i.Id > 1)
            .OrderBy(i => i.Id)
            .Select(i => i.Id)
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullCheckCombinedWithOrKeepsFiltering()
    {
        using TestDatabase db = Seed(nameof(NullCheckCombinedWithOrKeepsFiltering));
        List<int> expected = Items()
            .Where(i => Children().FirstOrDefault(c => c.ItemId == i.Id) != null || i.Id == 2)
            .Select(i => i.Id)
            .ToList();
        List<int> actual = db.Table<SubqNullShapeItem>()
            .Where(i => db.Table<SubqNullShapeChild>().FirstOrDefault(c => c.ItemId == i.Id) != null || i.Id == 2)
            .OrderBy(i => i.Id)
            .Select(i => i.Id)
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullCheckInProjectionTernaryMatches()
    {
        using TestDatabase db = Seed(nameof(NullCheckInProjectionTernaryMatches));
        List<string> expected = Items()
            .Select(i => Children().FirstOrDefault(c => c.ItemId == i.Id) == null ? "missing" : "present")
            .ToList();
        List<string> actual = db.Table<SubqNullShapeItem>()
            .OrderBy(i => i.Id)
            .Select(i => db.Table<SubqNullShapeChild>().FirstOrDefault(c => c.ItemId == i.Id) == null ? "missing" : "present")
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ScalarSubqueryNullComparisonReadsValue()
    {
        using TestDatabase db = Seed(nameof(ScalarSubqueryNullComparisonReadsValue));
        List<int> expected = Items()
            .Where(i => Children().Where(c => c.ItemId == i.Id).Select(c => c.Note).FirstOrDefault() == null)
            .Select(i => i.Id)
            .ToList();
        List<int> actual = db.Table<SubqNullShapeItem>()
            .Where(i => db.Table<SubqNullShapeChild>().Where(c => c.ItemId == i.Id).Select(c => c.Note).FirstOrDefault() == null)
            .OrderBy(i => i.Id)
            .Select(i => i.Id)
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MaxComparedToNullMatchesEmptyGroups()
    {
        using TestDatabase db = Seed(nameof(MaxComparedToNullMatchesEmptyGroups));
        List<int> expected = Items()
            .Where(i => Children().Where(c => c.ItemId == i.Id).Max(c => (int?)c.Id) == null)
            .Select(i => i.Id)
            .ToList();
        List<int> actual = db.Table<SubqNullShapeItem>()
            .Where(i => db.Table<SubqNullShapeChild>().Where(c => c.ItemId == i.Id).Max(c => (int?)c.Id) == null)
            .OrderBy(i => i.Id)
            .Select(i => i.Id)
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InstanceMethodResultComparedToNullTranslatesDirectly()
    {
        using TestDatabase db = Seed(nameof(InstanceMethodResultComparedToNullTranslatesDirectly));
        List<int> expected = Items()
            .Where(i => i.Name.ToUpper() != null)
            .Select(i => i.Id)
            .ToList();
        List<int> actual = db.Table<SubqNullShapeItem>()
            .Where(i => i.Name.ToUpper() != null)
            .OrderBy(i => i.Id)
            .Select(i => i.Id)
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedListFirstOrDefaultComparedToNullComputesInMemory()
    {
        using TestDatabase db = Seed(nameof(CapturedListFirstOrDefaultComparedToNullComputesInMemory));
        List<string> words = ["a", "bb", "ccc"];
        List<bool> expected = Items()
            .Select(i => words.FirstOrDefault(w => w.Length > i.Id) == null)
            .ToList();
        List<bool> actual = db.Table<SubqNullShapeItem>()
            .OrderBy(i => i.Id)
            .Select(i => words.FirstOrDefault(w => w.Length > i.Id) == null)
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FirstOrDefaultWithEntityDefaultValueStillThrows()
    {
        using TestDatabase db = Seed(nameof(FirstOrDefaultWithEntityDefaultValueStillThrows));
        SubqNullShapeChild fallback = new() { Id = -1, ItemId = -1, Note = "none" };
        Assert.ThrowsAny<Exception>(() => db.Table<SubqNullShapeItem>()
            .Where(i => db.Table<SubqNullShapeChild>().FirstOrDefault(fallback) == null)
            .Select(i => i.Id)
            .ToList());
    }
}
