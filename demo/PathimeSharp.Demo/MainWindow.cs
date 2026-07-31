using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaKey = Avalonia.Input.Key;
using ImeKey = PathimeSharp.Key;

namespace PathimeSharp.Demo
{
    /// <summary>
    /// A phone-shaped window around <see cref="PhoneKeyboard"/>: a text field,
    /// a candidate strip, and an on-screen keyboard. Physical keys work too —
    /// letters and space go to the engine first, digits 1–9 tap candidates,
    /// arrows slide/page the strip, Ctrl+E cycles engines, Ctrl+T commits,
    /// Ctrl+R discards.
    /// </summary>
    public class MainWindow : Window
    {
        private static readonly FontFamily CjkFonts = new FontFamily(
            "Segoe UI, Microsoft YaHei, Malgun Gothic, Yu Gothic UI, Noto Sans CJK SC, Noto Sans");

        private static readonly IBrush KeyBrush = new SolidColorBrush(Color.Parse("#2E2E38"));
        private static readonly IBrush KeyHoverBrush = new SolidColorBrush(Color.Parse("#3A3A48"));
        private static readonly IBrush AccentBrush = new SolidColorBrush(Color.Parse("#4C6FFF"));
        private static readonly IBrush SettledBrush = new SolidColorBrush(Color.Parse("#7CE38B"));
        private static readonly IBrush TailBrush = new SolidColorBrush(Color.Parse("#F2D479"));
        private static readonly IBrush DimBrush = new SolidColorBrush(Color.Parse("#8A8A99"));

        private readonly PhoneKeyboard _phone;
        private readonly Button _engineButton;
        private readonly TextBlock _documentBlock;
        private readonly StackPanel _candidatePanel;
        private readonly TextBlock _pageLabel;

        public MainWindow(PhoneKeyboard phone)
        {
            _phone = phone;

            Title = "PathimeSharp Demo";
            Width = 420;
            Height = 760;
            CanResize = true;
            Background = new SolidColorBrush(Color.Parse("#17171C"));
            FontFamily = CjkFonts;
            Focusable = true;

            _engineButton = MakeKey("", () => { _phone.SwitchEngine(); Redraw(); }, width: 96);
            _engineButton.Background = AccentBrush;

            _documentBlock = new TextBlock
            {
                FontSize = 20,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.White,
            };

            _candidatePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                Height = 44,
            };

            _pageLabel = new TextBlock
            {
                FontSize = 11,
                Foreground = DimBrush,
                Margin = new Thickness(8, 0, 0, 0),
            };

            Content = BuildLayout();
            Redraw();

            // Keyboard events route to the focused element; nothing else takes
            // focus (keys are Focusable=false), so the window itself holds it.
            Opened += (_, _) => Focus();
        }

