# 4. Build, test and release

Everything about getting from source code to a published installer.

---

## What you need installed

| Requirement | Why | Where |
|:---|:---|:---|
| **.NET 9 SDK** | Builds the app and runs the tests | [dotnet.microsoft.com/download/dotnet/9.0](https://dotnet.microsoft.com/download/dotnet/9.0) — get the **SDK**, not just the runtime |
| **Git** | Version control | [git-scm.com](https://git-scm.com) |
| **Inno Setup 6** | Compiles the installer | [jrsoftware.org/isdl.php](https://jrsoftware.org/isdl.php) — **only needed if you are building the installer locally** |
| **Visual Studio 2022** or **VS Code** | An editor | Optional. `dotnet` on the command line is enough, and VS Code with the C# extension is lighter. |

You do **not** need the .NET Desktop Runtime separately — the SDK includes it.

Check your setup:

```powershell
dotnet --version
```

It should report a 9.x version. If it reports 8.x or lower, the SDK is not installed correctly.

---

## Building

### Build everything

```powershell
dotnet build AizenSearch.sln
```

The first build restores NuGet packages and takes a minute or two. Later builds are seconds.

### Run it from source

```powershell
dotnet run --project src/AizenSearch.App/AizenSearch.App.csproj
```

This launches the app with the web assets read from the source tree. It is the fastest way to see an interface change — edit `index.html` or `app.css`, close the window, run it again.

**A useful detail:** because the assets are read from disk, you can often see a CSS change by closing and reopening the window without rebuilding at all.

### Build a release

```powershell
dotnet build AizenSearch.sln -c Release
```

### Produce the portable build

This is the command that creates what goes into the ZIP:

```powershell
dotnet publish src/AizenSearch.App/AizenSearch.App.csproj -c Release -r win-x64 --self-contained false -o Distribution/Portable
```

**What the flags mean:**

| Flag | Meaning |
|:---|:---|
| `-c Release` | Optimised build |
| `-r win-x64` | Target 64-bit Windows |
| `--self-contained false` | Framework-dependent — the user needs the .NET 9 Desktop Runtime installed. This keeps the download around 1.5 MB instead of ~70 MB. |
| `-o Distribution/Portable` | Where the output goes |

**If you want a standalone build** that needs no runtime installed, use `--self-contained true`. Expect a much larger download. The shipped releases use framework-dependent on purpose.

---

## Testing

### Run the tests

```powershell
dotnet test AizenSearch.sln
```

You should see four test classes run and pass. A failure means do not release — the pipeline enforces this too.

### Run a single test file

```powershell
dotnet test tests/AizenSearch.Core.Tests/AizenSearch.Core.Tests.csproj --filter "FullyQualifiedName~QueryParserTests"
```

### What the tests cover

| File | Covers |
|:---|:---|
| `QueryParserTests.cs` | The query language — each operator parses correctly |
| `SearchEngineTests.cs` | Query execution — filters, combinations, exclusions, ordering |
| `BinaryCacheTests.cs` | The index file format — round-tripping, bad headers, incompatible versions |
| `VirtualDriveTests.cs` | Indexer behaviour against a synthetic drive layout |

### Why the tests run without a display

The test project references **`AizenSearch.Core` only** — never the app. The engine has no UI dependency, so the suite runs anywhere .NET runs, including a build server with no desktop. That is the practical payoff of rule 4 in `00-START-HERE.md`.

**If you ever find yourself needing to reference the app project from the tests, stop.** That means logic has ended up in the wrong project. Move it to the engine instead.

### Writing a new test

Follow the shape of the existing ones. A good test here:

- Uses a small synthetic fixture, not the real filesystem.
- Asserts on behaviour, not on internal fields.
- Has a name that says what it proves.

```csharp
[Fact]
public void Parser_reads_size_greater_than_with_unit()
{
    var q = QueryParser.Parse("size:>500mb");
    Assert.Equal(500L * 1024 * 1024, q.MinSize);
}
```

---

## Building the installer locally

Only needed if you want to test the installer without going through the pipeline.

### 1. Produce the portable build

```powershell
dotnet publish src/AizenSearch.App/AizenSearch.App.csproj -c Release -r win-x64 --self-contained false -o Distribution/Portable
```

### 2. Make sure the web assets are in the output

`dotnet publish` copies the `Web/` folder because the project file marks it as content. If the published output is missing it, check the project file's content items — and make sure `Distribution/Portable/Web/` is in sync with `src/AizenSearch.App/Web/`.

### 3. Compile the installer

```powershell
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" aizen_search_installer.iss
```

The result appears in the `Installer` folder.

### The installer script's source root

The script does **not** hard-code where your project lives. It resolves the source root like this:

```iss
#define MySourceRoot GetEnv('AIZEN_SOURCE_ROOT')
```

If that environment variable is not set, it falls back to a path derived from the script's own location. So either:

- set `AIZEN_SOURCE_ROOT` to your project folder, or
- run `ISCC.exe` from the project root, or
- pass the location in.

**Never replace this with a hard-coded personal path.** That is exactly the kind of leak that had to be cleaned out of this repository once already.

---

## Releasing — the normal way

**You do not build releases on your machine.** The pipeline does it, on a GitHub-hosted Windows runner, so a release works with your PC switched off.

### The short version

```powershell
git add -A
git commit -m "Describe the change"
git push origin main

git tag v0.4.0
git push origin v0.4.0
```

Pushing the tag starts the pipeline. About four minutes later the release is published with the installer and the portable ZIP attached.

### What the pipeline does

The workflow is `.github/workflows/release.yml`, named **Build and Release**. It runs on any tag matching `v*`, or manually from the **Actions** tab.

| # | Step | Notes |
|---:|:---|:---|
| 1 | Checkout | |
| 2 | Resolve version | From the tag, or from the manual input |
| 3 | Setup .NET 9 | |
| 4 | Ensure application icons | Generates the `.ico` if missing |
| 5 | Restore | NuGet |
| 6 | Build | Release configuration |
| 7 | Run tests | **A failure stops the release** |
| 8 | Publish portable build | `Distribution/Portable` |
| 9 | **Verify version is consistent everywhere** | **Fails the build on a mismatch** |
| 10 | Package portable ZIP | |
| 11 | Install Inno Setup | |
| 12 | Build installer | |
| 13 | Sign installer | **Skipped** unless the certificate secrets exist |
| 14 | Upload build artifacts | Attached to the run |
| 15 | Publish GitHub release | Creates the release and attaches both files |

### Before you tag — the pre-release checklist

1. **Bump the version everywhere** — follow Recipe 12 in `03-HOW-TO-MODIFY.md`. Do not skip a file; step 9 will fail the build.
2. **Mirror the web assets** to `Distribution/Portable/Web/` and bump the cache-buster.
3. **Run the tests locally** so you find a failure in seconds rather than in a four-minute pipeline run.
4. **Run the app and look at it.** Especially if you changed anything visual.
5. **Write the release notes** — see below.
6. **Commit and push to `main`** *before* tagging. The tag points at a commit, and the pipeline builds what is in the repository, not what is on your machine.

### Writing the release notes

The pipeline creates the release; the notes are the part worth writing carefully. A good set of notes for this project contains:

- **What changed, and why it matters** — in the user's terms, not the commit log.
- **The SHA256 of both files**, so people can verify their download.
- **The SmartScreen note**, if the build is unsigned.
- **A short table of the downloads.**

The release notes for v0.3.9 in this repository are a good template.

### Running the pipeline manually

**Actions** tab → **Build and Release** → **Run workflow** → enter the version without the leading `v` → run.

Useful when you want to test the pipeline itself without creating a tag.

---

## Releasing — when something goes wrong

### The build failed at "Verify version is consistent everywhere"

This is the most common failure, and it is working as intended. One of the files listed in Recipe 12 still has the old version. The step's output names the file and the values it found. Fix it, commit, and re-tag.

**Do not delete or weaken the check.** Read rule 5 in `00-START-HERE.md`.

### The build failed at "Run tests"

A test is failing. Run them locally, fix, commit, re-tag.

### The release was created but the assets are missing

Check the **Upload build artifacts** and **Publish GitHub release** steps. If the installer build failed, the release may have been created without it.

### A tag already exists and you need to replace it

```powershell
git tag -d v0.4.0                  # delete locally
git push origin :refs/tags/v0.4.0  # delete on the remote
git tag v0.4.0                     # re-tag at the current commit
git push origin v0.4.0
```

If a release was already published for that tag, delete it in the GitHub UI as well, otherwise the pipeline's release step will refuse.

**Prefer a new patch version over rewriting a tag.** Rewriting a tag that anyone may have already downloaded is worse than shipping 0.4.1.

### The pipeline ran but the app does not offer the update

Check, in order:

1. Does `/releases/latest` report the new version? (Not `/releases` — the *latest* endpoint, which is what the app reads.)
2. Does `UpdateService.RepoName` match the actual repository name?
3. Did the version in `CurrentVersion` actually change?
4. Is the release marked as a **pre-release**? The latest endpoint skips those.

---

## Code signing

The installer is currently **unsigned**, which is why Windows SmartScreen shows "Windows protected your PC" on first run. That is a missing signature, not a detection.

### Turning signing on

1. Get a code-signing certificate (an EV certificate avoids SmartScreen reputation-building entirely; a standard one builds reputation over time).
2. Base64-encode the `.pfx`:

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("certificate.pfx")) | Set-Clipboard
```

3. Add two repository secrets — **Settings → Secrets and variables → Actions**:
   - `WINDOWS_CERT_PFX_BASE64` — the base64 string
   - `WINDOWS_CERT_PASSWORD` — the certificate password
4. Nothing else. The next release is signed and timestamped automatically.

### Why the secrets are safe

Repository secrets are encrypted, are **not readable even by the repository owner** after saving, and are masked as `***` in workflow logs. This is the correct place for a certificate — not in the repository, not in a file.

**Never commit a certificate.** A committed certificate is public permanently, and the correct response is to revoke it, not to delete the file.

More on this in `docs/SECURITY-AND-SECRETS.md`.

---

## Building without a certificate

Nothing changes. Step 13 reports that it skipped signing, and the release is published unsigned. Users see the SmartScreen prompt and click **More info → Run anyway**.

This is the current, expected state.

---

## Useful commands, collected

```powershell
# build
dotnet build AizenSearch.sln

# build release
dotnet build AizenSearch.sln -c Release

# run from source
dotnet run --project src/AizenSearch.App/AizenSearch.App.csproj

# test
dotnet test AizenSearch.sln

# one test class
dotnet test --filter "FullyQualifiedName~SearchEngineTests"

# publish portable
dotnet publish src/AizenSearch.App/AizenSearch.App.csproj -c Release -r win-x64 --self-contained false -o Distribution/Portable

# compile the installer
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" aizen_search_installer.iss

# clean build output
dotnet clean AizenSearch.sln

# release
git tag v0.4.0
git push origin v0.4.0

# see what the app's update check will see
curl https://api.github.com/repos/aizenrexx/AizenRex-Search-man/releases/latest
```

---

## If you cannot build locally

You do not have to. Push your branch and open a pull request — the pipeline builds and tests it. Or run the workflow manually from the **Actions** tab.

The only thing you lose is speed: a local build failure takes seconds to see, a pipeline failure takes minutes.
