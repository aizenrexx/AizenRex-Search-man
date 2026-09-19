# 3. How to modify

This is the chapter to work from. Every section is a recipe: what to change, where, and what will break if you forget a step.

Work through `00-START-HERE.md` first if you have not — the five rules there are assumed throughout.

---

## Before you change anything

### Get a working copy

```powershell
git clone https://github.com/aizenrexx/AizenRex-Search-man.git
cd AizenRex-Search-man
```

Or, if you already have a clone on your machine (the folder is named `AizenRex Search-man`), just work there.

### Make a branch

Never work directly on `main`. A branch means you can throw the whole attempt away without thinking about it.

```powershell
git checkout -b my-change
```

### Confirm it builds before you touch it

```powershell
dotnet build AizenSearch.sln
dotnet test AizenSearch.sln
```

If it does not build *before* your change, you will not be able to tell whether you broke it or it was already broken.

### Check the packaged assets are in sync before you start

```powershell
git status
```

If `Distribution/Portable/Web/` shows as modified before you have done anything, someone edited the source web assets and forgot to mirror them. Fix that first, as its own commit, so it does not get mixed into your change.

---

## The checklist that applies to every change

Almost every modification follows this shape. The recipes below only fill in the specifics.

1. **Find the file that owns the behaviour** — use the table at the end of `02-THE-CODEBASE.md`.
2. **Make the change** in the source location only.
3. **If you touched `src/AizenSearch.App/Web/`, mirror it** to `Distribution/Portable/Web/` and bump the cache-buster.
4. **If you changed engine behaviour, add or update a test** in `tests/AizenSearch.Core.Tests/`.
5. **Build and run the tests.**
6. **Run the app and actually look at it.** Compiling is not the same as working.
7. **If the change is user-visible, bump the version** (see the version recipe).
8. **Commit, push, and — for a release — tag.**

---

# Part 1 — Interface changes

Everything in this part lives in `src/AizenSearch.App/Web/`. None of it needs C#, and none of it needs a rebuild of the engine.

## Recipe 1 — Change a colour, font, spacing or any other style

**File:** `src/AizenSearch.App/Web/css/app.css` (2194 lines)

The stylesheet uses CSS custom properties for the things you are most likely to want to change. Start by searching for the theme blocks rather than editing individual rules — a value changed at the top flows everywhere.

**Steps:**

1. Open `css/app.css`.
2. Search for the theme definitions (look for the `:root` block and the dark-theme override block).
3. Change the custom property, not the individual rule, wherever the property exists.
4. Mirror the file to `Distribution/Portable/Web/css/app.css`.
5. Bump the cache-buster in `index.html`.
6. Run the app and check **both** themes — light and dark. A colour that reads well on one is often invisible on the other.

**Watch out for:**

- The themes are separate blocks. Changing a value in one and not the other is the most common mistake here.
- Some colours are set inline in `index.html` for one-off elements. If your change does not appear, check whether the element has an inline `style` attribute overriding the stylesheet.

## Recipe 2 — Change a label, add a menu item, or add a button

**File:** `src/AizenSearch.App/Web/index.html` (741 lines)

**To change existing text:** search for the text you see on screen and edit it in place. Keep it short — the toolbar and menus are laid out for short labels.

**To add a menu item:**

1. Find the menu's dropdown block. The menus are `dropdownFile`, `dropdownEdit`, `dropdownView` and `dropdownSearch`.
2. Copy an existing `<div class="menu-dropdown-item">` block as your template.
3. Give it an icon, a label and a shortcut hint.
4. Point its `onclick` at the function that should run.
5. If the function does not exist yet, see Recipe 3.

```html
<div class="menu-dropdown-item" onclick="window.myNewAction()">
  <span class="menu-icon">⭐</span>
  <span class="menu-label">My New Action</span>
  <span class="menu-shortcut">Ctrl+Alt+M</span>
</div>
```

**To add a toolbar button:** find the toolbar block, copy an existing `<button>` with its `id` and `title`, and wire its `onclick`.

**Watch out for:**

- `id` values must be unique. `app.js` looks elements up by id, and a duplicate silently returns the first match — which produces a bug that looks unrelated to your change.
- If you add a keyboard shortcut hint, actually implement the shortcut in `app.js` (Recipe 3). A hint that does nothing is worse than no hint.

## Recipe 3 — Add interface behaviour (a shortcut, a new action, a filter)

