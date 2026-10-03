# KASA Service Tracker

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet" alt=".NET 8" />
  <img src="https://img.shields.io/badge/C%23-12-239120?style=for-the-badge&logo=csharp" alt="C#" />
  <img src="https://img.shields.io/badge/Testing-xUnit-blue?style=for-the-badge" alt="xUnit" />
  <img src="https://img.shields.io/badge/Platform-Windows-0078D6?style=for-the-badge&logo=windows" alt="Platform" />
  <img src="https://img.shields.io/badge/License-MIT-green?style=for-the-badge" alt="License" />
</p>

Aplicación de consola en **.NET 8 (C#)** diseñada para la gestión, trazabilidad y control de órdenes de servicio automotriz, con almacenamiento persistente en **JSON** y generación automática de reportes mensuales en **Excel (XLSX)**.

---

## 🏛️ Arquitectura del Proyecto

El sistema está estructurado bajo principios de arquitectura limpia y desacoplamiento de dependencias:

```text
KASA-Service-Tracker/
├── KASA-Service-Tracker.sln        # Solución global .NET
├── INSTALAR-Y-CREAR-ACCESO-DIRECTO.bat # Instalador con acceso directo
├── src/
│   └── KasaServiceTracker/         # Proyecto principal de consola
│       ├── Domain/                 # Modelos de entidades y valor (Órdenes, Clientes, Vehículos)
│       ├── Services/               # Lógica de negocio, estados de servicio y filtrado semanal
│       ├── Persistence/            # Serialización JSON y generador de hojas Excel (.xlsx)
│       └── UI/                     # Menús interactivos de consola y validación de entrada
├── tests/
│   └── KasaServiceTracker.Tests/   # Suite de pruebas automatizadas con xUnit
└── Aplicacion/                     # Distribución ejecutable autónoma
    └── Data/
        ├── service-orders.json     # Base de datos local transaccional
        └── Exportaciones/          # Reportes generados (KASA-Service-YYYY-MM.xlsx)
```

---

## ⚙️ Características Técnicas

* **Persistencia Atómica en JSON:** Almacenamiento seguro en disco con serialización tipada.
* **Exportación Dinámica a Excel:** Generación automática mensual de hojas de cálculo con pestañas separadas de `ÓRDENES` e `HISTORIAL`, actualizándose ante cualquier edición.
* **Control de Ciclo de Vida de Órdenes:**
  * Estados: *Recepción* $\rightarrow$ *Diagnóstico* $\rightarrow$ *En Reparación* $\rightarrow$ *Pruebas* $\rightarrow$ *Terminado*.
  * Las órdenes terminadas se archivan del listado activo semanal, pero permanecen indexadas para búsquedas por folio o placas vehiculares.
* **Normalización de Datos:** Conversión automática a mayúsculas y validación de campos obligatorios en consola.

---

## 🚀 Compilación y Desarrollo

### Requisitos:
* [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) o posterior.

### Comandos de desarrollo:
```powershell
# 1. Restaurar paquetes NuGet
dotnet restore

# 2. Compilar solución en Release
dotnet build --configuration Release

# 3. Ejecutar pruebas unitarias automatizadas
dotnet test --configuration Release

# 4. Iniciar aplicación
dotnet run --project src/KasaServiceTracker
```

---

## 📸 Evidencia Visual & Capturas

<p align="center">
  <img width="507" height="338" alt="Captura de pantalla de KASA Service Tracker" src="https://github.com/user-attachments/assets/8e00aaa3-45a7-4564-bdd7-3bbe02fb3f1a" />
</p>

---

## 📄 Licencia

Este proyecto está distribuido bajo la Licencia [MIT](LICENSE).

