# Glance Landing Page

Este es el landing page para Glance PDF Viewer, servido a través de GitHub Pages.

## Configuración de GitHub Pages

Para habilitar GitHub Pages:

1. **Ir a Settings → Pages**
2. **Source:** Seleccionar `Deploy from a branch`
3. **Branch:** `master` / `main`
4. **Folder:** `/docs`
5. Click en **Save**

El sitio estará disponible en: `https://your-username.github.io/glance/`

## Estructura

- `index.html` - Landing page principal
- `style.css` - Estilos Fluent Design (Windows 11)
- `script.js` - Funcionalidad interactiva

## Integración Stripe

El botón "Buy Now" está preparado para integración con Stripe. Para activar:

1. Crear cuenta en Stripe
2. Obtener `Publishable Key` (pk_live_...)
3. Reemplazar en `script.js`:
   ```javascript
   const stripe = Stripe('pk_live_YOUR_KEY');
   ```
4. Crear productos en Stripe para cada plan ($9.99 y $7.99)
5. Configurar webhook para generar links de descarga

## Webhook Azure Functions

El webhook que procesa pagos está en Azure Functions:
- **Escucha:** `https://your-function.azurewebsites.net/api/stripe-webhook`
- **Evento:** `checkout.session.completed`
- **Acción:** Genera link SAS temporal → Envía email

## TODO

- [ ] Integrar Stripe Checkout
- [ ] Configurar Azure Functions webhook
- [ ] Setup SendGrid para emails
- [ ] Página de éxito post-pago
- [ ] Página de error
