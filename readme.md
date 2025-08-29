# RazorEngineSandbox

Eine kleine **WPF-Sandbox**, die C#-Scripting (Roslyn) mit RazorLight kombiniert.  
Ziel: Links ein C#-Script, Mitte ein Razor-Template, rechts die gerenderte HTML-Ausgabe.

---

## Features

- **Dreigeteiltes UI** (per Splitter anpassbar):
  - **Links:** C#-Script (`Microsoft.CodeAnalysis.CSharp.Scripting`)  
    → Erzeugt ein Datenobjekt (Model).
  - **Mitte:** Razor-Template (`RazorLight`)  
    → Nutzt das Model und generiert HTML.
  - **Rechts:** HTML-Preview (`WebBrowser`-Control in WPF).
- **Render-Button** und **Auto-Render** mit Debounce (500 ms).
- **Fehlerausgabe** für Script- und Templatekompilierung.
- **Dark Mode** Layout in `MainWindow.xaml`.