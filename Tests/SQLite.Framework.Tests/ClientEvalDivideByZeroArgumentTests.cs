using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class ClientEvalArithmeticRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }

    public int B { get; set; }
}

internal static class ClientEvalArithmeticHelper
{
    public static string Wrap(int value)
    {
        return "<" + value + ">";
    }
}

public class ClientEvalDivideByZeroArgumentTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<ClientEvalArithmeticRow>().Schema.CreateTable();
        db.Table<ClientEvalArithmeticRow>().Add(new ClientEvalArithmeticRow { Id = 1, A = 10, B = 0 });
        return db;
    }

    [Fact]
    public void DivideByZeroFedToClientMethodThrowsLikeLinqToObjects()
    {
        using TestDatabase db = SetupDatabase();

        Assert.Throws<DivideByZeroException>(() => db.Table<ClientEvalArithmeticRow>().AsEnumerable()
            .Select(x => ClientEvalArithmeticHelper.Wrap(x.A / x.B))
            .ToList());

        Assert.Throws<DivideByZeroException>(() => db.Table<ClientEvalArithmeticRow>()
            .Select(x => ClientEvalArithmeticHelper.Wrap(x.A / x.B))
            .ToList());
    }
}
