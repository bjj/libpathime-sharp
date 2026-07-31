# Binding notes for the libpathime team

Pain points and observations from writing the C# binding (PathimeSharp),
recorded as they are discovered — same flow as the Python binding's first
round, whose rulings landed in libpathime `docs/design-history.md` §12.
Nothing here requests a change to ruled-on behaviour unless marked as such.

## Headline

The binding needed **zero library changes**: all five engines, options with
isolation, the callback table, and a graphical phone-keyboard demo were built
and tested against the API as-is. `pathime_context_isolate_options()`,
`pathime_context_user_data()`, and `pathime_engine_name()` were used exactly
as designed, and the `struct_size` convention mapped cleanly onto
`Marshal.SizeOf`.

## Observations

1. **Scalar-unit `delete_surrounding_text` makes UTF-16 hosts keep the
   snapshot.** C# strings index by UTF-16 code units, so converting the
   callback's `(offset, count)` requires the text they are relative to — the
   binding retains the last `set_surrounding_text` payload purely as a
   conversion table. This works because the callback is defined against
   exactly that snapshot; the recipe is implied by the header but had to be
   derived. *Suggestion:* one sentence near `delete_surrounding_text` noting
   that the last-supplied surrounding text is also the conversion table a
   non-scalar-indexed client needs, so JVM/JS/C# binding authors find the
   pattern there instead of via astral-character corruption.

2. **`isolate_options` pins tier-3-derived values.** Isolating a context
   copies every supported option's *effective* value — including values a
   table declares (e.g. `TABLE_SINGLE_WILDCARD`). A context isolated before
   `TABLE_FILE` is set then shadows the wildcard the newly loaded table would
   declare, reading `""` where a non-isolated context reads the table's
   declaration. Coherent once understood (`reset_option` un-pins), but it
   surprised us in testing: this binding isolates every context by default,
   and the reference tests assumed live tier-3 re-resolution. *Suggestion:* a
   note in the `isolate_options` docs that tier-3 declarations arriving after
   isolation (table loads) are shadowed by the copies.

3. **Linux default probing misses `libpathime.so.0`.** .NET's
   `DllImport("pathime")` probes `libpathime.so`, not the versioned soname, so
   installs shipping only `libpathime.so.0` need `PATHIME_LIBRARY` (or our
   net8.0 resolver, which probes the soname explicitly). A dev-symlink note in
   BUILD.md's install section would help every non-C consumer.

4. **C `bool` in `pathime_option_info_t`.** One byte on every ABI that
   matters, and the binding maps it as such — but `uint8_t` would make the
   FFI contract explicit rather than inherited from the platform C ABI.
   Cosmetic; the `struct_size` handshake already catches a mismatch.

5. **Per-index candidate fetch is fine.** Building an eager snapshot costs one
   P/Invoke per candidate per composition change (≤ the max-candidates cap).
   Not measurable against the engines' own work; noted only to close the
   question of a bulk accessor — not needed.
