# Architecture

AizenRex Search-man is split into three projects plus the web interface. The goal of the split is that everything expensive and testable — indexing, parsing, ranking, caching — lives in a plain .NET class library with no UI dependency, and the desktop project is only a shell around it.

```
AizenSearch.App  (WPF + WebView2)
        │  IPC over WebView2 messages
        ▼
AizenSearch.Core (indexing, search, storage, services)
        ▲
        │
AizenSearch.Core.Tests (xUnit)
```

<div align="center">
<picture>
  <source media="(prefers-color-scheme: light)" srcset="assets/architecture-light.svg">
  <source media="(prefers-color-scheme: dark)" srcset="assets/architecture.svg">
  <img src="assets/architecture.svg" alt="AizenRex Search-man architecture diagram" width="100%">
</picture>
</div>

---

## `src/AizenSearch.Core` — the engine

### `Indexing/`

| File | Responsibility |
|---|---|
| `IndexManager.cs` | Owns the in-memory index and the cache. Chooses an index engine, raises progress events, keeps the total count, and coordinates rebuilds. |
| `NtfsMftIndexer.cs` | Reads the NTFS Master File Table directly through `FSCTL_ENUM_USN_DATA`. This is what makes a full-disk index take seconds rather than minutes. |
| `ParallelDirectoryIndexer.cs` | Fallback for volumes where the MFT is unavailable (non-NTFS, network drives, restricted access). Walks directories in parallel. |
| `IIndexEngine.cs` | The contract both indexers implement, so `IndexManager` does not care which one is active. |
| `ConnectedLibrarySeeder.cs` | Seeds the index with connected libraries and known folders. |

### `Search/`

| File | Responsibility |
|---|---|
| `QueryParser.cs` | Turns the raw query string into a `ParsedQuery`: positive terms, phrases, exclusions, extension and kind filters, size and date ranges, wildcard and regex matchers. |
| `SearchEngine.cs` | Executes a `ParsedQuery` against the index and returns `SearchResult` rows. |
| `RelevanceRanker.cs` | Scores results so the most likely match is first: exact-name beats prefix, prefix beats substring, and path matches rank below name matches. |

### `Storage/`

| File | Responsibility |
|---|---|
| `BinaryCacheService.cs` | Reads and writes `%LOCALAPPDATA%\AizenSearch\index.bin`. Persistence is atomic — a partially written cache is never loaded. |
| `DirectoryPool.cs` | Reuses directory string instances so a large index does not hold hundreds of thousands of duplicate path strings. |

### `Models/`

Plain data: `FileEntry`, `SearchQuery`, `SearchResult`, `PreviewData`, `IndexProgress`.

### `Services/`

| File | Responsibility |
|---|---|
| `UpdateService.cs` | The **single source of truth for the application version** (`CurrentVersion`). Queries this repository's latest release and reports whether an update is available. |
| `PreviewService.cs` | Produces preview data for the preview pane. |
| `ShellIconService.cs` | Extracts native Windows icons for file types. |
| `RealtimeIndexWatcher.cs` | Watches the filesystem and keeps the index current without a full rebuild. |
| `ElevationService.cs` | Detects and requests the elevation needed for raw MFT access. |
| `DosDeviceHelper.cs` | Resolves `\Device\HarddiskVolumeN` paths to drive letters. |
| `ProcessService.cs` | Launches files, folders, terminals, and the Properties dialog. |
| `MemoryOptimizer.cs` | Trims working set after large operations. |
| `LogService.cs` | Lightweight file logging. |

---

## `src/AizenSearch.App` — the shell

| File | Responsibility |
|---|---|
| `App.xaml.cs` | Application entry point and single-instance handling. A second launch forwards its command-line arguments to the running instance instead of opening a duplicate window. |
| `Views/MainWindow.xaml` | The window. Deliberately carries **no version number** — the title is set at runtime from `UpdateService.CurrentVersion`. |
| `Views/MainWindow.xaml.cs` | Hosts WebView2, maps the local `Web/` folder to `https://app.aizen`, sets the window title from the version constant, and pushes the version to the interface once navigation completes. |
| `Ipc/IpcBridge.cs` | The bridge. Receives JSON messages from the interface and dispatches them to core services; pushes results back with `PostWebMessageAsJson`. |

### The interface — `Web/`

The front end is plain HTML, CSS, and JavaScript with no framework and no build step. WebView2 loads it from a virtual host, so it behaves like a normal web page while talking to the native side over the IPC bridge.

| File | Responsibility |
|---|---|
| `index.html` | Structure: toolbar, filter chips, results area, preview pane, and the About / Changelog / Update / Help dialogs. Version labels are placeholders filled at runtime. |
| `js/app.js` | Search, rendering, view modes, context menu, dialogs, and the `appInfo` handler that receives the running version. |
| `js/virtual-scroll.js` | Renders only the visible rows, so a result set of hundreds of thousands stays smooth. |
| `js/preview.js` | Preview pane behaviour. |
| `css/app.css` | Theming. Light and dark, plus the font stacks (text / symbol / monospace) that keep pictograms rendering correctly on every Windows install. |

> **Rule for contributors:** after editing anything under `src/AizenSearch.App/Web/`, copy it to `Distribution/Portable/Web/` and bump the `css/app.css?v=N` cache-buster in `index.html`. WebView2 caches aggressively, and a stale stylesheet looks exactly like a bug.

---

## `tests/AizenSearch.Core.Tests`

| File | Covers |
|---|---|
| `QueryParserTests.cs` | Tokenising, `ext:` / `type:` / `path:` / `size:` / `date:` parsing, exclusions, phrases. |
| `SearchEngineTests.cs` | Filtering and result correctness against a fixture index. |
| `BinaryCacheTests.cs` | Cache round-trip and atomic persistence. |
| `VirtualDriveTests.cs` | Path and virtual-drive resolution. |

These run on every release build; a failing test blocks the release.

---

## How a search flows

<div align="center">
<picture>
  <source media="(prefers-color-scheme: light)" srcset="assets/search-flow-light.svg">
  <source media="(prefers-color-scheme: dark)" srcset="assets/search-flow.svg">
  <img src="assets/search-flow.svg" alt="Search flow diagram" width="100%">
</picture>
</div>



1. The user types — `app.js` posts `{action: "search", ...}` to the bridge.
2. `IpcBridge` deserialises it into a `SearchQuery` and calls `QueryParser.Parse`.
3. `SearchEngine` runs the parsed query against the index held by `IndexManager`.
4. `RelevanceRanker` orders the matches.
5. `IpcBridge` pushes the rows back with `PostWebMessageAsJson`.
6. `virtual-scroll.js` renders only what is on screen.

## How indexing flows

1. `IndexManager.Initialize` loads `index.bin` if it is present and valid.
2. If there is no usable cache, or the user rebuilds, `IndexManager` picks an engine — MFT when the volume allows it, parallel directory crawl otherwise.
3. Progress events are raised throughout and pushed to the interface as a percentage.
4. On completion the index is written back to `index.bin` atomically.
5. `RealtimeIndexWatcher` keeps it current afterwards.

## Design notes

- **The version lives in exactly one place.** `UpdateService.CurrentVersion`. The window title, the in-app badge, and the About dialog all read it at runtime. Hard-coding a version anywhere else is what caused title and dialog to disagree in earlier builds.
- **The engine has no UI dependency.** Everything above the IPC bridge is testable without a window.
- **Compatibility identifiers are intentional.** Assemblies, namespaces, the cache folder, and the executable still use `AizenSearch` rather than `AizenRex`. Renaming them would orphan existing installs and saved indexes.
