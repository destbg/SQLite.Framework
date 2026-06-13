using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SQLite.Framework.SourceGenerator.Models;

/// <summary>
/// Describes the key selector of a group-by invocation.
/// </summary>
public sealed class GroupByKeyInvocation
{
    /// <summary>
    /// Creates a new group-by key invocation.
    /// </summary>
    public GroupByKeyInvocation(string signature, ExpressionSyntax body, string parameterName, ISymbol parameterSymbol, ITypeSymbol parameterType, ITypeSymbol keyType)
    {
        Signature = signature;
        Body = body;
        ParameterName = parameterName;
        ParameterSymbol = parameterSymbol;
        ParameterType = parameterType;
        KeyType = keyType;
    }

    /// <summary>
    /// The signature string of the invocation.
    /// </summary>
    public string Signature { get; }

    /// <summary>
    /// The body of the key selector lambda.
    /// </summary>
    public ExpressionSyntax Body { get; }

    /// <summary>
    /// The name of the row variable the key selector body refers to.
    /// </summary>
    public string ParameterName { get; }

    /// <summary>
    /// The symbol of the row variable. A lambda parameter for fluent calls
    /// and a range variable for query syntax.
    /// </summary>
    public ISymbol ParameterSymbol { get; }

    /// <summary>
    /// The type of the row variable.
    /// </summary>
    public ITypeSymbol ParameterType { get; }

    /// <summary>
    /// The type of the group key.
    /// </summary>
    public ITypeSymbol KeyType { get; }
}
