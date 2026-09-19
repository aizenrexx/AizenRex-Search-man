# Contributing

Thanks for taking a look. This is a personal project, so the most useful thing you can do is open a clear issue. If you want to send code, the notes below will save us both time.

---

## Before you start

- **Bugs and feature requests** → [open an issue](https://github.com/aizenrexx/AizenSearch/issues). Say what you expected, what happened, and which version you are on.
- **Security problems** → follow [SECURITY.md](SECURITY.md), not a public issue.

---

## Getting set up

```powershell
git clone https://github.com/aizenrexx/AizenSearch.git
cd AizenSearch
dotnet restore AizenSearch.sln
dotnet build   AizenSearch.sln -c Release
dotnet test    AizenSearch.sln
```

You need the .NET 9 SDK. Building the desktop app additionally needs Windows; the engine and the tests do not.

---

## The rules that matter

### 1. The version lives in one place

Never hard-code a version number into `MainWindow.xaml`, `index.html`, or anywhere else that a user can see. Everything reads `UpdateService.CurrentVersion` at runtime. If you need the version in a new place, push it from the native side the way `appInfo` does.

The release pipeline fails on a mismatch, on purpose.

### 2. Keep the packaged web assets in sync

After editing anything under `src/AizenSearch.App/Web/`:

1. copy the file to `Distribution/Portable/Web/`,
2. bump the cache-buster in `index.html` — `css/app.css?v=N` becomes `?v=N+1`.

WebView2 caches hard. A stale stylesheet behaves exactly like a broken change.

### 3. Never commit anything private

No credentials, tokens, certificates, logs, build output, or paths containing a personal directory. See [docs/SECURITY-AND-SECRETS.md](docs/SECURITY-AND-SECRETS.md). Scripts resolve paths relative to the project — keep it that way.

### 4. Tests stay green

`dotnet test` must pass. If you add behaviour to the parser, the engine, the ranker, or the cache, add a test for it in `tests/AizenSearch.Core.Tests/`.

### 5. The engine stays UI-free

`AizenSearch.Core` must not reference WPF, WebView2, or anything else in `AizenSearch.App`. That separation is what makes the engine testable.

---

## Submitting a change

1. Branch from `main`.
2. Keep the change focused — one topic per pull request.
3. Make sure `dotnet test` passes.
4. Describe what changed and why, and mention anything you deliberately did not touch.

---

## Style

- Match the surrounding code. It is consistent.
- Comments explain *why*, not *what*. The code already says what.
- Prefer the smallest change that solves the problem.
