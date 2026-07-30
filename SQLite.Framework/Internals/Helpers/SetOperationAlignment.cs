namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Guards a set operation against branches that project different member sets. SQLite matches the
/// branches of a compound SELECT by position, so branches that bind different members would silently
/// put a value into the wrong member. Branches whose select list has no member names, such as scalar
/// projections, are skipped.
/// </summary>
internal static class SetOperationAlignment
{
    public static void ThrowIfBranchMembersMisaligned(bool isInnerQuery, IReadOnlyList<string> selectIdentifiers, IReadOnlyList<IReadOnlyList<string>> operandSelects)
    {
        if (isInnerQuery)
        {
            return;
        }

        if (operandSelects.Count == 0)
        {
            return;
        }

        foreach (IReadOnlyList<string> operand in operandSelects)
        {
            if (selectIdentifiers.Count < 2 && operand.Count < 2)
            {
                continue;
            }

            if (!AllNamedIdentifiers(selectIdentifiers) || !AllNamedIdentifiers(operand))
            {
                continue;
            }

            bool aligned = operand.Count == selectIdentifiers.Count
                && !operand.Where((identifier, i) =>
                    !string.Equals(identifier, selectIdentifiers[i], StringComparison.OrdinalIgnoreCase)).Any();
            if (!aligned)
            {
                throw new NotSupportedException(
                    "A set operation whose branches project different members is not supported, because a " +
                    "compound SELECT matches branch values by position. Project the same members in the same order in every branch.");
            }
        }
    }

    private static bool AllNamedIdentifiers(IReadOnlyList<string> identifiers)
    {
        return identifiers.Count > 0 && identifiers.All(identifier => !char.IsAsciiDigit(identifier[0]));
    }
}
