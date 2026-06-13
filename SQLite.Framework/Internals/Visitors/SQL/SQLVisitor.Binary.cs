namespace SQLite.Framework.Internals.Visitors.SQL;

internal partial class SQLVisitor
{
    public SQLiteExpression CoalesceLiftedOrderComparison(Expression operand, SQLiteExpression expr)
    {
        return IsLiftedOrderComparisonThatMayBeNull(operand)
            ? SQLiteExpression.Wrap(typeof(bool), Counters.NextIdentifier(), "COALESCE(", expr, ", 0)", expr.Parameters)
            : expr;
    }

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

        bool charComparisonOp = node.NodeType is ExpressionType.Equal or ExpressionType.NotEqual
            or ExpressionType.GreaterThan or ExpressionType.LessThan
            or ExpressionType.GreaterThanOrEqual or ExpressionType.LessThanOrEqual;

        if (charComparisonOp && Database.Options.CharStorage != CharStorageMode.Integer)
        {
            bool leftIsCharConvert = leftNode is UnaryExpression { NodeType: ExpressionType.Convert } lcc && lcc.Operand.Type == typeof(char);
            bool rightIsCharConvert = rightNode is UnaryExpression { NodeType: ExpressionType.Convert } rcc && rcc.Operand.Type == typeof(char);

            if (leftIsCharConvert && rightIsCharConvert)
            {
                leftNode = ((UnaryExpression)leftNode).Operand;
                rightNode = ((UnaryExpression)rightNode).Operand;
            }
            else if (leftIsCharConvert && TryGetInRangeCharText(rightNode, out string? rightText))
            {
                leftNode = ((UnaryExpression)leftNode).Operand;
                rightNode = Expression.Constant(rightText);
            }
            else if (rightIsCharConvert && TryGetInRangeCharText(leftNode, out string? leftText))
            {
                rightNode = ((UnaryExpression)rightNode).Operand;
                leftNode = Expression.Constant(leftText);
            }
        }

        ResolvedModel resolvedLeft = ResolveExpression(leftNode);
        ResolvedModel resolvedRight = ResolveExpression(rightNode);

        bool isArithmeticOp = node.NodeType is ExpressionType.Add or ExpressionType.AddChecked
            or ExpressionType.Subtract or ExpressionType.SubtractChecked
            or ExpressionType.Multiply or ExpressionType.MultiplyChecked
            or ExpressionType.Divide
            or ExpressionType.Modulo;
        bool eitherSideStoredAsTextOrBlob =
            Database.Options.HasTextOrBlobConverter(leftNode.Type) || Database.Options.HasTextOrBlobConverter(rightNode.Type);

        if (resolvedLeft.SQLiteExpression == null || resolvedRight.SQLiteExpression == null
            || (isArithmeticOp && eitherSideStoredAsTextOrBlob))
        {
            return Expression.MakeBinary(
                node.NodeType,
                CoerceClientExpression(resolvedLeft.Expression, leftNode.Type),
                CoerceClientExpression(resolvedRight.Expression, rightNode.Type),
                node.IsLiftedToNull,
                node.Method);
        }

        SQLiteExpression left = BracketBinaryOperand(leftNode, resolvedLeft.SQLiteExpression);
        SQLiteExpression right = BracketBinaryOperand(rightNode, resolvedRight.SQLiteExpression);

        if (isArithmeticOp
            && Database.Options.TimeSpanStorage == TimeSpanStorageMode.Text
            && (Nullable.GetUnderlyingType(node.Type) ?? node.Type) == typeof(DateTime))
        {
            left = CoerceConstantTimeSpanToTicks(resolvedLeft, left);
            right = CoerceConstantTimeSpanToTicks(resolvedRight, right);
        }

        SQLiteParameter[]? bothParameters = ParameterHelpers.CombineParameters(left, right);

