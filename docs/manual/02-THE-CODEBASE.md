# 2. The codebase

This is the map. Every folder and every file in the project, what it is responsible for, and when you would touch it.

If you are looking for *where to make a change*, find the file that owns the behaviour in the tables below, then go to `03-HOW-TO-MODIFY.md` for the recipe.

---

## The shape of the project at a glance

```
AizenRex-Search-man/
│
├── src/
│   ├── AizenSearch.Core/          the engine — no UI, fully testable
│   └── AizenSearch.App/           the desktop shell + the whole interface
│
├── tests/AizenSearch.Core.Tests/  xUnit test suite
│
├── Distribution/Portable/         packaged portable build (assemblies + Web/)
│
├── scripts/                       icon generation and screenshot helpers
│
├── legacy_rust/                   archived original Rust implementation
│
├── docs/                          architecture, syntax, build, security, this manual
│
├── .github/workflows/release.yml  the cloud build and release pipeline
│
├── AizenSearch.sln                the solution that ties the projects together
├── aizen_search_installer.iss     the Inno Setup installer script
├── license.txt                    the licence document
├── README.md                      the public front page
└── SECURITY.md, CONTRIBUTING.md   policies
```

**Two projects, one direction of dependency:** `AizenSearch.App` references `AizenSearch.Core`. Never the other way round. That single rule is what keeps the engine testable.

---

## `src/AizenSearch.Core/` — the engine

Everything that does real work lives here. There is no WPF, no WebView2, and no reference to the app project.

### The project file

| File | Lines | What it is |
|:---|---:|:---|
| `AizenSearch.Core.csproj` | 14 | Targets `net9.0-windows`. Note that it targets Windows even though it has no UI — it uses Win32 APIs for the MFT and shell integration. |

### `Models/` — the plain data types

These are the nouns of the system. They carry data and almost no behaviour.

| File | Lines | What it holds |
|:---|---:|:---|
| `FileEntry.cs` | 85 | One indexed file or folder. Name, the directory-pool id, size, created and modified timestamps, attributes, and the pre-computed lowercase extension. **The single most performance-sensitive type in the project** — it is allocated millions of times, so keep it small and never add a string field. |
| `SearchQuery.cs` | 24 | The parsed form of what the user typed: the terms, the filters, and the four toggle flags (match case, whole word, full path, regex). Produced by `QueryParser`, consumed by `SearchEngine`. |
| `SearchResult.cs` | 15 | A single result row as the interface receives it: the display name, the full path, size, dates, type, and the icon reference. |
| `IndexProgress.cs` | 13 | Progress reporting during a build — current phase, files found so far, percentage. Drives the progress overlay in the UI. |
| `PreviewData.cs` | 15 | What the preview pane needs: a kind tag (`image`, `text`, `none`), the content or a data URI for an image, and a short text excerpt. |

**When you touch these:** adding a field to `FileEntry` means the binary cache format changes — see the recipe in `03-HOW-TO-MODIFY.md`.

### `Indexing/` — getting the list of files

| File | Lines | What it is responsible for |
|:---|---:|:---|
| `IIndexEngine.cs` | 9 | The tiny interface the two indexers share. Both the MFT reader and the directory walker satisfy it, which is what lets `IndexManager` switch between them without caring which it has. |
| `NtfsMftIndexer.cs` | 195 | **The fast path.** Calls `FSCTL_ENUM_USN_DATA` to walk the USN journal, receives batches of records, and reconstructs full paths by resolving each entry's parent chain. Requires elevation. |
| `ParallelDirectoryIndexer.cs` | 245 | **The fallback.** Walks directory trees with a concurrent work queue. Handles access-denied folders, reparse points and symlinks, and very deep trees without recursing into the stack. Used when not elevated or on non-NTFS volumes. |
| `IndexManager.cs` | 646 | **The orchestrator, and the biggest file in the engine.** Decides which indexer to use per volume, runs the build, reports progress, holds the finished in-memory index, hands it to the cache service, and coordinates the watcher. Start here when you need to understand indexing as a whole. |
| `ConnectedLibrarySeeder.cs` | 134 | Seeds the index with additional locations — connected or mapped libraries that are not part of the normal fixed-drive sweep. |

**The most useful thing to know here:** the path-reconstruction logic in `NtfsMftIndexer` and the concurrency logic in `ParallelDirectoryIndexer` are the two pieces most likely to have subtle bugs. The test suite has `VirtualDriveTests` aimed specifically at indexer behaviour for this reason.

### `Search/` — answering the question

