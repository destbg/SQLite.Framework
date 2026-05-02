# SQLite.Framework Avalonia Sample

A single-project Avalonia app that mirrors the data model of the MAUI sample (Projects, Tasks, Categories, Tags) and shows how to use `SQLite.Framework` from a cross-platform Avalonia application with AOT and trimming enabled.

## Heads

The csproj multi-targets every Avalonia head from one project file:

| Head | TFM | Build mode |
|---|---|---|
| Desktop (Windows / macOS / Linux) | `net10.0` | NativeAOT + trimming |
| Android | `net10.0-android` | Mono AOT + trimming (default for the Android workload) |
| iOS | `net10.0-ios` | Mono full AOT (mandatory for iOS) |
| Mac Catalyst | `net10.0-maccatalyst` | Mono full AOT |

iOS and Mac Catalyst heads are excluded from the multi-target list on Linux because the workloads are not available there.

## Run a head

```sh
# Desktop (debug)
dotnet run -f net10.0

# Desktop (NativeAOT, release)
dotnet publish -f net10.0 -c Release

# Android
dotnet build -f net10.0-android -t:Run

# iOS (requires the iOS workload and a Mac)
dotnet build -f net10.0-ios -t:Run
```

## What is shown

* `App.axaml.cs` wires up `Microsoft.Extensions.DependencyInjection` and registers `AppDatabase` with `AddSQLiteDatabase<T>` from `SQLite.Framework.DependencyInjection`. WAL mode is enabled and the source generator is wired up so the reflection fallback can be turned off.
* `Data/AppDatabase.cs` is the `SQLiteDatabase` subclass that exposes typed `SQLiteTable<T>` properties for every model, the same pattern recommended for EF Core users in the migration guide.
* `Data/SeedDataService.cs` reads `Resources/SeedData.json` through Avalonia's `AssetLoader` and inserts everything inside a single transaction using `BeginTransactionAsync`.
* `Models/*.cs` only carry framework attributes (`[Key]`, `[AutoIncrement]`, `[Table]`, `[Column]`, `[NotMapped]`). No UI dependencies, so the same models would compile in any head.
* `ViewModels/*.cs` use `CommunityToolkit.Mvvm` with `[ObservableProperty]` and `[RelayCommand]`. All commands are AOT-friendly.
* `Views/MainView.axaml` uses compiled bindings (`x:DataType` and `x:CompileBindings="True"`) so XAML works under Native AOT and trimming.

## Database location

The sample stores the database in the per-platform local-application-data folder, resolved by `Constants.DatabasePath`. On Linux this is `~/.local/share/SQLite.Framework.Avalonia/AppSQLite.db3`. Delete that file to re-seed.
