using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("StoredReverseCteRows")]
public class StoredReverseCteRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

public class StoredCteBodyReverseTests
{
    [Fact]
    public void AStoredCteWhoseBodyEndsWithReverseIsRejected()
    {
        using TestDatabase db = Setup(nameof(AStoredCteWhoseBodyEndsWithReverseIsRejected));

        SQLiteCte<StoredReverseCteRow> cte = db.With(() => db.Table<StoredReverseCteRow>()
            .OrderBy(r => r.Id)
            .Reverse());

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => cte
            .Select(x => x.Id)
            .ToList());

        Assert.Contains("Reverse", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AStoredCteWhoseBodyReversesBeforeDistinctIsRejected()
    {
        using TestDatabase db = Setup(nameof(AStoredCteWhoseBodyReversesBeforeDistinctIsRejected));

        SQLiteCte<StoredReverseCteRow> cte = db.With(() => db.Table<StoredReverseCteRow>()
            .OrderBy(r => r.Id)
            .Reverse()
            .Distinct());

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => cte
            .Select(x => x.Id)
            .ToList());

        Assert.Contains("Reverse", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void JoiningAStoredCteWhoseBodyEndsWithReverseIsRejected()
    {
        using TestDatabase db = Setup(nameof(JoiningAStoredCteWhoseBodyEndsWithReverseIsRejected));

        SQLiteCte<StoredReverseCteRow> cte = db.With(() => db.Table<StoredReverseCteRow>()
            .OrderBy(r => r.Id)
            .Reverse());

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => db.Table<StoredReverseCteRow>()
            .Join(cte, l => l.Id, c => c.Id, (l, c) => l.Id)
            .ToList());

        Assert.Contains("Reverse", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void JoiningAStoredCteWhoseBodyReversesBeforeDistinctIsRejected()
    {
        using TestDatabase db = Setup(nameof(JoiningAStoredCteWhoseBodyReversesBeforeDistinctIsRejected));

        SQLiteCte<StoredReverseCteRow> cte = db.With(() => db.Table<StoredReverseCteRow>()
            .OrderBy(r => r.Id)
            .Reverse()
            .Distinct());

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => db.Table<StoredReverseCteRow>()
            .Join(cte, l => l.Id, c => c.Id, (l, c) => l.Id)
            .ToList());

        Assert.Contains("Reverse", ex.Message, StringComparison.Ordinal);
    }

    private static List<StoredReverseCteRow> Rows()
    {
        return
        [
            new StoredReverseCteRow { Id = 1, A = 10 },
            new StoredReverseCteRow { Id = 2, A = 20 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<StoredReverseCteRow>().Schema.CreateTable();
        db.Table<StoredReverseCteRow>().AddRange(Rows());
        return db;
    }
}
