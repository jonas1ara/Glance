// Annotation processor - Validation and processing logic
use crate::storage::json_adapter::SavedAnnotation;

/// Rectangle struct for geometry operations
#[derive(Clone, Copy)]
pub struct Rect {
    pub x: f64,
    pub y: f64,
    pub width: f64,
    pub height: f64,
}

/// Validate a single annotation
pub fn validate_annotation(annot: &SavedAnnotation) -> Result<(), String> {
    // Validate coordinates
    if annot.x < 0.0 {
        return Err("X coordinate cannot be negative".to_string());
    }

    if annot.y < 0.0 {
        return Err("Y coordinate cannot be negative".to_string());
    }

    // Validate dimensions
    if annot.width <= 0.0 {
        return Err("Width must be greater than 0".to_string());
    }

    if annot.height <= 0.0 {
        return Err("Height must be greater than 0".to_string());
    }

    // Validate color format (#RRGGBB)
    if !is_valid_hex_color(&annot.color_hex) {
        return Err(format!("Invalid color format: '{}' (expected #RRGGBB)", annot.color_hex));
    }

    Ok(())
}

/// Validate all annotations
pub fn validate_all(annotations: &[SavedAnnotation]) -> Result<(), String> {
    for (index, annot) in annotations.iter().enumerate() {
        validate_annotation(annot)
            .map_err(|e| format!("Annotation #{}: {}", index, e))?;
    }
    Ok(())
}

/// Calculate highlight transparency alpha value
/// Returns 0x50 (80 decimal = 31% opacity) for highlights
pub fn calculate_highlight_alpha(_hex: &str) -> Result<u8, String> {
    // 31% opacity = 80/255 ≈ 0.31 → 0x50 in hex
    Ok(0x50)
}

/// Check if hex color string is valid (#RRGGBB format)
fn is_valid_hex_color(hex: &str) -> bool {
    // Must start with #
    if !hex.starts_with('#') {
        return false;
    }

    // Must be #RRGGBB (7 chars total)
    if hex.len() != 7 {
        return false;
    }

    // Check if remaining 6 chars are hex digits
    hex[1..].chars().all(|c| c.is_ascii_hexdigit())
}

/// Check if two rectangles overlap
pub fn geometry_overlap(rect1: &Rect, rect2: &Rect) -> bool {
    let rect1_right = rect1.x + rect1.width;
    let rect1_bottom = rect1.y + rect1.height;

    let rect2_right = rect2.x + rect2.width;
    let rect2_bottom = rect2.y + rect2.height;

    !(rect1_right < rect2.x || rect1.x > rect2_right ||
      rect1_bottom < rect2.y || rect1.y > rect2_bottom)
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_valid_annotation() {
        let annot = SavedAnnotation {
            page_index: 0,
            annotation_type: 0,
            x: 10.0,
            y: 20.0,
            width: 100.0,
            height: 50.0,
            color_hex: "#FFFF00".to_string(),
            content: "test".to_string(),
            points: vec![],
        };

        assert!(validate_annotation(&annot).is_ok());
    }

    #[test]
    fn test_negative_x() {
        let annot = SavedAnnotation {
            page_index: 0,
            annotation_type: 0,
            x: -10.0,
            y: 20.0,
            width: 100.0,
            height: 50.0,
            color_hex: "#FFFF00".to_string(),
            content: "test".to_string(),
            points: vec![],
        };

        assert!(validate_annotation(&annot).is_err());
    }

    #[test]
    fn test_invalid_color() {
        let annot = SavedAnnotation {
            page_index: 0,
            annotation_type: 0,
            x: 10.0,
            y: 20.0,
            width: 100.0,
            height: 50.0,
            color_hex: "FFFF00".to_string(), // Missing #
            content: "test".to_string(),
            points: vec![],
        };

        assert!(validate_annotation(&annot).is_err());
    }

    #[test]
    fn test_hex_color_validation() {
        assert!(is_valid_hex_color("#FFFFFF"));
        assert!(is_valid_hex_color("#000000"));
        assert!(is_valid_hex_color("#FFFF00"));
        assert!(!is_valid_hex_color("FFFFFF")); // Missing #
        assert!(!is_valid_hex_color("#FFF")); // Too short
        assert!(!is_valid_hex_color("#GGGGGG")); // Invalid hex
    }

    #[test]
    fn test_calculate_alpha() {
        let alpha = calculate_highlight_alpha("#FFFF00").unwrap();
        assert_eq!(alpha, 0x50); // 31% opacity
    }

    #[test]
    fn test_rectangle_overlap() {
        let rect1 = Rect { x: 0.0, y: 0.0, width: 100.0, height: 100.0 };
        let rect2 = Rect { x: 50.0, y: 50.0, width: 100.0, height: 100.0 };
        assert!(geometry_overlap(&rect1, &rect2));

        let rect3 = Rect { x: 200.0, y: 200.0, width: 100.0, height: 100.0 };
        assert!(!geometry_overlap(&rect1, &rect3));
    }
}
