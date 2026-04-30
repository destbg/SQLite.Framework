using System.Linq.Expressions;
using SQLite.Framework.Internals;

namespace SQLite.Framework.Tests;

public class SelectSignatureInternalTests
{
    [Fact]
    public void Compute_MemberListBinding_AppendsInitializersInSignature()
    {
        Expression body = ((Expression<Func<SignatureSubject>>)(() => new SignatureSubject
        {
            Items = { 1, 2 }
        })).Body;

        string signature = SelectSignature.Compute(body);

        Assert.Contains("Items", signature);
    }

    [Fact]
    public void Compute_MemberMemberBinding_AppendsNestedBindingsInSignature()
    {
        Expression body = ((Expression<Func<SignatureSubject>>)(() => new SignatureSubject
        {
            Inner = { Tag = "x" }
        })).Body;

        string signature = SelectSignature.Compute(body);

        Assert.Contains("Inner", signature);
        Assert.Contains("Tag", signature);
    }

    [Fact]
    public void Compute_LambdaExpression_AppendsParametersAndBody()
    {
        Expression<Func<int, int>> body = x => x + 1;

        string signature = SelectSignature.Compute(body);

        Assert.Contains("System.Int32", signature);
    }

    private sealed class SignatureSubject
    {
        public List<int> Items { get; } = [];
        public InnerSubject Inner { get; } = new();
    }

    private sealed class InnerSubject
    {
        public string Tag { get; set; } = string.Empty;
    }
}
