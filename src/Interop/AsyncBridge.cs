using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace FluentPdfViewer.Interop;

/// <summary>
/// Bridge between C# async/await and synchronous Rust FFI calls.
/// Offloads FFI calls to thread pool to prevent UI blocking.
/// </summary>
public static class AsyncBridge
{
    /// <summary>
    /// Run a synchronous native FFI call on the thread pool asynchronously.
    /// This prevents blocking the UI thread during long operations.
    /// </summary>
    /// <typeparam name="T">Return type of native call</typeparam>
    /// <param name="nativeCall">The native FFI function to call (as lambda)</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Task that completes when native call returns</returns>
    public static async Task<T> RunNativeAsync<T>(
        Func<T> nativeCall,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(nativeCall, cancellationToken);
    }

    /// <summary>
    /// Run a synchronous native FFI call on the thread pool asynchronously (no return value).
    /// </summary>
    /// <param name="nativeCall">The native FFI action to call (as lambda)</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Task that completes when native call returns</returns>
    public static async Task RunNativeAsync(
        Action nativeCall,
        CancellationToken cancellationToken = default)
    {
        await Task.Run(nativeCall, cancellationToken);
    }

    /// <summary>
    /// Example: Render a PDF page asynchronously.
    /// Shows how to use AsyncBridge with P/Invoke FFI calls.
    /// </summary>
    public static async Task<byte[]> RenderPageAsync(
        IntPtr pdfEngine,
        uint pageIndex,
        uint width,
        uint height,
        CancellationToken cancellationToken = default)
    {
        return await RunNativeAsync(() =>
        {
            var opts = new GlanceNative.RenderOptions
            {
                PageIndex = pageIndex,
                Width = width,
                Height = height,
                Dpi = 96.0f
            };

            var response = GlanceNative.pdf_render_page(
                pdfEngine,
                ref opts,
                out var dataPtrNative,
                out var dataLen
            );

            if (!response.Success)
            {
                // Extract error message from Rust
                string errorMsg = response.ErrorMsg != IntPtr.Zero
                    ? Marshal.PtrToStringAnsi(response.ErrorMsg) ?? "Unknown error"
                    : "Unknown error (null error message)";

                // Free error message
                if (response.ErrorMsg != IntPtr.Zero)
                {
                    GlanceNative.memory_free(response.ErrorMsg);
                }

                throw new InvalidOperationException($"PDF rendering failed: {errorMsg}");
            }

            // Marshal PNG bytes from unmanaged to managed memory
            byte[] pngBytes = new byte[dataLen];
            if (dataPtrNative != IntPtr.Zero && dataLen > 0)
            {
                Marshal.Copy(dataPtrNative, pngBytes, 0, (int)dataLen);
            }

            // Free native memory (CRITICAL - prevents memory leak)
            if (dataPtrNative != IntPtr.Zero)
            {
                GlanceNative.memory_free(dataPtrNative);
            }

            return pngBytes;
        }, cancellationToken);
    }

    /// <summary>
    /// Example: Save annotations asynchronously.
    /// Shows how to handle Response struct and error checking.
    /// </summary>
    public static async Task SaveAnnotationsAsync(
        string annotationJson,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        await RunNativeAsync(() =>
        {
            var response = GlanceNative.annotations_save(annotationJson, filePath);

            if (!response.Success)
            {
                string errorMsg = response.ErrorMsg != IntPtr.Zero
                    ? Marshal.PtrToStringAnsi(response.ErrorMsg) ?? "Unknown error"
                    : "Unknown error";

                if (response.ErrorMsg != IntPtr.Zero)
                {
                    GlanceNative.memory_free(response.ErrorMsg);
                }

                throw new InvalidOperationException($"Failed to save annotations: {errorMsg}");
            }

            System.Diagnostics.Debug.WriteLine("✓ Annotations saved successfully");
        }, cancellationToken);
    }

    /// <summary>
    /// Example: Load annotations asynchronously.
    /// </summary>
    public static async Task<string> LoadAnnotationsAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        return await RunNativeAsync(() =>
        {
            var response = GlanceNative.annotations_load(filePath);

            if (!response.Success)
            {
                string errorMsg = response.ErrorMsg != IntPtr.Zero
                    ? Marshal.PtrToStringAnsi(response.ErrorMsg) ?? "Unknown error"
                    : "Unknown error";

                if (response.ErrorMsg != IntPtr.Zero)
                {
                    GlanceNative.memory_free(response.ErrorMsg);
                }

                throw new InvalidOperationException($"Failed to load annotations: {errorMsg}");
            }

            // Marshal JSON string from unmanaged memory
            string jsonData = response.DataPtr != IntPtr.Zero
                ? Marshal.PtrToStringAnsi(response.DataPtr) ?? ""
                : "";

            // Free native memory
            if (response.DataPtr != IntPtr.Zero)
            {
                GlanceNative.memory_free(response.DataPtr);
            }

            return jsonData;
        }, cancellationToken);
    }
}
