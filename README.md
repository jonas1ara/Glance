# Glance

![Glance Logo](docs/img/logo_paramarca.png)

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Framework: WinUI 3](https://img.shields.io/badge/Framework-WinUI_3-purple.svg)](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/)
[![Platform: Windows 11](https://img.shields.io/badge/Platform-Windows_11-blue.svg)](https://www.microsoft.com/windows/)
[![Language: C#](https://img.shields.io/badge/Language-C%23-green.svg)](https://learn.microsoft.com/en-us/dotnet/csharp/)

Glance is a fast, lightweight, and elegant PDF document viewer designed specifically for Windows 11. Built from the ground up following Fluent Design principles, it offers a visually integrated experience featuring Mica transparency and smooth transitions. It combines the speed and minimalist design of classic document viewers like GNOME Evince with modern annotation tools inspired by Adobe Acrobat.

<!-- ![Glance Demo](docs/img/Glance.gif) -->

---

## Key Features

* **Premium Aesthetics and Fluent Design:**
  * Native use of Windows 11 Mica backdrop that adapts dynamically to the user's desktop wallpaper.
  * Integrated window title bar and translucent sidebar for an immersive reading experience.
  * Full support for automatic light and dark system themes.
* **Visual Welcome Screen (Evince-style):**
  * Grid layout displaying recent documents using cover page thumbnails rendered from the first page of each PDF.
  * Single-click quick access to recently opened files with automatic registry cleanup if files are moved or deleted.
* **Freehand Drawing Tools:**
  * Smooth digital freehand ink drawing with a rounded pen pointer (PenMode), ideal for digital signatures, sketches, or writing handwritten notes directly on the page.
* **Precision Highlighter Tool:**
  * Accurate text selection highlight mapping.
  * Dynamic 7-color palette (Yellow, Green, Cyan, Magenta, Red, Blue, Black) that appears exclusively in editing modes.
  * Automatic Alpha channel calculation (31% opacity) to ensure the translucent color highlights the text without obscuring the original content.
* **Sticky Notes:**
  * Drop floating comment bubbles anywhere on the PDF with a clean popover overlay to write, view, edit, and store reader remarks.
* **Auto-saving Annotations:**
  * All notes, highlights, and freehand drawing strokes are saved automatically to a local JSON database upon pointer release, ensuring immediate persistence.
* **Real-time Document Rotation:**
  * Native rotation controls to rotate the document in 90-degree increments, dynamically updating dimensions to prevent page clipping.
* **Keyboard Shortcuts (Undo):**
  * Full edit history with support for undoing annotations using the universal Ctrl + Z shortcut.
* **Fluent Sidebar Index:**
  * Navigation via high-definition page thumbnails rendered sequentially to prevent visual layout scrambling during UI virtualization recycling.

---

## Architecture

Glance uses a **hybrid C# + Rust architecture** for optimal performance:

* **Frontend:** WinUI 3 (C#/.NET 10.0) - Native Windows 11 aesthetics with Mica backdrop
* **Backend:** Rust - Native performance for PDF rendering and storage
* **Bridge:** P/Invoke FFI - Type-safe C# ↔ Rust interop

---

## System Requirements

* **OS:** Windows 10 or later
* **Platform:** Windows App SDK 1.5+ (WinUI 3)
* **Runtime:** .NET 10.0

---

<details>
<summary><strong>📚 Development & Building (For Contributors)</strong></summary>

### Prerequisites

* **.NET 10.0 SDK** - [dotnet.microsoft.com](https://dotnet.microsoft.com)
* **Rust 1.70+** - [rustup.rs](https://rustup.rs)
* **Visual Studio 2022** (optional)

### Build Instructions

1. Clone repository:
   ```bash
   git clone https://github.com/your-username/glance.git
   cd glance
   ```

2. Build Rust backend:
   ```bash
   cd native/glance-native
   cargo build --release
   ```

3. Copy DLL:
   ```bash
   copy target\release\glance_native.dll ..\..\src\bin\Debug\
   ```

4. Build C# frontend:
   ```bash
   cd ../../src
   dotnet build
   ```

5. Run:
   ```bash
   dotnet run --project Glance.csproj
   ```

### Architecture Phases (Completed)

- **Phase 1** ✅ - FFI Foundation (P/Invoke bridge)
- **Phase 2** ✅ - Persistence (JSON file I/O)
- **Phase 3** ✅ - Annotation Processing (validation, geometry)
- **Phase 4** ✅ - PDF Rendering (lazy loading, async)

### Build Variants

- **Debug:** `cargo build --debug && dotnet build`
- **Release:** `cargo build --release && dotnet build -c Release`
- **Clean:** `cargo clean && dotnet clean`

</details>

---

## License

This project is licensed under the MIT License. See the LICENSE file for details.
