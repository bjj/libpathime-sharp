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
- [x] **CI workflow** (`.github/workflows/ci.yml`) — green on both runners.
      Native build/cache/stage lives in `.github/actions/native` (shared
      with release.yml). Deviations from the original plan, both deliberate:
      `windows-2022` rather than `-latest` (the `windows-msvc` preset pins
      the VS 17 generator; the 2025 image ships only VS 18), and the
      install-prefix cache is keyed on submodule commit + image label. LF
      checkout forced on Windows before checkout (anthy codegen chokes on
      CRLF; attributes cannot cross the submodule boundary). `dotnet test`
      (xUnit parallelization stays disabled — never re-enable),
      `dotnet pack` smoke, staged trees uploaded as `native-<rid>`
      artifacts.
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
      via **Trusted Publishing** (before the first tag: on nuget.org add a
      Trusted Publishing policy — owner `bjj`, repo `libpathime-sharp`,
      workflow file `release.yml`, no environment — and set the
      `NUGET_USER` repo secret to the nuget.org profile name), and a
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
- [x] add a screenshot of the demo — docs/demo.png, linked from the README

### Decisions to settle while building the above

- [x] MSVC CRT: documented in the win-x64 THIRD-PARTY.md — no Microsoft
      runtime DLLs shipped, VC++ 2015–2022 Redistributable (x64) required.
      Revisit app-local `msvcp140.dll`/`vcruntime140*.dll` later (upstream
      has the same open question in their TODO).
- [x] Verified empirically (SDK 9.0.300, test package + consumer app):
      `dotnet build` **preserves** a subtree under `runtimes/<rid>/native/`
      in the output's `runtimes/` layout, but `dotnet publish -r <rid>`
      **flattens** native assets into the publish root. So the subtree alone
      cannot carry `pathime-data/`; some copy step stays. The same
      experiment exposed the next item.
- [ ] **End-to-end consumer test of the native packages** — the current
      `.targets` arrangement has two likely gaps, neither yet exercised by a
      real consumer: (1) plain `dotnet build` loads the dll from the
      output's `runtimes/<rid>/native/`, but the .targets copies
      `pathime-data/` to the OutDir root, not beside the dll, so default
      module-relative data resolution misses it; (2) `dotnet publish`
      ignores stray OutDir files, so the data is not published at all.
      Candidate fix: ship the data under `runtimes/<rid>/native/pathime-data`
      in the nuspec (covers build, subtree preserved) and replace the Copy
      target with `None` items with `Link` +
      `CopyToPublishDirectory=PreserveNewest` sourced from that same package
      path (covers publish, one copy of the bytes in the nupkg). Verify
      both layouts with a console consumer that actually calls
      `Pathime.Init()` + `HasEngine`, then simplify the .targets.

## Later

- [ ] IL2CPP smoke test in a real Unity project (callback plumbing is designed
      for it: static thunks + name-matched MonoPInvokeCallback), plus a Unity
      sample scene exercising `PathimeUnity.Initialize` + a text field.
- [ ] Port the Python reference's demo-model tests (`test_demo.py` analog):
      drive `PhoneKeyboard` headlessly from the test suite (needs a
      ProjectReference from tests to the demo, or the model moved to a shared
      location).
