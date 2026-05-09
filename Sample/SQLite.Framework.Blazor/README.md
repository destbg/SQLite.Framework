# Blazor WebAssembly sample

A standalone Blazor WebAssembly app that runs SQLite directly in the browser.
Demonstrates that `SQLite.Framework` works end to end on `browser-wasm` with no
server round-trips: the full LINQ surface, the source generator, async IO, and
`ExecuteUpdate` / `ExecuteDelete` all run inside the WebAssembly process.

## How it works

- The app references `SQLite.Framework.Bundled`, which pulls in
  `SQLitePCLRaw.bundle_e_sqlite3`. That bundle ships a `browser-wasm` build of
  the SQLite native library, so the same `sqlite3_*` calls the framework makes
  on every other platform also work in the browser.
- The DB file lives in the WASM in-memory filesystem under the path `todos.db`.
  After every mutation `TodoStore.PersistAsync` reads the file bytes and writes
  them to `localStorage` as a base64 string. On the next page load the bytes
  are written back to the FS before the connection is opened, so the data
  survives reloads.
- The page only uses async APIs (`ToListAsync`, `AddAsync`,
  `ExecuteUpdateAsync`, `ExecuteDeleteAsync`). Blazor WASM is single threaded,
  so blocking on `SemaphoreSlim.Wait()` or `Thread.Sleep` would deadlock the
  UI thread. Stick to the `*Async` overloads in WASM apps.

## Running

```bash
dotnet workload install wasm-tools
dotnet run --project Sample/SQLite.Framework.Blazor
```

Open the printed URL. Add a few todos, reload the page, and they will still be
there. Click `Reset database` to drop the file and clear `localStorage`.

## Persistence options

`localStorage` is the simplest store but caps out around 5 MB and blocks the
main thread on read. For a real app, snapshot to one of:

- **OPFS** (Origin Private File System) via JS interop. Async, no quota for the
  origin, and the file write is durable. Best fit for SQLite.
- **IndexedDB** with a single blob entry. Async and large, but more JS surface
  to write than OPFS.
- A custom `SQLitePCLRaw` VFS that maps SQLite's page IO directly onto OPFS.
  Highest fidelity but considerably more code than the snapshot approach used
  here.