**File:** `src/AizenSearch.App/Web/js/app.js` (2047 lines — the largest file in the project)

This file is organised by feature. Search for the nearest existing feature and follow its shape.

**To add a keyboard shortcut:**

1. Find the global key handler — search for `ctrlKey`.
2. Add your case alongside the others, before any catch-all branch.
3. Call `e.preventDefault()` when you handle the key, or the browser will also act on it.

```javascript
if (e.ctrlKey && e.shiftKey && e.key.toLowerCase() === 'm') {
  window.myNewAction();
  e.preventDefault();
  return;
}
```

**To add a function that the markup can call:** assign it to `window`, because inline `onclick` handlers resolve against `window`.

```javascript
window.myNewAction = function () {
  // do the thing, or send a message to the native side:
  window.ipc.postMessage({ action: 'my_native_action', value: 'something' });
};
```

**To add a filter type:** the filter list is defined in `index.html` as `<option>` elements and handled in `app.js` by `setFilter`. Add the option, then extend the filter handling to map it to the right query — usually by appending a `type:` term.

**Watch out for:**

- If your action needs the native side to do something (reading a file, starting a process, touching the index), it is **not** an interface-only change. You need Recipe 6 as well.
- `app.js` runs inside WebView2, which is Chromium — modern JavaScript is fine. But there is no build step, so no imports, no JSX, no TypeScript.

## Recipe 4 — Change how the result list renders

**Files:** `js/app.js` (row rendering) and `js/virtual-scroll.js` (the list itself)

**To change what a row shows:** find the row template in `app.js` and edit the markup it produces. The fields available are the ones on `SearchResult`: name, path, size, dates, type, icon.

**To change how many rows are kept in the DOM:** that is a constant in `virtual-scroll.js`. Raising it costs memory; lowering it can make fast scrolling look jumpy.

**Watch out for:**

- The list is virtualised. Only the visible rows exist in the DOM, so **you cannot select or style "all results" with CSS or a query selector** — most of them are not there. If you need to act on every result, do it on the data array, not on DOM nodes.
- Row height must stay fixed and consistent with what `virtual-scroll.js` assumes, or the scrollbar will drift. If you change row height, update the constant the scroller uses to match.

## Recipe 5 — Change the preview pane

**Files:** `js/preview.js` (52 lines) for the interface, `Core/Services/PreviewService.cs` (299 lines) for what is produced.

**To change how a preview looks:** `preview.js`.

**To support a new kind of preview** (say, PDF first page, or video frame):

1. Add the kind to `Core/Models/PreviewData.cs`.
2. Produce it in `Core/Services/PreviewService.cs`.
3. Render it in `js/preview.js`.

**Watch out for:**

- `PreviewService` deliberately downscales images and truncates text. Do not remove that to "show the real thing" — handing a 40-megapixel image or a gigabyte log file to the renderer will hang the app.

---

# Part 2 — Native and engine changes

Everything here is C#. After any change in this part, rebuild and run the tests.

## Recipe 6 — Add a new native action (the interface needs the engine to do something)

This is the most common structural change, and it always touches the same three places.

**Step 1 — add the action to the bridge.**
File: `src/AizenSearch.App/Ipc/IpcBridge.cs` (407 lines)

Find the dispatch — it is a `switch` on the `action` field. Add a case:

```csharp
case "my_native_action":
    // read the payload, call the service, produce a result
    var value = msg.GetProperty("value").GetString();
    var output = _someService.DoSomething(value);
    // push the answer back to the page
    PostToWeb(new { action = "my_native_action_result", data = output });
    break;
```

**Step 2 — send it from the interface.**
File: `src/AizenSearch.App/Web/js/app.js`

```javascript
window.ipc.postMessage({ action: 'my_native_action', value: 'something' });
```

**Step 3 — receive the answer and update the page.**

```javascript
window.addEventListener('message', (event) => {
  const msg = event.data;
  if (msg.action === 'my_native_action_result') {
    // update the DOM
  }
});
```

**Step 4 — mirror the web assets and bump the cache-buster.**

**Watch out for:**

- The bridge already listens for messages. Add to the existing handler; do not register a second listener, or you will get duplicate handling.
- The action name is a string used in two languages. A typo produces a silent no-op, not an error. Check both spellings.

## Recipe 7 — Change how searching works

**File:** `src/AizenSearch.Core/Search/SearchEngine.cs` (384 lines)