| File | Lines | What it is responsible for |
|:---|---:|:---|
| `QueryParser.cs` | 282 | **The query language.** Turns typed text into a `SearchQuery`. Handles `ext:`, `type:` with aliases, `path:`, `size:` with units, `date:` with `after`/`before`/`between`, `!exclusion`, `"phrases"`, `*`/`?` wildcards, regex, and the smart-gap matching that makes `superman 2006` find `Superman.Returns.2006.mkv`. |
| `SearchEngine.cs` | 384 | **The scan.** Compiles the query into predicates, applies the cheap filters first, and walks the entry array with early exit on the first failed filter. Also implements duplicate detection. |
| `RelevanceRanker.cs` | 141 | Orders the results: exact filename match, then filename prefix, then filename substring, then path match. |

**When you touch these:** `QueryParser` is the file to extend for a new search operator, and `SearchEngine` is where you would add a new kind of filter. Both have tests — add to them.

### `Storage/` — the memory and the disk

| File | Lines | What it is responsible for |
|:---|---:|:---|
| `DirectoryPool.cs` | 31 | **The memory trick, in 31 lines.** Stores each distinct directory path exactly once and hands out integer ids. Every `FileEntry` holds an id instead of a path string. Small file, enormous effect. |
| `BinaryCacheService.cs` | 279 | Reads and writes `index.bin`. Owns the file format, the header and its version, validation of a loaded file, and atomic writes (temporary file, then rename into place). |

### `Services/` — everything else, one job each

| File | Lines | What it is responsible for |
|:---|---:|:---|
| `UpdateService.cs` | 110 | **Owns `CurrentVersion`, `RepoOwner` and `RepoName`.** Makes the app's only network call — `GET /repos/{owner}/{repo}/releases/latest` — and compares the tag against the running version. **This file is the single source of truth for the version number.** |
| `PreviewService.cs` | 299 | Builds preview data. Downscales images to a thumbnail instead of handing the renderer a full-resolution bitmap, and reads only a bounded prefix of text files. |
| `RealtimeIndexWatcher.cs` | 214 | Watches the indexed roots with `FileSystemWatcher` and patches the in-memory index as files appear, change, and disappear. Debounces bursts so a large copy does not cause a storm of updates. |
| `DosDeviceHelper.cs` | 212 | Translates NTFS device paths (`\Device\HarddiskVolume3`) into drive letters, and enumerates volumes. Without this, MFT results have paths nobody can use. |
| `ProcessService.cs` | 183 | Starts processes: open a file, open a folder, launch a terminal, run something elevated. |
| `ShellIconService.cs` | 150 | Fetches real Windows shell icons for files, with a cache, so the result list looks like Explorer. |
| `LogService.cs` | 113 | Writes the diagnostic log. The first place to look when something fails on a machine you cannot inspect. |
| `ElevationService.cs` | 44 | Requests administrator rights. |
| `MemoryOptimizer.cs` | 23 | Trims the working set after a big index build so unused memory returns to the system. |

---

## `src/AizenSearch.App/` — the shell and the interface

This project is deliberately thin. It hosts a browser control and relays messages. Almost everything interesting is in `Core`.

| File | Lines | What it is responsible for |
|:---|---:|:---|
| `AizenSearch.App.csproj` | 38 | WPF app targeting `net9.0-windows`, references `AizenSearch.Core` and the WebView2 package. **Holds `Version`, `AssemblyVersion` and `FileVersion`** — all three must match `CurrentVersion` at release time. |
| `App.xaml` | 9 | Application resources and startup. |
| `App.xaml.cs` | 135 | Startup logic: single-instance enforcement (a second launch forwards its arguments to the running window), global exception handling, and bringing the main window up. |
| `AssemblyInfo.cs` | 10 | Standard assembly attributes. |
| `Views/MainWindow.xaml` | 14 | The window definition — a WPF `Window` containing a single WebView2 control. Note what is **not** here: a version number. The title is set at runtime. |
| `Views/MainWindow.xaml.cs` | 315 | Hosts WebView2, maps `Web/` to a virtual host so relative URLs resolve, sets the window title from `UpdateService.CurrentVersion`, pushes the version into the page after navigation, wires the bridge, and handles window-level events. |
| `Ipc/IpcBridge.cs` | 407 | **The bridge, and the most important file in this project.** Receives the JSON messages listed in chapter 1, dispatches each `action` to the right core service, and pushes results back into the page. Adding a native-backed feature starts here. |
| `AizenRex.ico` | — | The application icon. This is the one the project file references. |
| `AizenSearch.ico` | — | A second icon file of the same size sitting alongside it. The project file does not reference it, so it appears to be a leftover. Verify before deleting. |

