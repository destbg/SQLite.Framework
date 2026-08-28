namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Translates `is` and `as` checks on polymorphic JSON columns into discriminator predicates.
/// </summary>
internal static class JsonTypeCheckTranslator
{
    public static SQLiteExpression? TryTranslateTypeIs(SQLVisitor visitor, Expression operand, Type testedType)
    {
        return TryTranslate(visitor, operand, testedType, positive: true);
    }

    public static SQLiteExpression? TryTranslateAsNullCheck(SQLVisitor visitor, BinaryExpression node)
    {
        UnaryExpression? typeAs = node.Left is UnaryExpression { NodeType: ExpressionType.TypeAs } left ? left : null;
        Expression nullSide = node.Right;
        if (typeAs == null)
        {
            typeAs = node.Right is UnaryExpression { NodeType: ExpressionType.TypeAs } right ? right : null;
            nullSide = node.Left;
        }

        if (typeAs == null || nullSide is not ConstantExpression { Value: null })
        {
            return null;
        }

        return TryTranslate(visitor, typeAs.Operand, typeAs.Type, positive: node.NodeType == ExpressionType.NotEqual);
    }

    private static SQLiteExpression? TryTranslate(SQLVisitor visitor, Expression operand, Type testedType, bool positive)
    {
        Type operandType = operand.Type;
        SQLiteOptions options = visitor.Database.Options;
        if (!options.HasJsonConverter(operandType))
        {
            return null;
        }

        JsonPolymorphismOptions? polymorphism = options.ResolveJsonTypeInfo(operandType)!.PolymorphismOptions;
        if (polymorphism == null)
        {
            return null;
        }

        if (polymorphism.UnknownDerivedTypeHandling != JsonUnknownDerivedTypeHandling.FailSerialization
            || polymorphism.IgnoreUnrecognizedTypeDiscriminators)
        {
            throw new NotSupportedException(
                $"An 'is' or 'as' check on JSON type '{operandType.Name}' is not supported. " +
                "Its polymorphic setup allows unknown type discriminators, so the stored value cannot be trusted. " +
                "Keep UnknownDerivedTypeHandling at FailSerialization and IgnoreUnrecognizedTypeDiscriminators off.");
        }

        SQLiteExpression column = (SQLiteExpression)visitor.Visit(operand);

        bool isContractBase = testedType == operandType;
        List<object> values = [];
        foreach (JsonDerivedType derived in polymorphism.DerivedTypes)
        {
            if (!isContractBase && !testedType.IsAssignableFrom(derived.DerivedType))
            {
                continue;
            }

            if (derived.TypeDiscriminator is not { } discriminator)
            {
                if (isContractBase)
                {
                    continue;
                }

                throw new NotSupportedException(
                    $"An 'is' or 'as' check on JSON type '{operandType.Name}' is not supported. " +
                    $"'{derived.DerivedType.Name}' has no type discriminator, so its rows are stored like '{operandType.Name}' rows.");
            }

            values.Add(discriminator);
        }

        SQLiteExpression extract = visitor.InternJsonExtract(column, polymorphism.TypeDiscriminatorPropertyName, typeof(object));
        return BuildPredicate(visitor, column, extract, isContractBase, values, options, positive);
    }

    private static SQLiteExpression BuildPredicate(SQLVisitor visitor, SQLiteExpression column, SQLiteExpression extract, bool isContractBase, List<object> values, SQLiteOptions options, bool positive)
    {
        int id = visitor.Counters.NextIdentifier();
        if (!isContractBase && values.Count == 0)
        {
            return SQLiteExpression.Leaf(typeof(bool), id, positive ? "0" : "1");
        }

        string membership = values.Count == 1
            ? (positive ? " = " : " <> ") + SqlLiteralHelper.FormatLiteral(values[0], options)
            : (positive ? " IN (" : " NOT IN (") + string.Join(", ", values.Select(v => SqlLiteralHelper.FormatLiteral(v, options))) + ")";

        if (isContractBase)
        {
            SQLiteExpression columnCheck = SQLiteExpression.Wrap(typeof(bool), visitor.Counters.NextIdentifier(), "", column,
                positive ? " IS NOT NULL" : " IS NULL", column.Parameters);
            SQLiteExpression extractNullCheck = SQLiteExpression.Wrap(typeof(bool), visitor.Counters.NextIdentifier(), "", extract,
                positive ? " IS NULL" : " IS NOT NULL", extract.Parameters);
            if (values.Count == 0)
            {
                return SQLiteExpression.Binary(typeof(bool), id, "(", columnCheck, positive ? " AND " : " OR ", extractNullCheck, ")", column.Parameters);
            }

            SQLiteExpression extractMembership = SQLiteExpression.Wrap(typeof(bool), visitor.Counters.NextIdentifier(), "", extract, membership, extract.Parameters);
            SQLiteExpression nullArm = SQLiteExpression.Binary(typeof(bool), visitor.Counters.NextIdentifier(), "(", extractNullCheck, positive ? " OR " : " AND ", extractMembership, ")", extract.Parameters);
            return SQLiteExpression.Binary(typeof(bool), id, "(", columnCheck, positive ? " AND " : " OR ", nullArm, ")", column.Parameters);
        }

        SQLiteExpression nullCheck = SQLiteExpression.Wrap(typeof(bool), visitor.Counters.NextIdentifier(), "", extract,
            positive ? " IS NOT NULL" : " IS NULL", extract.Parameters);
        SQLiteExpression membershipCheck = SQLiteExpression.Wrap(typeof(bool), visitor.Counters.NextIdentifier(), "", extract, membership, extract.Parameters);
        return SQLiteExpression.Binary(typeof(bool), id, "(", nullCheck, positive ? " AND " : " OR ", membershipCheck, ")", extract.Parameters);
    }
}