Read the file first. The structure is: compile the query into predicates, then scan the entries applying the cheapest predicates first.

**To change matching behaviour:** find where the predicates are built and adjust.

**To change ranking:** `src/AizenSearch.Core/Search/RelevanceRanker.cs` (141 lines). The order is exact filename, filename prefix, filename substring, path match. Changing this order changes what appears at the top of every search, so change it deliberately.

**Watch out for:**

- **Keep the cheap-filter-first ordering.** Extension and size comparisons are far cheaper than substring matches. Moving a substring check earlier will slow every search down, and the effect is invisible on a small test index and obvious on a real one.
- **Add a test.** `SearchEngineTests.cs` is where query behaviour is pinned down. A matching change without a test is a change that will silently regress.

## Recipe 8 — Add a new search operator

**File:** `src/AizenSearch.Core/Search/QueryParser.cs` (282 lines)

The parser reads the query text token by token and builds a `SearchQuery`.

**Steps:**

1. Read the existing operators first — `ext:`, `type:`, `path:`, `size:`, `date:` are all handled in the same shape. Follow it exactly.
2. Recognise your prefix while tokenising.
3. Parse the value, including the failure case — what happens if the user types `myop:` with nothing after it, or with nonsense? Decide and implement, because users will do it.
4. Store the result on `SearchQuery`.
5. Teach `SearchEngine` to honour the new field.
6. Add cases to `QueryParserTests.cs`.
7. Document it in `docs/SEARCH-SYNTAX.md`, in the README's syntax table, and in the in-app cheat sheet (Recipe 12).

**Watch out for:**

- The parser has to cope with the query being typed a character at a time — a half-typed `size:>` arrives on almost every keystroke. It must not throw, and it must not match everything.
- **Smart gap matching** is the feature that makes `superman 2006` find `Superman.Returns.2006.mkv`. If you are changing how multiple terms combine, you are touching it. Read the surrounding code carefully.

## Recipe 9 — Change indexing, or the fallback behaviour

**File:** `src/AizenSearch.Core/Indexing/IndexManager.cs` (646 lines) is the orchestrator. The two indexers are `NtfsMftIndexer.cs` (195) and `ParallelDirectoryIndexer.cs` (245).

**To change which indexer is chosen:** that decision is in `IndexManager`. The current logic is: use the MFT reader when elevated on an NTFS volume, otherwise fall back to the directory walker.

**To change what is stored per file:** start at `FileEntry` — and read the next warning before you do.

**Watch out for:**

- **Changing `FileEntry` changes the binary cache format.** See Recipe 10. If you add a field without bumping the format version, existing indexes will be read as garbage instead of being rebuilt.
- **The path reconstruction in `NtfsMftIndexer` is subtle.** The MFT gives each entry a parent reference, not a path. If paths come out wrong, the bug is in the parent-chain resolution, not in the enumeration.
- **`VirtualDriveTests.cs` is your safety net here.** Extend it rather than testing by hand.

## Recipe 10 — Change the index file format

**File:** `src/AizenSearch.Core/Storage/BinaryCacheService.cs` (279 lines)

**If you change what is stored in a `FileEntry`, or the pool, you must bump the format version.** That is what makes an old index get recognised as incompatible and rebuilt instead of misread.

**Steps:**

1. Make the change to `FileEntry` or the pool.
2. **Bump the format version constant in `BinaryCacheService`.**
3. Update the write path and the read path together.
4. Extend `BinaryCacheTests.cs` to cover the new field round-tripping.
5. Test the upgrade path: build an index with the old version, then run the new version against it. It must discard and rebuild, not crash.

**Watch out for:**

- **Write atomically.** The existing code writes a temporary file and renames it into place. Keep that — a crash mid-write must not leave a corrupt index that the app then trusts.
- Keep records fixed-width. Variable-width records mean parsing on load, which is exactly what this design avoids.

## Recipe 11 — Change the update check

**File:** `src/AizenSearch.Core/Services/UpdateService.cs` (110 lines)

This file owns three constants and makes the app's only network call.

```csharp
public const string CurrentVersion = "0.3.9";
public const string RepoOwner = "aizenrexx";
public const string RepoName = "AizenRex-Search-man";
```

**If the repository is ever renamed or moved**, `RepoName` and `RepoOwner` are the two values to update — and `docs/SECURITY-AND-SECRETS.md` and the installer's `MyAppURL` both contain the repository URL as well. Search the whole project for the old name.

