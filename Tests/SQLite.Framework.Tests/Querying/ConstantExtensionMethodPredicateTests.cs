using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using SQLite.Framework.Extensions;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.Querying;

public enum H26qProviderKind
{
    GitHub,
    Anthropic
}

public static class H26qProviderKinds
{
    public static string ToId(this H26qProviderKind kind)
    {
        return kind switch
        {
            H26qProviderKind.GitHub => "github",
            H26qProviderKind.Anthropic => "anthropic",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    public static string NormalizeId(string id)
    {
        return id.Trim().ToLowerInvariant();
    }

    public static string Identity(string id)
    {
        return id;
    }

    public static bool IsAnthropic(string id)
    {
        return id == "anthropic";
    }

    public static string ThrowingId(string id)
    {
        throw new InvalidOperationException(id);
    }
}

public class H26qProviderIdBuilder(string prefix)
{
    public string Build(string suffix)
    {
        return prefix + suffix;
    }
}

[Table("H26qProviderAccounts")]
public class H26qProviderAccount
{
    [Key]
    public string Id { get; set; } = "";

    public string Provider { get; set; } = "";

    public string? ProjectId { get; set; }
}

public class H26qDatabase : TestDatabase
{
    public SQLiteTable<H26qProviderAccount> ProviderAccounts => Table<H26qProviderAccount>();
}

public class ConstantExtensionMethodPredicateTests
{
    [Fact]
    public async Task ConstantExtensionMethodCallInWhereIsEvaluatedBeforeTranslation()
    {
        using H26qDatabase db = Setup();
        string projectId = "project-1";

        List<H26qProviderAccount> accounts = await db.ProviderAccounts.Where(a =>
                a.Provider == H26qProviderKind.Anthropic.ToId()
                && a.ProjectId == projectId)
            .ToListAsync(TestContext.Current.CancellationToken);

        H26qProviderAccount account = Assert.Single(accounts);
        Assert.Equal("matching", account.Id);
    }

    [Fact]
    public void StaticMethodWithCapturedArgumentInWhereIsEvaluatedBeforeTranslation()
    {
        using H26qDatabase db = Setup();
        string id = " ANTHROPIC ";

        H26qProviderAccount account = Assert.Single(db.ProviderAccounts.Where(a =>
            a.Provider == H26qProviderKinds.NormalizeId(id)
            && a.ProjectId == "project-1"));

        Assert.Equal("matching", account.Id);
    }

    [Fact]
    public void InstanceMethodOnCapturedObjectInWhereIsEvaluatedBeforeTranslation()
    {
        using H26qDatabase db = Setup();
        H26qProviderIdBuilder builder = new("anth");

        List<string> ids = db.ProviderAccounts
            .Where(a => a.Provider == builder.Build("ropic"))
            .OrderBy(a => a.Id)
            .Select(a => a.Id)
            .ToList();

        Assert.Equal(["matching", "other-project"], ids);
    }

    [Fact]
    public void NestedConstantMethodCallsInWhereAreEvaluatedBeforeTranslation()
    {
        using H26qDatabase db = Setup();

        List<string> ids = db.ProviderAccounts
            .Where(a => a.Provider == H26qProviderKinds.Identity(H26qProviderKind.Anthropic.ToId()))
            .OrderBy(a => a.Id)
            .Select(a => a.Id)
            .ToList();

        Assert.Equal(["matching", "other-project"], ids);
    }

    [Fact]
    public void UserMethodWithARowArgumentIsNotConstantFolded()
    {
        using H26qDatabase db = Setup();

        Assert.Throws<NotSupportedException>(() => db.ProviderAccounts
            .Where(a => H26qProviderKinds.IsAnthropic(a.Provider))
            .ToList());
    }

    [Fact]
    public void MethodCallReturningAByRefLikeTypeIsNotConstantFolded()
    {
        MethodInfo conversion = typeof(ReadOnlySpan<int>).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == "op_Implicit"
                && method.GetParameters() is [{ ParameterType: Type parameterType }]
                && parameterType == typeof(int[]));
        MethodCallExpression call = Expression.Call(conversion, Expression.Constant(new[] { 1 }));

        Assert.False(ExpressionHelpers.IsConstantMethodCall(call));
    }

    [Fact]
    public void ExceptionFromAConstantMethodIsNotWrappedInTargetInvocationException()
    {
        using H26qDatabase db = Setup();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => db.ProviderAccounts
            .Where(a => a.Provider == H26qProviderKinds.ThrowingId("boom"))
            .ToList());

        Assert.Equal("boom", exception.Message);
    }

    private static H26qDatabase Setup()
    {
        H26qDatabase db = new();
        db.ProviderAccounts.Schema.CreateTable();
        db.ProviderAccounts.AddRange(
        [
            new H26qProviderAccount { Id = "matching", Provider = "anthropic", ProjectId = "project-1" },
            new H26qProviderAccount { Id = "other-provider", Provider = "github", ProjectId = "project-1" },
            new H26qProviderAccount { Id = "other-project", Provider = "anthropic", ProjectId = "project-2" }
        ]);
        return db;
    }
}
