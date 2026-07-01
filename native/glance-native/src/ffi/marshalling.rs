// Marshalling utilities for C# ↔ Rust FFI
use std::ffi::CStr;

/// Convert C string pointer to Rust String
pub fn c_string_to_rust(ptr: *const u8) -> Result<String, String> {
    if ptr.is_null() {
        return Err("Null pointer".to_string());
    }

    unsafe {
        CStr::from_ptr(ptr as *const i8)
            .to_str()
            .map(|s| s.to_string())
            .map_err(|e| format!("UTF-8 error: {}", e))
    }
}

/// Convert Rust String to C string pointer (caller must free with memory_free)
pub fn rust_string_to_c(s: &str) -> *mut u8 {
    let bytes = s.as_bytes();
    let mut vec = bytes.to_vec();
    vec.push(0); // null terminator
    let ptr = vec.as_mut_ptr();
    std::mem::forget(vec); // Prevent Rust from freeing - C# will call memory_free
    ptr
}
