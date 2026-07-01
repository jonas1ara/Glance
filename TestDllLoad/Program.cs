using System;
using System.IO;
using System.Runtime.InteropServices;

Console.WriteLine("Testing DLL Load");
Console.WriteLine("");

string dllPath = @"..\src\bin\Debug\net10.0-windows10.0.26100.0\win-x64\glance_native.dll";

if (!File.Exists(dllPath))
{
    Console.WriteLine($"ERROR: DLL not found at {dllPath}");
    Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
    Console.WriteLine($"Files in current directory:");
    foreach (var f in Directory.GetFiles("."))
    {
        Console.WriteLine($"  {f}");
    }
    return;
}

Console.WriteLine($"DLL found at: {Path.GetFullPath(dllPath)}");
Console.WriteLine($"DLL size: {new FileInfo(dllPath).Length} bytes");
Console.WriteLine("");

try
{
    Console.WriteLine("Attempting to load DLL...");
    IntPtr handle = NativeLibrary.Load(dllPath);
    Console.WriteLine($"✓ SUCCESS: DLL loaded!");
    Console.WriteLine($"  Handle: {handle}");

    // Try to get a function
    try
    {
        if (NativeLibrary.TryGetExport(handle, "test_ffi_works", out IntPtr funcPtr))
        {
            Console.WriteLine($"✓ Found exported function: test_ffi_works");
        }
        else
        {
            Console.WriteLine($"✗ Could not find function: test_ffi_works");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Error getting function: {ex.Message}");
    }

    NativeLibrary.Free(handle);
}
catch (DllNotFoundException ex)
{
    Console.WriteLine($"✗ FAILED: DLL dependency not found!");
    Console.WriteLine($"  Message: {ex.Message}");
    Console.WriteLine("");
    Console.WriteLine("This means the DLL itself was found, but one of its dependencies is missing.");
    Console.WriteLine("Try installing Visual C++ Runtime or check DLL dependencies with Dependency Walker.");
}
catch (BadImageFormatException ex)
{
    Console.WriteLine($"✗ FAILED: DLL format is invalid!");
    Console.WriteLine($"  Message: {ex.Message}");
    Console.WriteLine("  Possible causes: corrupted DLL, wrong architecture, or not a valid Windows DLL");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ FAILED: {ex.GetType().Name}");
    Console.WriteLine($"  Message: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"  Inner: {ex.InnerException.Message}");
    }
}
