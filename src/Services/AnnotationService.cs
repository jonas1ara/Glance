using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FluentPdfViewer.Interop;

namespace Glance.Services;

/// <summary>
/// Service for validating annotations via Rust backend.
/// Ensures all annotations meet requirements before persisting.
/// </summary>
public class AnnotationService
{
    /// <summary>
    /// Validate annotations using Rust backend.
    ///
    /// The Rust side deserializes the JSON and validates each annotation:
    /// - Geometry bounds (x, y, width, height must be valid)
    /// - Color format (#RRGGBB)
    /// - Page index >= 0
    /// - Annotation type is valid
    ///
    /// If any annotation fails validation, an exception is thrown with the error.
    /// </summary>
    /// <param name="annotationJson">Serialized annotations as JSON string</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <exception cref="InvalidOperationException">If validation fails</exception>
    public async Task ValidateAnnotationsAsync(
        string annotationJson,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(annotationJson))
        {
            throw new ArgumentException("Annotations JSON cannot be null or empty", nameof(annotationJson));
        }

        await AsyncBridge.RunNativeAsync(() =>
        {
            var response = GlanceNative.annotations_process(annotationJson);

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

                throw new InvalidOperationException($"Annotation validation failed: {errorMsg}");
            }

            System.Diagnostics.Debug.WriteLine("✓ Annotations validated successfully");
        }, cancellationToken);
    }
}
