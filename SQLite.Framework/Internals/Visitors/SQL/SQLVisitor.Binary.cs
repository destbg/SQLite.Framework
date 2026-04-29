namespace SQLite.Framework.Internals.Visitors;

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

        if (node.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse)
        {
            string op = node.NodeType == ExpressionType.AndAlso ? "AND" : "OR";
            return new SQLiteExpression(typeof(bool), Counters.IdentifierIndex++, $"{left.Sql} {op} {right.Sql}", bothParameters);
        }

        if (node.NodeType is ExpressionType.Coalesce)
        {
            return new SQLiteExpression(node.Type, Counters.IdentifierIndex++, $"COALESCE({left.Sql}, {right.Sql})", bothParameters);
        }

        string sqlOp = null!;

        bool equalityOp = node.NodeType is ExpressionType.Equal or ExpressionType.NotEqual;
        bool isLeftNull = resolvedLeft is { IsConstant: true, Constant: null };
        bool isRightNull = resolvedRight is { IsConstant: true, Constant: null };

        if (equalityOp && (isLeftNull || isRightNull))
        {
            if (node.NodeType == ExpressionType.Equal)
            {
                sqlOp = "IS";
            }
            else if (node.NodeType == ExpressionType.NotEqual)
            {
                sqlOp = "IS NOT";
            }

            if (isLeftNull)
            {
                left = right;
            }

            return new SQLiteExpression(typeof(bool), Counters.IdentifierIndex++, $"{left.Sql} {sqlOp} NULL", left.Parameters);
        }

        (sqlOp, bool parenthesis) = node.NodeType switch
        {
            ExpressionType.Equal => ("=", false),
            ExpressionType.NotEqual => ("<>", false),
            ExpressionType.GreaterThan => (">", false),
            ExpressionType.LessThan => ("<", false),
            ExpressionType.GreaterThanOrEqual => (">=", false),
            ExpressionType.LessThanOrEqual => ("<=", false),
            ExpressionType.Add => (node.Type == typeof(string) ? "||" : "+", node.Type != typeof(string)),
            ExpressionType.Subtract => ("-", true),
            ExpressionType.Multiply => ("*", true),
            ExpressionType.Divide => ("/", true),
            ExpressionType.Modulo => ("%", true),
            _ => throw new NotSupportedException($"Unsupported binary op {node.NodeType}")
        };

        if (parenthesis)
        {
            return new SQLiteExpression(node.Type, Counters.IdentifierIndex++, $"({left.Sql} {sqlOp} {right.Sql})", bothParameters);
        }

        return new SQLiteExpression(node.Type, Counters.IdentifierIndex++, $"{left.Sql} {sqlOp} {right.Sql}", bothParameters);
    }
}
