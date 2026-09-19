<div align="center">

# AizenRex Search-man

**An ultra-fast native file search engine for Windows.**
Built on the NTFS Master File Table, driven by a modern web interface, packaged as a single desktop app.

[![Build and Release](https://github.com/aizenrexx/AizenSearch/actions/workflows/release.yml/badge.svg)](https://github.com/aizenrexx/AizenSearch/actions/workflows/release.yml)
[![Latest release](https://img.shields.io/github/v/release/aizenrexx/AizenSearch)](https://github.com/aizenrexx/AizenSearch/releases/latest)
[![License](https://img.shields.io/badge/license-Proprietary-blue)](license.txt)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11%20x64-lightgrey)]()

[Download](#download--install) · [Features](#features) · [Search syntax](docs/SEARCH-SYNTAX.md) · [Architecture](docs/ARCHITECTURE.md) · [Build & release](docs/BUILD-AND-RELEASE.md)

</div>

---

## What it is

AizenRex Search-man indexes your drives by reading the NTFS **Master File Table** directly, so an entire disk is searchable in seconds instead of minutes. Results appear as you type, and a WebView2 front end renders them in list, card, or compact view with instant previews.

It is a **.NET 9 desktop application** — no service to install, no background indexing daemon, no cloud account.

| | |
|---|---|
| **Language / runtime** | C# 13 on .NET 9 (`net9.0-windows`) |
| **Interface** | WPF shell hosting Microsoft Edge WebView2 |
| **Index source** | NTFS MFT / USN Journal via `FSCTL_ENUM_USN_DATA`, with a parallel directory crawl fallback |
| **Index storage** | Binary cache at `%LOCALAPPDATA%\AizenSearch\index.bin` |
| **Installer** | Inno Setup 6, per-machine, in-place upgrades |
| **Tests** | xUnit |

---

## Download & install

Grab the newest build from the [**Releases**](https://github.com/aizenrexx/AizenSearch/releases/latest) page.

| File | What it is |
|---|---|
| `AizenRex-Search-man-Setup-vX.Y.Z.exe` | Installer. Installs for all users, adds Start Menu entries, and offers optional desktop icon, launch-on-startup, and an Explorer folder context menu. Installing over an older version upgrades in place and keeps your index and settings. |
| `AizenRex-Search-man-vX.Y.Z-Portable.zip` | Portable build. Unzip anywhere and run `AizenSearch.App.exe`. Writes nothing outside its own folder except the index cache. |

**Requirement:** the [.NET 9 Desktop Runtime (x64)](https://dotnet.microsoft.com/download/dotnet/9.0). The build is framework-dependent, which keeps the download small.

### Verify your download

Every release prints the SHA256 of both files. Check yours before running it:

```powershell
Get-FileHash .\AizenRex-Search-man-Setup-vX.Y.Z.exe -Algorithm SHA256
```

The result must match the release page exactly. If it does not, do not run the file.

### About the Windows "unrecognized app" warning

Builds are **not code-signed**, so SmartScreen may show a blue *"Windows protected your PC"* box the first time. That warning means the file has no signature — not that anything was detected in it.

Click **More info** → **Run anyway**. It stops appearing once the download builds reputation.

Removing it permanently needs a code-signing certificate. The pipeline is already wired for one: add repository secrets `WINDOWS_CERT_PFX_BASE64` and `WINDOWS_CERT_PASSWORD` and every future release is signed and timestamped automatically. Details in [docs/SECURITY-AND-SECRETS.md](docs/SECURITY-AND-SECRETS.md).

---

## Features

**Searching**
- Live results as you type, over an index of the whole drive
- MFT/USN-based indexing — millions of entries in seconds, with a directory-crawl fallback for non-NTFS volumes
- Advanced query syntax: `ext:`, `type:`, `path:`, `size:`, `date:`, `!exclude`, `"exact phrases"`, `*`/`?` wildcards, and full regular expressions
- Match case, whole-word, and search-in-path toggles
- Relevance ranking, plus sort by name, path, or type
- Filter chips for Videos, Audio, Documents, Pictures, Applications, Archives, and Folders

**Working with results**
- List, card, and compact view modes
- Instant preview pane for images, text, and metadata
- Open, open containing folder, copy path, copy name, copy file hash, and Properties
- Export results to CSV
- Find duplicate files
- Right-click context menu with all of the above

**The app itself**
- Single-instance: opening a second copy forwards its arguments to the running one
- Real-time index updates via a filesystem watcher
- Light and dark themes
- Manual elevation ("Enable MFT / Admin") for raw MFT access
- In-app update check against this repository's latest release
- Built-in version history and query-help dialogs
- Memory-conscious defaults for low-spec machines

---

## Search syntax

| Syntax | Meaning | Example |
|---|---|---|
| `word` | Match anywhere in the name or path | `report` |
| `"exact phrase"` | Match the phrase as written | `"annual report"` |
| `ext:pdf` | Only this extension (pipe for several) | `ext:pdf\|docx` |
| `type:video` | Filter by kind: `file`, `folder`, `exe`, `doc`, `image`, `audio`, `video`, `archive` | `type:image` |
| `path:downloads` | Force the term to match the path | `path:downloads` |
| `size:>500mb` | Size filter — `>`, `<`, `min-max`, or an exact value | `size:100mb-2gb` |
| `date:after 2024-01-01` | Date filter — `after`, `before`, `between X and Y`, or a bare year | `date:2024` |
| `!term` | Exclude anything matching the term | `!backup` |
| `*.log` | Wildcard match | `*.tmp` |
| `regex:` *(menu toggle)* | Treat the query as a regular expression | `^IMG_\d{4}` |

Full reference with worked examples: **[docs/SEARCH-SYNTAX.md](docs/SEARCH-SYNTAX.md)**

---

## Repository layout

```
AizenSearch/
├── src/
│   ├── AizenSearch.Core/          Indexing, search, storage, services
│   └── AizenSearch.App/           WPF shell + WebView2 front end
│       └── Web/                   HTML, CSS, JavaScript (the interface)
├── tests/                         xUnit test project
├── Distribution/Portable/         Packaged portable build
├── scripts/                       Icon generation and screenshot helpers
├── legacy_rust/                   Archived original Rust implementation
├── docs/                          Architecture, syntax, build, security
└── .github/workflows/release.yml  Cloud build and release pipeline
```

A guided tour of the codebase is in **[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)**.

---

## Build and run

```powershell
# run from source
dotnet run --project src/AizenSearch.App/AizenSearch.App.csproj

# run the tests
dotnet test AizenSearch.sln

# produce the portable build
dotnet publish src/AizenSearch.App/AizenSearch.App.csproj -c Release -r win-x64 `
  --self-contained false -o Distribution/Portable
```

### Releases are built in the cloud

Tagging is all it takes — the local machine is not involved:

```powershell
git tag v0.3.9
git push origin v0.3.9
```

GitHub Actions then restores, builds, runs the full test suite, publishes the portable build, packages the ZIP, compiles the installer, and publishes the release. You can also run it manually from the Actions tab.

Full details, including the version rules: **[docs/BUILD-AND-RELEASE.md](docs/BUILD-AND-RELEASE.md)**

---

## Security

- The application makes exactly one network call: an update check against this repository's latest release.
- Your index and settings stay in `%LOCALAPPDATA%\AizenSearch` and are never uploaded anywhere.
- No credentials, certificates, tokens, or machine-specific paths are stored in this repository. See [`.gitignore`](.gitignore) and [docs/SECURITY-AND-SECRETS.md](docs/SECURITY-AND-SECRETS.md).
- Found a problem? See [SECURITY.md](SECURITY.md).

---

## About the name

The user-facing product is **AizenRex Search-man**. Internal assembly names, namespaces, the cache folder, and the executable keep their original `AizenSearch` identifiers on purpose — changing them would break existing installs, upgrade paths, and saved indexes.

---

<div align="center">

**Built by Riyad (Aizen)** · [Report an issue](https://github.com/aizenrexx/AizenSearch/issues) · [License](license.txt)

</div>
