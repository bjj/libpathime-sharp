using System;
using System.Collections.Generic;

namespace PathimeSharp
{
    /// <summary>
    /// An immutable snapshot of a context's composition state: preedit text
    /// and the candidate list. Unlike the C struct, everything here is an
    /// owned copy — it stays valid forever, and positions are UTF-16 code
    /// units.
    /// </summary>
    public sealed class Composition
    {
        /// <summary>The empty composition: no preedit, no candidates.</summary>
        public static readonly Composition Empty =
            new Composition(string.Empty, 0, Array.Empty<string>(), 0);

        internal Composition(string preedit, int preeditSettled, IReadOnlyList<string> candidates, int candidateCursor)
        {
            Preedit = preedit;
            PreeditSettled = preeditSettled;
            Candidates = candidates;
            CandidateCursor = candidateCursor;
        }

        /// <summary>Provisional, uncommitted text.</summary>
        public string Preedit { get; }

        /// <summary>
        /// The UTF-16 index into <see cref="Preedit"/> before which text is
        /// settled and not expected to change. Every candidate is an
        /// alternative for the span that begins here.
        /// </summary>
        public int PreeditSettled { get; }

        /// <summary>The complete candidate list, as owned copies.</summary>
        public IReadOnlyList<string> Candidates { get; }

        /// <summary>Shorthand for <c>Candidates.Count</c>.</summary>
        public int CandidateCount => Candidates.Count;

        /// <summary>
        /// The candidate a client draws highlighted. Always less than
        /// <see cref="CandidateCount"/>, and 0 when the list is empty. Read it
        /// from every fresh snapshot — the engine moves it too.
        /// </summary>
        public int CandidateCursor { get; }

        /// <summary>Whether any composition is in progress.</summary>
        public bool IsEmpty => Preedit.Length == 0 && Candidates.Count == 0;
    }
}