**Watch out for:**

- The comparison must handle a tag like `v0.3.9` against a version like `0.3.9`. Keep whatever normalisation exists.
- **Do not add network calls.** The app makes exactly one, and that is a documented privacy property. Adding a second one silently breaks a promise made to users in the README.

## Recipe 12 — Change the version number (do this for every user-visible release)

This is rule 1, so it gets a full recipe.

**The version number is `UpdateService.CurrentVersion`.** Everything else must be brought into line with it, and the build will fail if you miss one.

**Files to change, in this order:**

| # | File | What to change |
|---:|:---|:---|
| 1 | `src/AizenSearch.Core/Services/UpdateService.cs` | `CurrentVersion` — **the source of truth** |
| 2 | `src/AizenSearch.App/AizenSearch.App.csproj` | `<Version>`, `<AssemblyVersion>`, `<FileVersion>` — all three |
| 3 | `aizen_search_installer.iss` | `MyAppVersion` |
| 4 | `license.txt` | the version line |
| 5 | `README.md` | any version shown in an example |
| 6 | `.github/workflows/release.yml` | the manual-dispatch default and the examples |
| 7 | `src/AizenSearch.App/Web/index.html` | **add a new changelog card** — do not put the version anywhere else in the page |
| 8 | `docs/BUILD-AND-RELEASE.md` | the example tag and version |

**Then:**

1. Mirror `index.html` to `Distribution/Portable/Web/`.
2. Bump the cache-buster while you are in there.
3. Commit.
4. Tag and push — the pipeline publishes the release:

```powershell
git tag v0.4.0
git push origin v0.4.0
```

**Watch out for:**

- **Do not put the version in `MainWindow.xaml`.** The title is set at runtime from the constant. Putting it in the XAML is exactly the mistake that produced the original bug.
- **Do not put the version in `index.html` outside the changelog card.** The badge, the About dialog and the header all get the version pushed into them at runtime.
- The changelog card is different: it is history, so it is written literally, and it is the one place in the page where a version string is correct.

---

# Part 3 — Build, packaging and documentation

## Recipe 13 — Change the installer

**File:** `aizen_search_installer.iss` (105 lines)

This is an Inno Setup script. It defines what gets installed, where, and what shortcuts are created.

**Common changes:**

- **What gets installed:** the `[Files]` section.
- **Shortcuts, desktop icon, startup entry, Explorer context menu:** the `[Tasks]` and `[Icons]` sections.
- **The version:** `MyAppVersion` — must match `CurrentVersion` (Recipe 12).
- **The product name shown by the installer:** `MyAppName`.

**Watch out for:**

- **The source root is resolved from the environment, not hard-coded.** The script uses `GetEnv('AIZEN_SOURCE_ROOT')` and falls back to a path relative to the script itself. Do not replace that with a personal directory path — that is precisely the leak that was cleaned out of this project once already.
- `MyAppVersion` is checked by the pipeline. A mismatch fails the build, by design.

## Recipe 14 — Change the release pipeline

**File:** `.github/workflows/release.yml`

**Watch out for:**

- **Editing a workflow file needs the `workflow` scope on your token.** A fine-grained token without that scope returns a 404 on the path, which is confusing — it looks like the file does not exist. It does; the permission is missing.
- **Do not weaken the version-consistency step.** Read rule 5 in `00-START-HERE.md` for why it exists.
- **Keep the signing step conditional.** It is skipped when the certificate secrets are absent, which is what lets releases work without a certificate.
- **Test changes on a branch.** The pipeline runs on tags and manual dispatch; a broken workflow discovered at release time is expensive.

## Recipe 15 — Update the documentation

When you change behaviour, the docs to update are:

| Changed | Update |
|:---|:---|
| The query language | `docs/SEARCH-SYNTAX.md`, the README's syntax table, and the in-app cheat sheet (`index.html`, the query-help panel) |
| Anything user-visible | `README.md`, and a changelog card in `index.html` |
| The architecture | `docs/ARCHITECTURE.md` — and regenerate the diagrams if the shape changed |
| The build or release process | `docs/BUILD-AND-RELEASE.md` |
| Anything about secrets or what is excluded | `docs/SECURITY-AND-SECRETS.md` |
| A file being added, removed or repurposed | `docs/manual/02-THE-CODEBASE.md` — **keep the map honest, it is the map** |

