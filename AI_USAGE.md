# Declaración de uso de inteligencia artificial

Este documento declara, de forma honesta y completa, el uso de herramientas de
inteligencia artificial durante el desarrollo de esta evaluación.

## Herramientas utilizadas

- **Zed editor (agente de codificación "pab-ai" agente (combo) propio que se basa en subscripciones gratuitas via omniroute)**: asistente de IA integrado en
  el editor, utilizado para analizar el código, proponer y aplicar cambios, y
  ejecutar comandos de compilación y pruebas.

> Nota: no se utilizaron otras herramientas de IA (ChatGPT, Copilot de GitHub,
> Claude, etc.). Toda la asistencia provino exclusivamente del agente
> integrado en **Zed**.

## Tareas para las que se utilizaron

Se utilizaron funciones del asistente en las siguientes tareas:

1. **Análisis del código existente**: se le pidió inspeccionar el proyecto para
   identificar problemas en `OpenMeteoClient.cs`, `FavoriteService.cs`,
   `WeatherCacheService.cs`, `FavoritosController.cs` y `AppDbContext.cs`, además
   de revisar la estructura general del repositorio.

2. **Corrección de la integración HTTP**: se eliminaron bloqueos sincrónicos
   (`.Result`, `.Wait()`), se inyectó `HttpClient` mediante inyección de
   dependencias, se configuró timeout y se propagó `CancellationToken` en las
   llamadas a Open-Meteo.

3. **Seguridad**: se corrigieron operaciones por identificador para validar que
   el recurso pertenece al usuario actual, evitando acceso cruzado entre Ana y
   Bruno.

4. **Integridad y concurrencia**: se agregó un índice único en la base de datos
   sobre `(UserId, LocationId)` para impedir duplicados de ciudad por usuario,
   incluso ante solicitudes concurrentes.

5. **Caché y actualización**: se ajustó la clave de caché para que sea por
   ciudad (`LocationId`) en lugar de global, se implementó `RefreshAsync()` para
   la actualización manual y se respetó el `CancellationToken`.

6. **Persistencia y paginación**: se movió el filtro, ordenamiento y paginación
   a nivel de base de datos (SQLite) antes de materializar los registros, y se
   corrigió la traducción de búsquedas con `EF.Functions.Like` para ser
   compatible con SQLite.

7. **Implementación de la funcionalidad nueva (Alertas por umbral)**: se
   implementó de extremo a extremo el módulo de alertas:
   - Modelo `WeatherAlert` y configuración en `AppDbContext` (relación con
     `FavoriteCity`, cascade delete, índices).
   - Servicio `WeatherAlertService` completo (crear, listar, activar/desactivar,
     eliminar, evaluar) con validaciones de máximo 5 alertas activas por ciudad,
     rangos por métrica y operadores `>=` / `<=`.
   - Acciones en `FavoritosController` (CrearAlerta, ToggleAlerta,
     EliminarAlerta) y evaluación automática al abrir el detalle.
   - Interfaz en `Detalle.cshtml` con tabla de alertas, estados visuales
     (disparada / activa / inactiva) y formulario de creación.

8. **Depuración de errores reportados**: se resolvieron errores en tiempo de
   ejecución detectados durante la validación manual, como el `404` en la
   búsqueda (endpoint de geocoding distinto al de forecast), la tabla
   `WeatherAlerts` faltante (recreación de la base), y problemas de traducción
   de LINQ a SQL en SQLite.

9. **Pruebas**: se escribieron tests unitarios para el módulo de alertas
   (`WeatherAlertServiceTests.cs`) cubriendo creación, límites, rangos,
   evaluación, toggle, eliminación y aislamiento por usuario. Se ejecutó
   `dotnet test` (24 pruebas, todas aprobadas).

## Revisión personal

La asistencia de IA no se aceptó de forma automática. Se llevó a cabo la
siguiente revisión por mi parte:

- **Verificación de compilación**: se ejecutó `dotnet build` (configuración
  Release) y se confirmó que compila sin advertencias ni errores.
- **Ejecución de pruebas**: se ejecutó `dotnet test` y se confirmó que las 24
  pruebas (incluidas las nuevas para alertas) pasan correctamente.
- **Prueba manual**: se ejecutó la aplicación y se validó el flujo principal
  (búsqueda de ciudades, agregar a favoritos como Bruno, abrir el detalle y
  gestionar alertas).
- **Correcciones durante la depuración**: varios cambios propuestos por la IA
  generaron errores en tiempo de ejecución que corregí de forma iterativa junto
  con la herramienta:
  - Endpoint de búsqueda: la IA configuró un solo `BaseAddress` para el cliente
    HTTP que no cubría el endpoint de geocoding; lo corregí para que cada método
    use su URL base correcta.
  - Recreación de la base de datos: la tabla `WeatherAlerts` no existía; se ajustó
    el inicializador para recrear el esquema.
  - Búsqueda de favoritos: la traducción de `string.Contains` a SQL fallaba en
    SQLite; se reemplazó por `EF.Functions.Like`.
- **Comprensión del código**: revisé y comprendí cada cambio antes de conservarlo.
  Aunque la IA redactó la mayor parte del código, puedo explicar la lógica de cada
  componente (servicios, controlador, modelo de datos, vista) y sus decisiones.

## Declaración

Confirmo que comprendo el código entregado y que puedo explicarlo, modificarlo
y diagnosticarlo durante una instancia posterior. Si bien recibí asistencia de
una herramienta de IA (agente de Zed) para redactar y corregir gran parte de la
solución, revisé, comprendí y validé los cambios. Asumo la responsabilidad final
sobre el código y estoy en condiciones de defender los criterios técnicos
aplicados (integración HTTP, asincronía, seguridad, concurrencia, caché,
persistencia y alertas por umbral).
