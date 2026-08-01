# Third-party notices — PathimeSharp.NativeAssets.linux-x64

**STUB — must be completed before this package is ever published.** The
authoritative inventory is `libpathime/THIRD-PARTY.md`; this file must carry it
forward for every binary and data file the package ships. See TODO.md at the
repo root.

The payload includes, at minimum:

- **libpathime** (`libpathime.so`, `libpathime.so.0`) — MIT.
- **libhangul** (`libhangul.so.1`) — LGPL-2.1, built as a shared library.
- **anthy-unicode** (`libanthy-unicode.so.0`, `libanthydic-unicode.so.0`,
  `libanthyinput-unicode.so.0`) — LGPL-2.1, shared; built from the fork at
  github.com/bjj/anthy-unicode (branch `libpathime`), which is the
  corresponding source.
- **pyzy** (`libpyzy-1.0.so.1`) — LGPL-2.1, shared; fork at
  github.com/bjj/pyzy (branch `libpathime`).
- **pathime-data/anthy/anthy.dic** — compiled from GPL-2 sources.
- **pathime-data/table/*.db** — compiled from ibus-table-chinese tables, GPL-3.
- **pathime-data/pyzy/** — pyzy's database, LGPL-2.1.

Unlike win-x64 there is no vcpkg closure: glib, sqlite3 and libuuid are the
distro's own packages, loaded from the system, not shipped.
