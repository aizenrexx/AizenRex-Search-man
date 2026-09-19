# 5. User guide

Everything about using the app: every menu, every shortcut, the full query language, and what to do when something goes wrong.

If you just want to install it, read `01` first. This chapter assumes it is already running.

---

## The window at a glance

```
┌──────────────────────────────────────────────────────────────┐
│  File   Edit   View   Search                    ← the menu bar │
├──────────────────────────────────────────────────────────────┤
│  [🔍 search box ........................]  ✕   [views] [👁] [🌓] │
│  Everything ▾                                                 │
├──────────────────────────────────────────────────────────────┤
│  Name ▲            Path            Type      ← column headers │
├───────────────────────────────────────────┬──────────────────┤
│                                           │                  │
│   result rows                             │   preview pane   │
│   (scrolling, virtualised)                │                  │
│                                           │                  │
├───────────────────────────────────────────┴──────────────────┤
│  v0.3.9   2,500,000 indexed   42 results    ⟳ Updates  ⓘ About│
└──────────────────────────────────────────────────────────────┘
```

The interface has four parts: the **menu bar**, the **search toolbar**, the **results pane**, and the **status bar**. The preview pane appears on the right when it is switched on.

---

## The search box

Type and results appear as you type. There is no button to press.

| Element | What it does |
|:---|:---|
| The search field | Where you type your query |
| **✕** (clear) | Clears the search. Same as pressing `Esc`. |
| Ghost suggestion | A faint completion hint appears as you type — press `Tab` to accept it |
| Filter dropdown | Narrows results to one kind of file without typing a `type:` term |

### The filter dropdown

A shortcut for the most common filters. Choosing one is the same as typing the equivalent `type:` term.

| Option | Shows |
|:---|:---|
| **Everything** | No filter |
| **Files** | Files only, no folders |
| **Folders** | Folders only |
| **Applications** | Executables and installers |
| **Documents** | Documents |
| **Pictures** | Images |
| **Audio** | Audio files |
| **Video** | Video files |
| **Archives (Zip/Rar)** | Compressed archives |

### The view buttons

| Button | View | Shortcut |
|:---|:---|:---|
| ☰ | **Details list** — columns for name, path and type | `Ctrl+Shift+1` |
| 🗂 | **Card / tile grid** — thumbnails with names | `Ctrl+Shift+2` |
| ☰ compact | **Compact list** — tight rows, more results per screen | `Ctrl+Shift+3` |
| 👁 | **Preview pane** — toggles the right-hand panel | `Alt+P` |
| 🌓 | **Theme** — light or dark | `Ctrl+Shift+D` |

### Recent searches

Clicking into the search box shows your recent searches. Click one to run it again, or use **Clear all** to empty the list.

---

## The menu bar

### File

| Item | Shortcut | What it does |
|:---|:---|:---|
| Open Selected | `Enter` | Opens the selected file with its default program |
| Open Containing Folder | `Ctrl+Enter` | Opens the folder the file is in, with the file selected |
| Open in PowerShell / Terminal | `Ctrl+Shift+T` | Opens a terminal in the file's directory |
| Run as Administrator | — | Launches the selected executable elevated |
| Copy Full Path | `Ctrl+Shift+C` | Copies the complete path |
| Copy Filename | `Ctrl+Alt+C` | Copies just the name |
| Calculate SHA-256 Hash | — | Computes and copies the file's hash |
| Export Results to CSV… | `Ctrl+S` | Writes the current results to a CSV file |
| Rescan / Rebuild All Drives | `F5` | Rebuilds the index from scratch |
| Properties | `Alt+Enter` | Opens the Windows properties sheet |
| Exit | `Alt+F4` | Closes the app |

### Edit

| Item | Shortcut | What it does |
|:---|:---|:---|
| Copy Selected Path | `Ctrl+C` | |
| Copy File Name | — | |
| Copy Containing Directory | — | Copies the folder path only |
| Select All Visible Results | `Ctrl+A` | |
| Invert Selection | — | |
| Focus Search Box | `Ctrl+F` | Puts the cursor in the search field |
| Clear Search | `Esc` | |

### View

**Layout view**

| Item | Shortcut |
|:---|:---|
| Details List | `Ctrl+Shift+1` |
| Card / Tile Grid | `Ctrl+Shift+2` |
| Compact List | `Ctrl+Shift+3` |
| Preview Pane | `Alt+P` |
| Favorites | `Ctrl+Shift+F` |

**Quick searches**

| Item | What it runs |
|:---|:---|
| Large Files (1 GB+) | Every file over a gigabyte |
| Recently Modified (7 days) | Files changed in the last week |
| Find Duplicate Files | Starts a duplicate scan |

**Sort results by**

| Item | Shortcut |
|:---|:---|
| Name | `Ctrl+1` |
| Path | `Ctrl+2` |
| Type / Extension | `Ctrl+3` |

Clicking a column header in the list sorts by that column too.

**Filter by type**

