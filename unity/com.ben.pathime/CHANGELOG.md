# Changelog

## [0.1.2] - 2026-08-02

- Native library version checked at Init: the binding refuses a libpathime
  outside the supported major.minor instead of failing on a missing symbol.
- Releases carry this package as a .tgz with win-x64/linux-x64 natives and
  dictionaries inside (GPL-3 as distributed).
- No 0.1.1: versions move in lockstep with libpathime, which used 0.1.1 for
  a macOS-only release.

## [0.1.0] - 2026-08-01

- Initial version: C# binding for libpathime (Hangul, Anthy, Pinyin, Bopomofo,
  table-driven engines), desktop platforms.
