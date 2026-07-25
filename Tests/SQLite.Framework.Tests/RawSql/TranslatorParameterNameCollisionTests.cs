using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using SQLite.Framework.Internals.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ParamCollisionRows")]
public class ParamCollisionRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Score { get; set; }
}

public static class ParamCollisionFns
{
    public static string FixedParam(string value)
    {
        throw new InvalidOperationException("Only valid inside a LINQ query.");
    }

    public static string CustomNameParam(string value)
    {
        throw new InvalidOperationException("Only valid inside a LINQ query.");
    }

    public static string DoubleSuffixParam(string value)
    {
        throw new InvalidOperationException("Only valid inside a LINQ query.");
    }
}

public class TranslatorParameterNameCollisionTests
{
    private static void RegisterFixedParam(SQLiteOptionsBuilder builder)
    {
        MethodInfo fixedParam = typeof(ParamCollisionFns).GetMethod(nameof(ParamCollisionFns.FixedParam))!;
        builder.AddMethodTranslator(fixedParam, ctx =>
        {
            MethodCallExpression call = (MethodCallExpression)ctx.Node;
            SQLiteExpression arg = (SQLiteExpression)ctx.Visit(call.Arguments[0]);
            SQLiteExpression tail = SQLiteExpression.Leaf(typeof(string), ctx.Counters.NextIdentifier(), "@p0", "#F");
            SQLiteParameter[] parameters = [.. arg.Parameters ?? [], .. tail.Parameters ?? []];
            return SQLiteExpression.Binary(typeof(string), ctx.Counters.NextIdentifier(), "(", arg, " || ", tail, ")", parameters);
        });
    }

    private static void RegisterCustomNameParam(SQLiteOptionsBuilder builder)
    {
        MethodInfo customNameParam = typeof(ParamCollisionFns).GetMethod(nameof(ParamCollisionFns.CustomNameParam))!;
        builder.AddMethodTranslator(customNameParam, ctx =>
        {
            MethodCallExpression call = (MethodCallExpression)ctx.Node;
            SQLiteExpression arg = (SQLiteExpression)ctx.Visit(call.Arguments[0]);
            SQLiteExpression tail = SQLiteExpression.Leaf(typeof(string), ctx.Counters.NextIdentifier(), "@customsuffix", "#C");
            SQLiteParameter[] parameters = [.. arg.Parameters ?? [], .. tail.Parameters ?? []];
            return SQLiteExpression.Binary(typeof(string), ctx.Counters.NextIdentifier(), "(", arg, " || ", tail, ")", parameters);
        });
    }

    private static void RegisterDoubleSuffixParam(SQLiteOptionsBuilder builder)
    {
        MethodInfo doubleSuffixParam = typeof(ParamCollisionFns).GetMethod(nameof(ParamCollisionFns.DoubleSuffixParam))!;
        builder.AddMethodTranslator(doubleSuffixParam, ctx =>
        {
            MethodCallExpression call = (MethodCallExpression)ctx.Node;
            SQLiteExpression arg = (SQLiteExpression)ctx.Visit(call.Arguments[0]);
            string name = ctx.Counters.NextParamName();
            SQLiteParameter suffix = new() { Name = name, Value = "#D" };
            SQLiteExpression tail = SQLiteExpression.Leaf(typeof(string), ctx.Counters.NextIdentifier(), $"{name} || {name}", new[] { suffix, suffix });
            SQLiteParameter[] parameters = [.. arg.Parameters ?? [], .. tail.Parameters ?? []];
            return SQLiteExpression.Binary(typeof(string), ctx.Counters.NextIdentifier(), "(", arg, " || ", tail, ")", parameters);
        });
    }

    private static List<ParamCollisionRow> SeedRows(TestDatabase db)
    {
        db.Table<ParamCollisionRow>().Schema.CreateTable();
        List<ParamCollisionRow> rows =
        [
            new ParamCollisionRow { Id = 1, Name = "alpha", Score = 5 },
            new ParamCollisionRow { Id = 2, Name = "beta", Score = 8 },
            new ParamCollisionRow { Id = 3, Name = "gamma", Score = 2 },
        ];
        db.Table<ParamCollisionRow>().AddRange(rows);
        return rows;
    }

    [Fact]
    public void UserParameterNamedLikeFrameworkParameterThrowsInsteadOfClobbering()
    {
        using TestDatabase db = new(RegisterFixedParam);
        SeedRows(db);

        int minScore = 3;
        string wanted = "beta#F";

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => db.Table<ParamCollisionRow>()
            .Where(r => r.Score > minScore && ParamCollisionFns.FixedParam(r.Name) == wanted)
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList());

        Assert.Equal(
            "The parameter name \"@p0\" is used with two different values in one query. " +
            "A custom translator most likely hard-codes a parameter name that collides with a generated name. " +
            "Take parameter names from Counters.NextParamName instead.",
            exception.Message);
    }

    [Fact]
    public void UserParameterWithNonCollidingNameBindsCorrectly()
    {
        using TestDatabase db = new(RegisterCustomNameParam);
        List<ParamCollisionRow> rows = SeedRows(db);

        int minScore = 3;
        string wanted = "beta#C";

        List<int> expected = rows
            .Where(r => r.Score > minScore && r.Name + "#C" == wanted)
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> actual = db.Table<ParamCollisionRow>()
            .Where(r => r.Score > minScore && ParamCollisionFns.CustomNameParam(r.Name) == wanted)
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SameParameterNameReusedWithSameValueBindsOnce()
    {
        using TestDatabase db = new(RegisterDoubleSuffixParam);
        List<ParamCollisionRow> rows = SeedRows(db);

        int minScore = 3;
        string wanted = "beta#D#D";

        List<int> expected = rows
            .Where(r => r.Score > minScore && r.Name + "#D" + "#D" == wanted)
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> actual = db.Table<ParamCollisionRow>()
            .Where(r => r.Score > minScore && ParamCollisionFns.DoubleSuffixParam(r.Name) == wanted)
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
