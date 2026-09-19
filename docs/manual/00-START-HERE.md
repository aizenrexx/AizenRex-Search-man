# Read me first — the complete guide to AizenRex Search-man

This folder is the full, human-readable guide to **AizenRex Search-man**. It starts at "what is this thing" and ends at "how does a release get published". Nothing here assumes you already know the project.

It is written so that **anyone** can follow it: someone who has never opened the code, a new developer joining, or an AI assistant asked to change something. Read it in order the first time; after that, jump straight to whatever you need.

---

## The chapters

| File | What is in it |
|:---|:---|
| **`00-START-HERE.md`** | ← you are here. How to read this, and the rules that matter most. |
| **`01-HOW-IT-WORKS.md`** | What the software is, and how it actually works underneath — explained plainly first, then precisely. |
| **`02-THE-CODEBASE.md`** | Every folder and every file, with its line count and what it is responsible for. The map. |
| **`03-HOW-TO-MODIFY.md`** | **The most important chapter.** Copy-and-follow recipes for common changes, from changing a label to adding a search operator. |
| **`04-BUILD-TEST-RELEASE.md`** | Setting up a machine, building, testing, and how the installer and release are produced. |
| **`05-USER-GUIDE.md`** | Using the app: every menu, every shortcut, the full query language, and troubleshooting. |

Four shorter references also exist outside this folder, aimed at the public repository:

- [`../ARCHITECTURE.md`](../ARCHITECTURE.md) — the technical tour, with diagrams
- [`../SEARCH-SYNTAX.md`](../SEARCH-SYNTAX.md) — the query language, for users
- [`../BUILD-AND-RELEASE.md`](../BUILD-AND-RELEASE.md) — the release pipeline in detail
- [`../SECURITY-AND-SECRETS.md`](../SECURITY-AND-SECRETS.md) — what must never be committed

---

## Three ways to read this

**If you only want to use the app** — read `01`, then `05`. That is everything a user needs.

**If you want to change something** — read `01` to get the shape of the system, skim `02` to find the file that owns the thing you want to change, then go to `03-HOW-TO-MODIFY.md` and find the closest recipe. Chapter 3 is written to be copied from.

**If you are an AI assistant picking this project up cold** — read `00`, `01`, `02`, `03` and `04` before touching anything. Chapter 2 is the map that stops you editing the wrong place. Chapter 3 contains the rules that are easy to break without noticing, such as the version number living in exactly one file.

---

## The rules that matter most

If you remember nothing else, remember these. Each one exists because breaking it caused a real problem.

### 1. The version number lives in exactly one place

`UpdateService.CurrentVersion`, in `src/AizenSearch.Core/Services/UpdateService.cs`.

The window title, the version badge in the status bar, and the About dialog all read that constant **at runtime**. Never write a version number into `MainWindow.xaml`, into `index.html`, or anywhere else a user can see it.

> Why: the title bar and the About dialog used to carry their own hard-coded version strings, and they drifted apart — the title said v0.3.6 while the About dialog said v0.3.7. Nobody notices that kind of bug until a user reports it.

The release pipeline now **fails the build** if the constant and the release version disagree.

### 2. The packaged web assets must stay byte-identical to the source ones

After editing anything under `src/AizenSearch.App/Web/`, copy it to `Distribution/Portable/Web/`, and bump the cache-buster in `index.html` (`css/app.css?v=N` becomes `?v=N+1`).

> Why: WebView2 caches aggressively. A stale stylesheet looks exactly like a broken change and can waste hours.

### 3. Nothing private is ever committed

No credentials, tokens, certificates, logs, build output, or paths containing a personal directory. Scripts must resolve paths relative to the project folder — keep it that way.

> Why: the repository is public. Anything committed is in the history permanently; deleting the file later does not remove it.

### 4. The engine must not depend on the interface

`AizenSearch.Core` must never reference WPF, WebView2, or anything from `AizenSearch.App`. That separation is what makes the engine testable without opening a window, and it is why the tests can run on a build server with no display.

### 5. A build whose version is behind its own tag is worse than a failed build

If `CurrentVersion` says `0.3.8` but the release is tagged `v0.3.9`, the released app will not offer its own update — installed copies silently never see it. Failing the build is the correct behaviour. Do not "fix" that check by removing it.

---

## The two names, and why they differ

The product is called **AizenRex Search-man**.

The code, the assemblies, the namespaces, the cache folder and the executable are still called **AizenSearch**.

This is **intentional and must not be tidied up**. Renaming the assemblies would orphan every existing installation, break the upgrade path, and make saved indexes unreadable. A user-facing name and internal identifiers are allowed to differ; that is a normal compatibility decision, not an oversight.

| | Product name | Internal identifier |
|:---|:---|:---|
| Repository | AizenRex-Search-man | — |
| Installer file | `AizenRex-Search-man-Setup-vX.Y.Z.exe` | — |
| Executable | — | `AizenSearch.App.exe` |
| Assemblies | — | `AizenSearch.App.dll`, `AizenSearch.Core.dll` |
| Namespaces | — | `AizenSearch.App.*`, `AizenSearch.Core.*` |
| Cache folder | — | `%LOCALAPPDATA%\AizenSearch` |
| Solution | — | `AizenSearch.sln` |

---

## Where things live

| | |
|:---|:---|
| **Repository** | `https://github.com/aizenrexx/AizenRex-Search-man` |
| **Local working copy** | Your own clone of this repository, in a folder named `AizenRex Search-man` |
| **Releases** | `https://github.com/aizenrexx/AizenRex-Search-man/releases` |
| **Issues** | `https://github.com/aizenrexx/AizenRex-Search-man/issues` |
| **Default branch** | `main` |

The repository is the **single source of truth**. If a change is not committed and pushed, it does not exist — it is lost the moment the machine changes.

---

## The short version of the whole system

1. The app starts and looks for a saved index in `%LOCALAPPDATA%\AizenSearch\index.bin`.
2. If there is none, or if you ask for a rebuild, it enumerates every file on each fixed drive — the fast way is reading the NTFS Master File Table through the USN journal; the fallback is walking directories in parallel.
3. Every file becomes a `FileEntry` and is held in memory, with directory strings stored once in a shared pool instead of repeated per file.
4. The index is written to disk as a compact binary cache so the next launch is instant.
5. A filesystem watcher keeps the in-memory index current as files appear and disappear.
6. The interface is HTML/CSS/JS inside WebView2. Every click sends a small JSON message over a bridge; the native side does the work and pushes JSON results back.
7. Searches never touch the disk. They run against the in-memory index, are ranked, and only the rows actually on screen are drawn.

Details, diagrams and the reasoning behind each choice are in `01-HOW-IT-WORKS.md`.
