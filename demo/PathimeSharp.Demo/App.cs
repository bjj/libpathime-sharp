using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Themes.Fluent;

namespace PathimeSharp.Demo
{
    public class App : Application
    {
        private PhoneKeyboard? _phone;

        public override void Initialize()
        {
            Styles.Add(new FluentTheme());
            RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                try
                {
                    desktop.MainWindow = BuildPhone();
                }
                catch (Exception e)
                {
                    desktop.MainWindow = ErrorWindow(e.Message);
                }

                desktop.Exit += (_, _) =>
                {
                    _phone?.Dispose();
                    try
                    {
                        Pathime.Shutdown();
                    }
                    catch (DllNotFoundException)
                    {
                        // Never loaded; nothing to shut down.
                    }
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private Window BuildPhone()
        {
            // Same search the tests use: PATHIME_LIBRARY, then the staged copy.
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PATHIME_LIBRARY")))
            {
                string staged = StagedLibraryPath();
                if (File.Exists(staged))
                {
                    Pathime.Load(staged);
                }
            }

            Pathime.Init(); // default data dir: the demo genuinely learns

            var available = new List<EngineId>();
            foreach (EngineId id in Enum.GetValues(typeof(EngineId)))
            {
                if (Pathime.HasEngine(id))
                {
                    available.Add(id);
                }
            }

            if (available.Count == 0)
            {
                throw new InvalidOperationException(
                    "No engines available — is pathime-data/ beside the native library? " +
                    "Build libpathime with `cmake --install` and stage it (see README.md).");
            }

            // Start on pinyin when present; Ctrl+E cycles the rest.
            if (available.Remove(EngineId.Pinyin))
            {
                available.Insert(0, EngineId.Pinyin);
            }

            _phone = new PhoneKeyboard(available);
            return new MainWindow(_phone);
        }

        private static Window ErrorWindow(string message)
        {
            return new Window
            {
                Title = "PathimeSharp Demo",
                Width = 560,
                Height = 240,
                Content = new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(24),
                    VerticalAlignment = VerticalAlignment.Center,
                },
            };
        }

        private static string StagedLibraryPath()
        {
            string rid = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win-x64" : "linux-x64";
            string fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "pathime.dll" : "libpathime.so";
            // demo/PathimeSharp.Demo/bin/<cfg>/<tfm>/ -> repo root
            string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
            return Path.Combine(root, "artifacts", "native", rid, fileName);
        }
    }
}
