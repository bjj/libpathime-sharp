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

## Release hardening — from the 2026-08-02 three-repo review

Implemented the same day (the workflows and `scripts/` carry the reasoning
as comments): the NuGet push waits in its own job behind the
`nuget-production` environment after the draft exists;
`scripts/check-release-version.sh` guards the tag, the four version-bearing
files and the submodule pin in CI and release; `scripts/test-packages.ps1`
restores the packed packages into a consumer that references only the
NativeAssets package and proves run, RID publish and RID-less publish;
NativeAssets depend on exactly-matching PathimeSharp; `Pathime.Init` refuses
a native library outside the supported major.minor; release artifacts are
attested; releases carry a Unity `.tgz` with natives inside; dependabot and
SECURITY.md exist. What remains:

- [ ] **Ben, one-time, before the next tag:** create the `nuget-production`
      GitHub Environment (Settings → Environments) with yourself as required
      reviewer, restricted to `v*` tags — the workflow names it but only the
      settings give it teeth. An unprotected environment approves silently.
- [ ] **Release v0.1.2** in lockstep with libpathime and libpathime-python
      (libpathime's RELEASING.md has the order). Before tagging: bump
      `PathimeSharp.csproj`, both nuspecs, Unity `package.json` and its
      CHANGELOG to 0.1.2, and the submodule to libpathime's v0.1.2 tag —
      `scripts/check-release-version.sh v0.1.2` says when it is right.

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
