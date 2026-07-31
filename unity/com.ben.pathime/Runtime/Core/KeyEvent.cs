using System;

namespace PathimeSharp
{
    /// <summary>
    /// One key press (pathime_key_event_t). There are no key releases in this
    /// API. Keysyms follow the X11 convention; see
    /// <see cref="Pathime.KeysymForChar(char)"/> and <see cref="Key"/>.
    /// </summary>
    public readonly struct KeyEvent
    {
        /// <param name="keysym">The key's meaning under the user's layout.</param>
        /// <param name="layoutKey">
        /// The same physical key expressed as a US-QWERTY keysym, for engines
        /// (Hangul) defined by key position. 0 falls back to
        /// <paramref name="keysym"/>.
        /// </param>
        /// <param name="modifiers">Modifier state.</param>
        public KeyEvent(uint keysym, uint layoutKey = 0, KeyModifiers modifiers = KeyModifiers.None)
        {
            Keysym = keysym;
            LayoutKey = layoutKey;
            Modifiers = modifiers;
        }

        public KeyEvent(Key key, KeyModifiers modifiers = KeyModifiers.None)
            : this((uint)key, 0, modifiers)
        {
        }

        public uint Keysym { get; }
        public uint LayoutKey { get; }
        public KeyModifiers Modifiers { get; }

        /// <summary>
        /// A key event for a printable character, with an optional physical-key
        /// position for layout-defined engines.
        /// </summary>
        public static KeyEvent FromChar(char c, KeyModifiers modifiers = KeyModifiers.None, char layoutKey = '\0')
        {
            return new KeyEvent(
                Pathime.KeysymForChar(c),
                layoutKey == '\0' ? 0u : Pathime.KeysymForChar(layoutKey),
                modifiers);
        }
    }
}
