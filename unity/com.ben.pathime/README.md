# PathimeSharp (Unity package)

C# binding for [libpathime](https://github.com/bjj/libpathime) — CJK input
method engines (Korean Hangul, Japanese Anthy, Chinese Pinyin/Bopomofo, and
table-driven methods like Cangjie and Wubi) behind one synchronous API. You
send key events and render the preedit/candidates yourself — which is exactly
the shape an in-game input field wants.

`Runtime/Core/` is the canonical binding source, shared verbatim with the
PathimeSharp NuGet package (the repo's `src/PathimeSharp/` project compiles
these same files). It has no Unity dependencies. `Runtime/Unity/` holds the
Unity-specific loader helper and `Editor/` the build processor that copies
dictionary data next to desktop players.

## Native libraries

This package does **not** ship native binaries in git. `Plugins/` is populated
by the repo's staging script from a local CMake build of libpathime:

```powershell
scripts\stage-native.ps1 -Prefix <cmake-install-prefix> -Targets unity
```

That stages, per platform (`Plugins/Windows/x86_64/`, `Plugins/Linux/x86_64/`):

- `pathime.dll` / `libpathime.so` and the vendored backend libraries
- `pathime-data~/` — dictionaries (~30 MB). The trailing `~` hides the folder
  from Unity's asset importer; the runtime helper passes its full path to
  libpathime as the resource directory.

Without staged natives the package compiles but engine creation will fail with
`DllNotFoundException`; you can also point the binding at any libpathime build
via the `PATHIME_LIBRARY` environment variable or `Pathime.Load(path)`.

## Support envelope

Desktop (Windows/Linux, x64) in-editor and standalone players. Mobile and
consoles are out of scope for now (IL2CPP-safe callback plumbing is in place,
but data deployment and per-platform builds are not).
