# Third-party notices — PathimeSharp.Demo (win-x64, self-contained)

What this archive ships and under what terms, carried forward from
libpathime's `THIRD-PARTY.md` (the authoritative inventory, in the libpathime
repository). Everything sits flat beside `PathimeSharp.Demo.exe`; the licence
text of every libpathime component named below is in the archive's
`licenses/` directory.

**The archive as a whole is GPL-3.** It ships `pathime-data/table/*.db`
(GPL-3) and `pathime-data/anthy/anthy.dic` (GPL-2) alongside the MIT and LGPL
binaries, and the data is most of the bytes.

## libpathime payload

| File | Component | Licence | Text |
|---|---|---|---|
| `pathime.dll` | libpathime | MIT | `licenses/libpathime.txt` |
| `hangul.dll` | libhangul, upstream unmodified (`github.com/libhangul/libhangul`) | LGPL-2.1 | `licenses/libhangul.txt` |
| `anthy-unicode.dll` | anthy-unicode, built from `github.com/bjj/anthy-unicode` branch `libpathime` | LGPL-2.1 | `licenses/anthy-unicode.txt` |
| `pyzy-1.0.dll` | pyzy, built from `github.com/bjj/pyzy` branch `libpathime` | LGPL-2.1 | `licenses/pyzy.txt` |
| `glib-2.0-0.dll` | GLib (vcpkg-built) | LGPL-2.1-or-later | `licenses/glib.txt` |
| `iconv-2.dll` | libiconv (vcpkg-built) | LGPL-2.1 | `licenses/libiconv.txt` |
| `intl-8.dll` | gettext's libintl (vcpkg-built) | LGPL-2.1 | `licenses/gettext.txt` |
| `pcre2-8.dll` | PCRE2 (vcpkg-built) | BSD-3-Clause | `licenses/pcre2.txt` |
| `sqlite3.dll` | SQLite (vcpkg-built) | public domain | — |

Data files under `pathime-data/`, compiled at libpathime build time:

| Shipped file | Built from | Licence of the source | Text |
|---|---|---|---|
| `anthy/anthy.dic` | anthy-unicode's `alt-cannadic/*` and the `mkworddic/*.t` read by `mkworddic/dict.args` | **GPL-2** | `licenses/anthy-dictionary.txt` |
| `pyzy/main.db`, `pyzy/phrases.txt` | pyzy's data files | LGPL-2.1 | `licenses/pyzy.txt` |
| `table/*.db` | ibus-table-chinese table sources (`github.com/mike-fabian/ibus-table-chinese`, unmodified) | **GPL-3** | `licenses/ibus-table-chinese.txt` |

Every LGPL library is a separate, replaceable DLL, so the LGPL's relinking
requirement is satisfied by the arrangement of files. The corresponding
source is `github.com/bjj/libpathime` at the commit recorded as the
`libpathime` submodule pin in `github.com/bjj/libpathime-sharp` at the
release tag this archive was built from; for the two modified libraries the
forks' `libpathime` branches named above are the source they were built from.

**Not shipped: the Microsoft Visual C++ runtime.** `pathime.dll` and its
backends are built with MSVC and need the Visual C++ 2015–2022
Redistributable (x64) on the machine.

## .NET application bundle

The archive is a self-contained .NET publish: it also carries the .NET
runtime (MIT, `github.com/dotnet/runtime`) and the demo's NuGet
dependencies — PathimeSharp (MIT), Avalonia and the Inter font package (MIT,
`github.com/AvaloniaUI/Avalonia`; Inter is SIL OFL-1.1), and Avalonia's
native rendering stack SkiaSharp (MIT, wrapping Skia, BSD-3-Clause) and
HarfBuzzSharp (MIT, wrapping HarfBuzz, MIT-style). Each ships under its own
licence, available at the projects named.
