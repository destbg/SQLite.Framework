#if SQLITE_FRAMEWORK_SOURCE_GENERATOR && SQLITE_FRAMEWORK_TESTING
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using SQLite.Framework.Generated;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GeneratedMaterializersParityAsserts
{
    private const int MinEntityMaterializerCount = 3;

    [Fact]
    public void EntityMaterializer_IsInvoked_WhenReadingTable()
    {
        using TestDatabase db = new();

        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "title",
            AuthorId = 1,
            Price = 1.0
        });

        long before = db.EntityMaterializerHits;

        List<Book> books = db.Table<Book>().ToList();

        Assert.Single(books);
        Assert.True(db.EntityMaterializerHits > before,
            $"Expected EntityMaterializerHits to increase. Before: {before}, after: {db.EntityMaterializerHits}.");
    }

    [Fact]
    public void GeneratedMaterializerClass_IsPresentInAssembly()
    {
        Type generated = typeof(SQLiteFrameworkGeneratedMaterializers);

        MethodInfo[] methods = generated.GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
        int materializerMethods = methods.Count(m => m.Name.StartsWith("Materialize_", StringComparison.Ordinal));

        Assert.True(materializerMethods >= MinEntityMaterializerCount,
            $"Expected at least {MinEntityMaterializerCount} generated materializer methods, found {materializerMethods}.");
    }

    [Fact]
    public void UseGeneratedMaterializers_PopulatesRegistry()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder(":memory:")
            .UseGeneratedMaterializers()
            .Build();

        Assert.True(options.EntityMaterializers.Count >= MinEntityMaterializerCount,
            $"Expected at least {MinEntityMaterializerCount} registered entity materializers, found {options.EntityMaterializers.Count}.");
        Assert.Contains(typeof(Book), options.EntityMaterializers.Keys);
        Assert.Contains(typeof(Author), options.EntityMaterializers.Keys);
    }

    [Fact]
    public void UseGeneratedMaterializers_IncludesEntitiesWithTypeConverterProperties()
    {
        using TestDatabase db = new(b =>
            b.AddTypeConverter<Address>(new SQLiteJsonConverter<Address>(TestJsonContext.Default.Address)));

        _ = db.Table<EntityWithConvertedProperty>();

        SQLiteOptions options = new SQLiteOptionsBuilder(":memory:")
            .UseGeneratedMaterializers()
            .Build();

        Assert.Contains(typeof(EntityWithConvertedProperty), options.EntityMaterializers.Keys);
    }

    [Fact]
    public void EntityMaterializer_IsInvoked_ForDtoProjection()
    {
        using TestDatabase db = new();

        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "title",
            AuthorId = 1,
            Price = 1.0
        });

        long before = db.EntityMaterializerHits;

        List<BookDto> dtos = db.Table<Book>()
            .Select(b => new BookDto
            {
                Id = b.Id,
                Title = b.Title
            })
            .ToList();

        Assert.Single(dtos);
        Assert.Equal(1, dtos[0].Id);
        Assert.Equal("title", dtos[0].Title);
        Assert.True(db.EntityMaterializerHits > before,
            $"Expected EntityMaterializerHits to increase for DTO projection. Before: {before}, after: {db.EntityMaterializerHits}.");
    }

    public class BookDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
    }

    [Fact]
    public void SelectMaterializer_IsInvoked_ForClientSideCompositionOfRowMembers()
    {
        using TestDatabase db = new();

        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book
        {
            Id = 3,
            Title = "t",
            AuthorId = 5,
            Price = 1.0
        });

        long before = db.SelectMaterializerHits;

        int result = db.Table<Book>()
            .Select(b => CommonHelpers.ConvertString(b.Title) + CommonHelpers.ConvertString(b.Title))
            .First();

        Assert.Equal(-2, result);
        Assert.True(db.SelectMaterializerHits > before,
            $"Expected SelectMaterializerHits to increase for client-side composition. Before: {before}, after: {db.SelectMaterializerHits}.");
    }

    [Fact]
    public void SelectMaterializer_IsInvoked_ForPrivateMethodCall()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book
        {
            Id = 7,
            Title = "t",
            AuthorId = 1,
            Price = 1.0
        });

        long before = db.SelectMaterializerHits;
        long fallbacksBefore = db.SelectCompilerFallbacks;

        string label = db.Table<Book>()
            .Select(b => PrivateLabel(b.Id))
            .First();

        Assert.Equal("L7", label);
        Assert.True(db.SelectMaterializerHits > before,
            "Expected a generated select materializer to handle a projection that calls a private static helper.");
        Assert.Equal(fallbacksBefore, db.SelectCompilerFallbacks);
    }

    [Fact]
    public void SelectMaterializer_IsInvoked_ForClosureCapturedLocal()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book
        {
            Id = 4,
            Title = "t",
            AuthorId = 1,
            Price = 1.0
        });

        int bonus = 100;
        long before = db.SelectMaterializerHits;
        long fallbacksBefore = db.SelectCompilerFallbacks;

        int total = db.Table<Book>()
            .Select(b => CommonHelpers.ConvertString(b.Title) + bonus)
            .First();

        Assert.Equal(99, total);
        Assert.True(db.SelectMaterializerHits > before,
            "Expected a generated select materializer to handle a projection that reads a closure-captured local.");
        Assert.Equal(fallbacksBefore, db.SelectCompilerFallbacks);
    }

    private static string PrivateLabel(int value)
    {
        return "L" + value;
    }

    [Table("EntityWithConvertedProperties")]
    public class EntityWithConvertedProperty
    {
        [Key]
        public int Id { get; set; }
        public required string Name { get; set; }
        public required Address Payload { get; set; }
    }
}
#endif
