namespace SQLite.Framework.Internals.Visitors.SQL;

internal partial class SQLVisitor
{
    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "ToString does exist")]
    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node.NodeType == ExpressionType.ArrayIndex)
        {
            Expression resolvedIndex = ExpressionHelpers.IsConstant(node.Right) ? node.Right : Visit(node.Right);
            return Expression.MakeBinary(node.NodeType, node.Left, resolvedIndex, node.IsLiftedToNull, node.Method);
        }

        Expression leftNode = node.Left;
        Expression rightNode = node.Right;

        if (leftNode is UnaryExpression { NodeType: ExpressionType.Convert } leftEnumConvert && leftEnumConvert.Operand.Type.IsEnum)
        {
            Type enumType = leftEnumConvert.Operand.Type;
            leftNode = leftEnumConvert.Operand;
            if (ExpressionHelpers.IsConstant(rightNode) && rightNode.Type == Enum.GetUnderlyingType(enumType))
            {
                object? intValue = ExpressionHelpers.GetConstantValue(rightNode);
                rightNode = Expression.Constant(Enum.ToObject(enumType, intValue!), enumType);
            }
        }

        if (rightNode is UnaryExpression { NodeType: ExpressionType.Convert } rightEnumConvert && rightEnumConvert.Operand.Type.IsEnum)
        {
            Type enumType = rightEnumConvert.Operand.Type;
            rightNode = rightEnumConvert.Operand;
            if (ExpressionHelpers.IsConstant(leftNode) && leftNode.Type == Enum.GetUnderlyingType(enumType))
            {
                object? intValue = ExpressionHelpers.GetConstantValue(leftNode);
                leftNode = Expression.Constant(Enum.ToObject(enumType, intValue!), enumType);
            }
        }

        if (rightNode.Type == typeof(int) && leftNode is UnaryExpression leftUnary && leftUnary.Operand.Type == typeof(char))
        {
            leftNode = leftUnary.Operand;

            if (ExpressionHelpers.IsConstant(rightNode))
            {
                int value = (int)ExpressionHelpers.GetConstantValue(rightNode)!;
                rightNode = Expression.Constant(((char)value).ToString());
            }
            else
            {
                rightNode = Expression.MakeUnary(ExpressionType.Convert, rightNode, typeof(char));
            }
        }
        else if (leftNode.Type == typeof(int) && rightNode is UnaryExpression rightUnary && rightUnary.Operand.Type == typeof(char))
        {
            rightNode = rightUnary.Operand;

            if (ExpressionHelpers.IsConstant(leftNode))
            {
                int value = (int)ExpressionHelpers.GetConstantValue(leftNode)!;
                leftNode = Expression.Constant(((char)value).ToString());
            }
            else
            {
                leftNode = Expression.MakeUnary(ExpressionType.Convert, leftNode, typeof(char));
            }
        }

        ResolvedModel resolvedLeft = ResolveExpression(leftNode);
        ResolvedModel resolvedRight = ResolveExpression(rightNode);

        bool isArithmeticOp = node.NodeType is ExpressionType.Add
            or ExpressionType.Subtract
            or ExpressionType.Multiply
            or ExpressionType.Divide
            or ExpressionType.Modulo;
        bool eitherSideStoredAsTextOrBlob =
            Database.Options.HasTextOrBlobConverter(leftNode.Type) || Database.Options.HasTextOrBlobConverter(rightNode.Type);

        if (resolvedLeft.SQLiteExpression == null || resolvedRight.SQLiteExpression == null
            || (isArithmeticOp && eitherSideStoredAsTextOrBlob))
        {
            return Expression.MakeBinary(node.NodeType, resolvedLeft.Expression, resolvedRight.Expression, node.IsLiftedToNull, node.Method);
        }

        SQLiteExpression left = ExpressionHelpers.BracketIfNeeded(resolvedLeft.SQLiteExpression);
        SQLiteExpression right = ExpressionHelpers.BracketIfNeeded(resolvedRight.SQLiteExpression);

        SQLiteParameter[]? bothParameters = ParameterHelpers.CombineParameters(left, right);

        if (node.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse
            || (node.Type == typeof(bool) && node.NodeType is ExpressionType.And or ExpressionType.Or))
        {
            bool isAnd = node.NodeType is ExpressionType.AndAlso or ExpressionType.And;
            string spacedOp = isAnd ? " AND " : " OR ";

            SQLiteExpression boolLeft = isAnd ? BracketBooleanOr(leftNode, resolvedLeft.SQLiteExpression!) : resolvedLeft.SQLiteExpression!;
            SQLiteExpression boolRight = isAnd ? BracketBooleanOr(rightNode, resolvedRight.SQLiteExpression!) : resolvedRight.SQLiteExpression!;
            SQLiteParameter[]? boolParameters = ParameterHelpers.CombineParameters(boolLeft, boolRight);

            SQLiteExpression boolResult = SQLiteExpression.Binary(typeof(bool), Counters.NextIdentifier(), "", boolLeft, spacedOp, boolRight, "", boolParameters);

            boolResult.RequiresBrackets = !isAnd;
            return boolResult;
        }

        if (node.NodeType is ExpressionType.ExclusiveOr)
        {
            if (node.Type == typeof(bool))
            {
                return SQLiteExpression.Binary(typeof(bool), Counters.NextIdentifier(), "", left, " <> ", right, "", bothParameters);
            }

            return SQLiteExpression.Multi(node.Type, Counters.NextIdentifier(),
                ["((", " | ", ") - (", " & ", "))"],
                [left, right, left, right],
                bothParameters);
        }

        if (node.NodeType is ExpressionType.Coalesce)
        {
            return SQLiteExpression.Binary(node.Type, Counters.NextIdentifier(), "COALESCE(", left, ", ", right, ")", bothParameters);
        }

        bool equalityOp = node.NodeType is ExpressionType.Equal or ExpressionType.NotEqual;
        bool isLeftNull = resolvedLeft is { IsConstant: true, Constant: null };
        bool isRightNull = resolvedRight is { IsConstant: true, Constant: null };

        if (equalityOp && (isLeftNull || isRightNull))
        {
            string nullOp = node.NodeType == ExpressionType.Equal ? " IS NULL" : " IS NOT NULL";

            if (isLeftNull)
            {
                left = right;
            }

            return SQLiteExpression.Wrap(typeof(bool), Counters.NextIdentifier(), "", left, nullOp, left.Parameters);
        }

        if (node.NodeType is ExpressionType.Modulo)
        {
            Type modType = Nullable.GetUnderlyingType(node.Type) ?? node.Type;
            if (modType == typeof(double) || modType == typeof(float) || modType == typeof(decimal))
            {
                return SQLiteExpression.Multi(node.Type, Counters.NextIdentifier(),
                    ["(", " - ", " * CAST(", " / ", " AS INTEGER))"],
                    [left, right, left, right],
                    bothParameters);
            }
        }

        bool eitherOperandMayBeNull = MayBeNull(leftNode) || MayBeNull(rightNode);
        bool equalIsNullSafe = (IsNullableColumn(leftNode) && !resolvedLeft.IsConstant)
            || (IsNullableColumn(rightNode) && !resolvedRight.IsConstant);

        if (node.NodeType is ExpressionType.Add && node.Type == typeof(string))
        {
            SQLiteExpression concatLeft = IsNullableStringColumn(leftNode)
                ? SQLiteExpression.Wrap(typeof(string), Counters.NextIdentifier(), "COALESCE(", left, ", '')", left.Parameters)
                : left;
            SQLiteExpression concatRight = IsNullableStringColumn(rightNode)
                ? SQLiteExpression.Wrap(typeof(string), Counters.NextIdentifier(), "COALESCE(", right, ", '')", right.Parameters)
                : right;

            return SQLiteExpression.Binary(node.Type, Counters.NextIdentifier(), "", concatLeft, " || ", concatRight, "", ParameterHelpers.CombineParameters(concatLeft, concatRight));
        }

        if (leftNode.Type == typeof(ulong) && rightNode.Type == typeof(ulong)
            && node.NodeType is ExpressionType.GreaterThan or ExpressionType.LessThan
                or ExpressionType.GreaterThanOrEqual or ExpressionType.LessThanOrEqual)
        {
            return BuildUnsignedComparison(node.NodeType, left, right, bothParameters);
        }

        (string sqlOp, bool parenthesis) = node.NodeType switch
        {
            ExpressionType.Equal => (equalIsNullSafe ? " IS " : " = ", false),
            ExpressionType.NotEqual => (eitherOperandMayBeNull ? " IS NOT " : " <> ", false),
            ExpressionType.GreaterThan => (" > ", false),
            ExpressionType.LessThan => (" < ", false),
            ExpressionType.GreaterThanOrEqual => (" >= ", false),
            ExpressionType.LessThanOrEqual => (" <= ", false),
            ExpressionType.Add => (" + ", true),
            ExpressionType.Subtract => (" - ", true),
            ExpressionType.Multiply => (" * ", true),
            ExpressionType.Divide => (" / ", true),
            ExpressionType.Modulo => (" % ", true),
            ExpressionType.And => (" & ", true),
            ExpressionType.Or => (" | ", true),
            ExpressionType.LeftShift => (" << ", true),
            ExpressionType.RightShift => (" >> ", true),
            _ => throw new NotSupportedException($"Unsupported binary op {node.NodeType}")
        };

        if (parenthesis)
        {
            return SQLiteExpression.Binary(node.Type, Counters.NextIdentifier(), "(", left, sqlOp, right, ")", bothParameters);
        }

        return SQLiteExpression.Binary(node.Type, Counters.NextIdentifier(), "", left, sqlOp, right, "", bothParameters);
    }

    private SQLiteExpression BuildUnsignedComparison(ExpressionType nodeType, SQLiteExpression a, SQLiteExpression b, SQLiteParameter[]? parameters)
    {
        string signedOp = nodeType switch
        {
            ExpressionType.GreaterThan => " > ",
            ExpressionType.LessThan => " < ",
            ExpressionType.GreaterThanOrEqual => " >= ",
            _ => " <= "
        };

        SQLiteExpression elseOperand = nodeType is ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual ? a : b;

        return SQLiteExpression.Multi(
            typeof(bool),
            Counters.NextIdentifier(),
            ["(CASE WHEN ((", " < 0) = (", " < 0)) THEN ", signedOp, " ELSE ", " < 0 END)"],
            [a, b, a, b, elseOperand],
            parameters);
    }

    private static SQLiteExpression BracketBooleanOr(Expression node, SQLiteExpression expr)
    {
        Expression stripped = ExpressionHelpers.StripUpcast(ExpressionHelpers.StripQuotes(node));
        bool isOr = stripped.NodeType is ExpressionType.OrElse or ExpressionType.Or;
        return isOr
            ? SQLiteExpression.Wrap(expr.Type, expr.Identifier, "(", expr, ")", expr.Parameters)
            : expr;
    }

    private static SQLiteExpression BracketBooleanCompound(Expression node, SQLiteExpression expr)
    {
        Expression stripped = ExpressionHelpers.StripUpcast(ExpressionHelpers.StripQuotes(node));
        bool isCompound = stripped.NodeType is ExpressionType.OrElse or ExpressionType.AndAlso
            or ExpressionType.Or or ExpressionType.And;
        return isCompound
            ? SQLiteExpression.Wrap(expr.Type, expr.Identifier, "(", expr, ")", expr.Parameters)
            : expr;
    }

    private static bool MayBeNull(Expression operand)
    {
        Expression stripped = operand;
        while (stripped is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert)
        {
            stripped = convert.Operand;
        }

        if (stripped.Type.IsValueType)
        {
            return Nullable.GetUnderlyingType(stripped.Type) != null;
        }

        return stripped switch
        {
            ConstantExpression => false,
            MemberExpression { Member: PropertyInfo property } =>
                new NullabilityInfoContext().Create(property).ReadState == NullabilityState.Nullable,
            _ => true
        };
    }

    private static bool IsNullableStringColumn(Expression operand)
    {
        Expression stripped = operand;
        while (stripped is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert)
        {
            stripped = convert.Operand;
        }

        return stripped is MemberExpression { Member: PropertyInfo property }
            && property.PropertyType == typeof(string)
            && new NullabilityInfoContext().Create(property).ReadState == NullabilityState.Nullable;
    }

    private static bool IsNullableColumn(Expression operand)
    {
        Expression stripped = operand;
        while (stripped is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert)
        {
            stripped = convert.Operand;
        }

        if (stripped.Type.IsValueType)
        {
            return Nullable.GetUnderlyingType(stripped.Type) != null;
        }

        return stripped is MemberExpression { Member: PropertyInfo property }
            && new NullabilityInfoContext().Create(property).ReadState == NullabilityState.Nullable;
    }
}
