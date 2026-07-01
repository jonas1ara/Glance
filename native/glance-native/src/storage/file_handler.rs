// File handler - Read/write JSON files
use std::fs;
use std::path::Path;

/// Read JSON file from disk
pub fn read_json_file(path: &str) -> Result<String, String> {
    if path.is_empty() {
        return Err("File path cannot be empty".to_string());
    }

    fs::read_to_string(path)
        .map_err(|e| format!("Failed to read file '{}': {}", path, e))
}

/// Write JSON file to disk
pub fn write_json_file(data: &str, path: &str) -> Result<(), String> {
    if path.is_empty() {
        return Err("File path cannot be empty".to_string());
    }

    if data.is_empty() {
        return Err("Cannot write empty data".to_string());
    }

    // Create parent directory if needed
    if let Some(parent) = Path::new(path).parent() {
        if !parent.as_os_str().is_empty() {
            fs::create_dir_all(parent)
                .map_err(|e| format!("Failed to create directory: {}", e))?;
        }
    }

    fs::write(path, data)
        .map_err(|e| format!("Failed to write file '{}': {}", path, e))
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::fs;
    use tempfile::TempDir;

    #[test]
    fn test_write_and_read() {
        let dir = TempDir::new().unwrap();
        let file_path = dir.path().join("test.json").to_string_lossy().to_string();

        let test_data = r#"{"test":"data"}"#;
        write_json_file(test_data, &file_path).unwrap();

        let read_data = read_json_file(&file_path).unwrap();
        assert_eq!(test_data, read_data);
    }

    #[test]
    fn test_write_empty_data() {
        let dir = TempDir::new().unwrap();
        let file_path = dir.path().join("test.json").to_string_lossy().to_string();

        let result = write_json_file("", &file_path);
        assert!(result.is_err());
    }

    #[test]
    fn test_read_nonexistent_file() {
        let result = read_json_file("/nonexistent/path/file.json");
        assert!(result.is_err());
    }
}