| Item | Shortcut |
|:---|:---|
| Everything | `Alt+1` |
| Video & Movies | `Alt+2` |
| Audio & Music | `Alt+3` |
| Documents | `Alt+4` |
| Pictures / Images | `Alt+5` |
| Applications | `Alt+6` |
| Archives (Zip/Rar/7z) | `Alt+7` |
| Folders Only | `Alt+8` |

**Zoom**

| Item | Shortcut |
|:---|:---|
| Zoom In | `Ctrl++` |
| Zoom Out | `Ctrl+-` |
| Reset Zoom (100%) | `Ctrl+0` |
| Refresh View | `Ctrl+R` |

### Search

These four toggles change how your query is matched. They are not typed into the search box.

| Item | Shortcut | What it does |
|:---|:---|:---|
| Match Case | `Ctrl+I` | Case-sensitive matching |
| Match Whole Word | `Ctrl+B` | Only whole-word matches |
| Match Full Path | `Ctrl+U` | Match against the full path, not just the name |
| Enable Regular Expressions | `Ctrl+E` | Treat the query as a regex |

**Smart quick presets**

| Item | What it does |
|:---|:---|
| Recent Video Search | |
| Video Library | |
| All High-Definition Video Files | |
| Disk Images & Large Archives | |
| Search Syntax Cheat Sheet… | `F1` — opens the in-app help |

---

## The full query language

This is the part worth learning. Everything below can be combined in one query.

### Plain terms

```
report
```
Matches anywhere in the filename.

```
"annual report"
```
A phrase — matched as written.

```
superman 2006
```
**Smart gap matching.** Matches `Superman.Returns.2006.1080p.mkv`. The terms do not need to be adjacent or in order. This is what makes filename search actually useful.

### By extension

```
ext:pdf
ext:pdf|docx
ext:mkv
```

### By kind

```
type:video
type:image
type:doc
type:audio
type:exe
type:archive
type:file
type:folder
```

Aliases exist for each kind, so `type:movie` and `type:video` both work.

### By path

```
path:downloads
path:DCIM
path:projects\client
```
Forces the term to match against the directory path rather than the filename.

### By size

```
size:>500mb
size:<10kb
size:100mb-2gb
size:1.5gb
```

Units: `b`, `kb`, `mb`, `gb`, `tb`. Comparisons: `>`, `<`, or a range with `-`.

### By date

```
date:after 2024-01-01
date:before 2023-06-30
date:between 2024-01-01 and 2024-12-31
date:2024
```

A bare year means that whole year. Dates are matched against the modified date.

### Excluding things

```
!backup
!node_modules
```

Drops anything matching. Extremely useful for cutting noise out of a broad search.

### Wildcards

```
*.log
IMG_????.jpg
```

`*` matches any run of characters, `?` matches one.

### Regular expressions

With **Enable Regular Expressions** on (`Ctrl+E`):

```
^IMG_\d{4}
(mkv|mp4)$
```

---

## Real queries worth keeping

```
type:video 2160p size:>20gb
```
A 4K film over 20 GB.

```
type:doc date:after 2024-01-01 !draft
```
This year's documents, minus drafts.

```
path:downloads ext:exe|msi
```
Every installer sitting in Downloads.

```
path:DCIM type:image date:2024-03
```
Camera photos from March.

```
ext:log !archive size:>100mb
```
Big log files worth looking at, excluding archived ones.

```
type:folder !$RECYCLE.BIN !System32
```
Folders, with the usual noise removed.

---

## Keyboard shortcuts, all in one place

| Shortcut | Action |
|:---|:---|
| `Enter` | Open selected |
| `Ctrl+Enter` | Open containing folder |
| `Alt+Enter` | Properties |
| `Ctrl+Shift+T` | Open in terminal |
| `Ctrl+C` | Copy path |
| `Ctrl+Shift+C` | Copy full path |
| `Ctrl+Alt+C` | Copy filename |
| `Ctrl+A` | Select all |
| `Ctrl+S` | Export to CSV |
| `Ctrl+F` | Focus the search box |
| `Esc` | Clear the search |
| `F5` / `Ctrl+R` | Rebuild / refresh |
| `F1` | Search syntax help |
| `Tab` | Accept the ghost suggestion |
| `↑` `↓` | Move through results |
| `PgUp` `PgDn` `Home` `End` | Move a screen at a time |
| `Alt+P` | Toggle the preview pane |
| `Ctrl+Shift+D` | Toggle the theme |
| `Ctrl+Shift+1/2/3` | Details / Card / Compact view |
| `Ctrl+1/2/3` | Sort by name / path / type |
| `Alt+1` … `Alt+8` | Filter by kind |
| `Ctrl+I` | Match case |
| `Ctrl+B` | Match whole word |
| `Ctrl+U` | Match full path |
| `Ctrl+E` | Regular expressions |
| `Ctrl++` `Ctrl+-` `Ctrl+0` | Zoom in / out / reset |

---

## The preview pane

Turn it on with the 👁 button or `Alt+P`. It shows:

- **Images** — a scaled preview. The app downscales rather than loading the full file, so this stays fast even for huge photos.
- **Text files** — the beginning of the file. Long files are truncated deliberately.
- **Everything else** — the metadata: type, size, created, modified, path.

