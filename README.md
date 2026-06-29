# 👁️ Glance

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Framework: WinUI 3](https://img.shields.io/badge/Framework-WinUI_3-purple.svg)](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/)
[![Platform: Windows 11](https://img.shields.io/badge/Platform-Windows_11-blue.svg)](https://www.microsoft.com/windows/)
[![Language: C#](https://img.shields.io/badge/Language-C%23-green.svg)](https://learn.microsoft.com/en-us/dotnet/csharp/)

**Glance** es un visor de documentos PDF rápido, ligero y elegante para Windows 11. Diseñado desde cero siguiendo los principios de **Fluent Design**, ofrece una experiencia visualmente integrada con transparencias Mica y transiciones fluidas, combinando la velocidad y el diseño minimalista de visores clásicos como GNOME Evince con herramientas modernas de anotación inspiradas en Adobe Acrobat.

---

## ✨ Características Principales

* **🎨 Estética Premium & Fluent Design:**
  * Uso nativo del fondo translúcido **Mica** de Windows 11 que se adapta dinámicamente al fondo de pantalla del usuario.
  * Barra de título integrada en la ventana y barra lateral translúcida para mayor inmersión.
  * Soporte completo para temas claro y oscuro automáticos.
* **🏠 Pantalla de Bienvenida Visual (estilo Evince):**
  * Vista en cuadrícula de documentos recientes con tarjetas que muestran la portada (primera página) renderizada del PDF.
  * Acceso rápido en un clic a los últimos archivos abiertos con limpieza automática si los archivos originales son eliminados o movidos.
* **✍️ Plumones y Dibujo Libre:**
  * Dibujo libre digital suave con puntero redondo (`PenMode`), ideal para firmas digitales, bocetos o tomar notas manuscritas directas en la página.
* **🖍️ Subrayado Marca-textos de Precisión:**
  * Detección milimétrica de arrastre para resaltar secciones clave del documento.
  * Paleta dinámica de 7 colores (Amarillo, Verde, Celeste, Rosa, Rojo, Azul, Negro) que aparece únicamente en los modos de edición.
  * Cálculo dinámico del canal Alpha (opacidad al 31%) para asegurar que el color translúcido resalte el texto original sin ocultarlo.
* **💬 Notas Adhesivas (Sticky Notes):**
  * Coloca notas flotantes en cualquier parte del PDF con un botón popover limpio para ver, editar y almacenar comentarios adicionales del lector.
* **💾 Auto-guardado de Anotaciones:**
  * Todas las notas, subrayados y dibujos se guardan de forma local e invisible en formato JSON al soltar el puntero, asegurando que tus anotaciones persistan de forma inmediata.
* **🔄 Rotación en Tiempo Real:**
  * Botón nativo para girar el documento en saltos de 90° con re-cálculo dinámico de anchos y altos para evitar recortes en la visualización.
* **⌨️ Atajos del Teclado (Undo):**
  * Historial de cambios con soporte para deshacer anotaciones mediante la combinación universal `Ctrl + Z`.
* **📑 Índice Lateral Fluido:**
  * Navegación a través de miniaturas de página de alta definición renderizadas secuencialmente para evitar el desorden visual durante el proceso de virtualización.

---

## 🛠️ Requisitos del Sistema y Tecnologías

* **Sistema Operativo:** Windows 10 (versión 1809 o superior) / Windows 11 (Recomendado para Mica).
* **Plataforma:** Windows App SDK 1.5+ (WinUI 3).
* **Entorno de Ejecución:** .NET 10.0.
* **Librerías principales:** Librería nativa `Windows.Data.Pdf` para un parseo de páginas rápido y seguro sin dependencias externas pesadas.

---

## 🚀 Compilación y Ejecución

Para clonar y compilar **Glance** localmente utilizando el SDK de .NET:

1. **Clonar el repositorio:**
   ```bash
   git clone https://github.com/tu-usuario/glance.git
   cd glance
   ```

2. **Compilar y Ejecutar en modo empaquetado (nativamente):**
   ```bash
   dotnet run
   ```

*(El entorno de compilación de Windows App SDK registrará temporalmente el paquete de aplicación de prueba en tu sistema y desplegará la interfaz del visor Glance en tu pantalla).*

---

## 📜 Licencia

Este proyecto está bajo la Licencia **MIT**. Consulta el archivo [LICENSE](LICENSE) para obtener más información.
