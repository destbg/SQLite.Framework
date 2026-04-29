using System.Text;
using Microsoft.CodeAnalysis;

namespace SQLite.Framework.SourceGenerator;

/// <summary>
/// Emits per-property bind methods for entities and the <c>SQLiteOptions.EntityWriters</c>
/// dictionary registration. Used by <c>AddRange</c> / <c>UpdateRange</c> / <c>RemoveRange</c> to
/// skip <c>PropertyInfo.GetValue</c> on hot paths.
/// </summary>
internal static class EntityColumnWriterEmitter
{
    public static void EmitRegistration(StringBuilder sb, INamedTypeSymbol entity, string methodName)
    {
        List<IPropertySymbol> props = GetWritableProps(entity);
        if (props.Count == 0)
        {
            return;
        }

        sb.Append("            builder.EntityWriters[typeof(").Append(entity.ToDisplayString())
            .AppendLine(")] = new global::System.Collections.Generic.Dictionary<string, global::SQLite.Framework.SQLiteEntityColumnWriter>");
        sb.AppendLine("            {");
        for (int i = 0; i < props.Count; i++)
        {
            IPropertySymbol prop = props[i];
            string writerName = $"Bind_{methodName}_{SanitizeIdentifier(prop.Name)}";
            sb.Append("                [\"").Append(prop.Name).Append("\"] = ").Append(writerName);
            sb.AppendLine(i == props.Count - 1 ? "" : ",");
        }
        sb.AppendLine("            };");
    }

    public static void EmitWriters(StringBuilder sb, INamedTypeSymbol entity, string methodName)
    {
        List<IPropertySymbol> props = GetWritableProps(entity);
        if (props.Count == 0)
        {
            return;
        }

        string entityType = entity.ToDisplayString();
        foreach (IPropertySymbol prop in props)
        {
            string writerName = $"Bind_{methodName}_{SanitizeIdentifier(prop.Name)}";
            sb.Append("        private static void ").Append(writerName)
                .AppendLine("(global::SQLitePCL.sqlite3_stmt stmt, int idx, object item, global::SQLite.Framework.SQLiteOptions options)");
            sb.AppendLine("        {");
            sb.Append("            ").Append(entityType).Append(" typed = (").Append(entityType).AppendLine(")item;");
            EmitBody(sb, prop, "typed." + prop.Name);
            sb.AppendLine("        }");
            sb.AppendLine();
        }
    }

    private static List<IPropertySymbol> GetWritableProps(INamedTypeSymbol entity)
    {
        List<IPropertySymbol> result = new();
        foreach (IPropertySymbol prop in EnumerateInstanceProperties(entity))
        {
            if (prop.SetMethod == null
                || prop.DeclaredAccessibility != Accessibility.Public
                || prop.IsReadOnly
                || prop.IsStatic)
            {
                continue;
            }

            result.Add(prop);
        }
        return result;
    }

    private static void EmitBody(StringBuilder sb, IPropertySymbol prop, string accessExpr)
    {
        ITypeSymbol type = prop.Type;
        ITypeSymbol underlying = StripNullableSymbol(type);
        bool isNullableValueType = type is INamedTypeSymbol nt && nt.IsGenericType && nt.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T;

        if (TryEmitDirectBind(sb, underlying, accessExpr, isNullableValueType))
        {
            return;
        }

        sb.Append("            options.BindParameter(stmt, idx, (object?)").Append(accessExpr).AppendLine(");");
    }

