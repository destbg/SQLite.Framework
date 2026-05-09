# Samples

The repository ships a handful of small projects under [`Sample/`](https://github.com/destbg/SQLite.Framework/tree/main/Sample) that exercise `SQLite.Framework` in different host environments. Each one is self-contained, references the framework projects via `ProjectReference`, and is registered in the solution under the `Samples` solution folder.

| Project | What it shows | Run with |
|---|---|---|
| [`SQLite.Framework.Sample`](https://github.com/destbg/SQLite.Framework/tree/main/Sample/SQLite.Framework.Sample) | Console walkthrough of the LINQ surface: basic queries, joins, subqueries, group-by, bulk operations, transactions, raw SQL. | `dotnet run --project Sample/SQLite.Framework.Sample` |
| [`SQLite.Framework.AspNet`](https://github.com/destbg/SQLite.Framework/tree/main/Sample/SQLite.Framework.AspNet) | A Native AOT minimal-API service. Shows DI registration with `AddSQLiteDatabase`, the source generator with `UseGeneratedMaterializers`, `DisableReflectionFallback`, and `ExecuteUpdate` / `ExecuteDelete`. | `dotnet run --project Sample/SQLite.Framework.AspNet` |
| [`SQLite.Framework.Maui`](https://github.com/destbg/SQLite.Framework/tree/main/Sample/SQLite.Framework.Maui) | Full .NET MAUI app (Android / iOS / Mac Catalyst / Windows). Demonstrates a setup for mobile: singleton database, async-only access, MVVM with `CommunityToolkit.Mvvm`. | `dotnet build -t:Run -f net10.0-android Sample/SQLite.Framework.Maui` |
| [`SQLite.Framework.Avalonia`](https://github.com/destbg/SQLite.Framework/tree/main/Sample/SQLite.Framework.Avalonia) | Avalonia desktop + Android + iOS app. Same database layer as MAUI, different UI stack. Has Native AOT enabled for the desktop target. | `dotnet run --project Sample/SQLite.Framework.Avalonia` |
| [`SQLite.Framework.Blazor`](https://github.com/destbg/SQLite.Framework/tree/main/Sample/SQLite.Framework.Blazor) | Blazor WebAssembly app that runs SQLite directly in the browser via `SQLite.Framework.Bundled` (which ships `SQLitePCLRaw.bundle_e_sqlite3` with a `browser-wasm` build of SQLite). Snapshots the DB file to `localStorage` between page loads. UI is built with MudBlazor. | `dotnet run --project Sample/SQLite.Framework.Blazor` |
| [`SQLite.Framework.Benchmarks`](https://github.com/destbg/SQLite.Framework/tree/main/Sample/SQLite.Framework.Benchmarks) | BenchmarkDotNet head-to-head against EF Core 10 and `sqlite-net-pcl`. Backs the numbers in the README.md. | `dotnet run --project Sample/SQLite.Framework.Benchmarks -c Release` |

## Picking a starting point

- New to the framework: read [`SQLite.Framework.Sample`](https://github.com/destbg/SQLite.Framework/tree/main/Sample/SQLite.Framework.Sample). It is plain C# with no UI noise.
- Shipping a mobile app: [`SQLite.Framework.Maui`](https://github.com/destbg/SQLite.Framework/tree/main/Sample/SQLite.Framework.Maui) or [`SQLite.Framework.Avalonia`](https://github.com/destbg/SQLite.Framework/tree/main/Sample/SQLite.Framework.Avalonia).
- Building a small server with Native AOT: [`SQLite.Framework.AspNet`](https://github.com/destbg/SQLite.Framework/tree/main/Sample/SQLite.Framework.AspNet).
- Running entirely client-side in the browser: [`SQLite.Framework.Blazor`](https://github.com/destbg/SQLite.Framework/tree/main/Sample/SQLite.Framework.Blazor).
