# TODO

## Next session: GitHub upload → CI → release packages

Ordered; each step assumes the previous ones.

- [x] **Publish `libpathime` to GitHub** — live, tagged `v0.1.0`. The
      submodule's `.git/config` keeps orion as remote `local` alongside the
      GitHub `origin`.
- [x] **Update the submodule pin** — pinned at tag `v0.1.0` (`7c4d441`);
      84/84 tests pass against a fresh native build of that commit
      (installed at `C:\dev\dist-v0.1.0`).
- [ ] **Create `github.com/bjj/libpathime-sharp` and push.** README, nuspec
      `projectUrl`, and `RepositoryUrl` in Directory.Build.props already
      assume that name. Verify a fresh `git clone --recurse-submodules` works.
- [ ] **CI workflow** (`.github/workflows/ci.yml`) on push/PR:
      - Matrix: `windows-latest` (win-x64), `ubuntu-22.04` (linux-x64).
        Cross-compiling is unsupported upstream (the anthy dictionary is
        built by host tools), hence a real runner per OS.
      - Checkout with `submodules: recursive`.
      - Native build + `cmake --install`, **cached keyed on the submodule
        commit** so C#-only pushes skip the slow native step entirely:
        - Windows: vcpkg `glib sqlite3` (vcpkg binary caching → GHA cache),
          `cmake --preset windows-msvc`, install,
          `scripts/stage-native.ps1 -Prefix <prefix>`.
        - Linux (recipe validated in WSL Ubuntu 20.04): apt cmake ninja-build
          libglib2.0-dev libsqlite3-dev + dotnet-sdk-8.0,
          `cmake -G Ninja -DCMAKE_BUILD_TYPE=Release`, install,
          `scripts/stage-native.sh <prefix>`.
      - `dotnet test` (xUnit parallelization stays disabled — never
        re-enable), `dotnet pack` as a smoke check.
      - Upload the staged native trees as build artifacts (input for the
        release job and for local debugging).
- [ ] **Author `packaging/PathimeSharp.NativeAssets.linux-x64/`** (nuspec +
      buildTransitive .targets + THIRD-PARTY.md, mirroring win-x64). First
      settle the .so layout: staging currently dereferences the symlink chain
      into three copies of each library; the package should ship one real
      file per SONAME (`libpathime.so.0`, `libhangul.so.1`, …) plus whatever
      name the loader probes (the net8.0 resolver probes the soname
      explicitly; `DllImport("pathime")` alone probes only `libpathime.so`).
- [ ] **Release workflow** (`.github/workflows/release.yml`) on `v*` tags:
      native builds (same cache), then pack and publish:
      - `PathimeSharp` — `dotnet pack -p:Version=<tag>`
        (+ `-p:ContinuousIntegrationBuild=true`).
      - `PathimeSharp.NativeAssets.win-x64` / `.linux-x64` — nuspec pack from
        the staged trees. **Blocked on THIRD-PARTY.md completion** — ship
        PathimeSharp alone until the notices are done.
      - Push to NuGet.org (`NUGET_API_KEY` secret) and attach the nupkgs to a
        GitHub Release.
      - UPM needs no pipeline: consumers add the git URL
        (`?path=/unity/com.ben.pathime`) and stage natives locally.
- [ ] **THIRD-PARTY notices** for any package that ships binaries/data:
      carry forward `libpathime/THIRD-PARTY.md` for LGPL-2.1 backends
      (libhangul, anthy-unicode fork, pyzy fork — fork URLs are the
      corresponding source), GPL-derived data (anthy.dic, table DBs), and the
      exact vcpkg DLL closure (glib, pcre2, iconv, intl, sqlite3, …).
- [ ] add a screenshot of the demo

### Decisions to settle while building the above

- [ ] MSVC CRT: start by documenting the VC++ Redistributable requirement
      (ship no Microsoft runtime DLLs); revisit app-local
      `msvcp140.dll`/`vcruntime140*.dll` later (upstream has the same open
      question in their TODO).
- [ ] Verify empirically whether NuGet `runtimes/<rid>/native/` preserves the
      `pathime-data/` subtree on current SDKs; if it does, the
      `buildTransitive` .targets copy step can be simplified away.

## Later

- [ ] IL2CPP smoke test in a real Unity project (callback plumbing is designed
      for it: static thunks + name-matched MonoPInvokeCallback), plus a Unity
      sample scene exercising `PathimeUnity.Initialize` + a text field.
- [ ] Port the Python reference's demo-model tests (`test_demo.py` analog):
      drive `PhoneKeyboard` headlessly from the test suite (needs a
      ProjectReference from tests to the demo, or the model moved to a shared
      location).
