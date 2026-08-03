# Third-party notices — PathimeSharp.NativeAssets.linux-x64

What this package ships and under what terms, carried forward from
libpathime's `THIRD-PARTY.md` (the authoritative inventory, in the libpathime
repository). The licence text of every component named below is in this
package's `licenses/` directory, staged from the same libpathime install the
binaries came from.

**The package as a whole is GPL-3.** It ships `table/*.db` (GPL-3) and
`anthy/anthy.dic` (GPL-2) alongside the MIT and LGPL binaries, and the data is
most of the bytes. A consumer who wants different terms builds their own
libpathime with `LIBPATHIME_WITH_ANTHY=OFF` and/or `LIBPATHIME_WITH_TABLE=OFF`
and stages their own native package — with both off, `pathime-data/` holds
only pyzy's LGPL files.

## Binaries (`runtimes/linux-x64/native/`)

| File | Component | Licence | Text |
|---|---|---|---|
| `libpathime.so`, `libpathime.so.0.1` | libpathime | MIT | `licenses/libpathime.txt` |
| `libhangul.so.1` | libhangul, upstream unmodified (`github.com/libhangul/libhangul`) | LGPL-2.1 | `licenses/libhangul.txt` |
| `libanthy-unicode.so.0`, `libanthydic-unicode.so.0`, `libanthyinput-unicode.so.0` | anthy-unicode, built from `github.com/bjj/anthy-unicode` branch `libpathime` | LGPL-2.1 | `licenses/anthy-unicode.txt` |
| `libpyzy-1.0.so.1` | pyzy, built from `github.com/bjj/pyzy` branch `libpathime` | LGPL-2.1 | `licenses/pyzy.txt` |

**Nothing else ships.** GLib (LGPL-2.1-or-later), SQLite3 (public domain) and
libuuid (Modified BSD) are loaded from the system — install the distro's
`libglib2.0-0`, `libsqlite3-0` and `libuuid1` (or equivalents). The package
deliberately carries no copy of them.

## Data files (`data/pathime-data/`)

Compiled at libpathime build time from the sources named below. None is
linked into a binary.

| Shipped file | Built from | Licence of the source | Text |
|---|---|---|---|
| `anthy/anthy.dic` | anthy-unicode's `alt-cannadic/*` and the `mkworddic/*.t` read by `mkworddic/dict.args` | **GPL-2** | `licenses/anthy-dictionary.txt` |
| `pyzy/main.db` | pyzy's `data/db/android/rawdict_utf16_65105_freq.txt` | LGPL-2.1 | `licenses/pyzy.txt` |
| `pyzy/phrases.txt` | pyzy's `src/phrases.txt` | LGPL-2.1 | `licenses/pyzy.txt` |
| `table/*.db` | ibus-table-chinese table sources (`github.com/mike-fabian/ibus-table-chinese`, unmodified) | **GPL-3** | `licenses/ibus-table-chinese.txt` |

## Corresponding source, and the LGPL arrangement

Every library above is a separate, replaceable shared object — nothing LGPL
is linked into `libpathime.so` statically — so the LGPL's relinking
requirement is satisfied by the arrangement of files. The corresponding
source for the binaries is the libpathime repository
(`github.com/bjj/libpathime`) at the release's pinned submodule commit, with
every vendored submodule populated; for the two modified libraries, the
forks' `libpathime` branches named above are the source the binaries were
built from. This package's exact libpathime commit is recorded as the
`libpathime` submodule pin in `github.com/bjj/libpathime-sharp` at the
release tag the package was built from.
