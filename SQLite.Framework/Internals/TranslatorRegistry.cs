namespace SQLite.Framework.Internals;

internal static class TranslatorRegistry
{
    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "Open generic type methods are looked up by name for custom translator registration.")]
    public static bool TryGetMethodTranslator(this SQLiteOptions options, MethodInfo method, [NotNullWhen(true)] out SQLiteMemberTranslator? translator)
    {
        if (options.MemberTranslators.TryGetValue(method, out translator))
        {
            return true;
        }

        if (method.IsGenericMethod &&
            options.MemberTranslators.TryGetValue(method.GetGenericMethodDefinition(), out translator))
        {
            return true;
        }

        if (method.DeclaringType!.IsConstructedGenericType)
        {
            Type openType = method.DeclaringType.GetGenericTypeDefinition();
            foreach (MethodInfo openMethod in openType.GetMethods())
            {
                if (openMethod.Name == method.Name
                    && openMethod.GetParameters().Length == method.GetParameters().Length
                    && options.MemberTranslators.TryGetValue(openMethod, out translator))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static string? TranslateProperty(this SQLiteOptions options, string memberName, string instanceSql)
    {
        foreach (SQLitePropertyTranslator translator in options.PropertyTranslators)
        {
            string? sql = translator(memberName, instanceSql);
            if (sql != null)
            {
                return sql;
            }
        }

        return null;
    }
}