### `src/AizenSearch.App/Web/` — the entire user interface

This is the whole interface: no framework, no build step, no bundler. Edit a file and reload.

| File | Lines | What it is responsible for |
|:---|---:|:---|
| `index.html` | 741 | The whole markup: the menu bar and its dropdowns, the search box and its toolbar, the filter bar, the results pane, the preview aside, the context menu, the About dialog, the changelog dialog, the query-help panel, and the indexing progress overlay. |
| `js/app.js` | 2047 | **The largest file in the project.** All the interface behaviour: sending IPC messages, receiving results, rendering rows, menus, keyboard shortcuts, sorting, filters, favourites, themes, zoom, search history, CSV export triggers, the changelog, and the update pill. |
| `js/virtual-scroll.js` | 182 | The virtualised list. Keeps only the visible rows in the DOM so a result set of hundreds of thousands still scrolls smoothly. |
| `js/preview.js` | 52 | The preview pane: requesting preview data and showing the image or text. |
| `css/app.css` | 2194 | All styling, including the light and dark themes and the whole visual language. **This is the file to edit for anything visual.** |
| `assets/logo.svg` | 20 | The application logo used in the About dialog and the header. |

**The rule for this folder:** after editing anything here, copy it to `Distribution/Portable/Web/` and bump the cache-buster in `index.html` (`css/app.css?v=N` → `?v=N+1`). WebView2 caches hard, and a stale stylesheet behaves exactly like a broken change.

---

## `tests/AizenSearch.Core.Tests/` — the test suite

xUnit. Runs on every release build; a failure stops the release.

| File | Lines | What it covers |
|:---|---:|:---|
| `AizenSearch.Core.Tests.csproj` | 25 | The test project. References `AizenSearch.Core` only — never the app, which is why the suite runs on a build server with no display. |
| `SearchEngineTests.cs` | 168 | The largest test file. Query execution: filters, combinations, exclusions, ordering, edge cases. |
| `VirtualDriveTests.cs` | 112 | Indexer behaviour against a synthetic drive layout — the safety net for the path-reconstruction and traversal logic. |
| `BinaryCacheTests.cs` | 90 | The index file format: write, read back, validation of a bad header, rejection of an incompatible version. |
| `QueryParserTests.cs` | 36 | The query language: each operator parses into the expected structure. Small but the first line of defence for the syntax. |

**When you change engine behaviour, add a test here.** The suite is small enough to read in a few minutes, which is a feature.

---

## `Distribution/Portable/` — the packaged build

The output of `dotnet publish`, plus a copy of the web assets.

**Everything in this folder except `Web/` is build output and is deliberately not committed.** The assemblies and executables are produced by the release pipeline and shipped as release assets instead. `Distribution/Portable/Web/` is the exception: it mirrors `src/AizenSearch.App/Web/` and *is* committed, so the packaged build has its interface.

See `.gitignore` for the exclusion rules.

---

## `scripts/` — build helpers

| File | Lines | What it is responsible for |
|:---|---:|:---|
| `generate_ico.ps1` | 130 | Generates the multi-resolution `.ico` from the source artwork. Runs in the release pipeline before the build. |
| `build_fluent_ico.py` | 114 | A Python implementation of the same job, kept for use without PowerShell. |
| `capture_menus.ps1` | 76 | Screenshot helper used when documenting the menus. |

All three resolve paths relative to the project folder — never from a hard-coded personal directory. Keep it that way.

---

## `legacy_rust/` — the archived original

The project began as a Rust implementation. That code is kept for reference: it shows the original approach to MFT parsing and is occasionally useful when reasoning about the Windows APIs.

**It is not built, not tested, and not shipped.** Do not treat it as a second implementation to keep in sync. Its `Cargo.toml`, `Cargo.lock` and `src/` are self-contained inside the folder.

---

## `docs/` — the documentation

| File | Lines | What it is |
|:---|---:|:---|
| `ARCHITECTURE.md` | 141 | The technical tour with diagrams. Aimed at someone who wants the shape of the system quickly. |
| `SEARCH-SYNTAX.md` | 196 | The full query language for users, with worked examples. |
| `BUILD-AND-RELEASE.md` | 149 | Building locally and understanding the release pipeline. |
| `SECURITY-AND-SECRETS.md` | 95 | What must never be committed, and how secrets are stored. |
| `manual/` | — | **This manual.** The complete guide, start to finish. |
| `assets/*.svg` | — | The diagrams used by the README and the architecture document. Each exists in a dark and a light variant, selected by a `<picture>` element so it matches the reader's theme. |

