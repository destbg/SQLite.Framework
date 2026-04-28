using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Text;

namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Reads <see cref="FullTextSearchAttribute" /> and the related tokenizer/column attributes off a
/// CLR type and produces an <see cref="FtsTableInfo" />.
/// </summary>
internal static class FtsMappingReader
{
    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Caller passes a public-property-preserving type.")]
    public static FtsTableInfo? TryRead([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        FullTextSearchAttribute? fts = type.GetCustomAttribute<FullTextSearchAttribute>();
        if (fts == null)
        {
            return null;
        }

        if (fts.ContentMode == FtsContentMode.External && fts.ContentTable == null)
        {
            throw new InvalidOperationException($"FTS5 entity '{type.Name}' uses ContentMode.External but does not set ContentTable.");
        }

        PropertyInfo[] properties = type.GetProperties();

        List<FtsIndexedColumn> indexed = [];
        FtsRowIdInfo? rowId = null;

        foreach (PropertyInfo property in properties)
        {
            if (property.GetCustomAttribute<NotMappedAttribute>() != null)
            {
                continue;
            }

            FullTextRowIdAttribute? rowIdAttr = property.GetCustomAttribute<FullTextRowIdAttribute>();
            if (rowIdAttr != null)
            {
                if (rowId != null)
                {
                    throw new InvalidOperationException($"FTS5 entity '{type.Name}' has more than one [FullTextRowId] property.");
                }

                Type rowIdType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                if (rowIdType != typeof(int) && rowIdType != typeof(long))
                {
                    throw new InvalidOperationException($"FTS5 entity '{type.Name}' has a [FullTextRowId] on '{property.Name}' which is not int or long.");
                }

                rowId = new FtsRowIdInfo(property);
                continue;
            }

            FullTextIndexedAttribute? indexedAttr = property.GetCustomAttribute<FullTextIndexedAttribute>();
            if (indexedAttr != null)
            {
                ColumnAttribute? columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
                indexed.Add(new FtsIndexedColumn(property, columnAttribute?.Name ?? property.Name, indexedAttr.Weight, indexedAttr.Unindexed));
            }
        }

        if (indexed.Count == 0)
        {
            throw new InvalidOperationException($"FTS5 entity '{type.Name}' must have at least one property marked [FullTextIndexed].");
        }

        string tokenize = BuildTokenizerClause(type);

        return new FtsTableInfo(fts, indexed, rowId, tokenize);
    }

    private static string BuildTokenizerClause(Type type)
    {
        Unicode61TokenizerAttribute? unicode = type.GetCustomAttribute<Unicode61TokenizerAttribute>();
        PorterTokenizerAttribute? porter = type.GetCustomAttribute<PorterTokenizerAttribute>();
#if SQLITECIPHER
        object? trigram = null;
#else
        TrigramTokenizerAttribute? trigram = type.GetCustomAttribute<TrigramTokenizerAttribute>();
#endif
        AsciiTokenizerAttribute? ascii = type.GetCustomAttribute<AsciiTokenizerAttribute>();
        CustomTokenizerAttribute? custom = type.GetCustomAttribute<CustomTokenizerAttribute>();

        int count = (unicode != null ? 1 : 0) + (porter != null ? 1 : 0) + (trigram != null ? 1 : 0) + (ascii != null ? 1 : 0) + (custom != null ? 1 : 0);

        if (count > 1)
        {
            throw new InvalidOperationException($"FTS5 entity '{type.Name}' has more than one tokenizer attribute.");
        }

        if (custom != null)
        {
            return Render(custom.Name, custom.Arguments);
        }

#if !SQLITECIPHER
        if (trigram != null)
        {
            return Render("trigram", "case_sensitive", trigram.CaseSensitive ? "1" : "0", "remove_diacritics", trigram.RemoveDiacritics ? "1" : "0");
        }
#endif

        if (ascii != null)
        {
            List<string> args = [];
            if (ascii.Separators is { Length: > 0 } sep)
            {
                args.Add("separators");
                args.Add(sep);
            }
            if (ascii.TokenChars is { Length: > 0 } tok)
            {
                args.Add("tokenchars");
                args.Add(tok);
            }
            return Render("ascii", [.. args]);
        }

        if (porter != null)
        {
            string baseName = porter.Base == PorterBaseTokenizer.Ascii ? "ascii" : "unicode61";
            List<string> args = [baseName];
            if (porter.Base == PorterBaseTokenizer.Unicode61)
            {
                args.Add("remove_diacritics");
                args.Add(((int)porter.RemoveDiacritics).ToString(CultureInfo.InvariantCulture));
                if (porter.Categories is { Length: > 0 } cats)
                {
                    args.Add("categories");
                    args.Add(cats);
                }
            }
            if (porter.Separators is { Length: > 0 } psep)
            {
                args.Add("separators");
                args.Add(psep);
            }
            if (porter.TokenChars is { Length: > 0 } ptok)
            {
                args.Add("tokenchars");
                args.Add(ptok);
            }
            return Render("porter", [.. args]);
        }

        Unicode61TokenizerAttribute u = unicode ?? new Unicode61TokenizerAttribute();
        List<string> uargs = ["remove_diacritics", ((int)u.RemoveDiacritics).ToString(CultureInfo.InvariantCulture)];
        if (u.Categories is { Length: > 0 } ucats)
        {
            uargs.Add("categories");
            uargs.Add(ucats);
        }
        if (u.Separators is { Length: > 0 } usep)
        {
            uargs.Add("separators");
            uargs.Add(usep);
        }
        if (u.TokenChars is { Length: > 0 } utok)
        {
            uargs.Add("tokenchars");
            uargs.Add(utok);
        }
        return Render("unicode61", [.. uargs]);
    }

    private static string Render(string name, params string[] arguments)
    {
        StringBuilder sb = new();
        sb.Append(name);
        foreach (string arg in arguments)
        {
            sb.Append(' ');
            if (NeedsQuoting(arg))
            {
                sb.Append('\'');
                sb.Append(arg.Replace("'", "''"));
                sb.Append('\'');
            }
            else
            {
                sb.Append(arg);
            }
        }

        return sb.ToString();
    }

    private static bool NeedsQuoting(string arg)
    {
        if (string.IsNullOrEmpty(arg))
        {
            return true;
        }

        foreach (char ch in arg)
        {
            if (!char.IsLetterOrDigit(ch) && ch != '_')
            {
                return true;
            }
        }

        return false;
    }
}
