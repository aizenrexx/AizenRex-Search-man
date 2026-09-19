# Security policy

## Reporting a vulnerability

Please **do not** open a public issue for a security problem.

Use GitHub's private reporting instead: **Security → Report a vulnerability** on this repository. If that is unavailable, contact the maintainer directly through the profile at [@aizenrexx](https://github.com/aizenrexx).

Include what you can:

- what the problem is and where it is,
- how to reproduce it,
- what an attacker could do with it,
- the version you tested.

You will get an acknowledgement, and credit in the release notes once a fix ships — unless you would rather stay anonymous.

## Scope

In scope:

- the application (`src/AizenSearch.App`, `src/AizenSearch.Core`),
- the installer,
- the release pipeline in `.github/workflows`.

Out of scope:

- the unsigned-build SmartScreen warning — that is expected and documented in the README,
- issues that require an already-compromised machine,
- the archived `legacy_rust/` implementation, which is kept only for reference and is not built or shipped.

## Supported versions

Only the newest release receives fixes. Please confirm the problem still exists on the latest version before reporting.

## Our commitments

- No telemetry and no analytics. The application's only network request is the update check.
- No credentials, certificates, or machine-specific data in the repository.
- Builds are produced by the public pipeline, so what you download is what the source produces.
