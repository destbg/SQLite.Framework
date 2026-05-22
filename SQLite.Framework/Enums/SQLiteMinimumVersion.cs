namespace SQLite.Framework.Enums;

/// <summary>
/// Named SQLite version buckets. Pass one to
/// <see cref="SQLiteOptionsBuilder.UseMinimumSqliteVersion" /> to declare what SQLite version
/// the application is willing to commit to. The framework verifies the loaded SQLite meets the
/// minimum at <see cref="SQLiteDatabase" /> open time, and rejects calls to methods that need
/// a newer version than the configured floor. Each entry lists the SQLite version the floor
/// maps to, the lowest iOS and Android versions whose system SQLite satisfies that floor, and
/// the notable SQL features that become available at that floor.
/// </summary>
public enum SQLiteMinimumVersion
{
    /// <summary>
    /// Default. No minimum is enforced. Calls that need a newer SQLite version fall through to
    /// the engine, which may throw <c>no such function</c> or similar errors.
    /// </summary>
    Unspecified = 0,

#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    /// <summary>
    /// SQLite 3.8.0 (2013-08-26). Lowest floor the framework supports. Supported on iOS 8 and
    /// later (iOS 8 ships SQLite 3.8.5) and Android 5 (Lollipop, API 21) and later.
    /// Adds WITHOUT ROWID tables and partial indexes.
    /// </summary>
    V3_8 = 3008000,

    /// <summary>
    /// SQLite 3.8.1 (2013-10-17). Bug fixes only.
    /// </summary>
    V3_8_1 = 3008001,

    /// <summary>
    /// SQLite 3.8.2 (2013-12-06). Stabilizes the WITHOUT ROWID storage option introduced in 3.8.0.
    /// </summary>
    V3_8_2 = 3008002,

    /// <summary>
    /// SQLite 3.8.3 (2014-02-03). Adds the common table expression (CTE) <c>WITH</c> clause and
    /// the <c>printf()</c> SQL function.
    /// </summary>
    V3_8_3 = 3008003,

    /// <summary>
    /// SQLite 3.8.4 (2014-03-10). Bug fixes only.
    /// </summary>
    V3_8_4 = 3008004,

    /// <summary>
    /// SQLite 3.8.5 (2014-06-04). Adds the R-Tree virtual table. Shipped with iOS 8.
    /// </summary>
    V3_8_5 = 3008005,

    /// <summary>
    /// SQLite 3.8.6 (2014-08-15). Query planner enhancements.
    /// </summary>
    V3_8_6 = 3008006,

    /// <summary>
    /// SQLite 3.8.7 (2014-10-17). Performance improvements.
    /// </summary>
    V3_8_7 = 3008007,

    /// <summary>
    /// SQLite 3.8.8 (2015-01-16). Adds <c>PRAGMA data_version</c> and the <c>SQLITE_ENABLE_API_ARMOR</c> option.
    /// </summary>
    V3_8_8 = 3008008,

    /// <summary>
    /// SQLite 3.8.9 (2015-04-08). Adds the <c>FTS5</c> module as an experimental extension and
    /// the <c>eval()</c> function.
    /// </summary>
    V3_8_9 = 3008009,

    /// <summary>
    /// SQLite 3.8.10 (2015-05-07). Adds the <c>sqldiff.exe</c> utility and JSON1 extension as a
    /// loadable extension.
    /// </summary>
    V3_8_10 = 3008010,

    /// <summary>
    /// SQLite 3.8.11 (2015-07-27). Adds row-value comparisons.
    /// </summary>
    V3_8_11 = 3008011,

    /// <summary>
    /// SQLite 3.9.0 (2015-10-14). Adds expression indexes, FTS5 as a stable extension, the
    /// JSON1 extension built in by default, and the <c>json_each</c> and <c>json_tree</c>
    /// table-valued functions. Supported on iOS 10 and later (iOS 9 still ships SQLite 3.8.x)
    /// and Android 7 (Nougat, API 24) and later.
    /// </summary>
    V3_9 = 3009000,

    /// <summary>
    /// SQLite 3.10.0 (2016-01-06). LIKE operator handles unicode characters, ICU enhancements.
    /// </summary>
    V3_10 = 3010000,

    /// <summary>
    /// SQLite 3.11.0 (2016-02-15). FTS5 stable and recommended, JSON1 improvements.
    /// </summary>
    V3_11 = 3011000,

    /// <summary>
    /// SQLite 3.12.0 (2016-03-29). Default page size raised to 4096 bytes.
    /// </summary>
    V3_12 = 3012000,

    /// <summary>
    /// SQLite 3.13.0 (2016-05-18). Query planner improvements.
    /// </summary>
    V3_13 = 3013000,

    /// <summary>
    /// SQLite 3.14.0 (2016-08-08). Adds <c>WITHOUT ROWID</c> support for <c>VACUUM</c> and
    /// 64-bit BIGINT type affinity. Supported on iOS 10 (which ships SQLite 3.14.0) and
    /// Android 8 (Oreo, API 26) and later.
    /// </summary>
    V3_14 = 3014000,

