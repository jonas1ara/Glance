// Logging utilities - debug output
// Implemented in Phase 2+ as needed

#[cfg(debug_assertions)]
pub fn debug_log(msg: &str) {
    eprintln!("[Glance] {}", msg);
}

#[cfg(not(debug_assertions))]
pub fn debug_log(_msg: &str) {
    // No-op in release builds
}
