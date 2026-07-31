# libpathime-sharp

C# binding (**PathimeSharp**) for the C library libpathime, vendored as the
`libpathime/` submodule. A Python binding under `refs/` is the reference
implementation. Primary audience includes Unity users.

## Hard rules

- **Never commit to or modify `libpathime/` or `refs/`.** They are read-only.
  Pain points or requested changes for the libpathime team go in
  `BINDING-NOTES.md` (they take feedback upstream from there; the Python
  binding's first round became `libpathime/docs/design-history.md` §12 — read
  that section before questioning API shape).
- **Native binaries and dictionary data are never committed.** `artifacts/` and
  `unity/com.ben.pathime/Plugins/` are staged by `scripts/stage-native.ps1`
  from a local CMake install and are gitignored.
- Maintain `TODO.md` as work is discovered or completed.

## Source layout

The canonical binding source is `unity/com.ben.pathime/Runtime/Core/` — the
Unity UPM package must physically contain it, and `src/PathimeSharp/`'s csproj
compiles the same files via a `<Compile Include>` glob. One source of truth;
never copy files between the two.

Consequence: **the source must compile at C# 9 / netstandard2.0** (Unity
2021.3). No records, no init-only setters, no `NativeLibrary` outside
`#if NET8_0_OR_GREATER`, no unsafe code.

## Binding rules (from the C API contract in `libpathime/include/pathime/pathime.h`)

- No `string` returns in `NativeMethods` — default marshaling would free
  library-owned memory. `IntPtr` + manual UTF-8 decode, always.
- Copy everything out of native memory eagerly; borrowed pointers are valid
  only until the next mutating call.
- No finalizers anywhere: the library forbids overlapping calls and requires
  destroy order (contexts → engine → shutdown); a finalizer thread would break
  both. Dispose is the only release path.
- Callbacks fire synchronously inside library calls and must never let an
  exception cross the native frame: catch, stash, rethrow after the triggering
  call returns.
- Callbacks are static methods decorated with our own
  `MonoPInvokeCallbackAttribute` (IL2CPP matches it by name), routed to the
  wrapper via `user_data` GCHandle.
- Public API positions are UTF-16 code units; the C API uses Unicode scalar
  values. Conversion happens at the boundary (`UnicodeIndex`), including a
  cached surrounding-text snapshot for the delete-surrounding callback.
- `size_t`/`ptrdiff_t` are `UIntPtr`/`IntPtr` (32-bit Unity targets exist).
  C `bool` is 1 byte (`byte` in structs, `[MarshalAs(UnmanagedType.U1)]` on
  params/returns).
- Set `struct_size` on every input struct.

## Demo

`demo/PathimeSharp.Demo` is an Avalonia phone-keyboard app. The
`PhoneKeyboard` class is UI-free (mirrors the Python demo's model) —
`MainWindow` only draws it. It resolves the native library like the tests:
`PATHIME_LIBRARY`, then `artifacts/native/<rid>/`.

## Testing

`dotnet test` with `PATHIME_LIBRARY` pointing at a local libpathime build
(e.g. `C:\dev\dist\bin\pathime.dll`), or stage via
`scripts\stage-native.ps1 -Prefix <install-prefix>` for the fallback path.
xUnit parallelization is disabled assembly-wide — the library forbids
overlapping calls; never re-enable it. Engines missing at runtime skip their
tests. Test the binding contract, not the library (libpathime has its own
suite).
