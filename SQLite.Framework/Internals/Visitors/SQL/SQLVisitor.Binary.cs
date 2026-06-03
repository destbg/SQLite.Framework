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

        if (Database.Options.CharStorage != CharStorageMode.Integer && rightNode.Type == typeof(int) && leftNode is UnaryExpression leftUnary && leftUnary.Operand.Type == typeof(char))
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
        else if (Database.Options.CharStorage != CharStorageMode.Integer && leftNode.Type == typeof(int) && rightNode is UnaryExpression rightUnary && rightUnary.Operand.Type == typeof(char))
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

        SQLiteExpression left = BracketBinaryOperand(leftNode, resolvedLeft.SQLiteExpression);
        SQLiteExpression right = BracketBinaryOperand(rightNode, resolvedRight.SQLiteExpression);

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
            SQLiteExpression concatLeft = CoalesceNullableStringOperand(this, leftNode, resolvedLeft, left);
            SQLiteExpression concatRight = CoalesceNullableStringOperand(this, rightNode, resolvedRight, right);

            return SQLiteExpression.Binary(node.Type, Counters.NextIdentifier(), "", concatLeft, " || ", concatRight, "", ParameterHelpers.CombineParameters(concatLeft, concatRight));
        }

        if (leftNode.Type == typeof(ulong) && rightNode.Type == typeof(ulong))
        {
            switch (node.NodeType)
            {
                case ExpressionType.GreaterThan or ExpressionType.LessThan
                    or ExpressionType.GreaterThanOrEqual or ExpressionType.LessThanOrEqual:
                    return BuildUnsignedComparison(node.NodeType, left, right, bothParameters);
                case ExpressionType.Divide or ExpressionType.Modulo:
                    return BuildUnsignedDivOrMod(node.NodeType == ExpressionType.Modulo, node.Type, left, right, bothParameters);
            }
        }

        if (equalityOp)
        {
            left = CoalesceLiftedOrderComparison(leftNode, left);
            right = CoalesceLiftedOrderComparison(rightNode, right);
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

    private SQLiteExpression BuildUnsignedDivOrMod(bool isModulo, Type resultType, SQLiteExpression a, SQLiteExpression b, SQLiteParameter[]? parameters)
    {
        string caseBody = isModulo
            ? "CASE WHEN bb = 0 THEN aa % bb WHEN bb = 1 THEN 0 WHEN bb < 0 THEN (CASE WHEN (aa < 0 AND aa >= bb) THEN (aa - bb) ELSE aa END) ELSE (CASE WHEN (rh + a0) >= (bb - rh) THEN (rh + a0) - (bb - rh) ELSE (rh + a0 + rh) END) END"
            : "CASE WHEN bb = 0 THEN aa / bb WHEN bb = 1 THEN aa WHEN bb < 0 THEN (CASE WHEN (aa < 0 AND aa >= bb) THEN 1 ELSE 0 END) ELSE ((ah / bb) * 2 + (CASE WHEN (rh + a0) >= (bb - rh) THEN 1 ELSE 0 END)) END";

        string prefix = "(SELECT " + caseBody + " FROM (SELECT aa, bb, ah, (ah % bb) AS rh, (aa & 1) AS a0 FROM (SELECT aa, bb, ((aa >> 1) & 9223372036854775807) AS ah FROM (SELECT ";

        return SQLiteExpression.Multi(
            resultType,
            Counters.NextIdentifier(),
            [prefix, " AS aa, ", " AS bb))))"],
            [a, b],
            parameters);
    }

    private SQLiteExpression CoalesceLiftedOrderComparison(Expression operand, SQLiteExpression expr)
    {
        return IsLiftedOrderComparisonThatMayBeNull(operand)
            ? SQLiteExpression.Wrap(typeof(bool), Counters.NextIdentifier(), "COALESCE(", expr, ", 0)", expr.Parameters)
            : expr;
    }

    public static SQLiteExpression CoalesceNullableStringOperand(SQLVisitor visitor, Expression operand, ResolvedModel resolved, SQLiteExpression expr)
    {
        return StringConcatOperandMayBeNull(operand, resolved)
            ? SQLiteExpression.Wrap(typeof(string), visitor.Counters.NextIdentifier(), "COALESCE(", expr, ", '')", expr.Parameters)
            : expr;
    }

    private static bool IsLiftedOrderComparisonThatMayBeNull(Expression operand)
    {
        Expression stripped = operand;
        while (stripped is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert)
        {
            stripped = convert.Operand;
        }

        return stripped is BinaryExpression binary
            && binary.NodeType is ExpressionType.GreaterThan or ExpressionType.LessThan
                or ExpressionType.GreaterThanOrEqual or ExpressionType.LessThanOrEqual
            && (MayBeNull(binary.Left) || MayBeNull(binary.Right));
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

    private static SQLiteExpression BracketBinaryOperand(Expression node, SQLiteExpression expr)
    {
        Expression stripped = ExpressionHelpers.StripUpcast(ExpressionHelpers.StripQuotes(node));
        bool needsBrackets = expr.RequiresBrackets
            || stripped.NodeType is ExpressionType.Equal or ExpressionType.NotEqual
            || (stripped.Type == typeof(bool)
                && stripped.NodeType is ExpressionType.AndAlso or ExpressionType.And or ExpressionType.ExclusiveOr);

        return needsBrackets
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

    private static bool StringConcatOperandMayBeNull(Expression operand, ResolvedModel resolved)
    {
        return resolved is { IsConstant: true, Constant: null }
            || StringConcatExpressionMayBeNull(operand);
    }

    private static bool StringConcatExpressionMayBeNull(Expression operand)
    {
        Expression stripped = operand;
        while (stripped is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert)
        {
            stripped = convert.Operand;
        }

        return stripped switch
        {
            ConstantExpression constant => constant.Value == null,
            ConditionalExpression conditional =>
                StringConcatExpressionMayBeNull(conditional.IfTrue)
                || StringConcatExpressionMayBeNull(conditional.IfFalse),
            MemberExpression { Member: PropertyInfo property } =>
                new NullabilityInfoContext().Create(property).ReadState == NullabilityState.Nullable,
            _ => false
        };
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
