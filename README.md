# Glance

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Framework: WinUI 3](https://img.shields.io/badge/Framework-WinUI_3-purple.svg)](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/)
[![Platform: Windows 11](https://img.shields.io/badge/Platform-Windows_11-blue.svg)](https://www.microsoft.com/windows/)
[![Language: C#](https://img.shields.io/badge/Language-C%23-green.svg)](https://learn.microsoft.com/en-us/dotnet/csharp/)

Glance is a fast, lightweight, and elegant PDF document viewer designed specifically for Windows 11. Built from the ground up following Fluent Design principles, it offers a visually integrated experience featuring Mica transparency and smooth transitions. It combines the speed and minimalist design of classic document viewers like GNOME Evince with modern annotation tools inspired by Adobe Acrobat.

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

## System Requirements and Tech Stack

* **Operating System:** Windows 10 (version 1809 or higher) / Windows 11 (Recommended for native Mica backdrop).
* **Platform:** Windows App SDK 1.5+ (WinUI 3).
* **Runtime Environment:** .NET 10.0.
* **Core Libraries:** Native Windows.Data.Pdf API for fast and lightweight page parsing without heavy third-party dependencies.

---

## Building and Running

To clone and compile Glance locally using the .NET SDK:

1. **Clone the repository:**
   ```bash
   git clone https://github.com/your-username/glance.git
   cd glance
   ```

2. **Build and Run the packaged application natively:**
   ```bash
   dotnet run --project src/Glance.csproj
   ```

(The Windows App SDK build environment will register a temporary developer package identity on your system and launch the Glance viewer interface).

---

## License

This project is licensed under the MIT License. See the LICENSE file for details.