Below the preview are four buttons: **Open**, **Open Path**, **Copy Full Path**, **Copy Name**.

---

## Right-click menu

Right-clicking a result gives the same actions in a shorter form:

- Open
- Open Containing Folder
- Copy Full Path
- Copy Name
- ⭐ Pin to Favorites
- Toggle Preview (`Alt+P`)

---

## Favorites

Right-click a result and choose **Pin to Favorites** to keep it. Favorites are reached from **View → Favorites** (`Ctrl+Shift+F`). Useful for the handful of files or folders you return to constantly.

---

## The status bar

| Element | Meaning |
|:---|:---|
| **v0.3.9** | The running version. Click it to open the changelog. |
| **2,500,000 indexed** | How many files are in the index |
| **42 results** | How many the current query matched |
| **⟳ Updates** | Check for a newer release manually |
| **ⓘ About** | Version, credits and the architecture notes |

**Enable MFT (Admin)** appears in the status bar when the app is not elevated. Clicking it requests administrator rights, which turns on the much faster indexing path.

---

## Indexing

### What happens on first run

The app builds an index. A progress overlay shows the phase and a percentage. On a large drive this takes seconds if elevated, longer if not.

### When to rebuild

Use **File → Rescan / Rebuild All Drives** (`F5`) if:

- You attached a drive while the app was closed.
- The machine was asleep and files changed.
- Results look stale or wrong.

Otherwise the app keeps the index current by itself — it watches for file changes while it is running.

### If indexing seems stuck

- Click **Stop** on the progress overlay to cancel, then try again.
- If it is slow, enable MFT access — that is usually the whole difference.
- A very large or very fragmented drive genuinely takes time on the fallback path.

---

## Updates

The app checks for a new release about five seconds after launch. If one exists, a badge appears in the status bar. Clicking it takes you to the download.

It does **not** install anything by itself. You download and run the installer, or replace the portable folder.

To check manually: the **⟳ Updates** button in the status bar.

---

## Themes

`Ctrl+Shift+D` toggles light and dark. The whole interface switches, including the result list and dialogs.

---

## Troubleshooting

### The app will not start

Almost always the .NET 9 Desktop Runtime is missing. Windows usually says so. Install it from [dotnet.microsoft.com/download/dotnet/9.0](https://dotnet.microsoft.com/download/dotnet/9.0) and try again.

### "Windows protected your PC" when installing

Expected for an unsigned build. Click **More info** → **Run anyway**. See `04-BUILD-TEST-RELEASE.md` for how signing removes it.

### Search returns nothing, but I know the file exists

Check, in order:

1. **Is the drive indexed?** Look at the *indexed* count in the status bar. If it is low, the drive was skipped.
2. **Is a filter hiding it?** Check the filter dropdown and the `Alt+1`–`Alt+8` state.
3. **Is a toggle interfering?** *Match Case*, *Match Whole Word* or *Match Full Path* can all exclude a file you expect to see. Turn them off and retry.
4. **Is the index stale?** Press `F5` to rebuild.
5. **Did you exclude it?** A `!term` in the query may be dropping it.

### Search is slow

- Enable MFT access — the *Enable MFT (Admin)* button in the status bar.
- If it is slow while *typing*, you may have a very broad query. Narrow it with a `type:` or `ext:` filter.

### The index is huge, or memory use is high

A multi-million-file index needs hundreds of megabytes. That is expected. If it seems excessive:

- Check that you are not indexing network drives unnecessarily.
- A rebuild can help if the index has accumulated stale entries.

### Preview shows nothing for an image

Very large images are downscaled and some formats are not supported. If the preview is blank but the file opens normally, that is the reason.

### Results are stale after copying files in

Give it a moment — the watcher debounces bursts of changes so a large copy does not cause a storm of updates. If it stays wrong, press `F5`.

### The app seems to be running twice

It should not be. It is single-instance by design: a second launch forwards its arguments to the running window. If you genuinely have two, close both and start again.

### Something else

The log is the first place to look: **the app's log folder**, reachable from the interface. It records what the app was doing when it failed.

---

## Where your data lives

| What | Where |
|:---|:---|
| Index | `%LOCALAPPDATA%\AizenSearch\index.bin` |
| Settings | `%LOCALAPPDATA%\AizenSearch\` |
| Logs | `%LOCALAPPDATA%\AizenSearch\` |

Deleting any of it is safe — the app rebuilds or resets to defaults. Nothing is stored anywhere else, and nothing is uploaded anywhere.

---

## Privacy, in one paragraph

The app makes exactly **one** network call: an update check against this repository's latest release. There is no telemetry, no analytics, no account, and no cloud. Your index and your searches never leave your machine.

---

## Where to go next

- How it works underneath → `01-HOW-IT-WORKS.md`
- Changing something → `03-HOW-TO-MODIFY.md`
- The query language in more depth → [`../SEARCH-SYNTAX.md`](../SEARCH-SYNTAX.md)