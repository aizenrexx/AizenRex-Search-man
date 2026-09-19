# 1. How it works

This chapter explains the whole system. It starts in plain language and then goes precise. If you only read one technical chapter, read this one.

---

## Part A — The idea, in plain language

### The problem with normal search

Windows knows where every file on your disk is. It keeps a table of them as part of how NTFS works. When you use normal search, the program asks Windows about files one at a time — or walks through folders one by one, asking "what is in this folder?" over and over. On a drive with a few thousand files that is fine. On a drive with a few million it is hopeless.

### What this app does instead

It reads that table **once, in bulk**, and keeps the answer in memory.

Reading the table takes seconds. After that, searching never touches the disk at all — it is a loop over an array in memory. That is the whole trick. Every other design decision in the project exists to make that trick fast, reliable and pleasant.

### The three layers

```
┌─────────────────────────────────────────────┐
│  The interface (HTML / CSS / JavaScript)    │  what you see and click
├─────────────────────────────────────────────┤
│  The shell (WPF window + WebView2 + bridge) │  puts the two together
├─────────────────────────────────────────────┤
│  The engine (C#, no UI at all)              │  does the actual work
└─────────────────────────────────────────────┘
```

The engine knows nothing about windows, buttons or HTML. It exposes plain C# methods and events. That is deliberate: it means the engine can be tested without opening a window, which is why the test suite runs happily on a build server with no display.

---

## Part B — Getting the list of files

There are two ways the app can enumerate a drive. It tries the fast one first and falls back automatically.

### The fast way: the Master File Table

On an NTFS volume, every file and folder has an entry in a single system table called the **Master File Table**, or MFT. It is the volume's own index of itself — NTFS could not function without it.

