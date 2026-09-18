# AizenRex Search-man v0.3.8 (.NET 9)

An ultra-fast native Windows file search engine written in **C# (.NET 9)** with clean architecture and separate modular components.

## Architecture

- **`src/AizenSearch.Core/`** (internal compatibility project name retained):
  - `Models/`: File metadata, search queries, preview data, index progress events.
  - `Indexing/`: Direct NTFS Master File Table (MFT) / USN Journal reader via Win32 `FSCTL_ENUM_USN_DATA` with parallel directory crawl fallback.
  - `Search/`: Advanced syntax parser (`ext:`, `type:`, `path:`, `!exclude`, quotes, wildcard, regex) and relevance scoring.
  - `Storage/`: High-performance binary cache (`%LOCALAPPDATA%\\AizenSearch\\index.bin`) with atomic persistence.
  - `Services/`: Preview generation, native Win32 icon extraction, single-instance mutex, and elevation management.

- **`src/AizenSearch.App/`** (internal compatibility project name retained):
  - Desktop UI hosting Microsoft Edge WebView2.
  - Separated Web frontend assets under `Web/`.

- **`tests/AizenSearch.Core.Tests/`**:
  - xUnit automated tests for query parsing, search filtering, ranking, and cache persistence.

- **`legacy_rust/`**:
  - Preserved backup of the original Rust implementation.


## Download & Install

Grab the latest build from the [**Releases**](https://github.com/aizenrexx/AizenSearch/releases) page.

| File | What it is |
|---|---|
| `AizenRex-Search-man-Setup-vX.Y.Z.exe` | Inno Setup installer. Installs for all users, creates Start Menu entries, and offers optional desktop icon / launch-on-startup / Explorer context menu. Installing over an older version upgrades in place and keeps your index and settings. |
| `AizenRex-Search-man-vX.Y.Z-Portable.zip` | Portable build. Unzip anywhere and run `AizenSearch.App.exe`. No installation, nothing written outside the folder. |

**Requirement:** the [.NET 9 Desktop Runtime (x64)](https://dotnet.microsoft.com/download/dotnet/9.0) — the build is framework-dependent, which keeps the download small.

### Verifying your download

Every release lists the SHA256 of both files. To check yours:

```powershell
Get-FileHash .\AizenRex-Search-man-Setup-v0.3.8.exe -Algorithm SHA256
```

The result must match the checksum printed on the release page. If it does not, do not run the file.

### About the Windows "unrecognized app" warning

These builds are **not code-signed**, so Windows SmartScreen may show a blue *"Windows protected your PC"* box the first time you run the installer. That warning is about the missing signature, not about detected malware.

To continue:

1. Click **More info**
2. Click **Run anyway**

Once you run it a few times, or once the download builds reputation, the prompt stops appearing.

**Removing the warning entirely requires a code-signing certificate** (an EV or OV certificate from a trusted authority). The release pipeline is already set up to sign automatically: add the certificate as repository secrets named `WINDOWS_CERT_PFX_BASE64` (the .pfx file, base64-encoded) and `WINDOWS_CERT_PASSWORD`, and every future release will be signed and timestamped without any other change. Unsigned builds keep working if those secrets are absent.

## Security

- No credentials, certificates, tokens or machine-specific paths are stored in this repository — see `.gitignore`.
- The app makes exactly one network call: an update check against this repository's latest release.
- Your index and settings live in `%LOCALAPPDATA%\AizenSearch` and are never uploaded anywhere.

## Run & Build

### Development
```powershell
dotnet run --project src/AizenSearch.App/AizenSearch.App.csproj
```

### Run Tests
```powershell
dotnet test AizenSearch.sln
```

### Publish Release
```powershell
dotnet publish src/AizenSearch.App/AizenSearch.App.csproj -c Release -r win-x64 --self-contained false -o Distribution\\Portable
```

The user-facing product name is **AizenRex Search-man**. Internal assembly, namespace, cache-folder, repository, and executable identifiers remain compatibility identifiers so existing updates, indexes, preferences, and upgrades are not broken.
