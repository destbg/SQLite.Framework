using System.Diagnostics.CodeAnalysis;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.JsonB;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class BuildQueryObjectTests
{
    [Fact]
    public void EntityMaterializers_ManualRegistration_IsInvokedAndCounted()
    {
        using TestDatabase db = new(b =>
        {
            b.EntityMaterializers[typeof(Book)] = ctx =>
            {
                SQLiteDataReader reader = ctx.Reader!;
                Dictionary<string, int> columns = ctx.Columns!;
                long id = (long)reader.GetValue(columns["BookId"], reader.GetColumnType(columns["BookId"]), typeof(long))!;
                return new Book
                {
                    Id = (int)id,
                    Title = "stamped-by-materializer",
                    AuthorId = 0,
                    Price = 0.0
                };
            };
        });
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "real", AuthorId = 1, Price = 1 });

        long before = db.EntityMaterializerHits;
        Book result = db.CreateCommand("SELECT * FROM Books", []).ExecuteQuery<Book>().First();

        Assert.Equal("stamped-by-materializer", result.Title);
        Assert.Equal(1, result.Id);
        Assert.True(db.EntityMaterializerHits > before);
    }

    [Fact]
    public void ExecuteQuery_InterfaceElementWithRegisteredConverter_UsesConverter()
    {
        using TestDatabase db = new(b =>
        {
            b.AddJson();
            b.TypeConverters[typeof(List<int>)] =
                new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32);
        });

        List<IList<int>> rows = db.CreateCommand("SELECT '[1,2,3]'", []).ExecuteQuery<IList<int>>().ToList();

        Assert.Single(rows);
        Assert.Equal([1, 2, 3], rows[0]);
    }

    [Fact]
    public void ExecuteQuery_OutOfRangeEnumInteger_LeavesPropertyAtDefault()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Publisher>();
        db.CreateCommand("INSERT INTO Publisher (Id, Name, Type) VALUES (1, 'Pub', 999)", []).ExecuteNonQuery();

        Publisher result = db.CreateCommand("SELECT * FROM Publisher", []).ExecuteQuery<Publisher>().First();

        Assert.Equal(default(PublisherType), result.Type);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public void ExecuteQuery_InterfaceWithoutConverter_FallsThroughToMaterialization()
    {
        using TestDatabase db = new();

        Assert.ThrowsAny<Exception>(() =>
            db.CreateCommand("SELECT '[1,2,3]'", []).ExecuteQuery<IList<int>>().ToList());
    }

    [Fact]
    public void ExecuteQuery_AnonymousTypeMissingColumn_LeavesParameterAtDefault()
    {
        using TestDatabase db = new();

        var shape = new { Id = 0, MissingProp = (string?)null };
        var rows = RunAs(db.CreateCommand("SELECT 5 AS Id", []), shape).ToList();

        Assert.Single(rows);
        Assert.Equal(5, rows[0].Id);
        Assert.Null(rows[0].MissingProp);
    }

    private static IEnumerable<TAnon> RunAs<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TAnon>(SQLiteCommand cmd, TAnon shape)
    {
        _ = shape;
        return cmd.ExecuteQuery<TAnon>();
    }
}
