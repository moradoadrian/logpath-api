# ⚙️ LogPath API - Core Telemetry Backend

Este repositorio contiene el núcleo de procesamiento de **LogPath**, una API RESTful de alto rendimiento diseñada para la ingesta, almacenamiento y análisis de eventos de telemetría provenientes de sistemas de Punto de Venta (POS).

Desarrollado para la **Hackatón de CubePath 2026**.

## 🛠️ Stack Tecnológico
* **Framework:** .NET 10 (C#)
* **ORM:** Entity Framework Core
* **Base de Datos:** PostgreSQL
* **Despliegue:** Docker / Dokploy (Linux VPS)
* **Integraciones:** Discord Webhooks API

## ✨ Arquitectura y Características
* **Ingesta Asíncrona:** Endpoint optimizado para recibir cargas de trabajo de múltiples sucursales sin bloquear los clientes POS.
* **Auto-Migraciones (Code-First):** La base de datos se autoconfigura y migra a la última versión automáticamente al iniciar el contenedor, ideal para entornos CI/CD.
* **Alertas Proactivas en Tiempo Real:** El sistema no solo guarda logs de forma pasiva. Intercepta eventos de nivel `ERROR` o `CRITICAL` y dispara un Webhook hacia Discord en milisegundos para notificar al equipo de soporte/seguridad.
* **CORS Configurado:** Listo para integrarse de forma segura con aplicaciones cliente externas (SPA).

## 📡 Endpoints Principales

La API expone las siguientes rutas:

### `GET /api/logs`
Recupera el historial de eventos ordenados cronológicamente (más recientes primero).

### `POST /api/logs`
Ingesta un nuevo evento desde un cliente POS. Si el nivel es `ERROR`, dispara automáticamente el pipeline de notificaciones.

**Payload de ejemplo:**
```json
{
  "level": "ERROR",
  "action": "Falla de Impresora",
  "userId": "12345",
  "userName": "Cajero_Adrian",
  "details": "Falla térmica en caja principal."
}