---

## The repository root

| File | Lines | What it is |
|:---|---:|:---|
| `README.md` | 338 | The public front page: what it is, how to install, features, search syntax, the architecture diagram, build instructions, security, FAQ. |
| `AizenSearch.sln` | 71 | The Visual Studio solution. Ties the two projects and the test project together. Uses CRLF line endings by design (see `.gitattributes`). |
| `aizen_search_installer.iss` | 105 | The Inno Setup script. Defines the installer, its files, its shortcuts, the optional Explorer context-menu entry, and **`MyAppVersion`, which must match `CurrentVersion` at release time.** |
| `license.txt` | 56 | The licence document. Carries the product version, which must also match at release time. |
| `SECURITY.md` | 40 | How to report a vulnerability privately. |
| `CONTRIBUTING.md` | 72 | How to contribute, and the house rules. |
| `.gitignore` | 73 | What never gets committed: build output, packages, logs, certificates, keys, secrets, editor folders, local overrides. |
| `.gitattributes` | 21 | Line-ending normalisation. `* text=auto eol=lf` as the default, with CRLF forced for `.sln`, `.csproj` and `.ps1` because those tools expect it. Binaries marked as binary. |

---

## `.github/workflows/release.yml` — the pipeline

One workflow, named **Build and Release**. It runs on a tag push (`v*`) or manually. Its steps, in order:

| # | Step | What it does |
|---:|:---|:---|
| 1 | Checkout | Gets the code. |
| 2 | Resolve version | Works out the version from the tag or the manual input. |
| 3 | Setup .NET 9 | Installs the SDK. |
| 4 | Ensure application icons | Generates the `.ico` files if they are missing. |
| 5 | Restore | Restores NuGet packages. |
| 6 | Build | Compiles in Release. |
| 7 | Run tests | Runs the xUnit suite. A failure stops everything. |
| 8 | Publish portable build | Produces `Distribution/Portable`. |
| 9 | **Verify version is consistent everywhere** | **Fails the build** if `CurrentVersion`, the project file, the installer script and the licence disagree. |
| 10 | Package portable ZIP | Zips the portable build. |
| 11 | Install Inno Setup | Installs the installer compiler. |
| 12 | Build installer | Compiles `Setup.exe`. |
| 13 | Sign installer | Signs and timestamps — **skipped unless the certificate secrets exist.** |
| 14 | Upload build artifacts | Stores the files on the run. |
| 15 | Publish GitHub release | Creates the release and attaches both files. |

Step 9 is the one to understand. It exists because the version number used to be written in several places and they drifted apart. Do not weaken it.

---

## Where to look for a given kind of change

| I want to change… | Start in… |
|:---|:---|
| Anything visual — colours, spacing, fonts, layout | `src/AizenSearch.App/Web/css/app.css` |
| A menu item, a button, a dialog, a label | `src/AizenSearch.App/Web/index.html` |
| Interface behaviour — shortcuts, sorting, filters, rendering | `src/AizenSearch.App/Web/js/app.js` |
| How the list scrolls | `src/AizenSearch.App/Web/js/virtual-scroll.js` |
| The preview pane | `src/AizenSearch.App/Web/js/preview.js` and `Core/Services/PreviewService.cs` |
| What a native action does | `src/AizenSearch.App/Ipc/IpcBridge.cs` |
| The window itself, its title, its startup | `src/AizenSearch.App/Views/MainWindow.xaml.cs` |
| The search language | `src/AizenSearch.Core/Search/QueryParser.cs` |
| How matching works, or ranking | `Core/Search/SearchEngine.cs`, `Core/Search/RelevanceRanker.cs` |
| How files are enumerated | `Core/Indexing/IndexManager.cs`, then the two indexers |
| Memory use | `Core/Storage/DirectoryPool.cs`, `Core/Models/FileEntry.cs` |
| The saved index format | `Core/Storage/BinaryCacheService.cs` |
| Live updates as files change | `Core/Services/RealtimeIndexWatcher.cs` |
| The update check | `Core/Services/UpdateService.cs` |
| The version number | `Core/Services/UpdateService.cs` — **and nowhere else** |
| The installer | `aizen_search_installer.iss` |
| The release process | `.github/workflows/release.yml` |

Next: **`03-HOW-TO-MODIFY.md`** turns this map into step-by-step recipes.