namespace SQLite.Framework.Attributes;

/// <summary>
/// Configures a user-registered FTS5 tokenizer by its name and optional argument list.
/// Use this when you have registered a custom tokenizer through SQLite's C API.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class CustomTokenizerAttribute : Attribute
{
    /// <summary>
    /// Initializes a new <see cref="CustomTokenizerAttribute" /> with the given tokenizer name and arguments.
    /// </summary>
    public CustomTokenizerAttribute(string name, params string[] arguments)
    {
        Name = name;
        Arguments = arguments;
    }

    /// <summary>
    /// The tokenizer name that was registered with SQLite.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Extra arguments passed after the tokenizer name in the <c>tokenize=</c> table option.
    /// </summary>
    public string[] Arguments { get; }
}
