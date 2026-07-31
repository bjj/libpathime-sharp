using System;
using System.Collections.Generic;
using System.Text;
using PathimeSharp;

namespace PathimeSharp.Demo
{
    /// <summary>
    /// The phone: a document, an input context per engine, and a candidate
    /// strip. UI-free — the Avalonia window only draws it, so tests can type
    /// into it headlessly. One instance keeps every language's keyboard warm;
    /// <see cref="SwitchEngine"/> moves between them without disturbing the
    /// composition each one holds.
    /// </summary>
    /// <remarks>
    /// The document is an ordinary C# string world: <see cref="Cursor"/> and
    /// the delete-surrounding callback are UTF-16 code-unit quantities, which
    /// is exactly what the binding's public API speaks.
    /// </remarks>
    public sealed class PhoneKeyboard : IDisposable
    {
        /// <summary>Candidates visible at once: what digits 1–9 can tap.</summary>
        public const int StripSize = 9;

        public static readonly IReadOnlyDictionary<EngineId, string> EngineLabels =
            new Dictionary<EngineId, string>
            {
                { EngineId.Hangul, "한국어" },
                { EngineId.Anthy, "日本語" },
                { EngineId.Pinyin, "拼音" },
                { EngineId.Bopomofo, "注音" },
                { EngineId.Table, "倉頡" },
            };

        private readonly StringBuilder _document = new StringBuilder();
        private readonly List<EngineId> _engineIds = new List<EngineId>();
        private readonly Dictionary<EngineId, Engine> _engines = new Dictionary<EngineId, Engine>();
        private readonly Dictionary<EngineId, Context> _contexts = new Dictionary<EngineId, Context>();

        public PhoneKeyboard(IEnumerable<EngineId> engineIds)
        {
            foreach (EngineId id in engineIds)
            {
                var engine = new Engine(id);
                if (id == EngineId.Table)
                {
                    engine.SetOption(Option.TableFile, "cangjie5");
                }

                _engineIds.Add(id);
                _engines[id] = engine;
                _contexts[id] = new Context(
                    engine,
                    onCommit: Insert,
                    onDeleteSurrounding: Delete,
                    onCompositionChanged: _ => FollowCursor());
            }

            if (_engineIds.Count == 0)
            {
                throw new ArgumentException("At least one engine id is required.", nameof(engineIds));
            }

            Active = _engineIds[0];
            RefreshSurrounding();
        }

        public EngineId Active { get; private set; }

        /// <summary>The UTF-16 insertion position in <see cref="Text"/>.</summary>
        public int Cursor { get; private set; }

        public int Page { get; private set; }

        public Context Context => _contexts[Active];

        public Composition Composition => Context.Composition;

        public string Text => _document.ToString();

        public IReadOnlyList<EngineId> Engines => _engineIds;

        /* ---- the client side of the pathime contract ---- */

        private void Insert(string text)
        {
            _document.Insert(Cursor, text);
            Cursor += text.Length;
        }

        private void Delete(int utf16Offset, int utf16Count)
        {
            int start = Cursor + utf16Offset;
            _document.Remove(start, utf16Count);
            Cursor = start;
        }

        private void RefreshSurrounding()
        {
            Context.SetSurroundingText(_document.ToString(), Cursor);
        }

        private void FollowCursor()
        {
            Composition comp = Context.Composition;
            if (comp.CandidateCursor < Page * StripSize
                || comp.CandidateCursor >= (Page + 1) * StripSize)
            {
                Page = comp.CandidateCursor / StripSize;
            }
        }

        /* ---- what the UI reads ---- */

        /// <summary>
        /// The visible candidate page and the highlight's position in it
        /// (-1 when the highlight is on another page).
        /// </summary>
        public (IReadOnlyList<string> Visible, int Highlight) Strip()
        {
            Composition comp = Composition;
            int start = Page * StripSize;
            var visible = new List<string>();
            for (int i = start; i < comp.CandidateCount && i < start + StripSize; i++)
            {
                visible.Add(comp.Candidates[i]);
            }

            int highlight = comp.CandidateCursor - start;
            return (visible, highlight >= 0 && highlight < visible.Count ? highlight : -1);
        }

        /* ---- what the UI calls ---- */

