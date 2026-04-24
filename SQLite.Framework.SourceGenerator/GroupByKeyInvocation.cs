using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SQLite.Framework.SourceGenerator;

internal sealed class GroupByKeyInvocation
{
    public GroupByKeyInvocation(string signature, ExpressionSyntax body, ParameterSyntax parameterSyntax, IParameterSymbol parameterSymbol, ITypeSymbol keyType)
    {
        Signature = signature;
        Body = body;
        ParameterSyntax = parameterSyntax;
        ParameterSymbol = parameterSymbol;
        KeyType = keyType;
    }

    public string Signature { get; }
    public ExpressionSyntax Body { get; }
    public ParameterSyntax ParameterSyntax { get; }
    public IParameterSymbol ParameterSymbol { get; }
    public ITypeSymbol KeyType { get; }
}
