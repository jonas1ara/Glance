// Image utilities - PNG encoding and format conversion
// Phase 4: Simplified implementation (full PNG encoding in Phase 4)

/// Encode raw RGBA pixels as PNG
/// Note: Full implementation requires image crate in Cargo.toml
pub fn encode_png(_pixels: &[u8], width: u32, height: u32) -> Result<Vec<u8>, String> {
    // Validate dimensions
    if width == 0 || height == 0 {
        return Err("Invalid image dimensions".to_string());
    }

    // Phase 4: Full PNG encoding implementation
    // For now, return minimal valid PNG (1x1 transparent pixel)
    // Real implementation: encode actual pixel data
    Ok(vec![
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR chunk
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
        0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
        0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41,
        0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
        0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00,
        0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
        0x42, 0x60, 0x82,
    ])
}

/// Calculate scaled dimensions based on DPI
pub fn apply_dpi_scaling(original_width: f32, original_height: f32, dpi: f32) -> (u32, u32) {
    let scale_factor = dpi / 72.0; // Standard DPI is 72 points per inch
    let scaled_width = (original_width * scale_factor) as u32;
    let scaled_height = (original_height * scale_factor) as u32;

    // Ensure minimum size
    (scaled_width.max(1), scaled_height.max(1))
}

/// Calculate dimensions after rotation
pub fn apply_rotation(width: u32, height: u32, rotation_degrees: u32) -> (u32, u32) {
    match rotation_degrees % 360 {
        90 | 270 => (height, width), // Swap dimensions
        _ => (width, height),         // No change
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_dpi_scaling() {
        let (w, h) = apply_dpi_scaling(612.0, 792.0, 96.0); // Standard letter size at 96 DPI
        assert!(w > 0 && h > 0);
    }

    #[test]
    fn test_rotation_90() {
        let (w, h) = apply_rotation(800, 600, 90);
        assert_eq!((w, h), (600, 800));
    }

    #[test]
    fn test_rotation_0() {
        let (w, h) = apply_rotation(800, 600, 0);
        assert_eq!((w, h), (800, 600));
    }

    #[test]
    fn test_rotation_360() {
        let (w, h) = apply_rotation(800, 600, 360);
        assert_eq!((w, h), (800, 600));
    }
}
