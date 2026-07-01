using System;
using System.IO;
using System.Runtime.InteropServices;

namespace FluentPdfViewer.Interop;

/// <summary>
/// P/Invoke wrapper for Rust FFI functions in glance_native.dll
/// </summary>
public static class GlanceNative
{
    private const string DllName = "glance_native";

    // Static constructor to register DLL import resolver
    static GlanceNative()
    {
        NativeLibrary.SetDllImportResolver(typeof(GlanceNative).Assembly, DllImportResolver);
    }

    /// <summary>
    /// Custom DLL import resolver to locate glance_native.dll
    /// Searches in multiple locations for the DLL
    /// </summary>
    private static IntPtr DllImportResolver(string libraryName, System.Reflection.Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != DllName)
            return IntPtr.Zero;

        System.Diagnostics.Debug.WriteLine($"[GlanceNative] Resolving {libraryName}...");

        // Try multiple locations
        string[] searchPaths = new[]
        {
            // 1. Current application directory
            AppContext.BaseDirectory,

            // 2. Application directory + one level up (for packaged apps)
            Path.Combine(AppContext.BaseDirectory, ".."),

            // 3. System PATH (let OS handle it)
            null
        };

        foreach (var searchPath_item in searchPaths)
        {
            if (searchPath_item == null)
                break; // Skip system PATH for now, use it only as last resort

            string dllPath = Path.Combine(searchPath_item, "glance_native.dll");
            System.Diagnostics.Debug.WriteLine($"[GlanceNative]   Trying: {dllPath}");

            if (File.Exists(dllPath))
            {
                System.Diagnostics.Debug.WriteLine($"[GlanceNative] ✓ Found at: {dllPath}");
                try
                {
                    IntPtr handle = NativeLibrary.Load(dllPath);
                    System.Diagnostics.Debug.WriteLine($"[GlanceNative] ✓ Loaded successfully!");
                    return handle;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[GlanceNative] ✗ Load failed: {ex.Message}");
                    // Continue searching
                }
            }
        }

        // Let the system try to find it in PATH
        System.Diagnostics.Debug.WriteLine($"[GlanceNative] Falling back to system PATH...");
        try
        {
            return NativeLibrary.Load(DllName);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GlanceNative] ✗ Could not load from PATH: {ex.Message}");
            return IntPtr.Zero;
        }
    }

    // ========================================================================
    // Render Options Structure (must match Rust #[repr(C)])
    // ========================================================================

    [StructLayout(LayoutKind.Sequential)]
    public struct RenderOptions
    {
        public uint PageIndex;
        public uint Width;
        public uint Height;
        public float Dpi;
    }

    // ========================================================================
    // Response Structure (must match Rust #[repr(C)])
    // ========================================================================

    [StructLayout(LayoutKind.Sequential)]
    public struct Response
    {
        public bool Success;
        public IntPtr ErrorMsg;      // UTF-8 C string, must be freed with memory_free()
        public IntPtr DataPtr;       // PNG bytes or JSON string, must be freed with memory_free()
        public ulong DataLen;        // Length of data_ptr
    }

    // ========================================================================
    // PDF Engine Functions (Phase 4)
    // ========================================================================

    /// <summary>
    /// Create a new PDF engine for the given file path.
    /// Returns an opaque pointer to PdfEngine; must be freed with pdf_engine_destroy().
    /// </summary>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr pdf_engine_create(string pdfPath);

    /// <summary>
    /// Destroy a PDF engine and free its resources.
    /// </summary>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pdf_engine_destroy(IntPtr engine);

    /// <summary>
    /// Render a specific page from the PDF to PNG bytes.
    ///
    /// Parameters:
    /// - engine: Pointer from pdf_engine_create()
    /// - opts: RenderOptions struct (page index, width, height, dpi)
    /// - outPngData: Receives pointer to PNG bytes (must free with memory_free())
    /// - outPngLen: Receives length of PNG data
    ///
    /// Returns: Response struct with success flag and error message if failed
    /// </summary>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Response pdf_render_page(
        IntPtr engine,
        ref RenderOptions opts,
        out IntPtr outPngData,
        out ulong outPngLen
    );

    // ========================================================================
    // Annotation Functions (Phase 2-3)
    // ========================================================================

    /// <summary>
    /// Save annotations to a JSON file.
    ///
    /// Parameters:
    /// - json: JSON string with annotation data (UTF-8)
    /// - path: File path where to save (UTF-8)
    ///
    /// Returns: Response struct with success flag
    /// </summary>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern Response annotations_save(string json, string path);

    /// <summary>
    /// Load annotations from a JSON file.
    ///
    /// Parameters:
    /// - path: File path to load from (UTF-8)
    ///
    /// Returns: Response struct with JSON data (must free outDataPtr with memory_free())
    /// </summary>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern Response annotations_load(string path);

    /// <summary>
    /// Process and validate annotations.
    ///
    /// Parameters:
    /// - json: JSON string with annotation data (UTF-8)
    ///
    /// Returns: Response struct with validation result
    /// </summary>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern Response annotations_process(string json);

    // ========================================================================
    // Memory Management
    // ========================================================================

    /// <summary>
    /// Free memory allocated by Rust FFI functions.
    /// MUST be called for all non-null pointers returned in Response.error_msg or Response.data_ptr.
    /// </summary>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void memory_free(IntPtr ptr);

    // ========================================================================
    // Test Functions (Phase 1 only)
    // ========================================================================

    /// <summary>
    /// Test function to verify DLL loads and basic FFI works.
    /// Returns true if FFI is functional.
    /// </summary>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool test_ffi_works();
}
