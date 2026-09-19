# Security and secrets

This repository is public. This page explains what is deliberately kept out of it, and how to keep something private when you genuinely need it on GitHub.

---

## What is never committed

| Category | Examples | Where it lives instead |
|---|---|---|
| Credentials | API keys, access tokens, passwords | GitHub Actions secrets, or your own password manager |
| Certificates | `.pfx`, `.pem`, `.key`, `.snk` | GitHub Actions secrets (base64-encoded) |
| Build output | `bin/`, `obj/`, `.exe`, `.dll`, `.pdb` | Rebuilt from source; shipped as release assets |
| Logs and dumps | `*.log`, `*.dmp`, `*.bak` | Nowhere — they are disposable |
| Machine-specific paths | `H:\My Project Coding\...`, `C:\Users\<name>\...` | Never. Scripts resolve paths relative to the project. |
| Local settings | `appsettings.Local.json`, `.env`, `secrets.json` | Environment variables or secrets |

`.gitignore` enforces all of this. It is not a suggestion — anything matching it cannot be committed by accident.

---

## How secrets are actually stored

### 1. GitHub Actions secrets — for anything the build pipeline needs

**Settings → Secrets and variables → Actions → New repository secret.**

A secret stored this way:

- is encrypted at rest by GitHub;
- is **not** readable in the repository, in the file tree, or through the API — not even by you as the owner. Once saved, GitHub will never show you the value again, only its name;
- is masked as `***` in workflow logs;
- is available to workflow runs as `${{ secrets.NAME }}`.

This is how the code-signing certificate is handled: `WINDOWS_CERT_PFX_BASE64` and `WINDOWS_CERT_PASSWORD`.

**One important limit:** GitHub *does* hide secret values from everyone including you, but a workflow can still print a secret if the workflow is written to. So keep the secret out of `Write-Host` and out of anything that echoes its value. The signing step in this repository writes the certificate to a temporary file, uses it, and deletes it.

### 2. Private repository — for source you do not want public at all

If you would rather the whole project were not public, change it under **Settings → General → Danger Zone → Change visibility**. Then only you and the collaborators you invite can see anything.

### 3. GitHub encrypted secrets vs. a private repository

These solve different problems:

| | Public repo + Actions secrets | Private repo |
|---|---|---|
| Source code | Visible to everyone | Visible only to you |
| A key the build needs | Hidden and encrypted | Could sit in a file, still not public |
| Downloadable releases | Anyone can download | Only you and collaborators |

**The correct pattern for this project is: public repository, no secrets in files, pipeline secrets in Actions secrets.** That is what is set up now.

---

## What is *not* hidden by GitHub

Be clear about these, because they are the usual way secrets leak:

- **Anything committed to the repository is permanent.** GitHub keeps history. Deleting a file in a later commit does **not** remove it from the history — it can still be read from an earlier commit. If a real secret was ever committed, treat it as compromised and **rotate it**; do not just delete the file.
- **A token pasted into a chat, an issue, a commit message, or a code comment** is exposed. Anyone who can read that text has the token.
- **A value written into a workflow file** is public, because workflow files are part of the repository.

---

## If a secret is exposed

1. **Revoke it immediately** at the provider. For a GitHub token: **Settings → Developer settings → Personal access tokens →** delete the token. Revoking first means the leaked value is worthless.
2. **Create a replacement** with the narrowest scope that works.
3. **Store the replacement** as an Actions secret — never in a file.
4. If it was committed, rotate rather than delete, and consider rewriting history.

---

## Access tokens for automation

When a workflow or an external tool needs to act on this repository, prefer:

- **`GITHUB_TOKEN`** — created automatically for every workflow run, scoped to this repository, expires when the run ends. The release pipeline uses this and needs nothing else.
- A **fine-grained personal access token** only when something outside Actions needs access. Give it the minimum permissions — for this repository that means **Contents: Read and write**, plus **Workflows: Read and write** only if the token must write workflow files — set an expiry, and store it as an Actions secret.

Never paste a token into a chat, an issue, or a file.

---

## What the application does on the network

Exactly one request: an update check against

```
https://api.github.com/repos/aizenrexx/AizenRex-Search-man/releases/latest
```

It reads the version tag and the download link and nothing else. No telemetry, no analytics, no account, no upload. The index and settings stay in `%LOCALAPPDATA%\AizenSearch` on your own machine.
