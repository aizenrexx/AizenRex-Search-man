# Build and release

---

## Requirements

| | |
|---|---|
| .NET SDK | 9.0 or newer |
| OS (to build the desktop app) | Windows 10 or 11, x64 |
| Inno Setup | 6.x — only needed to compile the installer |
| Windows PowerShell | 5.1+ — only for the icon script |

Building the engine and running the tests needs nothing but the .NET SDK.

---

## Local development

```powershell
dotnet restore AizenSearch.sln
dotnet build   AizenSearch.sln -c Release

# run the app
dotnet run --project src/AizenSearch.App/AizenSearch.App.csproj

# run the tests
dotnet test AizenSearch.sln
```

---

## Producing the portable build

```powershell
dotnet publish src/AizenSearch.App/AizenSearch.App.csproj -c Release -r win-x64 `
  --self-contained false `
  -o Distribution/Portable
```

The build is framework-dependent, so the machine that runs it needs the .NET 9 Desktop Runtime. `Distribution/Portable/` is the complete runnable folder — zip it and it runs anywhere.

---

## Building the installer

The Inno Setup script resolves its source root in this order:

1. the `AIZEN_SOURCE_ROOT` environment variable, if set;
2. otherwise the folder the script itself lives in.

It never hard-codes a personal directory, so it compiles on any machine:

```powershell
$env:AIZEN_SOURCE_ROOT = (Get-Location).Path
$env:AIZEN_VERSION     = "0.3.9"
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" aizen_search_installer.iss
```

Output lands in `Distribution/Installer/`.

---

## Releases are built in the cloud

The local machine is never involved. The workflow at `.github/workflows/release.yml` runs on a Windows runner and performs, in order:

1. Checkout
2. Resolve the version (from the tag, or from the manual input)
3. Set up .NET 9
4. Ensure the application icons exist
5. Restore
6. Build
7. **Run the full test suite**
8. Publish the portable build
9. **Verify the version is consistent everywhere** — the build fails on a mismatch
10. Package the portable ZIP
11. Install Inno Setup
12. Compile the installer
13. Sign the installer (skipped unless a certificate is configured)
14. Upload the build artifacts
15. Publish the GitHub release

### Triggering a release

Tag and push:

```powershell
git tag v0.3.9
git push origin v0.3.9
```

Or run it manually from **Actions → Build and Release → Run workflow** and type the version.

Either way the release appears on the Releases page with the installer and the portable ZIP attached.

---

## The version rule

The version lives in **one place**: the `CurrentVersion` constant in `src/AizenSearch.Core/Services/UpdateService.cs`. Everything user-visible reads it at runtime — the window title, the in-app badge, and the About dialog.

When you bump a version, move these together:

| File | What changes |
|---|---|
| `src/AizenSearch.Core/Services/UpdateService.cs` | `CurrentVersion` — **the source of truth** |
| `src/AizenSearch.App/AizenSearch.App.csproj` | `Version`, `AssemblyVersion`, `FileVersion` |
| `aizen_search_installer.iss` | `MyAppVersion` |
| `license.txt` | the version line |
| `README.md` | the version in the title |
| `src/AizenSearch.App/Web/index.html` | add a changelog card for the new version |

Step 9 of the pipeline compares the constant against the version being released and **throws** if they disagree. This is deliberate: a build whose constant is behind its own tag cannot see its own update, so installed copies would silently never be offered it. Better to fail the build.

---

## Code signing

Builds are published unsigned, which is why Windows SmartScreen shows the "unrecognized app" prompt. Signing is already wired in and activates automatically once the certificate exists.

Add two repository secrets under **Settings → Secrets and variables → Actions**:

| Secret | Contents |
|---|---|
| `WINDOWS_CERT_PFX_BASE64` | your `.pfx` code-signing certificate, base64-encoded |
| `WINDOWS_CERT_PASSWORD` | the password for that `.pfx` |

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("certificate.pfx")) | Set-Clipboard
```

If the secrets are absent, step 13 logs a note and skips. If they are present, the installer is signed with SHA-256 and timestamped, and the prompt disappears for new downloads.

---

## Troubleshooting

**The build fails at "Verify version is consistent everywhere".**
`UpdateService.CurrentVersion` does not match the version being released. Bump the constant.

**The installer step fails with `ISCC.exe not found`.**
Inno Setup is missing, or it is installed somewhere other than the default path.

**The icon step fails.**
`src/AizenSearch.App/AizenRex.ico` should be committed. If it is missing, the workflow tries to generate it, which needs System.Drawing available to PowerShell.

**The app builds but shows the wrong version.**
Check that nothing re-introduced a hard-coded version into `MainWindow.xaml` or `index.html`. Both should be free of version numbers.
