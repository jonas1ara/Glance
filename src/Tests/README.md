# Glance Tests

Test scripts para validar cada fase de desarrollo.

## Tests Disponibles

### TEST_DLL_LOAD.ps1
**Propósito:** Verificar que el DLL nativo (`glance_native.dll`) carga correctamente.

**Uso:**
```powershell
.\TEST_DLL_LOAD.ps1
```

**Qué valida:**
- Archivo DLL existe
- DLL puede cargarse con .NET NativeLibrary
- Información del archivo (tamaño, fecha)

**Fase:** 1 (FFI Foundation)

---

### test-phase2.cs
**Propósito:** Test end-to-end de Fase 2 (Persistencia).

**Uso:**
```bash
dotnet script test-phase2.cs
```

**Qué valida:**
- Serialización JSON de anotaciones
- Guardado de archivos a disco
- Carga de archivos desde disco
- Deserialización JSON
- Round-trip integrity (datos no se corrompen)
- Error handling (JSON inválido)
- Múltiples anotaciones

**Fase:** 2 (Persistence)

---

## Cómo agregar nuevos tests

1. Crear archivo `test-phaseX-name.cs` o `TEST_NAME.ps1`
2. Incluir propósito y uso en este README
3. Ejecutar antes de cada merge a `master`
