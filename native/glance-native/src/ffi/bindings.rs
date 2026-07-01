// FFI Bindings - extern "C" functions callable from C#

/// Opaque PDF Engine type (alias for internal implementation)
pub type PdfEngine = crate::rendering::pdf_engine::PdfEngineImpl;

/// Rendering options passed from C#
#[repr(C)]
pub struct RenderOptions {
    pub page_index: u32,
    pub width: u32,
    pub height: u32,
    pub dpi: f32,
}

/// Response structure for FFI calls
#[repr(C)]
pub struct Response {
    pub success: bool,
    pub error_msg: *const u8,    // NULL if success=true
    pub data_ptr: *mut u8,       // PNG bytes or JSON string
    pub data_len: u64,
}

// ============================================================================
// PDF Engine Functions (Phase 4)
// ============================================================================

/// Create a new PDF engine for the given file path
#[no_mangle]
pub extern "C" fn pdf_engine_create(pdf_path: *const u8) -> *mut PdfEngine {
    if pdf_path.is_null() {
        return std::ptr::null_mut();
    }

    let path_str = match crate::ffi::marshalling::c_string_to_rust(pdf_path) {
        Ok(s) => s,
        Err(_) => return std::ptr::null_mut(),
    };

    match PdfEngine::new(&path_str) {
        Ok(engine) => Box::into_raw(Box::new(engine)),
        Err(_) => std::ptr::null_mut(),
    }
}

/// Destroy a PDF engine and free resources
#[no_mangle]
pub extern "C" fn pdf_engine_destroy(engine: *mut PdfEngine) {
    if !engine.is_null() {
        unsafe {
            drop(Box::from_raw(engine));
        }
    }
}

/// Render a specific page from PDF to PNG bytes
#[no_mangle]
pub extern "C" fn pdf_render_page(
    engine: *mut PdfEngine,
    opts: *const RenderOptions,
    out_png_data: *mut *mut u8,
    out_png_len: *mut u64,
) -> Response {
    if engine.is_null() || opts.is_null() || out_png_data.is_null() || out_png_len.is_null() {
        return Response {
            success: false,
            error_msg: crate::utils::error::make_error_string("Invalid parameters"),
            data_ptr: std::ptr::null_mut(),
            data_len: 0,
        };
    }

    let engine_ref = unsafe { &*engine };
    let opts_ref = unsafe { &*opts };

    match engine_ref.render_page_to_png(opts_ref.page_index, opts_ref.width, opts_ref.height, opts_ref.dpi) {
        Ok(png_bytes) => {
            let len = png_bytes.len() as u64;
            let mut vec = png_bytes;
            let ptr = vec.as_mut_ptr();
            std::mem::forget(vec); // Caller must free with memory_free

            unsafe {
                *out_png_data = ptr;
                *out_png_len = len;
            }

            Response {
                success: true,
                error_msg: std::ptr::null(),
                data_ptr: std::ptr::null_mut(),
                data_len: 0,
            }
        }
        Err(e) => Response {
            success: false,
            error_msg: crate::utils::error::make_error_string(&e),
            data_ptr: std::ptr::null_mut(),
            data_len: 0,
        },
    }
}

// ============================================================================
// Annotation Functions (Phase 2-3)
// ============================================================================

/// Save annotations to JSON file
#[no_mangle]
pub extern "C" fn annotations_save(
    json: *const u8,
    path: *const u8,
) -> Response {
    if json.is_null() || path.is_null() {
        return Response {
            success: false,
            error_msg: crate::utils::error::make_error_string("Null parameters"),
            data_ptr: std::ptr::null_mut(),
            data_len: 0,
        };
    }

    // Convert C strings to Rust
    let json_str = match crate::ffi::marshalling::c_string_to_rust(json) {
        Ok(s) => s,
        Err(e) => {
            return Response {
                success: false,
                error_msg: crate::utils::error::make_error_string(&e),
                data_ptr: std::ptr::null_mut(),
                data_len: 0,
            };
        }
    };

    let path_str = match crate::ffi::marshalling::c_string_to_rust(path) {
        Ok(s) => s,
        Err(e) => {
            return Response {
                success: false,
                error_msg: crate::utils::error::make_error_string(&e),
                data_ptr: std::ptr::null_mut(),
                data_len: 0,
            };
        }
    };

    // Validate JSON
    match crate::storage::json_adapter::deserialize_annotations(&json_str) {
        Ok(_) => {
            // JSON is valid, write to file
            match crate::storage::file_handler::write_json_file(&json_str, &path_str) {
                Ok(_) => Response {
                    success: true,
                    error_msg: std::ptr::null(),
                    data_ptr: std::ptr::null_mut(),
                    data_len: 0,
                },
                Err(e) => Response {
                    success: false,
                    error_msg: crate::utils::error::make_error_string(&e),
                    data_ptr: std::ptr::null_mut(),
                    data_len: 0,
                },
            }
        }
        Err(e) => Response {
            success: false,
            error_msg: crate::utils::error::make_error_string(&e),
            data_ptr: std::ptr::null_mut(),
            data_len: 0,
        },
    }
}

