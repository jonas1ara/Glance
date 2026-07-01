# Glance Native Backend (Rust)

Native performance layer for Glance PDF viewer. Handles PDF rendering, validation, and persistent storage.

## What Rust Does Here

Glance's backend is written in Rust to provide:

- **Performance:** Native speed for PDF processing (no managed overhead)
- **Memory Safety:** Rust's type system prevents memory bugs at compile-time
- **Zero-Cost Abstractions:** High-level code with C performance
- **Seamless C# Interop:** Via P/Invoke FFI bridge (`glance_native.dll`)

## Architecture

```
glance-native/
├── src/
│   ├── lib.rs                    # FFI entry points
│   ├── ffi/
│   │   └── bindings.rs          # C# ↔ Rust type definitions
│   └── rendering/
│       ├── pdf_engine.rs        # PDF parsing & page rendering
│       └── image_utils.rs       # PNG encoding, DPI scaling
└── Cargo.toml
```

## Building

```bash
# Debug build
cargo build

# Release build (optimized)
cargo build --release
```

Output: `target/release/glance_native.dll` (or `.so` on Linux)

## FFI Exports

C# calls these functions via P/Invoke:

- `pdf_engine_create(path)` - Open PDF file
- `pdf_engine_destroy(engine)` - Close and cleanup
- `pdf_render_page(engine, options)` - Render page to PNG bytes
- `annotations_save(json, path)` - Save annotations
- `annotations_load(path)` - Load annotations
- `memory_free(ptr)` - Free allocated memory

## Dependencies

- **pdfium-render** 0.8+ - PDF parsing and rendering
- **serde_json** - Annotation serialization
- **libc** - Low-level type definitions

## Development

The Rust backend is designed to be:
- **Minimal:** Only essential PDF operations, delegating UI to C#
- **Stable:** No breaking changes to FFI signatures
- **Testable:** Core functions have unit tests in Phase 1-4

See `/src/tests/` for integration tests.