Windows exposes a way to walk that table in bulk: a filesystem control code called `FSCTL_ENUM_USN_DATA`, which enumerates the **USN journal** (Update Sequence Number journal — NTFS's running log of changes). Walking it returns file records in large batches.

Compare the two approaches on a volume with 2,500,000 files:

| | Ask about each file | Walk the MFT |
|:---|:---|:---|
| Operations | millions of individual calls | a few hundred bulk reads |
| Time | minutes | seconds |
| Depends on folder depth | yes — deep trees are slower | no — flat table |

Each returned record gives the file's **file reference number**, its **parent's file reference number**, its **name**, and its **attributes**. Notice what is *not* in the record: the full path. The MFT stores each entry with a pointer to its parent, so a full path is reconstructed by walking that chain upwards to the root.

That reconstruction is why `NtfsMftIndexer` is more involved than a simple loop. It builds a map of file-reference-number → entry, then resolves parents. The `DosDeviceHelper` service exists for the same reason: volumes are identified by NTFS device paths like `\Device\HarddiskVolume3`, and something has to translate that into `C:` so the paths are usable.

**Elevation:** reading the raw MFT requires administrator rights. That is why the app offers an *Enable MFT / Admin* action. Without it, the fast path is unavailable.

### The fallback: walk the directories

If the app is not elevated, or the volume is not NTFS (a network share, a FAT-formatted USB stick, an optical disc), it walks the directory tree instead.

This is not naive recursion. `ParallelDirectoryIndexer` uses a work queue so that multiple directories are enumerated concurrently, and it handles the awkward cases — access-denied folders, reparse points and symlinks that would otherwise cause infinite loops, very deep trees.

It is slower than the MFT, but it is *correct everywhere*, which is what matters. Once the index exists, searching is equally fast either way.

### What gets stored per file

Each file becomes a `FileEntry`:

| Field | Why it exists |
|:---|:---|
| **Name** | the filename itself |
| **Directory id** | an index into the shared string pool (see below) — not the path text |
| **Size** | for `size:` queries and for the Size column |
| **Modified / Created** | for `date:` queries and for sorting |
| **Attributes** | to tell files from folders, hidden from visible, and so on |
| **Extension** | pre-computed, because `ext:` is the most common query and lowercasing it once beats doing it a million times |

### The memory trick: one copy of each directory

Here is the thing that makes holding millions of files in memory practical.

If you store the full path as text in every entry, a directory containing 50,000 files has its own path duplicated 50,000 times. Across a real drive, the same directory paths repeat millions of times, and most of the memory is spent storing the same strings over and over.

So the app does not do that. **`DirectoryPool` stores each distinct directory path exactly once**, and each `FileEntry` holds a small integer id pointing at it. Two files in the same folder share one string.

The effect is dramatic: directory strings are the bulk of the memory in a naive design, so interning them cuts resident memory by roughly an order of magnitude. A multi-million-entry index fits comfortably in a few hundred megabytes.

There is a trade-off: building the pool means hashing every directory path, and reading an entry's full path means a lookup rather than a field access. Both costs are paid at index time or in bulk, not per result row.

### The index, written to disk

When indexing finishes, the in-memory index is serialised to a file: **`%LOCALAPPDATA%\AizenSearch\index.bin`**.

The format is deliberately simple and fast to read: a header, then the directory pool, then the entries as fixed-width records. Because the records are fixed-width and the directory strings are stored once, the file is small relative to the data it describes, and reading it back is a straight sequential scan with no parsing — the reason a cold start is measured in milliseconds rather than seconds.

`BinaryCacheService` owns this. It also owns the awkward parts: writing to a temporary file and renaming it into place so an interrupted write cannot corrupt the index, validating the header so a stale or truncated file is detected and discarded rather than trusted, and versioning the format so an index written by an older build is recognised as incompatible and rebuilt instead of misread.

### Keeping the index current

A saved index would go stale immediately — files are created and deleted constantly. So a **filesystem watcher** (`RealtimeIndexWatcher`) subscribes to change notifications for the indexed roots. When files are added, removed or renamed, it updates the in-memory index directly. No rebuild, no disk scan.

The watcher is why the app can be left running: the index stays correct as you work. It is also why the app offers a manual *Rescan / Rebuild All Drives* — if the machine was asleep, or a drive was attached while the app was closed, the watcher may have missed changes, and a rebuild puts that right.

---

## Part C — Answering a search

Once the index is in memory, a search is a pipeline with three stages.

### Stage 1 — parse the query (`QueryParser`)

The raw text in the search box is turned into a `SearchQuery` object. The parser understands a small query language:

| Written | Meaning |
|:---|:---|
| `report` | a plain term, matched anywhere |
| `"annual report"` | a phrase, matched as written |
| `ext:pdf` | extension filter — `ext:pdf\|docx` for several |
| `type:video` | kind filter — `file`, `folder`, `exe`, `doc`, `image`, `audio`, `video`, `archive`, and aliases |
| `path:downloads` | force a term to match against the directory path |
| `size:>500mb` | size filter — `>`, `<`, `min-max`, or exact, with `b`/`kb`/`mb`/`gb`/`tb` |
| `date:after 2024-01-01` | date filter — `after`, `before`, `between X and Y`, or a bare year |
| `!backup` | exclusion — drop anything matching |
| `*.log` | wildcard |
| `^IMG_\d{4}` | regular expression, when the Regex toggle is on |

The parser is also where a genuinely friendly feature lives: **smart gap matching**. Typing `superman 2006` matches `Superman.Returns.2006.1080p.mkv` — the terms do not have to be adjacent or in order. Plain filename search rarely matches what people actually type; this is the fix.

Two details worth knowing when modifying it:

- The parser decides, for each term, whether it is a name match or a path match. `path:` forces the latter, and the *Match Full Path* toggle changes the default.
- The *Match Case*, *Match Whole Word*, *Match Full Path* and *Regex* toggles are not part of the query text — they arrive as separate flags on the `SearchQuery` and change how terms are compiled.

The output is a structure of already-compiled predicates, not raw text. By the time searching starts, there is no string parsing left to do — that work happens once per keystroke, not once per file.

### Stage 2 — scan the index (`SearchEngine`)

The engine walks the entry array and applies the compiled predicates. Two things make this fast:

- **Cheapest filter first.** An extension check or a size comparison is far cheaper than a substring match, so restrictive filters run before expensive ones and eliminate most entries immediately.
- **Early exit.** An entry that fails any filter is dropped without evaluating the rest.

Because everything is in memory and the predicates are pre-compiled, a scan over millions of entries is a fraction of a second — which is why results can appear as you type.

### Stage 3 — rank the results (`RelevanceRanker`)

A plain filename match and an incidental path match are not equally interesting, so results are ordered before display:

1. **Exact filename match** — highest
2. **Filename starts with the term**
3. **Filename contains the term**
4. **Path contains the term** — lowest

Sorting by name, path or type is a separate concern the interface controls; ranking decides the default order.

### Then only what you can see is drawn

Returning 200,000 results is easy. Drawing 200,000 rows is not — it would freeze the interface. So the result list is **virtualised**: the DOM holds only the rows currently on screen, and as you scroll the contents of those rows are swapped.

`virtual-scroll.js` implements this. It keeps the total height correct so the scrollbar behaves naturally, renders a window of rows around the viewport, and recycles the row elements as you move. The practical effect is that scrolling is smooth whether the query matched ten files or half a million.

---

## Part D — How the interface and the engine talk

The interface is a web page — HTML, CSS and JavaScript — displayed inside **WebView2**, which is the Edge browser engine embedded in the app. The native side is a **WPF** window whose only job is to host that control.

This is why the UI can be restyled without touching C#: the entire interface is ordinary web technology.

### The bridge

Every action in the interface — a search, opening a file, toggling the preview — becomes a small JSON message:

```javascript
window.ipc.postMessage({ action: 'search', query: 'type:video size:>1gb' });
```

`IpcBridge` receives it, reads the `action` field, and dispatches to the right engine service. When there is something to report back, it pushes JSON the other way:

```csharp
webView.CoreWebView2.PostWebMessageAsJson(json);
```

The interface listens for those messages and updates the page.

### The full list of actions

| Action | What it does |
|:---|:---|
| `search` | run a query |
| `preview` | build preview data for a file |
| `open` | open the file with its default program |
| `folder` | open the containing folder |
| `terminal` | open a terminal in the file's directory |
| `run_admin` | relaunch a program elevated |
| `copy`, `copy_name`, `copy_hash` | put text on the clipboard |
| `properties` | show the Windows properties sheet |
| `export_csv` | write the current results to CSV |
| `find_duplicates` | scan for duplicate files |
| `rebuild` | rebuild the index |
| `cancel_indexing` | stop an in-progress build |
| `elevate` | request administrator rights |
| `check_update` | check GitHub for a newer release |
| `icons` | fetch shell icons for result rows |
| `open_log_folder` | reveal the log directory |
| `exit_app` | quit |

That list is the complete vocabulary between the two halves of the app. **Adding a feature that needs native work means adding an action here** — the recipe is in `03-HOW-TO-MODIFY.md`.

### The one place with a rule to remember

The interface has no idea what version it is. The version is pushed into the page at runtime from the native side, which reads it from `UpdateService.CurrentVersion`. That is rule 1 in `00-START-HERE.md`, and it is the single most common way to break the app.

---

## Part E — The supporting services

Beyond indexing and searching, the engine contains a set of focused services. Each does one thing.

| Service | Responsibility |
|:---|:---|
| **`UpdateService`** | Owns `CurrentVersion`, `RepoOwner` and `RepoName`. Makes the one network call the app ever makes: a GET of this repository's latest release, compared against the running version. |
| **`PreviewService`** | Builds what the preview pane shows. For images it produces a scaled-down thumbnail rather than handing the full-resolution file to the interface — a 40-megapixel photo would otherwise stall the renderer. For text files it reads a bounded prefix, because some "text" files are gigabytes of log. |
| **`ShellIconService`** | Asks Windows for the real shell icon for a file, so the list looks like Explorer rather than a generic set of glyphs. |
| **`ElevationService`** | Requests administrator rights, used for raw MFT access and for *Run as Administrator*. |
| **`DosDeviceHelper`** | Translates NTFS device paths (`\Device\HarddiskVolume3`) into drive letters, and enumerates volumes. |
| **`ProcessService`** | Starts processes on the app's behalf — opening a file, opening a folder, launching a terminal, running something elevated. |
| **`MemoryOptimizer`** | Trims the process working set after a large index build, so memory that is no longer needed goes back to the system instead of sitting in the process. |
| **`LogService`** | Writes a diagnostic log to disk. When something goes wrong on a machine you cannot inspect, this is the first place to look. |

---

## Part F — The things that make it feel fast

Summarised, because these are the decisions to preserve when modifying the code:

1. **The disk is read once, in bulk, and never during a search.**
2. **Directory strings are stored once, not per file** — the single biggest memory win.
3. **Extensions are pre-computed** — the most common filter becomes a string comparison.
4. **Filters are compiled before the scan, not during it.**
5. **Cheap filters run before expensive ones**, so most entries are eliminated without a substring match.
6. **Results are virtualised** — cost is proportional to screen height, not result count.
7. **The index is a fixed-width sequential file** — no parsing on load.
8. **Images are downscaled in the engine**, so the renderer never sees a huge bitmap.
9. **Index writes are atomic** — a temporary file renamed into place, so an interrupted write cannot leave a corrupt index.

---

## Part G — Limits, honestly

Worth knowing before you plan work on this:

- **It searches metadata, not content.** Names, paths, extensions, sizes and dates. Full-text search inside documents is on the backlog.
- **The fast path is Windows- and NTFS-specific.** Non-NTFS volumes use the slower fallback, and non-Windows platforms are not supported.
- **Raw MFT access needs elevation.** Declining it works fine, just slower to build the index.
- **The index is per-machine.** It is not portable and is not synced anywhere.
- **The index can go stale** if the app was closed while files changed. The watcher handles changes while running; a rebuild handles the rest.
- **Huge drives take real memory.** Interning makes it practical, but millions of entries still need hundreds of megabytes.

---

## Where to go next

- The map of the code, file by file → **`02-THE-CODEBASE.md`**
- Recipes for changing things → **`03-HOW-TO-MODIFY.md`**
- Building and releasing → **`04-BUILD-TEST-RELEASE.md`**
- Using the app → **`05-USER-GUIDE.md`**
- Diagrams of all of the above → [`../ARCHITECTURE.md`](../ARCHITECTURE.md)
