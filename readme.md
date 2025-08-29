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

```
┌─────────────────────────────┬─────────────────────────────┬─────────────────────────────┐
│        ScriptBox (C#)       │       RazorBox (Template)   │     HtmlPreview (Browser)   │
│                             │                             │                             │
│   // C#-Skript ...          │   @model dynamic            │   <h1>Hello Welt</h1>       │
│   new {Name = "Welt"        │   <h1>Hello @Model.Name</h1>│   <p>Jetzt: ...</p>         │
│        ... }                │   ...                       │   <ul><li>Alpha</li> ...    │
└─────────────────────────────┴─────────────────────────────┴─────────────────────────────┘
```
