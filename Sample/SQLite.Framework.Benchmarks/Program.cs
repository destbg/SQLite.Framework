using System.ComponentModel.DataAnnotations;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using SQLite.Framework;
using SQLite.Framework.Benchmarks;
using SQLite.Framework.Generated;

BenchmarkRunner.Run<MaterializerBenchmarks>(
    DefaultConfig.Instance.AddJob(Job.Default.WithWarmupCount(3).WithIterationCount(8)));

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class MaterializerBenchmarks
{
    private SQLiteDatabase db = null!;

    [Params(false, true)]
    public bool UseSourceGenerator;

    [Params(0, 50)]
    public int Rows;

    [GlobalSetup]
    public void Setup()
    {
        SQLitePCL.raw.SetProvider(new NoOpSQLite());
        NoOpSQLite.RowsPerQuery = Rows;

        SQLiteOptionsBuilder builder = new("noop.db");
        if (UseSourceGenerator)
        {
            builder.UseGeneratedMaterializers();
        }

        db = new SQLiteDatabase(builder.Build());
        db.Schema.CreateTable<Book>();

        Warmup();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        db.Dispose();
    }

    private void Warmup()
    {
        _ = SimpleScalar();
        _ = SimpleList().Count;
    }

    [Benchmark, BenchmarkCategory("SimpleScalar")]
    public int SimpleScalar()
    {
        return db.Table<Book>().Count(b => b.AuthorId == 5);
    }

    [Benchmark, BenchmarkCategory("SimpleList")]
    public List<Book> SimpleList()
    {
        return db.Table<Book>().Where(b => b.AuthorId == 5).ToList();
    }
}

public class Book
{
    [Key]
    public int Id { get; set; }

    public required string Title { get; set; }

    public required int AuthorId { get; set; }

    public required int PublisherId { get; set; }

    public required double Price { get; set; }
}