        /// <summary>A tap on the on-screen keyboard: engine first, document second.</summary>
        public void Key(in KeyEvent keyEvent)
        {
            if (!Context.ProcessKey(keyEvent))
            {
                Fallthrough(keyEvent);
            }

            RefreshSurrounding();
        }

        public void Key(char c) => Key(KeyEvent.FromChar(c));

        public void Key(Key key) => Key(new KeyEvent(key));

        private void Fallthrough(in KeyEvent keyEvent)
        {
            switch (keyEvent.Keysym)
            {
                case (uint)PathimeSharp.Key.Space:
                    Insert(" ");
                    break;
                case (uint)PathimeSharp.Key.Return:
                    Insert("\n");
                    break;
                case (uint)PathimeSharp.Key.Backspace:
                    if (Cursor > 0)
                    {
                        int len = ScalarLengthBefore(Cursor);
                        _document.Remove(Cursor - len, len);
                        Cursor -= len;
                    }
                    break;
                case (uint)PathimeSharp.Key.Left:
                    if (Cursor > 0)
                    {
                        Cursor -= ScalarLengthBefore(Cursor);
                    }
                    break;
                case (uint)PathimeSharp.Key.Right:
                    if (Cursor < _document.Length)
                    {
                        Cursor += char.IsHighSurrogate(_document[Cursor])
                            && Cursor + 1 < _document.Length ? 2 : 1;
                    }
                    break;
                default:
                    // A printable key the engine declined becomes document text.
                    uint keysym = keyEvent.Keysym;
                    if (keysym < 0x100)
                    {
                        Insert(((char)keysym).ToString());
                    }
                    else if (keysym >= 0x01000000)
                    {
                        Insert(char.ConvertFromUtf32((int)(keysym - 0x01000000)));
                    }
                    break;
            }
        }

        private int ScalarLengthBefore(int utf16Index)
        {
            return utf16Index >= 2
                && char.IsLowSurrogate(_document[utf16Index - 1])
                && char.IsHighSurrogate(_document[utf16Index - 2]) ? 2 : 1;
        }

        /// <summary>Digit 1–9: tap that candidate on the visible strip.</summary>
        public void TapCandidate(int digit)
        {
            int index = Page * StripSize + digit - 1;
            if (index < Composition.CandidateCount)
            {
                Context.SelectCandidate(index);
                Page = 0;
                RefreshSurrounding();
            }
        }

        /// <summary>Slide the highlight; the preedit may preview the hover.</summary>
        public void Slide(int direction)
        {
            Composition comp = Composition;
            if (comp.CandidateCount == 0)
            {
                return;
            }

            int index = comp.CandidateCursor + direction;
            if (index >= 0 && index < comp.CandidateCount)
            {
                try
                {
                    Context.SetCandidateCursor(index);
                }
                catch (PathimeUnsupportedException)
                {
                    // Some engines have no client-movable cursor.
                }
            }
        }

        /// <summary>
        /// Page the strip, growing the list past its cap on the way down —
        /// the cap is composition-safe and only ever appends.
        /// </summary>
        public void PageStrip(int direction)
        {
            Composition comp = Composition;
            if (direction > 0)
            {
                int wanted = (Page + 1) * StripSize;
                if (wanted >= comp.CandidateCount)
                {
                    long cap = Context.GetOptionInt(Option.MaxCandidates);
                    if (comp.CandidateCount == cap) // maybe truncated; ask for more
                    {
                        Context.SetOption(Option.MaxCandidates, cap + StripSize);
                    }
                }

                if (wanted < Composition.CandidateCount)
                {
                    Page++;
                }
            }
            else if (Page > 0)
            {
                Page--;
            }
        }

        public void SwitchEngine()
        {
            Active = _engineIds[(_engineIds.IndexOf(Active) + 1) % _engineIds.Count];
            Page = 0;
            RefreshSurrounding();
        }

        public void Commit()
        {
            Context.Commit();
            RefreshSurrounding();
        }

        public void Reset()
        {
            Context.Reset();
            RefreshSurrounding();
        }

        public void Dispose()
        {
            foreach (Context context in _contexts.Values)
            {
                context.Dispose();
            }

            foreach (Engine engine in _engines.Values)
            {
                engine.Dispose();
            }
        }
    }
}