        private Control BuildLayout()
        {
            var root = new DockPanel { Margin = new Thickness(12) };

            // Header: engine key + hints.
            var header = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
            DockPanel.SetDock(_engineButton, Dock.Left);
            header.Children.Add(_engineButton);
            header.Children.Add(new TextBlock
            {
                Text = "Ctrl+E engine · Ctrl+T commit · Ctrl+R discard\n1–9 tap · ←→ slide · ↑↓ page",
                FontSize = 11,
                Foreground = DimBrush,
                TextAlignment = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
            });
            DockPanel.SetDock(header, Dock.Top);
            root.Children.Add(header);

            // Keyboard at the bottom.
            var keyboard = BuildKeyboard();
            DockPanel.SetDock(keyboard, Dock.Bottom);
            root.Children.Add(keyboard);

            // Candidate strip above the keyboard.
            var strip = new DockPanel { Margin = new Thickness(0, 8, 0, 8) };
            DockPanel.SetDock(_pageLabel, Dock.Right);
            strip.Children.Add(_pageLabel);
            strip.Children.Add(new ScrollViewer
            {
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Hidden,
                Content = _candidatePanel,
            });
            DockPanel.SetDock(strip, Dock.Bottom);
            root.Children.Add(strip);

            // The text field fills the rest.
            root.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.Parse("#1F1F26")),
                BorderBrush = new SolidColorBrush(Color.Parse("#33333F")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14),
                Child = new ScrollViewer { Content = _documentBlock },
            });

            return root;
        }

        private Control BuildKeyboard()
        {
            var rows = new StackPanel { Spacing = 6 };

            rows.Children.Add(KeyRow("qwertyuiop"));
            rows.Children.Add(KeyRow("asdfghjkl"));

            var thirdRow = KeyRow("zxcvbnm");
            thirdRow.Children.Add(MakeKey("⌫", () => Press(ImeKey.Backspace), width: 56));
            rows.Children.Add(thirdRow);

            var bottom = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            var space = MakeKey("␣", () => Press(' '), width: 180);
            bottom.Children.Add(MakeKey(",", () => Press(',')));
            bottom.Children.Add(space);
            bottom.Children.Add(MakeKey(".", () => Press('.')));
            bottom.Children.Add(MakeKey("⏎", () => Press(ImeKey.Return), width: 56));
            rows.Children.Add(bottom);

            return rows;
        }

        private StackPanel KeyRow(string letters)
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            foreach (char c in letters)
            {
                char key = c;
                row.Children.Add(MakeKey(c.ToString(), () => Press(key)));
            }

            return row;
        }

        private Button MakeKey(string label, Action onClick, double width = 32)
        {
            var button = new Button
            {
                Content = label,
                Width = width,
                Height = 42,
                Background = KeyBrush,
                Foreground = Brushes.White,
                CornerRadius = new CornerRadius(8),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(0),
                Focusable = false, // physical typing keeps working
            };
            button.PointerEntered += (_, _) => button.Background = KeyHoverBrush;
            button.PointerExited += (_, _) => button.Background = ReferenceEquals(button, _engineButton) ? AccentBrush : KeyBrush;
            button.Click += (_, _) => onClick();
            return button;
        }

        private void Press(char c)
        {
            _phone.Key(c);
            Redraw();
        }

        private void Press(ImeKey key)
        {
            _phone.Key(key);
            Redraw();
        }

        /* ---- physical keyboard ---- */

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);
            string? text = e.Text;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            foreach (char c in text!)
            {
                if (char.IsControl(c))
                {
                    continue;
                }

                if (c >= '1' && c <= '9' && _phone.Composition.CandidateCount > 0)
                {
                    _phone.TapCandidate(c - '0');
                }
                else
                {
                    _phone.Key(c);
                }
            }

            Redraw();
            e.Handled = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            bool ctrl = e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control);
            switch (e.Key)
            {
                case AvaloniaKey.E when ctrl:
                    _phone.SwitchEngine();
                    break;
                case AvaloniaKey.T when ctrl:
                    _phone.Commit();
                    break;
                case AvaloniaKey.R when ctrl:
                    _phone.Reset();
                    break;
                case AvaloniaKey.Back:
                    _phone.Key(ImeKey.Backspace);
                    break;
                case AvaloniaKey.Enter:
                    _phone.Key(ImeKey.Return);
                    break;
                case AvaloniaKey.Escape:
                    _phone.Key(ImeKey.Escape);
                    break;
                case AvaloniaKey.Left:
                    _phone.Slide(-1);
                    break;
                case AvaloniaKey.Right:
                    _phone.Slide(+1);
                    break;
                case AvaloniaKey.Up:
                    _phone.PageStrip(-1);
                    break;
                case AvaloniaKey.Down:
                    _phone.PageStrip(+1);
                    break;
                default:
                    base.OnKeyDown(e);
                    return;
            }

            Redraw();
            e.Handled = true;
        }

        /* ---- drawing ---- */

        private void Redraw()
        {
            EngineId active = _phone.Active;
            _engineButton.Content = PhoneKeyboard.EngineLabels.TryGetValue(active, out string? label)
                ? label
                : active.ToString();

            Composition comp = _phone.Composition;
            string doc = _phone.Text;

            // Document with the preedit drawn in at the cursor: settled green,
            // still-changing tail yellow, both underlined.
            var inlines = new InlineCollection();
            inlines.Add(new Run(doc.Substring(0, _phone.Cursor)));
            string settled = comp.Preedit.Substring(0, comp.PreeditSettled);
            string tail = comp.Preedit.Substring(comp.PreeditSettled);
            if (settled.Length > 0)
            {
                inlines.Add(new Run(settled)
                {
                    Foreground = SettledBrush,
                    TextDecorations = TextDecorations.Underline,
                });
            }

            if (tail.Length > 0)
            {
                inlines.Add(new Run(tail)
                {
                    Foreground = TailBrush,
                    TextDecorations = TextDecorations.Underline,
                });
            }

            inlines.Add(new Run("▏") { Foreground = AccentBrush });
            inlines.Add(new Run(doc.Substring(_phone.Cursor)));
            _documentBlock.Inlines = inlines;

            // The candidate strip.
            _candidatePanel.Children.Clear();
            var (visible, highlight) = _phone.Strip();
            if (_phone.Page > 0)
            {
                _candidatePanel.Children.Add(StripButton("◂", () => { _phone.PageStrip(-1); Redraw(); }, false));
            }

            for (int i = 0; i < visible.Count; i++)
            {
                int digit = i + 1;
                _candidatePanel.Children.Add(StripButton(
                    $"{digit} {visible[i]}",
                    () => { _phone.TapCandidate(digit); Redraw(); },
                    i == highlight));
            }

            if (comp.CandidateCount > (_phone.Page + 1) * PhoneKeyboard.StripSize
                || (comp.CandidateCount > 0
                    && comp.CandidateCount == _phone.Context.GetOptionInt(Option.MaxCandidates)))
            {
                _candidatePanel.Children.Add(StripButton("▸", () => { _phone.PageStrip(+1); Redraw(); }, false));
            }

            _pageLabel.Text = comp.CandidateCount > 0
                ? $"p{_phone.Page + 1} · {comp.CandidateCount}"
                : "";
        }

        private Button StripButton(string label, Action onClick, bool highlighted)
        {
            var button = new Button
            {
                Content = label,
                Height = 40,
                FontSize = 16,
                Background = highlighted ? AccentBrush : Brushes.Transparent,
                Foreground = Brushes.White,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 0),
                VerticalContentAlignment = VerticalAlignment.Center,
                Focusable = false,
            };
            button.Click += (_, _) => onClick();
            return button;
        }
    }
}
