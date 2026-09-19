<div align="center">

# AizenRex Search-man — the complete manual

**Everything about this software, from "what is it" to "how does a release get published".**

</div>

---

## Start here

**[→ `00-START-HERE.md`](00-START-HERE.md)**

It explains how to read the manual, the five rules that matter most, and why the product name and the internal code name are deliberately different.

---

## The chapters

| # | Chapter | Read it if you want to… |
|:---|:---|:---|
| **00** | [**Start here**](00-START-HERE.md) | know how to use this manual and the rules you must not break |
| **01** | [**How it works**](01-HOW-IT-WORKS.md) | understand the whole system — the idea, the indexing, the searching, the interface, the limits |
| **02** | [**The codebase**](02-THE-CODEBASE.md) | find which file owns the thing you want to change |
| **03** | [**How to modify**](03-HOW-TO-MODIFY.md) | actually make a change — 15 recipes, from a colour to a new search operator |
| **04** | [**Build, test and release**](04-BUILD-TEST-RELEASE.md) | build it, test it, or publish a release |
| **05** | [**User guide**](05-USER-GUIDE.md) | use the app — every menu, every shortcut, the full query language, troubleshooting |

---

## The short tour

**AizenRex Search-man** is a Windows file-search application. Instead of asking Windows about files one at a time, it reads the whole NTFS **Master File Table** in bulk, holds the result in memory, and answers searches from there. The disk is read once; searching never touches it.

| | |
|:---|:---|
| **Built with** | C# on .NET 9 · WPF shell · WebView2 interface |
| **Indexes** | NTFS MFT / USN journal, with a parallel directory-crawl fallback |
| **Interface** | Plain HTML, CSS and JavaScript — no framework, no build step |
| **Tests** | xUnit, run on every release build |
| **Releases** | Built and published by GitHub Actions |
| **Network calls** | Exactly one: an update check against this repository |

---

## The three ways to read this

**You just want to use the app.**
Read [`01-HOW-IT-WORKS.md`](01-HOW-IT-WORKS.md) for what it is, then [`05-USER-GUIDE.md`](05-USER-GUIDE.md). That is everything a user needs.

**You want to change something.**
Read [`01`](01-HOW-IT-WORKS.md) for the shape of the system, skim [`02-THE-CODEBASE.md`](02-THE-CODEBASE.md) to find the file that owns the behaviour, then go to [`03-HOW-TO-MODIFY.md`](03-HOW-TO-MODIFY.md) and find the closest recipe. Chapter 3 is written to be copied from.

**You are an AI assistant picking this up cold.**
Read `00`, `01`, `02`, `03` and `04` before touching anything. Chapter 2 is the map that stops you editing the wrong place. Chapter 3 lists the rules that are easy to break without noticing — the version number living in exactly one file being the most common.

---

## The rules that matter most

Collected here because every one of them exists because breaking it caused a real problem.

1. **The version number lives in exactly one place** — `UpdateService.CurrentVersion`. Never in `MainWindow.xaml`, never in `index.html` outside the changelog card.
2. **Never commit secrets, certificates, logs, build output, or personal paths.**
3. **`AizenSearch.Core` must never reference the app project** — no WPF, no WebView2.
4. **Mirror web asset changes to `Distribution/Portable/Web/` and bump the cache-buster.**
5. **Never weaken the version-consistency check in the pipeline.**
6. **Keep the engine's cheap-filter-first ordering** in `SearchEngine`.
7. **Keep index writes atomic** — temporary file, then rename.
8. **Bump the cache format version** whenever the stored shape changes.
9. **Do not add network calls.** The app makes exactly one, and users are told so.
10. **Do not rename the assemblies, namespaces, executable or cache folder** — the product name and the internal identifiers are deliberately different.
11. **Keep the index small** — no string fields on `FileEntry`.
12. **Keep tests passing, and add one when you change behaviour.**

---

## The other references

These sit outside this folder, aimed at the public repository:

| Document | What it is |
|:---|:---|
| [`../ARCHITECTURE.md`](../ARCHITECTURE.md) | The technical tour, with diagrams |
| [`../SEARCH-SYNTAX.md`](../SEARCH-SYNTAX.md) | The query language, for users |
| [`../BUILD-AND-RELEASE.md`](../BUILD-AND-RELEASE.md) | The release pipeline in detail |
| [`../SECURITY-AND-SECRETS.md`](../SECURITY-AND-SECRETS.md) | What must never be committed |
| [`../../README.md`](../../README.md) | The public front page |
| [`../../CONTRIBUTING.md`](../../CONTRIBUTING.md) | How to contribute |
| [`../../SECURITY.md`](../../SECURITY.md) | How to report a vulnerability |

---

<div align="center">

**AizenRex Search-man** · Professional Edition

Built by Riyad (Aizen)

</div>