        Type nodeUnderlyingType = Nullable.GetUnderlyingType(node.Type) ?? node.Type;
        if (node.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse
            || (nodeUnderlyingType == typeof(bool) && node.NodeType is ExpressionType.And or ExpressionType.Or))
        {
            bool isAnd = node.NodeType is ExpressionType.AndAlso or ExpressionType.And;
            string spacedOp = isAnd ? " AND " : " OR ";

            SQLiteExpression coalescedLeft = CoalesceLiftedOrderComparison(leftNode, resolvedLeft.SQLiteExpression!);
            SQLiteExpression coalescedRight = CoalesceLiftedOrderComparison(rightNode, resolvedRight.SQLiteExpression!);

            SQLiteExpression boolLeft = isAnd ? BracketBooleanOr(leftNode, coalescedLeft) : coalescedLeft;
            SQLiteExpression boolRight = isAnd ? BracketBooleanOr(rightNode, coalescedRight) : coalescedRight;
            SQLiteParameter[]? boolParameters = ParameterHelpers.CombineParameters(boolLeft, boolRight);

            SQLiteExpression boolResult = SQLiteExpression.Binary(node.Type, Counters.NextIdentifier(), "", boolLeft, spacedOp, boolRight, "", boolParameters);

            boolResult.RequiresBrackets = !isAnd;
            return boolResult;
        }

