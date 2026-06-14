using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class ClientEvalUnsupportedRow
{
    [Key]
    public int Id { get; set; }

    public int Value { get; set; }

    public int? Maybe { get; set; }
}

public class ClientEvalUnsupportedMethodTests
{
    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<ClientEvalUnsupportedRow>().Schema.CreateTable();
        db.Table<ClientEvalUnsupportedRow>().Add(new ClientEvalUnsupportedRow { Id = 1, Value = 5, Maybe = null });
        return db;
    }

    [Fact]
    public void NativeIntegerComplementIsNotSupported()
    {
        using TestDatabase db = Setup();

        Assert.ThrowsAny<Exception>(() =>
            db.Table<ClientEvalUnsupportedRow>().Select(r => ~(nint)r.Value).First());
    }

    [Fact]
    public void GetTypeOnNullNullableThrows()
    {
        using TestDatabase db = Setup();

#pragma warning disable CS8629
        Assert.ThrowsAny<Exception>(() =>
            db.Table<ClientEvalUnsupportedRow>().Where(r => r.Id == 1).Select(r => r.Maybe.GetType().Name).First());
#pragma warning restore CS8629
    }
}
