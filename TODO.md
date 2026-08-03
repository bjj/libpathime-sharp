# TODO

## Next

- [ ] IL2CPP smoke test in a real Unity project (callback plumbing is designed
      for it: static thunks + name-matched MonoPInvokeCallback), plus a Unity
      sample scene exercising `PathimeUnity.Initialize` + a text field.
- [ ] Port the Python reference's demo-model tests (`test_demo.py` analog):
      drive `PhoneKeyboard` headlessly from the test suite (needs a
      ProjectReference from tests to the demo, or the model moved to a shared
      location).
- [ ] Revisit app-local MSVC runtime DLLs (`msvcp140.dll`/`vcruntime140*`).
      Current, documented position (win-x64 THIRD-PARTY.md): ship none,
      require the VC++ 2015–2022 Redistributable. Upstream tracks the same
      question.

## Release hardening — from the 2026-08-02 three-repo review, in priority order

An external review of libpathime and both bindings; every claim below was
verified against this tree. The core library's share of the same review is in
its own `TODO.md`.

- [ ] **Gate the NuGet push behind a protected environment.** Today
      `release.yml` pushes all three packages to NuGet.org *before* creating
      the draft GitHub release, and NuGet only unlists — a bad version stays
      downloadable forever. Split the push into its own job bound to a
      `nuget-production` GitHub Environment with a required reviewer: the tag
      still builds, packs, and drafts immediately; the push waits for one
      click. **Ben: create the environment + required-reviewer rule in repo
      settings** (restrict it to `v*` tags while there); the workflow change
      rides on that.
- [ ] **`scripts/check-release-version`, run in CI and release.** The tag
      overrides every version at pack time, so nothing notices that
      `PathimeSharp.csproj`, both nuspecs, and Unity's `package.json` all
      still say 0.1.0 — and Unity git-URL consumers get whatever
      `package.json` says. Fail if the tag, those four files, and the
      libpathime submodule's tag disagree. The submodule's own release guard
      (`libpathime/.github/workflows/release.yml`, "The tag names the built
      version") is the model. Also: the Unity CHANGELOG still says
      "0.1.0 - unreleased" — fix with the first use.
- [ ] **Test the packages users install, not the staged tree.** CI never
      runs `nuget pack` on the nuspecs (release-only today) and never
      restores the packed nupkgs into anything. Add a consumer job: pack all
      three, point a local feed at them, create a bare console app
      referencing `PathimeSharp` + the RID's NativeAssets, `dotnet run`, then
      `dotnet publish -r win-x64`/`linux-x64` and assert `pathime-data/`
      survived the publish transform — the mechanical version of the one-time
      local-feed validation recorded below. Release runs it before the
      approval gate.
- [ ] **NativeAssets depend on PathimeSharp** (decided 2026-08-02). Each
      `PathimeSharp.NativeAssets.<rid>` nuspec gains a `<dependencies>` entry
      on exactly `[X.Y.Z]` `PathimeSharp`, so installing the platform package
      alone yields a working setup. GPL→MIT direction only: installing
      `PathimeSharp` must never pull GPL assets transitively. The exact-pin
      is what the version-guard item keeps honest.
- [ ] **Reject an unsupported native library at init.** The binding never
      checks `pathime_version()`; pre-1.0 the C library's ABI promise is
      per-minor (its SONAME is moving to track that), so validate
      major.minor at first load and fail with a clear message instead of
      whatever a missing/changed symbol produces.
- [ ] **Attest the release artifacts.** The core repo already runs
      `actions/attest-build-provenance` over everything it publishes; add the
      same over the nupkgs and demo bundle here (`attestations: write` +
      `id-token: write` on the release job only).
- [ ] **A Unity `.tgz` release artifact** (decided 2026-08-02). A versioned
      UPM tarball with the win-x64/linux-x64 natives staged in, so the Unity
      channel is installable without a native toolchain. GPL-3 as
      distributed — say so in the artifact's own README. Until it exists,
      the root README should call the git-URL install a **source-only** UPM
      package at first mention (the caveat currently lives only in the
      package README).
- [ ] **Workflow hygiene.** `dependabot.yml` with `github-actions` +
      `gitsubmodule` (the core repo's is the model — the latter turns the
      submodule bump into an arriving PR); `SECURITY.md`.

## Record: v0.1.0 shipped 2026-08-01

All three packages (`PathimeSharp`, `PathimeSharp.NativeAssets.win-x64`,
`.linux-x64`) on NuGet.org, pushed by the `v0.1.0` tag through
`release.yml` (Trusted Publishing); GitHub Release published, carrying the
nupkgs and a self-contained win-x64 demo bundle (retrofitted onto v0.1.0;
future tags build it in the workflow — a linux demo bundle would be the
same recipe on the other runner if ever wanted); CI green on
`windows-2022` + `ubuntu-22.04`. libpathime submodule pinned at its
`v0.1.0` tag.

The reasoning that accumulated here during the release push now lives in
the artifacts it describes — go to the file, not the git history of this
one:

- Native build/cache recipe, runner-image pinning, LF-checkout rule:
  `.github/actions/native/action.yml` and the workflow comments.
- NuGet auth (Trusted Publishing policy + `NUGET_USER`): `release.yml`
  header. The policy is permanently active since the first publish; a
  release is now just `git tag vX.Y.Z && git push origin vX.Y.Z`.
- Native package layout (flat soname .so set; `pathime-data/` under
  `runtimes/<rid>/native/`; publish-flatten repair): the nuspecs and
  `buildTransitive/*.targets` comments. Validated end-to-end from a local
  feed — 5/5 engines in build, `publish -r`, and RID-less publish, on
  Windows and Linux.
- Staging (soname trimming, licence texts, needs a libpathime ≥ v0.1.0
  install): `scripts/stage-native.ps1` / `.sh` comments.
- Licensing inventory: `packaging/*/THIRD-PARTY.md`, reviewed by Ben
  2026-08-01.
