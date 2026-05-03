using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class RequestedProbeTests
{
#if !SQLITECIPHER
    [Fact]
    public void Trigram_DefaultSettings_OmitsExplicitFlags()
    {
        using TestDatabase db = new();
        db.Table<Trigram_Default_Search>().Schema.CreateTable();

        string sql = (string)db.ExecuteScalar<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = @n",
            [new SQLiteParameter { Name = "@n", Value = "Trigram_Default_Search" }])!;

        Assert.Contains("trigram", sql);
    }

    [Fact]
    public void Trigram_SubstringMatch_FindsMiddleOfWord()
    {
        using TestDatabase db = new();
        db.Table<Trigram_Default_Search>().Schema.CreateTable();

        db.Table<Trigram_Default_Search>().AddRange([
            new Trigram_Default_Search { Id = 1, Body = "sqlite database" },
            new Trigram_Default_Search { Id = 2, Body = "completely unrelated" },
        ]);

        List<int> ids = db.Table<Trigram_Default_Search>()
            .Where(s => SQLiteFTS5Functions.Match(s, "qli"))
            .Select(s => s.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void Trigram_SubstringMatch_IsCaseInsensitiveByDefault()
    {
        using TestDatabase db = new();
        db.Table<Trigram_Default_Search>().Schema.CreateTable();

        db.Table<Trigram_Default_Search>().AddRange([
            new Trigram_Default_Search { Id = 1, Body = "SQLite" },
        ]);

        int count = db.Table<Trigram_Default_Search>()
            .Count(s => SQLiteFTS5Functions.Match(s, "sql"));

        Assert.Equal(1, count);
    }

    [Fact]
    public void Trigram_CaseSensitive_RespectsCase()
    {
        using TestDatabase db = new();
        db.Table<Trigram_CaseSensitive_Search>().Schema.CreateTable();

        db.Table<Trigram_CaseSensitive_Search>().AddRange([
            new Trigram_CaseSensitive_Search { Id = 1, Code = "SQLite" },
            new Trigram_CaseSensitive_Search { Id = 2, Code = "sqlite" },
        ]);

        List<int> upperHits = db.Table<Trigram_CaseSensitive_Search>()
            .Where(s => SQLiteFTS5Functions.Match(s, "SQL"))
            .OrderBy(s => s.Id)
            .Select(s => s.Id)
            .ToList();

        List<int> lowerHits = db.Table<Trigram_CaseSensitive_Search>()
            .Where(s => SQLiteFTS5Functions.Match(s, "sql"))
            .OrderBy(s => s.Id)
            .Select(s => s.Id)
            .ToList();

        Assert.Equal([1], upperHits);
        Assert.Equal([2], lowerHits);
    }

    [Fact]
    public void Trigram_TooShortQuery_ReturnsZeroRows()
    {
        using TestDatabase db = new();
        db.Table<Trigram_Default_Search>().Schema.CreateTable();

        db.Table<Trigram_Default_Search>().AddRange([
            new Trigram_Default_Search { Id = 1, Body = "anything" },
        ]);

        int count = db.Table<Trigram_Default_Search>()
            .Count(s => SQLiteFTS5Functions.Match(s, "an"));

        Assert.Equal(0, count);
    }
#endif

    [Fact]
    public void CustomConverter_NotNullColumnRejectsNull()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(Score)] = new ScoreConverter());
        db.Table<ScoreRow>().Schema.CreateTable();

        Assert.Throws<SQLite.Framework.Exceptions.SQLiteException>(() =>
            db.Execute("INSERT INTO ScoreRow (Id, Value) VALUES (1, NULL)"));
    }

    [Fact]
    public void CustomConverter_NullableColumn_NullRoundTrips()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(Score)] = new ScoreConverter());
        db.Table<NullableScoreRow>().Schema.CreateTable();

        db.Table<NullableScoreRow>().AddRange([
            new NullableScoreRow { Id = 1, Value = null },
            new NullableScoreRow { Id = 2, Value = new Score(5) },
        ]);

        List<NullableScoreRow> rows = db.Table<NullableScoreRow>().OrderBy(r => r.Id).ToList();

        Assert.Null(rows[0].Value);
        Assert.Equal(new Score(5), rows[1].Value);
    }

    [Fact]
    public void CustomConverter_NullableColumn_WhereIsNull_FiltersNullRows()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(Score)] = new ScoreConverter());
        db.Table<NullableScoreRow>().Schema.CreateTable();

        db.Table<NullableScoreRow>().AddRange([
            new NullableScoreRow { Id = 1, Value = null },
            new NullableScoreRow { Id = 2, Value = new Score(5) },
            new NullableScoreRow { Id = 3, Value = null },
        ]);

        List<int> nullIds = db.Table<NullableScoreRow>()
            .Where(r => r.Value == null)
            .OrderBy(r => r.Id)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([1, 3], nullIds);
    }

    [Fact]
    public void CustomConverter_UpdateValueToNull_PersistsNull()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(Score)] = new ScoreConverter());
        db.Table<NullableScoreRow>().Schema.CreateTable();

        db.Table<NullableScoreRow>().AddRange([
            new NullableScoreRow { Id = 1, Value = new Score(7) },
        ]);

        db.Table<NullableScoreRow>().Update(new NullableScoreRow { Id = 1, Value = null });

        NullableScoreRow row = db.Table<NullableScoreRow>().First();

        Assert.Null(row.Value);
    }

    [Fact]
    public void Transaction_RollbackInsideEnumerator_DataReverts()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();

        db.Table<NullableEntity>().AddRange([
            new NullableEntity { Id = 1, Value = 1 },
            new NullableEntity { Id = 2, Value = 2 },
            new NullableEntity { Id = 3, Value = 3 },
        ]);

        using (SQLiteTransaction tx = db.BeginTransaction())
        {
            db.Table<NullableEntity>().Add(new NullableEntity { Id = 4, Value = 4 });

            using IEnumerator<NullableEntity> enumerator = ((IEnumerable<NullableEntity>)db.Table<NullableEntity>()).GetEnumerator();
            enumerator.MoveNext();
            tx.Rollback();
        }

        int count = db.Table<NullableEntity>().Count();
        Assert.Equal(3, count);
    }

    [Fact]
    public void Transaction_DisposeWithoutCommit_RollsBack()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();

        db.Table<NullableEntity>().AddRange([
            new NullableEntity { Id = 1, Value = 1 },
        ]);

        using (SQLiteTransaction tx = db.BeginTransaction())
        {
            db.Table<NullableEntity>().Add(new NullableEntity { Id = 2, Value = 2 });
        }

        int count = db.Table<NullableEntity>().Count();
        Assert.Equal(1, count);
    }

    [Fact]
    public void Transaction_PartiallyConsumedEnumerator_ThenRollback_Reverts()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();

        db.Table<NullableEntity>().AddRange(
            Enumerable.Range(1, 5).Select(i => new NullableEntity { Id = i, Value = i }).ToList()
        );

        using (SQLiteTransaction tx = db.BeginTransaction())
        {
            db.Table<NullableEntity>().Where(e => e.Id <= 2).ExecuteDelete();

            using IEnumerator<NullableEntity> enumerator = db.Table<NullableEntity>().OrderBy(e => e.Id).GetEnumerator();
            enumerator.MoveNext();
            int first = enumerator.Current.Id;

            tx.Rollback();
            Assert.Equal(3, first);
        }

        int count = db.Table<NullableEntity>().Count();
        Assert.Equal(5, count);
    }

    private readonly record struct Score(int Value);

    private sealed class ScoreConverter : ISQLiteTypeConverter
    {
        public SQLiteColumnType ColumnType => SQLiteColumnType.Integer;

        public object? ToDatabase(object? value) => value is Score s ? s.Value : null;

        public object FromDatabase(object? value) =>
            value is long l ? new Score((int)l) : new Score(0);
    }

    private class ScoreRow
    {
        [Key]
        public int Id { get; set; }

        public required Score Value { get; set; }
    }

    private class NullableScoreRow
    {
        [Key]
        public int Id { get; set; }

        public Score? Value { get; set; }
    }

    [Fact]
    public void Unicode_MultiScript_RoundTrips()
    {
        using TestDatabase db = new();
        db.Table<NullableStringEntity>().Schema.CreateTable();

        string text = "Здравей \U0001F60A 你好 مرحبا";

        db.Table<NullableStringEntity>().AddRange([
            new NullableStringEntity { Id = 1, Name = text },
        ]);

        string read = db.Table<NullableStringEntity>().Select(e => e.Name!).First();

        Assert.Equal(text, read);
    }

    [Fact]
    public void Blob_OneMegabyte_RoundTrips()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();

        byte[] blob = new byte[1024 * 1024];
        new Random(42).NextBytes(blob);

        db.Table<NumericType>().Add(new NumericType
        {
            Id = 1,
            BlobValue = blob,
            CharValue = 'a',
            DoubleValue = 0,
            FloatValue = 0,
            DecimalValue = 0,
            ByteValue = 0,
            SByteValue = 0,
            ShortValue = 0,
            UShortValue = 0,
            IntValue = 0,
            UIntValue = 0,
            LongValue = 0,
            ULongValue = 0,
        });

        byte[] read = db.Table<NumericType>().Select(n => n.BlobValue!).First();

        Assert.Equal(blob.Length, read.Length);
        Assert.Equal(blob, read);
    }

    [Fact]
    public void Select_NullableArithmetic_PropagatesNull()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();

        db.Table<NullableEntity>().AddRange([
            new NullableEntity { Id = 1, Value = 10 },
            new NullableEntity { Id = 2, Value = null },
        ]);

        List<int?> doubled = db.Table<NullableEntity>()
            .OrderBy(e => e.Id)
            .Select(e => e.Value * 2)
            .ToList();

        Assert.Equal(2, doubled.Count);
        Assert.Equal(20, doubled[0]);
        Assert.Null(doubled[1]);
    }

    [Fact]
    public void ExecuteUpdate_SetNullableColumnToNull()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();

        db.Table<NullableEntity>().AddRange([
            new NullableEntity { Id = 1, Value = 10 },
            new NullableEntity { Id = 2, Value = 20 },
        ]);

        int affected = db.Table<NullableEntity>()
            .Where(e => e.Id == 1)
            .ExecuteUpdate(s => s.Set(e => e.Value, (int?)null));
        int? value = db.Table<NullableEntity>().Where(e => e.Id == 1).Select(e => e.Value).First();

        Assert.Equal(1, affected);
        Assert.Null(value);
    }

    [Fact]
    public void Max_OnEmptySource_NullableReturnType_ReturnsNull()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        int? maxId = db.Table<Book>().Where(b => b.Id > 100).Max(b => (int?)b.Id);

        Assert.Null(maxId);
    }

    [Fact]
    public void PositionalRecord_NoParameterlessCtor_ThrowsAtRead()
    {
        using TestDatabase db = new();

        db.Table<PositionalRecord>().Schema.CreateTable();
        db.Table<PositionalRecord>().AddRange([
            new PositionalRecord(1, "alpha"),
        ]);

        Assert.Throws<MissingMethodException>(() =>
            db.Table<PositionalRecord>().ToList());
    }

    public record PositionalRecord([property: Key] int Id, string Name);


    [Fact]
    public void Pragma_UserVersion_RoundTrip()
    {
        using TestDatabase db = new();

        db.Pragmas.UserVersion = 42;

        Assert.Equal(42, db.Pragmas.UserVersion);
    }

    [Fact]
    public void Backup_RestoresIntoDestinationFile()
    {
        string sourcePath = Path.Combine(Path.GetTempPath(), $"backup_src_{Guid.NewGuid():N}.db");
        string destPath = Path.Combine(Path.GetTempPath(), $"backup_dst_{Guid.NewGuid():N}.db");

        try
        {
            using (SQLiteDatabase src = new(new SQLiteOptionsBuilder(sourcePath).Build()))
            {
                src.Table<NullableEntity>().Schema.CreateTable();
                src.Table<NullableEntity>().AddRange([
                    new NullableEntity { Id = 1, Value = 100 },
                    new NullableEntity { Id = 2, Value = 200 },
                ]);

                src.BackupTo(destPath);
            }

            using SQLiteDatabase dest = new(new SQLiteOptionsBuilder(destPath).Build());
            int sum = dest.Table<NullableEntity>().Sum(e => e.Value ?? 0);
            Assert.Equal(300, sum);
        }
        finally
        {
            if (File.Exists(sourcePath)) File.Delete(sourcePath);
            if (File.Exists(destPath)) File.Delete(destPath);
        }
    }

    [Fact]
    public void DateTime_MinAndMaxValue_RoundTrip()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();

        db.Table<Author>().AddRange([
            new Author { Id = 1, Name = "min", Email = "x@x", BirthDate = DateTime.MinValue },
            new Author { Id = 2, Name = "max", Email = "x@x", BirthDate = DateTime.MaxValue },
        ]);

        List<DateTime> dates = db.Table<Author>().OrderBy(a => a.Id).Select(a => a.BirthDate).ToList();

        Assert.Equal(DateTime.MinValue, dates[0]);
        Assert.Equal(DateTime.MaxValue, dates[1]);
    }

    [Fact]
    public void Int_MinAndMaxValue_RoundTrip()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();

        db.Table<NullableEntity>().AddRange([
            new NullableEntity { Id = 1, Value = int.MinValue },
            new NullableEntity { Id = 2, Value = int.MaxValue },
        ]);

        List<int?> values = db.Table<NullableEntity>().OrderBy(e => e.Id).Select(e => e.Value).ToList();

        Assert.Equal(int.MinValue, values[0]);
        Assert.Equal(int.MaxValue, values[1]);
    }

    [Fact]
    public void Select_DivisionByZero_ProducesNull()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();

        db.Table<NullableEntity>().AddRange([
            new NullableEntity { Id = 1, Value = 10 },
            new NullableEntity { Id = 2, Value = 0 },
            new NullableEntity { Id = 3, Value = 100 },
        ]);

        List<int?> divided = db.Table<NullableEntity>()
            .OrderBy(e => e.Id)
            .Select(e => 100 / e.Value)
            .ToList();

        Assert.Equal(10, divided[0]);
        Assert.Null(divided[1]);
        Assert.Equal(1, divided[2]);
    }

    [Fact]
    public async Task ToListAsync_CancelledToken_Throws()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();

        db.Table<NullableEntity>().AddRange(
            Enumerable.Range(1, 100).Select(i => new NullableEntity { Id = i, Value = i }).ToList()
        );

        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await db.Table<NullableEntity>().ToListAsync(cts.Token));
    }

    [Fact]
    public void AddHook_Throws_ExceptionPropagatesAndRowNotInserted()
    {
        using TestDatabase db = new(b =>
            b.OnAdd<NullableEntity>((entity, action) => throw new InvalidOperationException("hook fail")));

        db.Table<NullableEntity>().Schema.CreateTable();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            db.Table<NullableEntity>().Add(new NullableEntity { Id = 1, Value = 1 }));

        Assert.Equal("hook fail", ex.Message);
        Assert.Equal(0, db.Table<NullableEntity>().Count());
    }

    [Fact]
    public void String_WithNulCharacter_RoundTrips()
    {
        using TestDatabase db = new();
        db.Table<NullableStringEntity>().Schema.CreateTable();

        string nullChar = "before\0after";

        db.Table<NullableStringEntity>().AddRange([
            new NullableStringEntity { Id = 1, Name = nullChar },
        ]);

        string read = db.Table<NullableStringEntity>().Select(e => e.Name!).First();

        Assert.Equal(nullChar.Length, read.Length);
        Assert.Equal(nullChar, read);
    }
}