/// Load annotations from JSON file
#[no_mangle]
pub extern "C" fn annotations_load(
    path: *const u8,
) -> Response {
    if path.is_null() {
        return Response {
            success: false,
            error_msg: crate::utils::error::make_error_string("Null path"),
            data_ptr: std::ptr::null_mut(),
            data_len: 0,
        };
    }

    // Convert C string to Rust
    let path_str = match crate::ffi::marshalling::c_string_to_rust(path) {
        Ok(s) => s,
        Err(e) => {
            return Response {
                success: false,
                error_msg: crate::utils::error::make_error_string(&e),
                data_ptr: std::ptr::null_mut(),
                data_len: 0,
            };
        }
    };

    // Read file
    match crate::storage::file_handler::read_json_file(&path_str) {
        Ok(json_str) => {
            // Validate JSON
            match crate::storage::json_adapter::deserialize_annotations(&json_str) {
                Ok(_) => {
                    // Return JSON data
                    let data_ptr = crate::ffi::marshalling::rust_string_to_c(&json_str);
                    let data_len = json_str.len() as u64;
                    Response {
                        success: true,
                        error_msg: std::ptr::null(),
                        data_ptr,
                        data_len,
                    }
                }
                Err(e) => Response {
                    success: false,
                    error_msg: crate::utils::error::make_error_string(&e),
                    data_ptr: std::ptr::null_mut(),
                    data_len: 0,
                },
            }
        }
        Err(e) => Response {
            success: false,
            error_msg: crate::utils::error::make_error_string(&e),
            data_ptr: std::ptr::null_mut(),
            data_len: 0,
        },
    }
}

/// Process and validate annotations
#[no_mangle]
pub extern "C" fn annotations_process(
    json: *const u8,
) -> Response {
    if json.is_null() {
        return Response {
            success: false,
            error_msg: crate::utils::error::make_error_string("Null JSON"),
            data_ptr: std::ptr::null_mut(),
            data_len: 0,
        };
    }

    // Convert C string to Rust
    let json_str = match crate::ffi::marshalling::c_string_to_rust(json) {
        Ok(s) => s,
        Err(e) => {
            return Response {
                success: false,
                error_msg: crate::utils::error::make_error_string(&e),
                data_ptr: std::ptr::null_mut(),
                data_len: 0,
            };
        }
    };

    // Deserialize JSON to annotations
    match crate::storage::json_adapter::deserialize_annotations(&json_str) {
        Ok(annotations) => {
            // Validate all annotations
            match crate::annotations::processor::validate_all(&annotations) {
                Ok(_) => Response {
                    success: true,
                    error_msg: std::ptr::null(),
                    data_ptr: std::ptr::null_mut(),
                    data_len: 0,
                },
                Err(e) => Response {
                    success: false,
                    error_msg: crate::utils::error::make_error_string(&e),
                    data_ptr: std::ptr::null_mut(),
                    data_len: 0,
                },
            }
        }
        Err(e) => Response {
            success: false,
            error_msg: crate::utils::error::make_error_string(&e),
            data_ptr: std::ptr::null_mut(),
            data_len: 0,
        },
    }
}

// ============================================================================
// Memory Management
// ============================================================================

/// Free memory allocated by Rust and returned to C#
#[no_mangle]
pub extern "C" fn memory_free(ptr: *mut u8) {
    if !ptr.is_null() {
        unsafe {
            // Reconstruct Vec to let it drop and deallocate
            drop(Vec::from_raw_parts(ptr, 0, 1024));
        }
    }
}

// ============================================================================
// Test Helper Function (Phase 1 only)
// ============================================================================

/// Simple test function to verify DLL loads
#[no_mangle]
pub extern "C" fn test_ffi_works() -> bool {
    true
}
