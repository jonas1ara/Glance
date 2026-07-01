# Glance Frontend (C# WinUI 3)

User interface and application logic for Glance PDF viewer. Handles rendering, annotations, and all user interactions.

## What C# Does Here

Glance's frontend is built in C# with WinUI 3 to provide:

- **Native Windows 11 UI:** Mica backdrop, Fluent Design, native aesthetics
- **Rapid Development:** High-level C# with .NET 10.0 ecosystem
- **Modern XAML:** Declarative UI with data binding and MVVM
- **FFI Bridge:** P/Invoke interop with Rust backend (`glance_native.dll`)

## Architecture

```
src/
├── MainPage.xaml(.cs)           # Main UI + page rendering
├── App.xaml(.cs)                # Application entry point
├── Interop/
│   ├── GlanceNative.cs          # P/Invoke FFI declarations
│   └── AsyncBridge.cs           # C# async ↔ Rust sync bridge
├── Services/
│   ├── PdfRenderService.cs      # PDF rendering wrapper
│   ├── AnnotationService.cs     # Annotation processing
│   └── PersistenceService.cs    # JSON file I/O
├── ViewModels/
│   ├── PdfPageViewModel.cs      # Page data + UI state
│   └── AnnotationViewModel.cs   # Annotation data
├── Assets/                      # Images, icons, resources
├── Properties/                  # App configuration
└── build/                       # Build automation scripts
```

## Key Components

### MainPage.xaml / MainPage.xaml.cs
- **PDF Display:** ScrollViewer with virtualized page list
- **Annotations:** Canvas overlay for drawing, highlighting, notes
- **Toolbar:** Edit modes (Pen, Highlight, Note), zoom, rotation
- **Sidebar:** Page thumbnails for navigation
- **Lazy Loading:** First 10 pages instant, remainder in background

### Services

#### PdfRenderService.cs
- `InitializeAsync(filePath)` - Load PDF document
- `RenderPageAsync(pageIndex)` - Render single page to bitmap
- Called via AsyncBridge for thread-safe FFI

#### AnnotationService.cs
- Validate annotations (geometry, bounds)
- Process highlights, notes, pen strokes
- Calculate opacity (31% for highlight transparency)

#### PersistenceService.cs
- Save annotations to JSON file (`annotations.json`)
- Load annotations on PDF open
- Auto-save on pointer release

### Interop Layer

#### GlanceNative.cs
- P/Invoke declarations for `glance_native.dll`
- Struct definitions: `RenderOptions`, `Response`
- DllImportResolver for dynamic DLL discovery
- Memory management (`memory_free()`)

#### AsyncBridge.cs
- `RunNativeAsync<T>(nativeCall)` - Wrapper for FFI calls
- Runs on thread pool to prevent UI blocking
- Returns Task<T> for async/await integration

## Building

```bash
# Prerequisites
dotnet tool install --global Microsoft.Windows.CsWin32

# Build
dotnet build

# Build Release (optimized)
dotnet build --configuration Release

# Run
dotnet run
```

## Requirements

- **.NET 10.0 SDK** - [dotnet.microsoft.com](https://dotnet.microsoft.com)
- **Windows App SDK 1.5+** - For WinUI 3
- **Visual Studio 2022** (optional) - For IDE development

## Testing

Tests located in `/tests/`:

- `TestDllLoad/` - Verify `glance_native.dll` loads
- `CHECK_DLL_DEPENDENCIES.ps1` - Check missing dependencies
- `test-phase2.cs` - Persistence layer validation

Run from project root:
```bash
dotnet script src/tests/test-phase2.cs
.\src\tests\CHECK_DLL_DEPENDENCIES.ps1
cd src/tests/TestDllLoad && dotnet run
```

## Development Workflow

1. **Edit XAML** (`MainPage.xaml`) - Layout, bindings
2. **Update C#** (`MainPage.xaml.cs`) - Event handlers, logic
3. **Call Rust** (via AsyncBridge + GlanceNative) - PDF operations
4. **Test locally** (`dotnet run`) - Launch app
5. **Commit** - Git push to GitHub

## Dependencies

- **Microsoft.WindowsAppSDK** 2.2.0 - WinUI 3 framework
- **System.Text.Json** - Built-in (annotation serialization)
- **Windows.Data.Pdf** - Built-in (PDF fallback rendering)

See `Glance.csproj` for full dependency list.

## Philosophy

- **Fluent Design First:** Respect Windows 11 native aesthetics
- **Performance Conscious:** Lazy loading, virtualization, async/await
- **User-Focused:** Keyboard shortcuts, smooth transitions, responsive
- **Minimal Dependencies:** Use built-in .NET libraries when possible
