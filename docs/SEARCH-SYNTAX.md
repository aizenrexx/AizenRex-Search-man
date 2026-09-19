# Search syntax

The query box accepts plain words, or a combination of the operators below. Everything is case-insensitive unless you turn on **Match case** in the Search menu.

---

## Plain terms

Type one or more words and they are matched against the file name and path.

```
report
annual report
```

Several words are treated as a group — a file has to satisfy all of them.

---

## Exact phrases

Wrap a term in double quotes to match it as written, spaces included.

```
"annual report"
"season 2"
```

---

## Extension

```
ext:pdf
ext:pdf|docx
ext:.mp4
```

The leading dot is optional and a pipe matches several extensions. A query-level extension replaces any extension filter set in the interface.

---

## Kind

Filters by category rather than by extension. The aliases in the right column all work.

| Kind | Aliases |
|---|---|
| `type:file` | `file`, `files` |
| `type:folder` | `folder`, `folders`, `dir`, `directory` |
| `type:exe` | `exe`, `app`, `apps`, `application`, `applications` |
| `type:doc` | `doc`, `docs`, `document`, `documents` |
| `type:image` | `image`, `images`, `pic`, `pics`, `picture`, `pictures`, `photo`, `photos` |
| `type:audio` | `audio`, `music`, `sound` |
| `type:video` | `video`, `videos`, `movie`, `movies` |
| `type:archive` | `archive`, `archives`, `zip`, `compressed` |

```
type:video
type:image holiday
```

---

## Path

Normally a term matches the name first and the path second. `path:` forces the term to match the path.

```
path:downloads
path:steam report
```

---

## Size

Units are `b`, `kb`, `mb`, `gb`, and `tb`. The unit is optional and case does not matter.

| Form | Meaning |
|---|---|
| `size:>500mb` | larger than 500 MB |
| `size:<10mb` | smaller than 10 MB |
| `size:100mb-2gb` | between 100 MB and 2 GB |
| `size:700mb` | exactly 700 MB |

```
type:video size:>2gb
size:1gb-4gb
```

---

## Date

Matches the file's modified date.

| Form | Meaning |
|---|---|
| `date:after 2024-01-01` | modified after that date |
| `date:before 2024-06-01` | modified before that date |
| `date:between 2024-01-01 and 2024-06-01` | within the range |
| `date:2024` | any time in 2024 |
| `date:2024-03-15` | that specific day |

```
type:doc date:after 2024-01-01
date:between 2023-01-01 and 2023-12-31 report
```

---

## Exclusion

A leading `!` removes anything matching the term.

```
report !draft
type:video !sample
```

---

## Wildcards

`*` matches any run of characters, `?` matches a single character.

```
*.log
IMG_???.jpg
```

---

## Regular expressions

Turn on **Regex** in the Search menu, then write the pattern as the query.

```
^IMG_\d{4}
(mkv|mp4)$
```

An invalid pattern is ignored rather than failing the search.

---

## Menu toggles

| Toggle | Effect |
|---|---|
| **Match case** | Makes every term and phrase case-sensitive |
| **Whole word** | Only matches whole words, not substrings |
| **Search in path** | Always consider the full path, not just the name |
| **Regex** | Treat the query as a regular expression |
| **Preview** | Show or hide the preview pane |

---

## Worked examples

**A 4K movie larger than 20 GB:**

```
type:video 2160p size:>20gb
```

**Documents modified this year, excluding drafts:**

```
type:doc date:after 2024-01-01 !draft
```

**Every installer in Downloads:**

```
path:downloads ext:exe|msi
```

**Photos from a specific camera folder, shot in March:**

```
path:DCIM type:image date:between 2024-03-01 and 2024-03-31
```

**Log files matching a naming pattern:**

```
*.log !archive
```

---

## Interface filters

The toolbar also offers the same filters as controls: **min/max size**, **date before/after**, and the filter chips for Videos, Audio, Documents, Pictures, Applications, Archives, and Folders. A filter typed into the query box takes precedence over the equivalent control for that search.
