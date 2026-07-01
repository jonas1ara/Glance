# Build Native (Rust) Library for Glance
# Compiles glance_native.dll and copies to C# bin/ directory

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [ValidateSet("x64", "x86", "arm64")]
    [string[]]$Platforms = @("x64")
)

$ErrorActionPreference = "Stop"

$NativeDir = Join-Path $PSScriptRoot "..\native\glance-native"
$SolutionRoot = Join-Path $PSScriptRoot ".."
$OutputDir = Join-Path $SolutionRoot "src\bin\$Configuration"

Write-Host ""
Write-Host "🔨 Building Glance Native Library (Rust)" -ForegroundColor Cyan
Write-Host "   Configuration: $Configuration" -ForegroundColor Gray
Write-Host "   Platforms: $($Platforms -join ', ')" -ForegroundColor Gray
Write-Host ""

# Verify native directory exists
if (-not (Test-Path $NativeDir)) {
    Write-Host "❌ Error: native/glance-native/ directory not found" -ForegroundColor Red
    exit 1
}

# Create output directory if it doesn't exist
if (-not (Test-Path $OutputDir)) {
    Write-Host "   Creating output directory: $OutputDir" -ForegroundColor Gray
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

$successCount = 0
$failureCount = 0

foreach ($platform in $Platforms) {
    Write-Host "  ▶ Building for $platform..." -ForegroundColor Cyan

    $rustTarget = switch ($platform) {
        "x64"   { "x86_64-pc-windows-msvc" }
        "x86"   { "i686-pc-windows-msvc" }
        "arm64" { "aarch64-pc-windows-msvc" }
        default { throw "Unknown platform: $platform" }
    }

    # Build command
    $cargoCmd = if ($Configuration -eq "Release") {
        "cargo build --release --target $rustTarget"
    } else {
        "cargo build --target $rustTarget"
    }

    # Execute cargo build
    Push-Location $NativeDir
    try {
        Write-Host "     Running: $cargoCmd" -ForegroundColor Gray
        Invoke-Expression $cargoCmd 2>&1 | ForEach-Object { Write-Host "     $_" }

        if ($LASTEXITCODE -ne 0) {
            Write-Host "    ❌ cargo build failed with exit code $LASTEXITCODE" -ForegroundColor Red
            $failureCount++
            Pop-Location
            continue
        }
    } catch {
        Write-Host "    ❌ Exception: $_" -ForegroundColor Red
        $failureCount++
        Pop-Location
        continue
    }
    Pop-Location

    # Determine build profile directory
    $buildDir = if ($Configuration -eq "Release") { "release" } else { "debug" }

    # Copy DLL from target to bin
    $dllName = "glance_native.dll"
    $srcPath = Join-Path $NativeDir "target\$rustTarget\$buildDir\$dllName"

    if (Test-Path $srcPath) {
        $destPath = Join-Path $OutputDir $dllName
        Copy-Item $srcPath $destPath -Force
        Write-Host "    ✅ Copied $dllName to bin/$Configuration/" -ForegroundColor Green
        $successCount++
    } else {
        Write-Host "    ⚠️  DLL not found at: $srcPath" -ForegroundColor Yellow
        Write-Host "       Expected location: target\$rustTarget\$buildDir\$dllName" -ForegroundColor Yellow
        $failureCount++
    }
}

Write-Host ""
Write-Host "📊 Build Summary" -ForegroundColor Cyan
Write-Host "   ✅ Success: $successCount / $($Platforms.Count)" -ForegroundColor Green
if ($failureCount -gt 0) {
    Write-Host "   ❌ Failure: $failureCount / $($Platforms.Count)" -ForegroundColor Red
}

if ($failureCount -gt 0) {
    Write-Host ""
    Write-Host "❌ Native build failed" -ForegroundColor Red
    exit 1
} else {
    Write-Host ""
    Write-Host "✨ Native build complete!" -ForegroundColor Green
    exit 0
}
