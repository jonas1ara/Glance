using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FluentPdfViewer.Interop;

namespace Glance.Services;

/// <summary>
/// Service for persisting annotations to disk via Rust backend.
/// Handles serialization, Rust FFI calls, and async/await bridge.
/// </summary>
public class PersistenceService
{
    /// <summary>
    /// Save annotations to a JSON file using Rust backend.
    ///
    /// The annotations JSON is validated on the Rust side before writing to disk.
    /// If validation fails, an exception is thrown with the error message.
    /// </summary>
    /// <param name="annotationJson">Serialized annotations as JSON string (UTF-8)</param>
    /// <param name="filePath">File path where annotations will be saved</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <exception cref="InvalidOperationException">If Rust validation or file write fails</exception>
    public async Task SaveAnnotationsAsync(
        string annotationJson,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(annotationJson))
        {
            throw new ArgumentException("Annotations JSON cannot be null or empty", nameof(annotationJson));
        }

        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
        }

        await AsyncBridge.RunNativeAsync(() =>
        {
            var response = GlanceNative.annotations_save(annotationJson, filePath);

            if (!response.Success)
            {
                string errorMsg = response.ErrorMsg != IntPtr.Zero
                    ? Marshal.PtrToStringAnsi(response.ErrorMsg) ?? "Unknown error"
                    : "Unknown error (null error message)";

                // Free error message
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
    /// Load annotations from a JSON file using Rust backend.
    ///
    /// The Rust side reads the file and validates the JSON structure.
    /// If validation fails, an exception is thrown.
    /// </summary>
    /// <param name="filePath">File path to load annotations from</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>JSON string with loaded annotations</returns>
    /// <exception cref="InvalidOperationException">If file read or validation fails</exception>
    public async Task<string> LoadAnnotationsAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
        }

        return await AsyncBridge.RunNativeAsync(() =>
        {
            var response = GlanceNative.annotations_load(filePath);

            if (!response.Success)
            {
                string errorMsg = response.ErrorMsg != IntPtr.Zero
                    ? Marshal.PtrToStringAnsi(response.ErrorMsg) ?? "Unknown error"
                    : "Unknown error (null error message)";

                // Free error message
                if (response.ErrorMsg != IntPtr.Zero)
                {
                    GlanceNative.memory_free(response.ErrorMsg);
                }

                throw new InvalidOperationException($"Failed to load annotations: {errorMsg}");
            }

            // Marshal JSON string from unmanaged memory
            string jsonData = response.DataPtr != IntPtr.Zero && response.DataLen > 0
                ? Marshal.PtrToStringAnsi(response.DataPtr) ?? ""
                : "";

            // Free native memory (CRITICAL)
            if (response.DataPtr != IntPtr.Zero)
            {
                GlanceNative.memory_free(response.DataPtr);
            }

            System.Diagnostics.Debug.WriteLine($"✓ Annotations loaded ({response.DataLen} bytes)");
            return jsonData;
        }, cancellationToken);
    }
}