    private static bool TryEmitDirectBind(StringBuilder sb, ITypeSymbol underlying, string accessExpr, bool isNullableValueType)
    {
        switch (underlying.SpecialType)
        {
            case SpecialType.System_Boolean:
                if (isNullableValueType)
                {
                    sb.Append("            if (").Append(accessExpr).AppendLine(".HasValue)");
                    sb.Append("                global::SQLitePCL.raw.sqlite3_bind_int(stmt, idx, ").Append(accessExpr).AppendLine(".Value ? 1 : 0);");
                    sb.AppendLine("            else");
                    sb.AppendLine("                global::SQLitePCL.raw.sqlite3_bind_null(stmt, idx);");
                }
                else
                {
                    sb.Append("            global::SQLitePCL.raw.sqlite3_bind_int(stmt, idx, ").Append(accessExpr).AppendLine(" ? 1 : 0);");
                }
                return true;
            case SpecialType.System_Byte:
            case SpecialType.System_SByte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
                if (isNullableValueType)
                {
                    sb.Append("            if (").Append(accessExpr).AppendLine(".HasValue)");
                    sb.Append("                global::SQLitePCL.raw.sqlite3_bind_int(stmt, idx, ").Append(accessExpr).AppendLine(".Value);");
                    sb.AppendLine("            else");
                    sb.AppendLine("                global::SQLitePCL.raw.sqlite3_bind_null(stmt, idx);");
                }
                else
                {
                    sb.Append("            global::SQLitePCL.raw.sqlite3_bind_int(stmt, idx, ").Append(accessExpr).AppendLine(");");
                }
                return true;
            case SpecialType.System_UInt32:
                if (isNullableValueType)
                {
                    sb.Append("            if (").Append(accessExpr).AppendLine(".HasValue)");
                    sb.Append("                global::SQLitePCL.raw.sqlite3_bind_int(stmt, idx, (int)").Append(accessExpr).AppendLine(".Value);");
                    sb.AppendLine("            else");
                    sb.AppendLine("                global::SQLitePCL.raw.sqlite3_bind_null(stmt, idx);");
                }
                else
                {
                    sb.Append("            global::SQLitePCL.raw.sqlite3_bind_int(stmt, idx, (int)").Append(accessExpr).AppendLine(");");
                }
                return true;
            case SpecialType.System_Int64:
                if (isNullableValueType)
                {
                    sb.Append("            if (").Append(accessExpr).AppendLine(".HasValue)");
                    sb.Append("                global::SQLitePCL.raw.sqlite3_bind_int64(stmt, idx, ").Append(accessExpr).AppendLine(".Value);");
                    sb.AppendLine("            else");
                    sb.AppendLine("                global::SQLitePCL.raw.sqlite3_bind_null(stmt, idx);");
                }
                else
                {
                    sb.Append("            global::SQLitePCL.raw.sqlite3_bind_int64(stmt, idx, ").Append(accessExpr).AppendLine(");");
                }
                return true;
            case SpecialType.System_UInt64:
                if (isNullableValueType)
                {
                    sb.Append("            if (").Append(accessExpr).AppendLine(".HasValue)");
                    sb.Append("                global::SQLitePCL.raw.sqlite3_bind_int64(stmt, idx, (long)").Append(accessExpr).AppendLine(".Value);");
                    sb.AppendLine("            else");
                    sb.AppendLine("                global::SQLitePCL.raw.sqlite3_bind_null(stmt, idx);");
                }
                else
                {
                    sb.Append("            global::SQLitePCL.raw.sqlite3_bind_int64(stmt, idx, (long)").Append(accessExpr).AppendLine(");");
                }
                return true;
            case SpecialType.System_Single:
            case SpecialType.System_Double:
                if (isNullableValueType)
                {
                    sb.Append("            if (").Append(accessExpr).AppendLine(".HasValue)");
                    sb.Append("                global::SQLitePCL.raw.sqlite3_bind_double(stmt, idx, ").Append(accessExpr).AppendLine(".Value);");
                    sb.AppendLine("            else");
                    sb.AppendLine("                global::SQLitePCL.raw.sqlite3_bind_null(stmt, idx);");
                }
                else
                {
                    sb.Append("            global::SQLitePCL.raw.sqlite3_bind_double(stmt, idx, ").Append(accessExpr).AppendLine(");");
                }
                return true;
            case SpecialType.System_String:
                sb.Append("            { string? __v = ").Append(accessExpr).AppendLine(";");
                sb.AppendLine("              if (__v == null) global::SQLitePCL.raw.sqlite3_bind_null(stmt, idx);");
                sb.AppendLine("              else global::SQLitePCL.raw.sqlite3_bind_text(stmt, idx, __v); }");
                return true;
            case SpecialType.System_Char:
                if (isNullableValueType)
                {
                    sb.Append("            if (").Append(accessExpr).AppendLine(".HasValue)");
                    sb.Append("                global::SQLitePCL.raw.sqlite3_bind_text(stmt, idx, ").Append(accessExpr).AppendLine(".Value.ToString());");
                    sb.AppendLine("            else");
                    sb.AppendLine("                global::SQLitePCL.raw.sqlite3_bind_null(stmt, idx);");
                }
                else
                {
                    sb.Append("            global::SQLitePCL.raw.sqlite3_bind_text(stmt, idx, ").Append(accessExpr).AppendLine(".ToString());");
                }
                return true;
        }

        if (underlying is IArrayTypeSymbol arr && arr.ElementType.SpecialType == SpecialType.System_Byte)
        {
            sb.Append("            { byte[]? __v = ").Append(accessExpr).AppendLine(";");
            sb.AppendLine("              if (__v == null) global::SQLitePCL.raw.sqlite3_bind_null(stmt, idx);");
            sb.AppendLine("              else global::SQLitePCL.raw.sqlite3_bind_blob(stmt, idx, __v); }");
            return true;
        }

        return false;
    }

    private static IEnumerable<IPropertySymbol> EnumerateInstanceProperties(INamedTypeSymbol entity)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        for (INamedTypeSymbol? current = entity; current != null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
        {
            foreach (ISymbol member in current.GetMembers())
            {
                if (member is not IPropertySymbol prop || prop.IsStatic || prop.IsIndexer)
                {
                    continue;
                }

                if (!seen.Add(prop.Name))
                {
                    continue;
                }

                yield return prop;
            }
        }
    }

    private static ITypeSymbol StripNullableSymbol(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol nt && nt.IsGenericType && nt.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            return nt.TypeArguments[0];
        }
        if (type.NullableAnnotation == NullableAnnotation.Annotated && type.IsReferenceType)
        {
            return type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
        }
        return type;
    }

    private static string SanitizeIdentifier(string name)
    {
        StringBuilder sb = new(name.Length);
        foreach (char c in name)
        {
            sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
        }
        return sb.ToString();
    }
}
