// Error handling utilities

use crate::ffi::marshalling::rust_string_to_c;

pub fn make_error_string(msg: &str) -> *const u8 {
    rust_string_to_c(msg) as *const u8
}
