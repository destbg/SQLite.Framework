using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SQLite.Framework.SourceGenerator.Models;
using SQLite.Framework.SourceGenerator.Writers;

namespace SQLite.Framework.SourceGenerator.Helpers;

/// <summary>
/// Emits a key-selector method for a client-side <c>GroupBy(keySelector)</c> call. The method
/// takes the already-materialized row from <c>SQLiteQueryContext.Input</c> and returns the group key.
/// </summary>
public static class GroupByKeyMaterializerEmitter
{
    /// <summary>
    /// Emits the key-selector method when the key expression is simple enough to translate.
    /// </summary>
    public static bool TryEmit(StringBuilder sb, string methodName, GroupByKeyInvocation invocation, SemanticModel model)
    {
        if (invocation.ParameterType is ITypeParameterSymbol)
        {
            return false;
        }

        Dictionary<ISymbol, RowBinding> bindings = new(SymbolEqualityComparer.Default)
        {
            [invocation.ParameterSymbol] = new RowBinding((string?)null, invocation.ParameterType)
        };
        SelectSignatureCtx writerCtx = new(invocation.ParameterType, bindings, model);

        if (!IsEmittable(invocation.Body, invocation.ParameterSymbol, writerCtx))
        {
            return false;
        }

        GroupKeyBodyRewriter rewriter = new(writerCtx);
        if (rewriter.Visit(invocation.Body) is not ExpressionSyntax rewrittenBody)
        {
            return false;
        }

        string paramName = invocation.ParameterName;
        string paramType = SelectMaterializerEmitter.FormatType(invocation.ParameterType);

        sb.Append("        private static object? ").Append(methodName).AppendLine("(SQLite.Framework.Models.SQLiteQueryContext ctx)");
        sb.AppendLine("        {");
        sb.Append("            ").Append(paramType).Append(" ").Append(paramName).Append(" = (")
            .Append(paramType).AppendLine(")ctx.Input!;");
        sb.Append("            return ").Append(rewrittenBody.ToString()).AppendLine(";");
        sb.AppendLine("        }");
        sb.AppendLine();
        return true;
    }

    private static bool IsEmittable(ExpressionSyntax node, ISymbol parameter, SelectSignatureCtx ctx)
    {
        switch (node)
        {
            case ParenthesizedExpressionSyntax paren:
                return IsEmittable(paren.Expression, parameter, ctx);

            case LiteralExpressionSyntax:
                return true;

            case IdentifierNameSyntax ident:
                if (SymbolEqualityComparer.Default.Equals(ctx.Model.GetSymbolInfo(ident).Symbol, parameter))
                {
                    return true;
                }
                return SelectSignatureWriter.IsCapturedValue(ident, ctx);

            case MemberAccessExpressionSyntax ma when ma.Kind() == SyntaxKind.SimpleMemberAccessExpression:
                if (SelectSignatureWriter.IsCapturedValue(ma, ctx))
                {
                    return true;
                }
                if (ctx.Model.GetSymbolInfo(ma).Symbol is IFieldSymbol { ContainingType.TypeKind: TypeKind.Enum })
                {
                    return true;
                }
                return IsEmittable(ma.Expression, parameter, ctx);

            case BinaryExpressionSyntax bin:
                return IsEmittable(bin.Left, parameter, ctx) && IsEmittable(bin.Right, parameter, ctx);

            case PrefixUnaryExpressionSyntax prefix:
                return IsEmittable(prefix.Operand, parameter, ctx);

            case PostfixUnaryExpressionSyntax postfix:
                return IsEmittable(postfix.Operand, parameter, ctx);

            case ConditionalExpressionSyntax cond:
                return IsEmittable(cond.Condition, parameter, ctx)
                    && IsEmittable(cond.WhenTrue, parameter, ctx)
                    && IsEmittable(cond.WhenFalse, parameter, ctx);

            case AnonymousObjectCreationExpressionSyntax anon:
                foreach (AnonymousObjectMemberDeclaratorSyntax init in anon.Initializers)
                {
                    if (!IsEmittable(init.Expression, parameter, ctx))
                    {
                        return false;
                    }
                }
                return true;

            case TupleExpressionSyntax tuple:
                foreach (ArgumentSyntax arg in tuple.Arguments)
                {
                    if (!IsEmittable(arg.Expression, parameter, ctx))
                    {
                        return false;
                    }
                }
                return true;

            case CastExpressionSyntax cast:
                return IsBuiltInType(ctx.Model.GetTypeInfo(cast.Type).Type)
                    && IsEmittable(cast.Expression, parameter, ctx);

            default:
                return false;
        }
    }

    private static bool IsBuiltInType(ITypeSymbol? type)
    {
        if (type == null)
        {
            return false;
        }

        if (type is INamedTypeSymbol { IsGenericType: true } named
            && named.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            return IsBuiltInType(named.TypeArguments[0]);
        }

        switch (type.SpecialType)
        {
            case SpecialType.System_Boolean:
            case SpecialType.System_Byte:
            case SpecialType.System_SByte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_Decimal:
            case SpecialType.System_Char:
            case SpecialType.System_String:
                return true;
            default:
                return false;
        }
    }
}
