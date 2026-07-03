# Platform Support

Every package exposes the same API and assembly name, so your code does not change between platforms. What changes is which native SQLite gets loaded and a few practical notes per platform. For picking a version floor, see [Choosing a SQLite Version](Choosing%20a%20SQLite%20Version).

## Which SQLite loads where

With the default `SQLite.Framework` package the provider is picked once, when the first database is created:

| Platform | SQLite used |
|---|---|
| Windows | The system `winsqlite3.dll` |
| macOS and Linux | The system `libsqlite3` |
| iOS and Mac Catalyst | The system `libsqlite3`, tied to the OS version |
| Android | Bundled, the package ships its own SQLite |

`SQLite.Framework.Bundled` and `SQLite.Framework.Cipher` ship their own SQLite on every platform. `SQLite.Framework.Base` loads whatever SQLitePCLRaw provider you initialize yourself with `SQLitePCL.Batteries_V2.Init()` before creating a database.

## Windows

The system `winsqlite3.dll` works, but Microsoft treats it as undocumented and internal, so its version is not something you can plan around. Prefer `SQLite.Framework.Bundled` for Windows desktop apps. If you stay on the system SQLite, declare a version floor so a too-old DLL fails fast at open instead of failing later on a query.

## macOS and Linux

Both document the SQLite they ship, so the default package works well. On Linux servers the system SQLite has also measured faster than the bundled one, see [Choosing a SQLite Version](Choosing%20a%20SQLite%20Version).

One Linux packaging note. The .NET library loader looks for the unversioned `libsqlite3.so` name, but on Debian and Ubuntu the runtime package (`libsqlite3-0`) only ships the versioned `libsqlite3.so.0`, so loading fails even though SQLite is installed. The unversioned name comes with the dev package. Install `libsqlite3-dev` on Debian and Ubuntu, `sqlite-devel` on Fedora or `sqlite-dev` on Alpine. Minimal container images may not include SQLite at all, so add the package to the image or switch to `SQLite.Framework.Bundled`, which carries its own native binary.

## iOS and Mac Catalyst

iOS apps use the system SQLite and its version is tied to the iOS version. The framework encodes this in platform attributes. APIs that need a newer SQLite than early iOS versions provide are annotated with `[SupportedOSPlatform("ios...")]`, so the platform-compatibility analyzer warns at build time when your declared minimum iOS is too low for a call. For example, window functions are annotated as needing iOS 13 and the migration runner as needing iOS 15. The warnings mirror the SQLite floor table on the [version page](Choosing%20a%20SQLite%20Version).

At runtime the same rule is enforced by `UseMinimumSqliteVersion`, which checks the loaded SQLite when the connection opens.

## Android

The framework always bundles its own recent SQLite on Android, because apps cannot bind to the C API of the Android system SQLite and the Java route would be far slower. Android therefore never limits your version floor. The minimum supported Android version is API level 21 (Android 5).

## .NET MAUI

Use `FileSystem.AppDataDirectory` for the database path and register the database through the [Dependency Injection](Dependency%20Injection) package, as shown in [Getting Started](Getting%20Started).

```csharp
public static class Constants
{
    public const string DatabaseFilename = "AppSQLite.db3";

    public static string DatabasePath =>
        Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename);
}
```

The MAUI sample in the repository targets iOS 15.0 and Android API 21 and enables `UseWalMode`, `UseGeneratedMaterializers` and `DisableReflectionFallback`, a good starting point for mobile apps. See [Samples](Samples).

## Avalonia

Avalonia has no unified app-data API, so resolve a writable folder per platform yourself. The Avalonia sample uses this pattern:

```csharp
public static string DatabasePath
{
    get
    {
        string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrEmpty(folder))
        {
            folder = AppContext.BaseDirectory;
        }

        string appFolder = Path.Combine(folder, "SQLite.Framework.Avalonia");
        Directory.CreateDirectory(appFolder);
        return Path.Combine(appFolder, Constants.DatabaseFilename);
    }
}
```

The sample also publishes with Native AOT on desktop, which works with the [Source Generator](Source%20Generator).

## Blazor WebAssembly

SQLite runs in the browser through `SQLite.Framework.Bundled`, whose native bundle includes a `browser-wasm` build. The Blazor sample in the repository shows the full setup. Three things to know:

- Install the wasm tooling once with `dotnet workload install wasm-tools`.
- The browser file system is in-memory, so the database is lost on reload unless you persist it yourself. The sample snapshots the file to `localStorage`. OPFS or IndexedDB are the options for real apps.
- Blazor WebAssembly is single threaded. Use the `*Async` methods everywhere and never block on a lock or `Thread.Sleep`, which would deadlock the page.

## Servers

ASP.NET works with the default package, see the minimal API sample in [Samples](Samples). Turn on WAL mode for concurrent readers alongside a writer, see [Multi-threading](Multi-threading).

## Encryption

`SQLite.Framework.Cipher` swaps the native library for SQLCipher. Set the key on the options builder and the framework applies it with `PRAGMA key` when the connection opens:

```csharp
SQLiteOptions options = new SQLiteOptionsBuilder("secure.db")
    .UseEncryptionKey(key)
    .Build();
```

`UseEncryptionKey` only has an effect on the Cipher package. One trade-off to know about is that SQLCipher's bundled SQLite does not include JSONB, so the JSONB APIs are unavailable there, see [JSON and JSONB](JSON%20and%20JSONB).
