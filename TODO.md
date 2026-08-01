# TODO

## Next session: GitHub upload → CI → release packages

Ordered; each step assumes the previous ones.

- [x] **Publish `libpathime` to GitHub** — live, tagged `v0.1.0`. The
      submodule's `.git/config` keeps orion as remote `local` alongside the
      GitHub `origin`.
- [x] **Update the submodule pin** — pinned at tag `v0.1.0` (`7c4d441`);
      84/84 tests pass against a fresh native build of that commit
      (installed at `C:\dev\dist-v0.1.0`).
- [x] **Create `github.com/bjj/libpathime-sharp` and push.** Live; a fresh
      `git clone --recurse-submodules` resolves every submodule from public
      URLs and `dotnet build` succeeds in the clone.
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
- [x] **Author `packaging/PathimeSharp.NativeAssets.linux-x64/`** — done,
      THIRD-PARTY.md still the stub. Settled .so layout: flat, one real file
      per SONAME plus `libpathime.so` for DllImport default probing. Flat is
      sound because every library carries `RUNPATH $ORIGIN` and names its
      siblings by soname, so the standard `runtimes/linux-x64/native` flow
      resolves the whole closure — only `pathime-data/` needs the
      buildTransitive copy. `stage-native.sh` now stages exactly that set
      (84/84 tests pass on it in WSL; both nuspecs smoke-packed).
- [x] **Release workflow** (`.github/workflows/release.yml`) authored: on
      `v*` tags, natives via the shared `.github/actions/native` (same cache
      as CI), `dotnet pack -p:Version -p:ContinuousIntegrationBuild=true`,
      nuspec pack of both native packages, PathimeSharp pushed to NuGet
      (`NUGET_API_KEY` secret — **create it before the first tag**), and a
      draft GitHub Release carrying all three nupkgs. The native packages
      are packed and attached but not pushed to NuGet — flip that in
      release.yml once the completed THIRD-PARTY notices have been
      reviewed. UPM needs no pipeline: consumers add the git URL
      (`?path=/unity/com.ben.pathime`) and stage natives locally.
- [x] **THIRD-PARTY notices** — written for both native packages, carrying
      forward `libpathime/THIRD-PARTY.md`: full DLL/.so inventory with the
      vcpkg closure on win-x64, system-library policy on linux-x64,
      GPL-3-as-a-whole statement, corresponding-source pointers. The
      packages also ship `licenses/` (per-component texts, staged from the
      install prefix's `share/doc/pathime/` — needs libpathime ≥ v0.1.0).
      **Review the notices before publishing the native packages.**
- [ ] add a screenshot of the demo

### Decisions to settle while building the above

- [x] MSVC CRT: documented in the win-x64 THIRD-PARTY.md — no Microsoft
      runtime DLLs shipped, VC++ 2015–2022 Redistributable (x64) required.
      Revisit app-local `msvcp140.dll`/`vcruntime140*.dll` later (upstream
      has the same open question in their TODO).
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
