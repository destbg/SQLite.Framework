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
    public GroupByKeyInvocation(string signature, ExpressionSyntax body, ParameterSyntax parameterSyntax, IParameterSymbol parameterSymbol, ITypeSymbol keyType)
    {
        Signature = signature;
        Body = body;
        ParameterSyntax = parameterSyntax;
        ParameterSymbol = parameterSymbol;
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
    /// The syntax of the lambda parameter.
    /// </summary>
    public ParameterSyntax ParameterSyntax { get; }

    /// <summary>
    /// The symbol of the lambda parameter.
    /// </summary>
    public IParameterSymbol ParameterSymbol { get; }

    /// <summary>
    /// The type of the group key.
    /// </summary>
    public ITypeSymbol KeyType { get; }
}