        if (node.NodeType is ExpressionType.ExclusiveOr)
        {
            if (node.Type == typeof(bool))
            {
                SQLiteExpression xorLeft = CoalesceLiftedOrderComparison(leftNode, left);
                SQLiteExpression xorRight = CoalesceLiftedOrderComparison(rightNode, right);
                return SQLiteExpression.Binary(typeof(bool), Counters.NextIdentifier(), "", xorLeft, " <> ", xorRight, "", ParameterHelpers.CombineParameters(xorLeft, xorRight));
            }

            SQLiteExpression xorLeftMulti = CoalesceLiftedOrderComparison(leftNode, left);
            SQLiteExpression xorRightMulti = CoalesceLiftedOrderComparison(rightNode, right);
            return SQLiteExpression.Multi(node.Type, Counters.NextIdentifier(),
                ["((", " | ", ") - (", " & ", "))"],
                [xorLeftMulti, xorRightMulti, xorLeftMulti, xorRightMulti],
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

        if (node.NodeType is ExpressionType.Subtract
            && leftNode.Type == typeof(TimeOnly) && rightNode.Type == typeof(TimeOnly))
        {
            long day = TimeSpan.TicksPerDay;
            return SQLiteExpression.Multi(node.Type, Counters.NextIdentifier(),
                ["(((", " - ", $") % {day} + {day}) % {day})"],
                [left, right],
                bothParameters);
        }

        bool eitherOperandMayBeNull = MayBeNull(leftNode) || MayBeNull(rightNode);
        bool equalIsNullSafe = (IsNullableColumn(leftNode) && !resolvedLeft.IsConstant)
            || (IsNullableColumn(rightNode) && !resolvedRight.IsConstant);

        if (node.NodeType is ExpressionType.Add && node.Type == typeof(string))
        {
            SQLiteExpression concatLeft = CoalesceNullableStringOperand(leftNode, resolvedLeft, BracketConcatOperand(leftNode, resolvedLeft.SQLiteExpression!));
            SQLiteExpression concatRight = CoalesceNullableStringOperand(rightNode, resolvedRight, BracketConcatOperand(rightNode, resolvedRight.SQLiteExpression!));

            return SQLiteExpression.Binary(node.Type, Counters.NextIdentifier(), "", concatLeft, " || ", concatRight, "", ParameterHelpers.CombineParameters(concatLeft, concatRight));
        }

        if (TypeHelpers.UnsignedIntegerKey(leftNode.Type) == typeof(ulong) && TypeHelpers.UnsignedIntegerKey(rightNode.Type) == typeof(ulong))
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

        if (TypeHelpers.UnsignedIntegerKey(leftNode.Type) == typeof(uint) && TypeHelpers.UnsignedIntegerKey(rightNode.Type) == typeof(uint))
        {
            switch (node.NodeType)
            {
                case ExpressionType.Add or ExpressionType.AddChecked:
                    return BuildUnsignedWrap32(node.Type, " + ", left, right, bothParameters);
                case ExpressionType.Subtract or ExpressionType.SubtractChecked:
                    return BuildUnsignedWrap32(node.Type, " - ", left, right, bothParameters);
            }
        }

        if (equalityOp)
        {
            left = CoalesceLiftedOrderComparison(leftNode, left);
            right = CoalesceLiftedOrderComparison(rightNode, right);
        }

        if (node.NodeType is ExpressionType.LeftShift or ExpressionType.RightShift)
        {
            return BuildShift(node, nodeUnderlyingType, left, right, bothParameters);
        }

        (string sqlOp, bool parenthesis) = node.NodeType switch
        {
            ExpressionType.Equal => (equalIsNullSafe ? " IS " : " = ", false),
            ExpressionType.NotEqual => (eitherOperandMayBeNull ? " IS NOT " : " <> ", false),
            ExpressionType.GreaterThan => (" > ", false),
            ExpressionType.LessThan => (" < ", false),
            ExpressionType.GreaterThanOrEqual => (" >= ", false),
            ExpressionType.LessThanOrEqual => (" <= ", false),
            ExpressionType.Add or ExpressionType.AddChecked => (" + ", true),
            ExpressionType.Subtract or ExpressionType.SubtractChecked => (" - ", true),
            ExpressionType.Multiply or ExpressionType.MultiplyChecked => (" * ", true),
            ExpressionType.Divide => (" / ", true),
            ExpressionType.Modulo => (" % ", true),
            ExpressionType.And => (" & ", true),
            ExpressionType.Or => (" | ", true),
            _ => throw new NotSupportedException($"Unsupported binary op {node.NodeType}")
        };

        if (parenthesis)
        {
            return SQLiteExpression.Binary(node.Type, Counters.NextIdentifier(), "(", left, sqlOp, right, ")", bothParameters);
        }

        return SQLiteExpression.Binary(node.Type, Counters.NextIdentifier(), "", left, sqlOp, right, "", bothParameters);
    }

    private SQLiteExpression CoerceConstantTimeSpanToTicks(ResolvedModel resolved, SQLiteExpression current)
    {
        if (resolved.IsConstant && resolved.Constant is TimeSpan ts)
        {
            return SQLiteExpression.Leaf(typeof(long), Counters.NextIdentifier(), Counters.NextParamName(), ts.Ticks);
        }

        if (current.Parameters is { Length: 1 } && current.Parameters[0].Value is TimeSpan paramTs)
        {
            return SQLiteExpression.Leaf(typeof(long), Counters.NextIdentifier(), Counters.NextParamName(), paramTs.Ticks);
        }

        return current;
    }

    private SQLiteExpression BuildUnsignedWrap32(Type resultType, string sqlOp, SQLiteExpression a, SQLiteExpression b, SQLiteParameter[]? parameters)
    {
        return SQLiteExpression.Multi(
            resultType,
            Counters.NextIdentifier(),
            ["((", sqlOp, $") & {Constants.UInt32Mask})"],
            [a, b],
            parameters);
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

        string prefix = "(SELECT " + caseBody + $" FROM (SELECT aa, bb, ah, (ah % bb) AS rh, (aa & 1) AS a0 FROM (SELECT aa, bb, ((aa >> 1) & {Constants.Int64SignMask}) AS ah FROM (SELECT ";

        return SQLiteExpression.Multi(
            resultType,
            Counters.NextIdentifier(),
            [prefix, " AS aa, ", " AS bb))))"],
            [a, b],
            parameters);
    }

    private SQLiteExpression BuildShift(BinaryExpression node, Type shiftType, SQLiteExpression value, SQLiteExpression count, SQLiteParameter[]? parameters)
    {
        bool is64Bit = shiftType == typeof(long) || shiftType == typeof(ulong);
        string spacedOp = node.NodeType == ExpressionType.LeftShift ? " << (" : " >> (";
        string maskedCount = is64Bit ? $" & {Constants.Shift64CountMask}))" : $" & {Constants.Shift32CountMask}))";

        if (node.NodeType is ExpressionType.RightShift && shiftType == typeof(ulong))
        {
            return SQLiteExpression.Multi(node.Type, Counters.NextIdentifier(),
                ["(CASE WHEN (", $" & {Constants.Shift64CountMask}) = 0 THEN ", " ELSE ((", $" >> 1) & {Constants.Int64SignMask}) >> ((", $" & {Constants.Shift64CountMask}) - 1) END)"],
                [count, value, value, count],
                parameters);
        }

        if (node.NodeType is ExpressionType.RightShift || is64Bit || shiftType == typeof(uint))
        {
            string[] parts = node.NodeType == ExpressionType.LeftShift && shiftType == typeof(uint)
                ? ["((", spacedOp, $" & {Constants.Shift32CountMask})) & {Constants.UInt32Mask})"]
                : ["(", spacedOp, maskedCount];

            return SQLiteExpression.Multi(node.Type, Counters.NextIdentifier(), parts, [value, count], parameters);
        }

        return SQLiteExpression.Multi(node.Type, Counters.NextIdentifier(),
            ["(((((", spacedOp, $" & {Constants.Shift32CountMask})) & {Constants.UInt32Mask}) + {Constants.Int32SignBit}) % {Constants.UInt32Modulus}) - {Constants.Int32SignBit})"],
            [value, count],
            parameters);
    }

    public SQLiteExpression CoalesceNullableStringOperand(Expression operand, ResolvedModel resolved, SQLiteExpression expr)
    {
        bool mayBeNull = StringConcatOperandMayBeNull(operand, resolved);

        if ((Nullable.GetUnderlyingType(expr.Type) ?? expr.Type) == typeof(char)
            && Database.Options.CharStorage == CharStorageMode.Integer)
        {
            return mayBeNull
                ? SQLiteExpression.Binary(typeof(string), Counters.NextIdentifier(), "(CASE WHEN ", expr, " IS NULL THEN '' ELSE CHAR(", expr, ") END)", expr.Parameters)
                : SQLiteExpression.Wrap(typeof(string), Counters.NextIdentifier(), "CHAR(", expr, ")", expr.Parameters);
        }

        return mayBeNull
            ? SQLiteExpression.Wrap(typeof(string), Counters.NextIdentifier(), "COALESCE(", expr, ", '')", expr.Parameters)
            : expr;
    }

    private static bool TryGetInRangeCharText(Expression node, out string? text)
    {
        text = null;
        if (node.Type == typeof(int) && ExpressionHelpers.IsConstant(node)
            && ExpressionHelpers.GetConstantValue(node) is int value
            && value is >= 0 and <= char.MaxValue)
        {
            text = ((char)value).ToString();
            return true;
        }

        return false;
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

    private static SQLiteExpression BracketConcatOperand(Expression node, SQLiteExpression expr)
    {
        Expression stripped = ExpressionHelpers.StripUpcast(ExpressionHelpers.StripQuotes(node));
        if (TranslationPatterns.IsConcatBracketNodeType(stripped.NodeType))
        {
            return SQLiteExpression.Wrap(expr.Type, expr.Identifier, "(", expr, ")", expr.Parameters);
        }

        return expr;
    }

    private static SQLiteExpression BracketBinaryOperand(Expression node, SQLiteExpression expr)
    {
        Expression stripped = ExpressionHelpers.StripUpcast(ExpressionHelpers.StripQuotes(node));
        bool needsBrackets = expr.RequiresBrackets
            || stripped.NodeType is ExpressionType.Equal or ExpressionType.NotEqual
            || ((Nullable.GetUnderlyingType(stripped.Type) ?? stripped.Type) == typeof(bool)
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
