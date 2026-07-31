# TODO

- [ ] **Before pushing to GitHub**: fix `.gitmodules` — the `libpathime`
      submodule points at `ssh://git@orion.local:222/bjj/libpathime.git`;
      repoint at the GitHub URL.
- [ ] CI: native builds (win-x64, linux-x64) producing staged artifacts.
      Cross-compiling is unsupported upstream (anthy dictionary is built by
      host tools), so Linux needs a Linux runner. The Linux recipe is proven
      (validated in WSL Ubuntu 20.04): cmake ≥3.21 + ninja +
      libglib2.0-dev + libsqlite3-dev + dotnet-sdk-8.0, then
      `cmake -G Ninja -DCMAKE_BUILD_TYPE=Release`, install,
      `scripts/stage-native.sh <prefix>`, `dotnet test`.
- [ ] NuGet publish pipeline: `PathimeSharp` first, then the
      `PathimeSharp.NativeAssets.*` packages once CI produces binaries.
- [ ] THIRD-PARTY notices for any package that ships binaries/data: LGPL-2.1
      backends (libhangul, anthy-unicode fork, pyzy fork — fork URLs are the
      corresponding source), GPL-derived data (anthy.dic, table DBs), and the
      vcpkg DLL closure (glib, pcre2, iconv, intl, sqlite3, …).
- [ ] MSVC CRT decision: document the VC++ redistributable requirement vs.
      shipping `msvcp140.dll`/`vcruntime140*.dll` app-local in NativeAssets
      (upstream has the same open question in their TODO).
- [ ] Verify empirically whether NuGet `runtimes/<rid>/native/` preserves the
      `pathime-data/` subtree on current SDKs; if it does, the
      `buildTransitive` .targets copy step can be simplified away.
- [ ] IL2CPP smoke test in a real Unity project (callback plumbing is designed
      for it: static thunks + name-matched MonoPInvokeCallback), plus a Unity
      sample scene.
- [ ] linux-x64: NativeAssets.linux-x64 package. Staging
      (`stage-native.sh`, UPM `Plugins/Linux/x86_64/`) and the full test
      suite are validated on Linux (WSL). Remaining decision for packages:
      the staging script dereferences the symlink chain into three copies of
      each `.so`; a published package should ship one real file per SONAME
      (`libpathime.so.0`, `libhangul.so.1`, …) plus whatever name the loader
      probes.
- [ ] Confirm UPM package id `com.ben.pathime` before first publish.
- [ ] Port the Python reference's demo-model tests (`test_demo.py` analog):
      drive `PhoneKeyboard` headlessly from the test suite (needs a
      ProjectReference from tests to the demo, or the model moved to a shared
      location).
- [ ] Unity sample scene exercising `PathimeUnity.Initialize` + a text field.
