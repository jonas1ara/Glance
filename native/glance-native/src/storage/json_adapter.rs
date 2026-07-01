// JSON adapter - Serialization/deserialization with serde_json
use serde::{Deserialize, Serialize};

/// Save annotation struct for JSON serialization
#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct SavedAnnotation {
    #[serde(rename = "pageIndex")]
    pub page_index: u32,
    #[serde(rename = "type")]
    pub annotation_type: u32, // 0=Highlight, 1=Note, 2=Pen
    pub x: f64,
    pub y: f64,
    pub width: f64,
    pub height: f64,
    #[serde(rename = "colorHex")]
    pub color_hex: String,
    pub content: String,
    pub points: Vec<SavedPoint>,
}

/// Point for pen strokes
#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct SavedPoint {
    pub x: f64,
    pub y: f64,
}

/// Deserialize JSON string to AnnotationData vector
pub fn deserialize_annotations(json: &str) -> Result<Vec<SavedAnnotation>, String> {
    if json.is_empty() {
        return Ok(vec![]);
    }

    serde_json::from_str::<Vec<SavedAnnotation>>(json)
        .map_err(|e| format!("Failed to parse annotations JSON: {}", e))
}

/// Serialize AnnotationData vector to JSON string
pub fn serialize_annotations(data: &[SavedAnnotation]) -> Result<String, String> {
    serde_json::to_string_pretty(data)
        .map_err(|e| format!("Failed to serialize annotations: {}", e))
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_serialize_empty() {
        let result = serialize_annotations(&[]);
        assert!(result.is_ok());
        assert_eq!(result.unwrap(), "[]");
    }

    #[test]
    fn test_serialize_and_deserialize() {
        let annotations = vec![
            SavedAnnotation {
                page_index: 0,
                annotation_type: 0, // Highlight
                x: 10.0,
                y: 20.0,
                width: 100.0,
                height: 50.0,
                color_hex: "#FFFF00".to_string(),
                content: "".to_string(),
                points: vec![],
            },
        ];

        let json = serialize_annotations(&annotations).unwrap();
        let deserialized = deserialize_annotations(&json).unwrap();

        assert_eq!(annotations.len(), deserialized.len());
        assert_eq!(annotations[0].page_index, deserialized[0].page_index);
        assert_eq!(annotations[0].color_hex, deserialized[0].color_hex);
    }

    #[test]
    fn test_deserialize_invalid_json() {
        let result = deserialize_annotations("invalid json");
        assert!(result.is_err());
    }

    #[test]
    fn test_field_renames() {
        let json = "[{\"pageIndex\":5,\"type\":1,\"x\":10,\"y\":20,\"width\":30,\"height\":40,\"colorHex\":\"Yellow\",\"content\":\"test\",\"points\":[]}]";
        let result = deserialize_annotations(json);
        assert!(result.is_ok());
        let annotations = result.unwrap();
        assert_eq!(annotations[0].page_index, 5);
        assert_eq!(annotations[0].annotation_type, 1);
    }
}
