// MainWindow.xaml.cs — RazorEngineSandbox
// Code-Behind: Enthält die Logik für C#-Scripting (Roslyn) + RazorLight Rendering + HTML-Preview
// Benötigte NuGet-Pakete: Microsoft.CodeAnalysis.CSharp.Scripting, RazorLight (v2)

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;

using RazorLight;
using RazorLight.Compilation;

namespace RazorEngineSandbox
{
    public partial class MainWindow : Window
    {
        // Timer für Auto-Render mit Verzögerung (Debounce)
        private readonly DispatcherTimer _debounce;

        // RazorLight Engine zum Kompilieren und Rendern von Templates
        private readonly RazorLightEngine _razorEngine;

        public MainWindow()
        {
            InitializeComponent();

            // RazorLight Engine initialisieren
            _razorEngine = new RazorLightEngineBuilder()
                .UseMemoryCachingProvider() // Speicher-Caching nutzen
                .Build();

            // Debounce-Timer initialisieren (500ms)
            _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _debounce.Tick += async (s, e) => { _debounce.Stop(); await RenderAsync(); };

            InitializeDefaults();
            HookEvents();
        }

        // Events an UI-Elemente binden
        private void HookEvents()
        {
            RenderBtn.Click += async (s, e) => await RenderAsync();
            AutoRenderChk.Checked += (s, e) => StatusText.Text = "Auto-Render an";
            AutoRenderChk.Unchecked += (s, e) => StatusText.Text = "Auto-Render aus";

            ScriptBox.TextChanged += OnEditorTextChanged;
            RazorBox.TextChanged += OnEditorTextChanged;
        }

        // Textänderungen im Editor → Debounce starten, falls AutoRender aktiv
        private void OnEditorTextChanged(object sender, TextChangedEventArgs e)
        {
            if (AutoRenderChk.IsChecked == true)
            {
                _debounce.Stop();
                _debounce.Start();
            }
        }

        // Standardwerte für Script und Razor-Template setzen
        private void InitializeDefaults()
        {
            ScriptBox.Text =
                "// C#-Script: Erzeuge ein Datenobjekt für das Razor-Template\n" +
                "// Das Ergebnis ist der Wert des letzten Ausdrucks.\n" +
                "new {\n" +
                "    Name = \"Welt\",\n" +
                "    Items = new [] { \"Alpha\", \"Beta\", \"Gamma\" },\n" +
                "    Now = DateTime.Now\n" +
                "}\n";

            RazorBox.Text =
                "@model dynamic\n" +
                "<h1>Hello @Model.Name</h1>\n" +
                "<p>Jetzt: @Model.Now</p>\n" +
                "<ul>\n" +
                "@foreach (var it in Model.Items) {<li>@it</li>}\n" +
                "</ul>\n";

            _ = RenderAsync();
        }

        // Render-Prozess: Modell evaluieren, Razor-Template anwenden, HTML anzeigen
        private async Task RenderAsync()
        {
            try
            {
                ErrorText.Text = string.Empty;
                StatusText.Text = "Render läuft…";

                // 1) Model aus C#-Script erzeugen
                var model = await EvaluateModelAsync(ScriptBox.Text);

                // 2) Razor-Template anwenden
                string template = RazorBox.Text ?? string.Empty;
                string key = "tpl_" + template.GetHashCode();

                string bodyHtml = await _razorEngine.CompileRenderStringAsync(key, template, (object)model);

                // 3) HTML umschließen und im Browser anzeigen
                string doc = WrapHtml(bodyHtml);
                HtmlPreview.NavigateToString(doc);

                StatusText.Text = $"Gerendert um {DateTime.Now:HH:mm:ss}";
            }
            catch (CompilationErrorException cex)
            {
                // Fehler beim C#-Scripting
                ErrorText.Text = "C#-Skriptfehler:\n" + string.Join("\n", cex.Diagnostics.Select(d => d.ToString()));
                StatusText.Text = "Fehler bei Skriptauswertung";
            }
            catch (TemplateCompilationException tex)
            {
                // Fehler beim Kompilieren des Razor-Templates
                var sb = new StringBuilder();
                sb.AppendLine("Razor-Templatefehler:");
                foreach (var err in tex.CompilationErrors)
                {
                    sb.AppendLine(err);
                }
                ErrorText.Text = sb.ToString();
                StatusText.Text = "Fehler bei Templatekompilierung";
            }
            catch (Exception ex)
            {
                // Generische Fehlerausgabe
                ErrorText.Text = ex.ToString();
                StatusText.Text = "Fehler";
            }
        }

        // HTML-Dokument mit Basis-Struktur und Dark-Mode-Farben umschließen
        private static string WrapHtml(string body)
        {
            return "<!doctype html><html style=\"color:white;background-color:#252526\"><head><meta charset=\"utf-8\"><meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\"></head><body>" + body + "</body></html>";
        }

        // Script auswerten und Objektmodell erzeugen
        private async Task<object> EvaluateModelAsync(string script)
        {
            if (string.IsNullOrWhiteSpace(script))
                return new { };

            var options = ScriptOptions.Default
                .WithReferences(GetAllMetadataReferences())
                .WithImports(
                    "System",
                    "System.Linq",
                    "System.Collections.Generic",
                    "System.Dynamic"
                );

            var result = await CSharpScript.EvaluateAsync<object>(script, options);
            return result ?? new { };
        }

        // Alle geladenen Assemblies einsammeln und als MetadataReferences bereitstellen
        private static IEnumerable<MetadataReference> GetAllMetadataReferences()
        {
            var list = new List<MetadataReference>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.IsDynamic) continue;
                try
                {
                    var location = asm.Location;
                    if (!string.IsNullOrWhiteSpace(location) && File.Exists(location))
                    {
                        list.Add(MetadataReference.CreateFromFile(location));
                    }
                }
                catch { }
            }

            // Wichtige Basis-Assemblies sicherstellen
            TryAdd(typeof(object).Assembly);
            TryAdd(typeof(Enumerable).Assembly);
            TryAdd(typeof(Uri).Assembly);

            return list;

            void TryAdd(Assembly a)
            {
                try
                {
                    if (!a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
                    {
                        var mr = MetadataReference.CreateFromFile(a.Location);
                        if (!list.Any(x => (x as PortableExecutableReference)?.FilePath == a.Location))
                            list.Add(mr);
                    }
                }
                catch { }
            }
        }
    }
}
