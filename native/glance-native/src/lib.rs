// Glance Native Library - PDF rendering and annotation backend
// Rust FFI for C# WinUI frontend

pub mod ffi;
pub mod rendering;
pub mod annotations;
pub mod storage;
pub mod utils;

// Re-export FFI functions so they're available at crate root
pub use ffi::bindings::*;
