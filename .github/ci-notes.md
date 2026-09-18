# Cloud release pipeline (setup notes)

Ready-to-use: the pipeline is available in this repository. It builds, tests, packages and
releases AizenRex Search-man entirely in the cloud - no local PC required.

## Activate it (about a minute)

1. On GitHub, open **Add file -> Create new file**.
2. Set the file name to exactly: `.github/workflows/release.yml`
3. Open `.github/ci-notes.md` (this file) or `cloud-release-workflow.yml.staged`, copy the
   pipeline below/inside, paste it in, and commit.
4. The **Actions** tab then shows **Build and Release**.

## How it works

- Tag a commit (`git tag v0.3.7 && git push origin v0.3.7`) and GitHub builds it, runs the
  tests, makes the portable ZIP plus the Inno Setup installer, and attaches both to that tag's
  Release.
- Manual run: **Actions -> Build and Release -> Run workflow**.
- The app's update check reads `/releases/latest`, so publishing a release also switches
  in-app auto-update on.
- `aizen_search_installer.iss` and `scripts/generate_ico.ps1` read optional
  `AIZEN_SOURCE_ROOT` / `AIZEN_VERSION` variables and otherwise fall back to the original local
  paths and version, so local builds are unchanged.