    /// <summary>
    /// SQLite 3.15.0 (2016-10-14). Adds row-value expressions (<c>(a, b) IN ((1, 2), (3, 4))</c>)
    /// and the <c>JSON_GROUP_ARRAY</c> / <c>JSON_GROUP_OBJECT</c> aggregates.
    /// </summary>
    V3_15 = 3015000,

    /// <summary>
    /// SQLite 3.16.0 (2017-01-02). Adds the table-valued pragma functions like
    /// <c>pragma_table_info()</c>, <c>pragma_index_list()</c>, <c>pragma_foreign_key_list()</c>.
    /// </summary>
    V3_16 = 3016000,

    /// <summary>
    /// SQLite 3.17.0 (2017-02-13). Performance enhancements.
    /// </summary>
    V3_17 = 3017000,

    /// <summary>
    /// SQLite 3.18.0 (2017-04-10). Adds the <c>PRAGMA optimize</c> command. Supported on iOS 11
    /// (which ships SQLite 3.19.x) and Android 8 (Oreo, API 26) and later.
    /// </summary>
    V3_18 = 3018000,

    /// <summary>
    /// SQLite 3.19.0 (2017-05-22). Adds <c>PRAGMA shrink_memory</c> and minor optimizations.
    /// Supported on iOS 11 (which ships SQLite 3.19.3) and Android 9 (Pie, API 28) and later
    /// (Android 8 still ships SQLite 3.18.x).
    /// </summary>
    V3_19 = 3019000,

    /// <summary>
    /// SQLite 3.20.0 (2017-08-01). Adds pointer-passing functions for application extensions.
    /// </summary>
    V3_20 = 3020000,

    /// <summary>
    /// SQLite 3.21.0 (2017-10-24). Adds the <c>SQLITE_DBCONFIG_RESET_DATABASE</c> option.
    /// </summary>
    V3_21 = 3021000,

    /// <summary>
    /// SQLite 3.22.0 (2018-01-22). Adds <c>INDEXED BY</c> for <c>UPDATE</c> and <c>DELETE</c>
    /// and <c>pragma_function_list()</c>. Supported on iOS 12 (which ships SQLite 3.24.0) and
    /// Android 9 (Pie, API 28) and later.
    /// </summary>
    V3_22 = 3022000,

    /// <summary>
    /// SQLite 3.23.0 (2018-04-02). Adds Boolean literals <c>TRUE</c> and <c>FALSE</c>.
    /// </summary>
    V3_23 = 3023000,

    /// <summary>
    /// SQLite 3.24.0 (2018-06-04). Adds <c>UPSERT</c> (<c>ON CONFLICT DO UPDATE</c>), window
    /// functions, R-Tree auxiliary columns, and <c>PRAGMA reverse_unordered_selects</c>.
    /// Supported on iOS 12 (which ships SQLite 3.24.0) and Android 11 (API 30) and later
    /// (Android 9 and 10 still ship SQLite 3.22.x).
    /// </summary>
    V3_24 = 3024000,

    /// <summary>
    /// SQLite 3.25.0 (2018-09-15). Adds <c>ALTER TABLE RENAME COLUMN</c> and window function
    /// frame specifications (<c>RANGE BETWEEN</c>).
    /// </summary>
    V3_25 = 3025000,

    /// <summary>
    /// SQLite 3.26.0 (2018-12-01). Adds <c>SQLITE_DBCONFIG_DEFENSIVE</c>.
    /// </summary>
    V3_26 = 3026000,

    /// <summary>
    /// SQLite 3.27.0 (2019-02-07). Adds <c>VACUUM INTO</c>. Supported on iOS 13 (which ships
    /// SQLite 3.28.0) and Android 11 (API 30) and later.
    /// </summary>
    V3_27 = 3027000,

    /// <summary>
    /// SQLite 3.28.0 (2019-04-16). Adds <c>FILTER</c> clause for aggregates and window function
    /// frame <c>EXCLUDE</c> options. Supported on iOS 13 (which ships SQLite 3.28.0) and
    /// Android 11 (API 30) and later.
    /// </summary>
    V3_28 = 3028000,

    /// <summary>
    /// SQLite 3.29.0 (2019-07-10). Adds <c>PRAGMA legacy_alter_table</c>.
    /// </summary>
    V3_29 = 3029000,

    /// <summary>
    /// SQLite 3.30.0 (2019-10-04). Adds <c>NULLS FIRST</c> / <c>NULLS LAST</c> in <c>ORDER BY</c>
    /// and allows the <c>RIGHT</c> and <c>FULL OUTER JOIN</c> keywords in the grammar (still
    /// rejected at compile time until 3.39).
    /// </summary>
    V3_30 = 3030000,

    /// <summary>
    /// SQLite 3.31.0 (2020-01-22). Adds generated/computed columns. Supported on iOS 14 (which
    /// ships SQLite 3.32.x) and Android 12 (API 31) and later.
    /// </summary>
    V3_31 = 3031000,

