# KASA Service Tracker

Aplicación de consola para Windows que registra y da seguimiento a órdenes de servicio automotriz.

## Uso rápido

1. Ejecuta `INSTALAR-Y-CREAR-ACCESO-DIRECTO.bat` con doble clic.
2. El instalador crea en el Escritorio el acceso directo **KASA Service Tracker**.
3. La aplicación se abre al terminar. En adelante puedes iniciarla desde ese acceso directo.

La aplicación compilada está en `Aplicacion/`. El proyecto C# completo permanece en `src/` y sus pruebas en `tests/`.

## Datos y Excel

- La fuente persistente de todas las órdenes es `Aplicacion/Data/service-orders.json`.
- Los Excel mensuales se crean automáticamente en `Aplicacion/Data/Exportaciones/` con nombres como `KASA-Service-2026-08.xlsx`.
- Cada libro contiene las hojas **ÓRDENES** e **HISTORIAL**. Se actualiza al registrar, corregir o cambiar el estado de una orden.
- Al llegar a **Terminado**, la orden permanece en JSON y en su Excel mensual, pero deja de aparecer en el listado semanal activo.
- Las búsquedas por folio o placas, el historial y los tickets siguen consultando todos los datos persistidos, incluidos los terminados.

## Menú

`Listar órdenes activas de la semana` usa la semana de lunes a domingo y excluye las órdenes terminadas. La opción `Editar / corregir registro` permite conservar cualquier dato pulsando ENTER y registra la corrección en el historial. Los textos capturados se guardan en mayúsculas.

## Desarrollo y pruebas

Con .NET SDK 8 o posterior:

```powershell
dotnet build
dotnet test
dotnet run --project src/KasaServiceTracker
```

## Capturas de Pantalla

<img width="507" height="338" alt="image" src="https://github.com/user-attachments/assets/8e00aaa3-45a7-4564-bdd7-3bbe02fb3f1a" />

