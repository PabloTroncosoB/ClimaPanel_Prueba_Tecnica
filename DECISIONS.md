# Decisiones técnicas

Documento con las decisiones técnicas principales tomadas durante la
implementación, los problemas encontrados, la solución elegida, alternativas
descartadas y trabajo pendiente.

## 1. Problemas principales identificados

Al revisar la aplicación existente se detectaron los siguientes problemas:

| Problema | Impacto |
|---|---|
| Uso de `.Result` / `.Wait()` en llamadas HTTP. | Bloqueos del hilo, riesgo de deadlock y baja capacidad de concurrencia. |
| `HttpClient` creado con `new HttpClient()` en cada llamada. | Agotamiento de sockets, sin reutilización ni control centralizado. |
| `CancellationToken` ignorado en llamadas HTTP. | Imposible cancelar operaciones ante timeouts o abandono de petición. |
| Operaciones por ID sin validar pertenencia al usuario. | Permite a un usuario acceder/eliminar recursos de otro (IDOR). |
| Duplicación de ciudades solo controlada por consulta previa. | Race condition: dos solicitudes simultáneas podían crear duplicados. |
| Caché con clave única global. | Una ciudad invalidaba/contaminaba el clima de todas las demás. |
| Paginación/filtrado en memoria (`ToListAsync` temprano). | Consultas ineficientes y lectura innecesaria de toda la tabla. |
| Búsqueda de favoritos con `string.Contains(...OrdinalIgnoreCase)`. | No traducible por SQLite → error en tiempo de ejecución. |
| Tabla `WeatherAlerts` inexistente en la BD existente. | `EnsureCreated` no migra esquemas; había que recrear la BD. |

## 2. Integración HTTP, asincronía, cancelación y timeout

**Problema:** `OpenMeteoClient` usaba `.Result`, creaba `HttpClient` por llamada y
ignoraba `CancellationToken`.

**Solución elegida:**
- Registro de `IWeatherClient` a través de `AddHttpClient` (factory), que gestiona
  el ciclo de vida de `HttpClient` de forma segura y reutilizable.
- Métodos asincrónicos con `async`/`await`, sin bloqueos sincrónicos.
- Timeout configurado en 30 segundos en el cliente HTTP.
- `CancellationToken` propagado a `GetAsync` y a la deserialización JSON.

**Alternativa descartada:** un único `HttpClient` singleton manual. Se descartó
porque `AddHttpClient` ya aplica las mejores prácticas (rotación de DNS, manejo de
sockets) sin código extra.

**Detalle de endpoints:** la búsqueda usa el endpoint de geocoding
(`geocoding-api.open-meteo.com`) y el pronóstico el de forecast
(`api.open-meteo.com`). Son dominios distintos, por lo que cada método construye su
URL base a partir de la configuración, en lugar de depender de un único `BaseAddress`.

## 3. Integridad y concurrencia al crear favoritos

**Problema:** la unicidad dependía de `AnyAsync` previo al guardado, lo que no es
seguro ante dos solicitudes simultáneas del mismo usuario+ciudad.

**Solución elegida:** índice único a nivel de base de datos sobre
`(UserId, LocationId)` con `.IsUnique()`.

**Justificación:** la garantía de unicidad debe estar en la base de datos, no solo
en la lógica de aplicación. La consulta previa (validación amigable) se conserva,
pero la restricción real la impone el índice único, rechazando el segundo insert
aunque ambas solicitudes se ejecuten a la vez.

## 4. Separación de datos entre usuarios

**Problema:** `GetAsync` y `DeleteAsync` consultaban solo por `Id`, sin filtrar por
usuario → acceso cruzado entre Ana y Bruno.

**Solución elegida:** toda operación por identificador filtra siempre por
`UserId` del usuario autenticado (`x.Id == id && x.UserId == userId`), tanto en
`FavoriteService` como en `WeatherAlertService` (que valida la pertenencia del
favorito antes de operar sobre alertas).

**Resultado:** un usuario no puede consultar, actualizar ni eliminar recursos que
no le pertenecen; en caso de intentarlo obtiene "No se encontró..." (comportamiento
equivalente a 404, sin revelar la existencia del recurso).

## 5. Estrategia de caché y actualización forzada

**Problema:** `WeatherCacheService` usaba una clave única (`"forecast"`), por lo que
todas las ciudades compartían el mismo clima.

**Solución elegida:**
- Clave de caché dinámica por ciudad: `forecast_{LocationId}` con
  `GetCacheKey(FavoriteCity)`.
- Duración configurable (por defecto 90 s) vía `OpenMeteo:CacheSeconds`.
- Estados `LIVE`, `CACHE` y `STALE` reflejados en la interfaz.
- `GetAsync(city, forceRefresh, ...)`: si `forceRefresh` es `true`, se omite la
  lectura de caché y se obtiene un pronóstico nuevo del proveedor.
- `RefreshAsync` en `FavoriteService` fuerza el refresco (botón "Actualizar ahora").

**Alternativa descartada:** caché externa (Redis). Se descartó por añadir una
dependencia de infraestructura innecesaria para un escenario de una sola instancia.

