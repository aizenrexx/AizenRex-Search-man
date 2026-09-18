# AizenRex Search-man v0.3.6 (.NET 9)

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