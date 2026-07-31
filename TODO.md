# TODO

- [ ] **Before pushing to GitHub**: fix `.gitmodules` — the `libpathime`
      submodule points at `ssh://git@orion.local:222/bjj/libpathime.git`;
      repoint at the GitHub URL.
- [ ] CI: native builds (win-x64, linux-x64) producing staged artifacts.
      Cross-compiling is unsupported upstream (anthy dictionary is built by
      host tools), so Linux needs a Linux runner.
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
- [ ] linux-x64: staging support in `stage-native.sh`, UPM
      `Plugins/Linux/x86_64/`, NativeAssets.linux-x64 package. Confirm the
      shipped `.so` SONAME story (`libpathime.so.0` vs dev symlink).
- [ ] Confirm UPM package id `com.ben.pathime` before first publish.
- [ ] Port the Python reference's demo-model tests (`test_demo.py` analog):
      drive `PhoneKeyboard` headlessly from the test suite (needs a
      ProjectReference from tests to the demo, or the model moved to a shared
      location).
- [ ] Unity sample scene exercising `PathimeUnity.Initialize` + a text field.