---

# Part 4 — Backlog features, with a starting plan

These are the features known to be wanted. Each entry says where to start, so the work is not a blank page.

## Full-text content search

**Where to start:** a new index alongside the file index — most likely an inverted index of word → file ids. It needs a new file format (so a new version in `BinaryCacheService`, or a separate file), a new extraction step in `IndexManager`, and a new query operator in `QueryParser`.

**The hard part is not the index — it is extraction.** Reading the text out of PDFs, Word documents and the rest is a large job on its own, and it is slow. Plan for it to run separately from the filename index, in the background, with its own progress reporting.

## Fuzzy matching

**Where to start:** `SearchEngine`, in the matching predicate. Levenshtein distance or a trigram index over filenames.

**The hard part is cost.** Fuzzy matching is dramatically more expensive than substring matching, so it cannot simply replace the existing path — it needs to be a toggle, and probably needs a pre-built structure (a trigram index) rather than a per-entry calculation.

## Installer improvements

**Where to start:** `aizen_search_installer.iss`.

Candidates: a per-user install option (no administrator prompt), silent-install switches for scripted deployment, and a proper upgrade-detection step that closes a running instance before replacing files.

## Drag and drop

**Where to start:** the results pane in `index.html` and `app.js`, plus a new bridge action for the native side.

Dropping files **onto** the window (to open their folder, or to search within them) is the easier half. Dragging results **out** to Explorer needs native support — WPF/WebView2 drag-and-drop, not HTML5 drag events.

## Folder operations

**Where to start:** new bridge actions in `IpcBridge.cs`, and menu items in `index.html`.

Creating, renaming, moving and deleting files or folders from within the app. **This is the change with the highest risk in the whole backlog** — these are destructive, irreversible operations on the user's real files. Any implementation needs a confirmation step, a clear error path, and a decision about whether to send files to the Recycle Bin rather than deleting them outright. Do not ship this without thinking about what happens when it goes wrong.

## Export expansion

**Where to start:** the existing `export_csv` action in `IpcBridge.cs`.

Candidates: JSON, plain text, and an HTML report. The existing CSV path is the template — it already solves choosing a destination and writing the file.

## Unicode and non-Latin filenames

**Where to start:** everywhere strings are compared or stored — `QueryParser`, `SearchEngine`, `FileEntry`, and the binary cache's string encoding.

The risks are case-insensitive comparison (which behaves differently across scripts), normalisation (the same visible name can have more than one byte sequence), and the cache's string encoding. Test with real Bangla, Arabic and CJK filenames, not with ASCII.

## Settings panel

**Where to start:** the interface — a new dialog in `index.html`, styled in `app.css`, driven from `app.js` — plus persistence, which means a new bridge action and somewhere to save.

Candidates to expose: which drives to index, excluded folders, whether to start elevated, the default view mode, the theme, and the preview size limit.

---

# Part 5 — The rules to never break

Collected in one place, because these are the mistakes that actually happened.

1. **The version number lives in exactly one place** — `UpdateService.CurrentVersion`. Never in `MainWindow.xaml`, never in `index.html` outside the changelog card.
2. **Never commit secrets, certificates, logs, build output, or personal paths.**
3. **`AizenSearch.Core` must never reference the app project** — no WPF, no WebView2.
4. **Mirror web asset changes to `Distribution/Portable/Web/` and bump the cache-buster.**
5. **Never weaken the version-consistency check in the pipeline.**
6. **Keep the engine's cheap-filter-first ordering** in `SearchEngine`.
7. **Keep index writes atomic** — temporary file, then rename.
8. **Bump the cache format version** whenever the stored shape changes.
9. **Do not add network calls.** The app makes exactly one, and users are told so.
10. **Do not rename the assemblies, namespaces, executable or cache folder** — the product name and the internal identifiers are deliberately different.
11. **Keep the index small:** no string fields on `FileEntry`.
12. **Keep tests passing, and add one when you change behaviour.**

---

## When you are done

```powershell
git add -A
git commit -m "Describe what changed and why"
git push -u origin my-change
```

Then open a pull request, or — if you are working alone on `main` — merge it and, if it is a user-visible change, tag a release (Recipe 12).

**If you changed anything user-visible, the work is not finished until it is released.** A change that only exists on your machine is not a change; the repository is the source of truth, and the release is how users get it.
