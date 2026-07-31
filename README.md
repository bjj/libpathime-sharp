# libpathime-sharp

**PathimeSharp** — a C# binding for [libpathime](https://github.com/bjj/libpathime),
a CJK input method engine library: Korean Hangul, Japanese kana–kanji (Anthy),
Chinese Pinyin/Bopomofo (pyzy), and table-driven methods (Cangjie, Wubi,
Stroke5, Zhuyin). libpathime is a plain synchronous C library — you feed it key
events and render the preedit and candidate list yourself — which makes it a
natural fit for games and custom UIs, including Unity.

Repo layout:

- `unity/com.ben.pathime/Runtime/Core/` — the canonical binding source
  (netstandard2.0-compatible, C# 9, no Unity dependencies). Both distribution
  channels use these files:
  - `src/PathimeSharp/` — NuGet/.NET project, compiles the same source.
  - `unity/com.ben.pathime/` — Unity UPM package (git URL:
    `https://github.com/.../libpathime-sharp.git?path=/unity/com.ben.pathime`).
- `tests/PathimeSharp.Tests/` — xUnit suite for the binding contract.
- `demo/PathimeSharp.Demo/` — Avalonia phone-keyboard demo.
- `libpathime/` — the C library, as a git submodule (never modified here).
- `scripts/` — staging of locally built native binaries into package layouts.

## Building the native library

The binding loads a separately built libpathime at runtime; nothing native is
built by `dotnet build` and no binaries are committed. On Windows:

```powershell
# once: LF endings matter (dictionary codegen reads in binary mode) — set BEFORE
# cloning/initializing submodules
git config --global core.autocrlf input

git clone https://github.com/microsoft/vcpkg C:\dev\vcpkg
C:\dev\vcpkg\bootstrap-vcpkg.bat
C:\dev\vcpkg\vcpkg install glib sqlite3
$env:VCPKG_ROOT = "C:\dev\vcpkg"

git submodule update --init --recursive
cmake --preset windows-msvc -S libpathime -DCMAKE_INSTALL_PREFIX=C:\dev\pathime-dist
cmake --build --preset windows-msvc
cmake --install libpathime\build\windows-msvc
```

The `cmake --install` step is what lays `pathime-data/` (the dictionaries)
beside `pathime.dll` — a bare build tree is not enough. See libpathime's
`BUILD.md` for Linux and details.

## Running the tests

```powershell
$env:PATHIME_LIBRARY = "C:\dev\pathime-dist\bin\pathime.dll"
dotnet test
```

Or stage once and let the tests find the fallback location:

```powershell
scripts\stage-native.ps1 -Prefix C:\dev\pathime-dist
dotnet test
```

Engines whose native backend or dictionary data is missing skip their tests.

## Using the binding

```csharp
using PathimeSharp;

Pathime.Init();                      // finds pathime-data beside the native lib
using var engine = new Engine(EngineId.Pinyin);
using var context = new Context(engine, onCommit: text => Console.Write(text));
context.Type("nihao");
Console.WriteLine(context.Composition.Preedit);      // preedit so far
Console.WriteLine(context.Composition.Candidates[0]); // e.g. 你好
context.SelectCandidate(0);          // commits via onCommit
```

The native library is located via (in order) `Pathime.Load(path)`, the
`PATHIME_LIBRARY` environment variable, or the platform's default library
search. All positions in the public API are UTF-16 code units (normal C#
string indices); the binding converts to libpathime's Unicode scalar values at
the boundary.

## Running the demo

A graphical phone keyboard (Avalonia, so it runs on Windows and Linux):

```powershell
dotnet run --project demo\PathimeSharp.Demo
```

Tap the on-screen keys or type on the physical keyboard: letters and space go
to the engine first, digits 1–9 tap candidates on the strip, ←/→ slide the
highlight, ↑/↓ page (paging past the end grows the candidate cap, phone
style), Ctrl+E cycles engines, Ctrl+T commits, Ctrl+R discards. The engine key
(top left) also switches on tap. It finds the native library the same way the
tests do: `PATHIME_LIBRARY` or the staged `artifacts\native\<rid>\` copy.

## Unity

`unity/com.ben.pathime` is a Unity package (UPM) containing the same binding
source plus a loader helper and a build processor for desktop players; add it
from disk or by git URL with `?path=/unity/com.ben.pathime`. Stage natives
into it with `scripts\stage-native.ps1 -Targets unity`, then call
`PathimeSharp.Unity.PathimeUnity.Initialize()` once at startup. See the
package README for the support envelope (desktop-first; IL2CPP designed-for
but not yet smoke-tested).
