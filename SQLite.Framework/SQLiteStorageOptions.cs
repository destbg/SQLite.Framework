using System.Reflection;
using SQLite.Framework.Enums;

namespace SQLite.Framework;

/// <summary>
/// Controls how specific .NET types are stored in and read from the database.
/// Using the default values allows for higher control over what is translated into SQLite.
/// </summary>
public class SQLiteStorageOptions
{
    /// <summary>
    /// Controls how DateTime values are stored. Defaults to <see cref="DateTimeStorageMode.Integer" />.
    /// </summary>
    public DateTimeStorageMode DateTimeStorage { get; set; } = DateTimeStorageMode.Integer;

    /// <summary>
    /// The format string used when <see cref="DateTimeStorage" /> is set to <see cref="DateTimeStorageMode.TextFormatted" />.
    /// Defaults to "yyyy-MM-dd HH:mm:ss".
    /// </summary>
    public string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";

    /// <summary>
    /// Controls how DateTimeOffset values are stored. Defaults to <see cref="DateTimeOffsetStorageMode.Ticks" />.
    /// </summary>
    public DateTimeOffsetStorageMode DateTimeOffsetStorage { get; set; } = DateTimeOffsetStorageMode.Ticks;

    /// <summary>
    /// The format string used when <see cref="DateTimeOffsetStorage" /> is set to <see cref="DateTimeOffsetStorageMode.TextFormatted" />.
    /// Defaults to "yyyy-MM-dd HH:mm:ss zzz".
    /// </summary>
    public string DateTimeOffsetFormat { get; set; } = "yyyy-MM-dd HH:mm:ss zzz";

    /// <summary>
    /// Controls how TimeSpan values are stored. Defaults to <see cref="TimeSpanStorageMode.Integer" />.
    /// </summary>
    public TimeSpanStorageMode TimeSpanStorage { get; set; } = TimeSpanStorageMode.Integer;

    /// <summary>
    /// The format string used when <see cref="TimeSpanStorage" /> is set to <see cref="TimeSpanStorageMode.Text" />.
    /// Defaults to "c" (constant/invariant format, e.g. "2.03:04:05.0060070").
    /// </summary>
    public string TimeSpanFormat { get; set; } = "c";

    /// <summary>
    /// Controls how DateOnly values are stored. Defaults to <see cref="DateOnlyStorageMode.Integer" />.
    /// </summary>
    public DateOnlyStorageMode DateOnlyStorage { get; set; } = DateOnlyStorageMode.Integer;

    /// <summary>
    /// The format string used when <see cref="DateOnlyStorage" /> is set to <see cref="DateOnlyStorageMode.Text" />.
    /// Defaults to "yyyy-MM-dd".
    /// </summary>
    public string DateOnlyFormat { get; set; } = "yyyy-MM-dd";

    /// <summary>
    /// Controls how TimeOnly values are stored. Defaults to <see cref="TimeOnlyStorageMode.Integer" />.
    /// </summary>
    public TimeOnlyStorageMode TimeOnlyStorage { get; set; } = TimeOnlyStorageMode.Integer;

    /// <summary>
    /// The format string used when <see cref="TimeOnlyStorage" /> is set to <see cref="TimeOnlyStorageMode.Text" />.
    /// Defaults to "HH:mm:ss".
    /// </summary>
    public string TimeOnlyFormat { get; set; } = "HH:mm:ss";

    /// <summary>
    /// Controls how decimal values are stored. Defaults to <see cref="DecimalStorageMode.Real" />.
    /// </summary>
    public DecimalStorageMode DecimalStorage { get; set; } = DecimalStorageMode.Real;

    /// <summary>
    /// The format string used when <see cref="DecimalStorage" /> is set to <see cref="DecimalStorageMode.Text" />.
    /// Defaults to "G" (general format).
    /// </summary>
    public string DecimalFormat { get; set; } = "G";

    /// <summary>
    /// Controls how enum values are stored. Defaults to <see cref="EnumStorageMode.Integer" />.
    /// </summary>
    public EnumStorageMode EnumStorage { get; set; } = EnumStorageMode.Integer;

    /// <summary>
    /// Custom type converters that define how specific .NET types are stored in and read from SQLite.
    /// The key is the .NET type; the value is the converter implementation.
    /// </summary>
    public Dictionary<Type, ISQLiteTypeConverter> TypeConverters { get; } = [];

    /// <summary>
    /// Custom method translators that convert specific .NET method calls into SQL fragments.
    /// The key is the <see cref="MethodInfo" /> of the method to translate; the value is the translator delegate.
    /// </summary>
    public Dictionary<MethodInfo, SQLiteMethodTranslator> MethodTranslators { get; } = [];
}
