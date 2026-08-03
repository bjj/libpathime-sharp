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

## Record: v0.1.2 shipped 2026-08-02

The 2026-08-02 three-repo release review, adopted and shipped the same day,
in lockstep with libpathime and libpathime-python (libpathime's RELEASING.md
owns the order; there is no 0.1.1 here — libpathime used it for a macOS-only
release). The reasoning lives in the artifacts:

- The gated release shape (draft first, NuGet push held by the
  `nuget-production` environment's required reviewer): `release.yml` header.
- Version lockstep enforcement (tag ↔ csproj ↔ nuspecs ↔ Unity package.json
  ↔ submodule pin): `scripts/check-release-version.sh`, run by CI and
  release.
- Packed-package consumer proof (NativeAssets-only reference, PathimeSharp
  transitive, run + RID publish + RID-less publish, `pathime-data` survives):
  `scripts/test-packages.ps1`, run by CI and release.
- NativeAssets → PathimeSharp exact-pin dependency, and the Init-time native
  version check: the nuspecs' comments and `Pathime.CheckNativeVersion`.
- New release artifacts: provenance attestations on everything, and
  `com.ben.pathime-<version>.tgz` — the UPM package with both platforms'
  natives and dictionaries inside, GPL-3 as distributed.

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
