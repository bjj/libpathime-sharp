# Third-party notices — PathimeSharp.NativeAssets.win-x64

**STUB — must be completed before this package is ever published.** The
authoritative inventory is `libpathime/THIRD-PARTY.md`; this file must carry it
forward for every binary and data file the package ships, including the vcpkg
dependency closure. See TODO.md at the repo root.

The payload includes, at minimum:

- **libpathime** (`pathime.dll`) — MIT.
- **libhangul** (`hangul.dll`) — LGPL-2.1, built as a shared library.
- **anthy-unicode** (`anthy-unicode.dll`) — LGPL-2.1, shared; built from the
  fork at github.com/bjj/anthy-unicode (branch `libpathime`), which is the
  corresponding source.
- **pyzy** (`pyzy-1.0.dll`) — LGPL-2.1, shared; fork at github.com/bjj/pyzy
  (branch `libpathime`).
- **vcpkg-built dependencies** (`glib-2.0-0.dll`, `pcre2-8.dll`, `iconv-2.dll`,
  `intl-8.dll`, `sqlite3.dll`, …) — various licenses (LGPL-2.1, BSD,
  public-domain); enumerate the exact closure and licenses at packaging time.
- **pathime-data/anthy/anthy.dic** — compiled from GPL-2 sources.
- **pathime-data/table/*.db** — compiled from ibus-table-chinese tables, GPL-3.
- **pathime-data/pyzy/** — pyzy's database, LGPL-2.1.

The MSVC runtime question (ship `msvcp140.dll`/`vcruntime140*.dll` app-local
vs. require the VC++ Redistributable) is also unresolved; see TODO.md.