    /// <summary>
    /// SQLite 3.32.0 (2020-05-22). Adds the <c>IIF</c> function. Supported on iOS 14 (which
    /// ships SQLite 3.32.3) and Android 12 (API 31) and later.
    /// </summary>
    V3_32 = 3032000,

    /// <summary>
    /// SQLite 3.33.0 (2020-08-14). Adds <c>UPDATE FROM</c> (joined updates). Supported on iOS 15
    /// (which ships SQLite 3.36.0) and Android 14 (API 34) and later (Android 12 and 13 still
    /// ship SQLite 3.32.x).
    /// </summary>
    V3_33 = 3033000,

    /// <summary>
    /// SQLite 3.34.0 (2020-12-01). Adds <c>sqlite3_txn_state()</c> and improved error reporting.
    /// </summary>
    V3_34 = 3034000,

    /// <summary>
    /// SQLite 3.35.0 (2021-03-12). Adds built-in math functions (<c>SIN</c>, <c>COS</c>,
    /// <c>SQRT</c>, <c>LN</c>, etc.), the <c>RETURNING</c> clause, <c>ALTER TABLE DROP COLUMN</c>,
    /// and <c>MATERIALIZED</c> / <c>NOT MATERIALIZED</c> CTE hints. Supported on iOS 15 (which
    /// ships SQLite 3.36.0) and Android 14 (API 34) and later.
    /// </summary>
    V3_35 = 3035000,

    /// <summary>
    /// SQLite 3.36.0 (2021-06-18). Performance enhancements and bug fixes. Supported on iOS 15
    /// (which ships SQLite 3.36.0) and Android 14 (API 34) and later.
    /// </summary>
    V3_36 = 3036000,

    /// <summary>
    /// SQLite 3.37.0 (2021-11-27). Adds <c>STRICT</c> tables. Supported on iOS 16 (which ships
    /// SQLite 3.39.x) and Android 14 (API 34) and later.
    /// </summary>
    V3_37 = 3037000,

    /// <summary>
    /// SQLite 3.38.0 (2022-02-22). Adds the <c>->></c> and <c>-></c> JSON operators, the
    /// <c>format()</c> SQL function (alias of <c>printf</c>), the <c>unixepoch()</c> function,
    /// and makes the JSON1 extension built in by default. Supported on iOS 16 (which ships
    /// SQLite 3.39.x) and Android 14 (API 34) and later.
    /// </summary>
    V3_38 = 3038000,

    /// <summary>
    /// SQLite 3.39.0 (2022-06-25). Adds <c>RIGHT</c> and <c>FULL OUTER JOIN</c> support and
    /// <c>IS DISTINCT FROM</c>. Supported on iOS 16 (which ships SQLite 3.39.5)
    /// and Android 14 (API 34) and later.
    /// </summary>
    V3_39 = 3039000,

    /// <summary>
    /// SQLite 3.40.0 (2022-11-16). WAL improvements and BLOB I/O enhancements.
    /// </summary>
    V3_40 = 3040000,

    /// <summary>
    /// SQLite 3.41.0 (2023-02-21). Adds the <c>unhex()</c> SQL function. Supported on iOS 17
    /// (which ships SQLite 3.43.x) and Android 14 (API 34) and later.
    /// </summary>
    V3_41 = 3041000,

    /// <summary>
    /// SQLite 3.42.0 (2023-05-16). Adds the <c>JSONB</c> binary JSON format and
    /// <c>jsonb_*</c> functions.
    /// </summary>
    V3_42 = 3042000,

    /// <summary>
    /// SQLite 3.43.0 (2023-08-24). Adds the <c>timediff()</c> SQL function. Supported on iOS 17
    /// (which ships SQLite 3.43.2) and Android 15 (API 35) and later (Android 14 still ships SQLite 3.41.x).
    /// </summary>
    V3_43 = 3043000,

    /// <summary>
    /// SQLite 3.44.0 (2023-11-01). Adds <c>ORDER BY</c> in aggregates and <c>string_agg()</c>.
    /// </summary>
    V3_44 = 3044000,

    /// <summary>
    /// SQLite 3.45.0 (2024-01-15). Built-in JSONB and improved JSON handling.
    /// </summary>
    V3_45 = 3045000,

    /// <summary>
    /// SQLite 3.46.0 (2024-05-23). JSON path enhancements.
    /// </summary>
    V3_46 = 3046000,

    /// <summary>
    /// SQLite 3.47.0 (2024-10-21). Improved error reporting for JSON functions.
    /// </summary>
    V3_47 = 3047000,

    /// <summary>
    /// SQLite 3.48.0 (2025-01-14). Bug fixes and minor enhancements.
    /// </summary>
    V3_48 = 3048000,

    /// <summary>
    /// SQLite 3.49.0 (2025-02-06). Bug fixes and minor enhancements.
    /// </summary>
    V3_49 = 3049000,

    /// <summary>
    /// SQLite 3.50.0 (2025-05-29). Bug fixes and minor enhancements.
    /// </summary>
    V3_50 = 3050000,
#endif
}
