// Annotation data models
use serde::{Deserialize, Serialize};

#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct AnnotationData {
    #[serde(rename = "pageIndex")]
    pub page_index: i32,
    pub x: f64,
    pub y: f64,
    pub width: f64,
    pub height: f64,
    #[serde(rename = "colorHex")]
    pub color_hex: String,
    pub content: String,
}
