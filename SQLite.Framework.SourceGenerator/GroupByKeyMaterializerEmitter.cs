using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SQLite.Framework.SourceGenerator;

/// <summary>
/// Emits a key-selector method for a client-side <c>GroupBy(keySelector)</c> call. The method
/// takes the already-materialized row from <c>SQLiteQueryContext.Input</c> and returns the group key.
/// </summary>
internal static class GroupByKeyMaterializerEmitter
{
    public static bool TryEmit(StringBuilder sb, string methodName, GroupByKeyInvocation invocation, SemanticModel model)
    {
        if (invocation.ParameterSymbol.Type is ITypeParameterSymbol)
        {
            return false;
        }

        if (!IsEmittable(invocation.Body, invocation.ParameterSymbol, model))
        {
            return false;
        }

        string paramName = invocation.ParameterSyntax.Identifier.ValueText;
        string paramType = SelectMaterializerEmitter.FormatType(invocation.ParameterSymbol.Type);

        sb.Append("        private static object? ").Append(methodName).AppendLine("(SQLite.Framework.Models.SQLiteQueryContext ctx)");
        sb.AppendLine("        {");
        sb.Append("            ").Append(paramType).Append(" ").Append(paramName).Append(" = (")
            .Append(paramType).AppendLine(")ctx.Input!;");
        sb.Append("            return ").Append(invocation.Body.ToString()).AppendLine(";");
        sb.AppendLine("        }");
        sb.AppendLine();
        return true;
    }

    private static bool IsEmittable(ExpressionSyntax node, IParameterSymbol parameter, SemanticModel model)
    {
        switch (node)
        {
            case ParenthesizedExpressionSyntax paren:
                return IsEmittable(paren.Expression, parameter, model);

            case LiteralExpressionSyntax:
                return true;

            case IdentifierNameSyntax ident:
                return SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(ident).Symbol, parameter);

            case MemberAccessExpressionSyntax ma when ma.Kind() == SyntaxKind.SimpleMemberAccessExpression:
                return IsEmittable(ma.Expression, parameter, model);

            case BinaryExpressionSyntax bin:
                return IsEmittable(bin.Left, parameter, model) && IsEmittable(bin.Right, parameter, model);

            case PrefixUnaryExpressionSyntax prefix:
                return IsEmittable(prefix.Operand, parameter, model);

            case PostfixUnaryExpressionSyntax postfix:
                return IsEmittable(postfix.Operand, parameter, model);

            case ConditionalExpressionSyntax cond:
                return IsEmittable(cond.Condition, parameter, model)
                    && IsEmittable(cond.WhenTrue, parameter, model)
                    && IsEmittable(cond.WhenFalse, parameter, model);

            case AnonymousObjectCreationExpressionSyntax anon:
                foreach (AnonymousObjectMemberDeclaratorSyntax init in anon.Initializers)
                {
                    if (!IsEmittable(init.Expression, parameter, model))
                    {
                        return false;
                    }
                }
                return true;

            case TupleExpressionSyntax tuple:
                foreach (ArgumentSyntax arg in tuple.Arguments)
                {
                    if (!IsEmittable(arg.Expression, parameter, model))
                    {
                        return false;
                    }
                }
                return true;

            case CastExpressionSyntax cast:
                return IsBuiltInType(model.GetTypeInfo(cast.Type).Type)
                    && IsEmittable(cast.Expression, parameter, model);

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
