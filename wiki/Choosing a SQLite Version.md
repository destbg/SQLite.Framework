# Choosing a SQLite Version

On the default `SQLite.Framework` package the OS provides SQLite on desktop and iOS, so the version varies from device to device. `UseMinimumSqliteVersion` declares the lowest SQLite version your app commits to. The framework calls this the floor.

```csharp
SQLiteOptions options = new SQLiteOptionsBuilder("app.db")
    .UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_37)
    .Build();
```

## What the floor does

Two checks come from the floor:

- When the connection opens, the framework reads the version of the loaded SQLite and throws when it is below the floor. A device that is too old fails fast with one clear message instead of failing later on some query.
- A method that needs a newer SQLite than the floor throws up front and the message names the feature and the version it needs.

The default is `Unspecified`, which enforces nothing. Every call falls through to the engine, so on an old device a query fails at runtime with a raw error like `no such function`. Declare a floor whenever the SQLite version is not under your control.

The floor only exists on the packages where the version varies:

| Package | Who provides SQLite | Floor |
|---|---|---|
| `SQLite.Framework` | The OS on desktop and iOS, bundled on Android | Declare one |
| `SQLite.Framework.Base` | You, through your own SQLitePCLRaw provider | Declare one |
| `SQLite.Framework.Bundled` | The package itself | Known version, so the enum only has `Unspecified` |
| `SQLite.Framework.Cipher` | The package itself, through SQLCipher | Known version, so the enum only has `Unspecified` |

## How to choose

The strategy depends on what you are building.

### Desktop apps

Use `SQLite.Framework.Bundled` and do not worry about the version at all. The package ships its own SQLite, so every user runs the same version on every OS.

Staying on `SQLite.Framework` means the OS provides SQLite. That works well on macOS and Linux, where the shipped SQLite is documented. On Windows the system SQLite (`winsqlite3.dll`) is undocumented and considered internal, so there is no reliable way to know what version your users have. That is why `Bundled` is the safe default for desktop.

### Mobile apps

Use `SQLite.Framework`.

Android is never the limit. Apps cannot bind to the C API of the Android system SQLite. The supported route goes through Android's Java classes. Translating every C# call to Java makes that path slower. So the framework always bundles a recent SQLite on Android.

That leaves iOS, where the framework uses the system SQLite and the version is tied to the iOS version. Pick the floor that matches the lowest iOS version your app supports, the `SupportedOSPlatformVersion` in your csproj:

| iOS version | SQLite version |
|---|---|
| 2.2 | 3.4.0 |
| 3.1.3 | 3.6.12 |
| 4.0.2 | 3.6.22 |
| 4.1.0 | 3.6.23.2 |
| 4.2.0 | 3.6.23.2 |
| 5.1.1 | 3.7.7 |
| 6.0.1 | 3.7.13 |
| 7.0 | 3.7.13 |
| 7.0.6 | 3.7.13 |
| 8.0.2 | 3.7.13 |
| 8.2 | 3.8.5 |
| 9.0 | 3.8.8 |
| 9.3.1 | 3.8.10.2 |
| 10.0 | 3.14.0 |
| 10.2 | 3.14.0 |
| 10.3.1 | 3.16.0 |
| 11.0 | 3.19.3 |
| 12.0 | 3.24.0 |
| 12.1 | 3.24.0 |
| 13.1.3 | 3.28.0 |
| 14.1 | 3.32.3 |
| 14.2 | 3.32.3 |
| 14.4 | 3.32.3 |
| 14.5 | 3.32.3 |
| 14.8 | 3.32.3 |
| 15.0 | 3.36.0 |
| 15.1 | 3.36.0 |
| 15.2 | 3.36.0 |
| 15.4.1 | 3.37.0 |
| 15.6.1 | 3.37.0 |
| 16.0 | 3.39.0 |
| 16.0.3 | 3.39.0 |
| 16.5.2 | 3.39.5 |
| 16.7.4 | 3.39.5 |
| 17.0 | 3.43.2 |

For example, an app that supports iOS 15.0 and later can declare `V3_36`. Declaring `V3_37` to get [STRICT tables](Schema) means raising the lowest supported iOS to 15.4.1.

The lowest floor the framework supports is `V3_8`, so iOS releases before 8.2 sit below every floor.

### Servers

On Linux use `SQLite.Framework`. In our measurements the system SQLite is faster than the one SQLitePCLRaw bundles.

When the server OS is not under your control, `SQLite.Framework.Bundled` is a better pick.

### One package per platform

The provider packages (`SQLite.Framework`, `Bundled`, `Cipher`, `Base`) share the same assembly name and API, so your class libraries do not lock the choice in. Only the package referenced by the root project, the app you actually ship, decides which SQLite loads. A shared library can reference `SQLite.Framework` while the desktop head references `SQLite.Framework.Bundled`. NuGet replaces the provider in every referenced project with the root's choice.

## What each floor unlocks

The floor is also an opt-in. A framework method that needs a newer SQLite than the floor throws `NotSupportedException` and a few translations pick a better SQL shape when the floor allows it. The main gates:

| Feature | Needs at least |
|---|---|
| Common table expressions, `SQLiteFunctions.Printf` | `V3_8_3` |
| R-Tree tables | `V3_8_5` |
| FTS5 tables, expression indexes, JSON columns | `V3_9` |
| Row values, used by tuple comparisons and tuple `Contains` | `V3_15` |
| `Pragmas.Optimize` | `V3_18` |
| `Upsert`, R-Tree auxiliary columns | `V3_24` |
| Window functions, `RenameColumn` | `V3_25` |
| `VacuumInto` | `V3_27` |
| `FILTER (WHERE ...)` on aggregates and window functions, `GROUPS` frames, frame `EXCLUDE` options | `V3_28` |
| `NULLS FIRST` / `NULLS LAST` ordering | `V3_30` |
| Computed columns, computed column defaults | `V3_31` |
| `SQLiteFunctions.Iif` | `V3_32` |
| `ExecuteUpdate` with a joined source | `V3_33` |
| `Returning`, `DropColumn`, `Math` functions, `MATERIALIZED` CTE hints, in-place [Migrations](Migrations) | `V3_35` |
| STRICT tables | `V3_37` |
| `SQLiteFunctions.UnixEpoch`, `SQLiteFunctions.Format` | `V3_38` |
| Right and full outer joins, `SQLiteFunctions.DistinctFrom` | `V3_39` |
| `SQLiteFunctions.Unhex` | `V3_41` |
| `SQLiteDateFunctions.Timediff` | `V3_43` |
| `GroupConcat` with an `ORDER BY` inside the aggregate | `V3_44` |
| The JSONB members of `SQLiteJsonFunctions` | `V3_45` |

Every member of `SQLiteMinimumVersion` documents what that SQLite release added and the lowest iOS whose system SQLite satisfies it, so your IDE shows the details on hover.