## 6. Diseño completo de alertas por umbral

**Modelo `WeatherAlert`:**
- `Id`, `FavoriteId` (FK a `FavoriteCity`), `Metric` (enum), `Operator` (enum),
  `Threshold`, `IsEnabled`, `IsTriggered`, `CreatedAtUtc`, `LastEvaluatedAtUtc`,
  `LastTriggeredAtUtc`.

**Configuración de base de datos:**
- Tabla `WeatherAlerts`, clave primaria `Id`.
- Relación con `FavoriteCity` con `OnDelete(Cascade)` para que las alertas se
  eliminen junto con el favorito.
- Índice sobre `(FavoriteId, IsEnabled)` para evaluar solo las habilitadas de una
  ciudad de forma eficiente.

**Servicio `WeatherAlertService`:**
- `ListAsync`: lista alertas de un favorito del usuario (ordena por más recientes).
- `CreateAsync`: valida pertenencia del favorito, límite de **5 alertas activas**
  por ciudad y rangos de umbral según la métrica; crea la alerta habilitada.
- `ToggleAsync`: activa/desactiva una alerta validando pertenencia.
- `DeleteAsync`: elimina una alerta validando pertenencia.
- `EvaluateAsync`: evalúa todas las alertas habilitadas contra el clima actual;
  actualiza `IsTriggered`, `LastEvaluatedAtUtc` y `LastTriggeredAtUtc`.

**Métricas y rangos:**
- Temperatura: -80 a 80 °C.
- Humedad: 0 a 100 %.
- Precipitación: 0 a 500 mm.
- Viento: 0 a 300 km/h.

**Operadores:** mayor o igual (`>=`) y menor o igual (`<=`).

**Integración:**
- `FavoritosController.Detalle` evalúa las alertas al abrir el detalle.
- Acciones `CrearAlerta`, `ToggleAlerta` y `EliminarAlerta` (todas `POST` con
  `ValidateAntiForgeryToken`).
- La vista `Detalle.cshtml` muestra la tabla de alertas con estados visuales
  (disparada / activa / inactiva), formulario de creación y límite de 5.

## 7. Persistencia, consultas y paginación

**Solución elegida:**
- En `ListAsync` las operaciones de filtro (`Where`), ordenamiento (`OrderBy`),
  conteo (`CountAsync`) y paginación (`Skip`/`Take`) se ejecutan en SQLite antes
  de materializar.
- Solo sobre el conjunto final paginado se llama a `.AsEnumerable()` para mapear a
  view model.
- Búsqueda con `EF.Functions.Like` para compatibilidad con SQLite (la traducción
  de `string.Contains(...OrdinalIgnoreCase)` no es soportada por el proveedor).

**Manejo de errores:** no se exponen stack traces ni detalles internos; los
errores operativos muestran mensajes genéricos y se registran para diagnóstico.

## 8. Pruebas agregadas

Se añadieron pruebas unitarias para el módulo de alertas en
`tests/ClimaPanel.Tests/WeatherAlertServiceTests.cs`:

- Crear alerta con entradas válidas.
- Límite de 5 alertas activas por ciudad.
- Rango de umbral fuera de límites (tabla de casos: temperatura, humedad,
  precipitación, viento).
- Listar alertas de un favorito.
- Toggle de estado activo/inactivo.
- Eliminar alerta.
- Evaluación con condición cumplida (temperatura `>=`) y no cumplida.
- Evaluación con humedad `<=`.
- Alertas deshabilitadas no se evalúan.
- Aislamiento: no se puede listar ni eliminar alertas de otro usuario.

Se ejecutaron las pruebas con `dotnet test`: **24 pruebas, todas aprobadas.**

## 9. Limitaciones conocidas y trabajo pendiente

**Limitaciones conocidas:**
- La concurrencia de actualización forzada de caché no está bloqueada con un lock;
  dos refrescos simultáneos de la misma ciudad podrían generar llamadas duplicadas
  al proveedor. No afecta la corrección, pero podría optimizarse con
  un bloqueo por clave de caché.
- Al recrear la base de datos en desarrollo se pierden los favoritos creados
  manualmente (comportamiento de un escenario de demo; los usuarios de ejemplo se
  vuelven a sembrar).
- Los estados `LIVE`/`CACHE`/`STALE` se muestran en la interfaz, pero la lógica
  distingue principalmente LIVE (datos frescos) y CACHE (en memoria); la
  distinción fina de STALE depende de la expiración de la clave.

**Trabajo pendiente / mejoras opcionales:**
- Agregar un `SemaphoreSlim` (o `GetOrCreateAsync` con bloqueo) por clave de caché
  para evitar el "thundering herd" en refrescos simultáneos.
- Añadir migraciones de EF Core versionadas (en lugar de `EnsureCreated`) para
  evolucionar el esquema sin recrear la base.
- Cubrir con pruebas el flujo completo del controlador (crear alerta vía HTTP).
- Considerar métricas adicionales o umbrales configurables por país/estación.
