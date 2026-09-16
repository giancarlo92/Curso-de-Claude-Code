# CLAUDE.md

Este archivo ofrece guía a Claude Code (claude.ai/code) al trabajar con código en este repositorio.
Cambio

## Qué es este repositorio

Un laboratorio docente en .NET 10 ("Curso de Claude Code") construido para ejercitar Claude Code contra un proyecto que reproduce a propósito tres modos de fallo clásicos: un error de compilación, una excepción en tiempo de ejecución y un test unitario que falla. También incluye un pequeño conector de SQL Server que se ejercita contra un SQL Server local en Docker.

**Este repositorio es un ejercicio de dos ramas.** `main`/`develop` contienen intencionadamente el código roto/con errores descrito abajo; la versión corregida vive en `DEV-8094_fix_dotnet_errors`. No "arregles" en silencio los errores intencionados en `develop`/`main` a menos que el usuario lo haya pedido explícitamente (ver Skills más abajo) — el estado roto es el objetivo del ejercicio.

## Comandos

No hay `global.json`; se usa el SDK de `dotnet` que esté en el PATH (desarrollado con `dotnet 10.0.401`, `net10.0`, `LangVersion 14.0`).

```powershell
# Restaurar / compilar / testear toda la solución
dotnet restore CursoClaudeCode.sln --nologo
dotnet build CursoClaudeCode.sln --nologo --no-restore
dotnet test CursoClaudeCode.sln --nologo --no-build

# Ejecutar solo los tests unitarios (errors/UnitTestError.csproj)
dotnet test errors\UnitTestError.csproj --nologo

# Ejecutar el orquestador principal (programa basado en archivo, no se ejecuta vía msbuild)
dotnet run --file main.cs

# Ejecutar directamente un escenario de error individual
dotnet run --file errors\compilation-error.cs
dotnet run --file errors\runtime-error.cs
```

Compilar `CursoClaudeCode.csproj` (directamente o vía la solución) **siempre falla a propósito**: tiene un target de MSBuild `BeforeTargets="Build"` (`ValidateCompilationErrorExample`) que ejecuta `dotnet run --file errors\compilation-error.cs` como paso previo a la compilación, y ese script tiene un error de tipos intencionado (`CS0029`). Esto es lo esperado en `develop`/`main`, no una regresión que haya que corregir por reflejo.

Laboratorio de SQL Server (necesario para que `main.cs` consiga conectar):

```powershell
docker compose up -d          # inicia SQL Server 2022 local en localhost,11433
docker compose down            # detiene, conserva el volumen de datos
docker compose down -v         # detiene y elimina también el volumen de datos
```

Requiere un `.env` local (copiar `.env.example`) con `MSSQL_SA_PASSWORD` definido; `SQLSERVER_HOST_PORT` por defecto es `11433`. `.env` está en `.gitignore`.

## Arquitectura

- **`CursoClaudeCode.csproj`** — proyecto ejecutable principal (`OutputType=Exe`, con las características de file-based-program activadas, `EnableDefaultCompileItems=false` por lo que los fuentes se listan explícitamente). Compila `main.cs` y `database\SqlServerConnector.cs`. `errors\compilation-error.cs` y `errors\runtime-error.cs` están incluidos como elementos `None` (se copian a la salida, no se compilan) porque `main.cs` los invoca como proceso externo vía `dotnet run --file`.
- **`main.cs`** — orquestador. Al arrancar construye un `SqlServerConnector`, verifica la conectividad y el acceso CRUD completo (crea/lee/actualiza/elimina/borra una tabla temporal con nombre único), y luego ejecuta tres escenarios **en paralelo** como procesos `dotnet` hijos, comprobando que cada uno reproduce la firma de error esperada:
  - Compilación → `errors\compilation-error.cs` → espera `CS0029`
  - Tiempo de ejecución → `errors\runtime-error.cs` → espera `IndexOutOfRangeException`
  - Test unitario → `errors\UnitTestError.csproj` (vía `dotnet test`) → espera `Expected: 5`
  Si se reproducen los tres errores, `main.cs` lanza una excepción (código de salida distinto de cero) — ese es el camino de "éxito" de este laboratorio, ya que el objetivo es demostrar que los errores son detectables. Si algún escenario no reproduce su error esperado, imprime un fallo distinto y devuelve `2`.
- **`database\SqlServerConnector.cs`** (`CursoClaudeCode.Database.SqlServerConnector`) — conector sellado (`sealed`) que envuelve `Microsoft.Data.SqlClient`. Lee `.env` (subiendo desde el directorio actual/`AppContext.BaseDirectory` hasta encontrarlo) y variables de entorno (`MSSQL_SA_PASSWORD`, `SQLSERVER_HOST_PORT`, `SQLSERVER_DATABASE`) para construir la cadena de conexión (`sa`@`localhost,<puerto>`, `Encrypt=True;TrustServerCertificate=True`). Expone `ExecuteQueryAsync`/`ExecuteScalarAsync`/`ExecuteNonQueryAsync` (parametrizados con `SqlParameter`) además de `ExecuteReadOnlyScalarAsync`, que rechaza sentencias que no sean `SELECT`. Ver [docs/sql-server-local.md](docs/sql-server-local.md) para los detalles de conexión.
- **`errors\`** — los tres fixtures de fallo intencionado, cada uno un script C# independiente basado en archivo (o, en el caso del test, un proyecto de test xUnit `UnitTestError.csproj`):
  - `compilation-error.cs` — `int total = "texto";` (error de tipos, `CS0029`).
  - `runtime-error.cs` — indexa más allá del final de un array de un solo elemento (`IndexOutOfRangeException`).
  - `unit-test-error.cs` — `Calculator.Add` está implementado como una resta, por lo que la aserción xUnit `Assert.Equal(5, result)` falla.
- **`CursoClaudeCode.sln`** une los dos proyectos: `CursoClaudeCode.csproj` (principal/ejecutable) y `errors\UnitTestError.csproj` (proyecto de test).

## Skills

- `.claude/skills/compile-dotnet-application` — solo diagnóstico: descubre los objetivos, ejecuta restore/build/test/run, informa los fallos con archivo:línea y causa, **nunca** modifica código.
  - Anidado `.claude/skills/compile-dotnet-application/fix-dotnet-errors` — anula la regla de "solo diagnóstico" del skill padre y está autorizado a corregir errores de verdad, repitiendo restore/build/test/run hasta que quede limpio. Úsalo (o espera que se ejecute) solo cuando el usuario pida explícitamente corregir los errores de este laboratorio, ya que arreglarlos en `develop`/`main` anula el propósito del ejercicio.
- `.claude/skills/git-jira-branch-commit` — dado un código Jira (p. ej. `DEV-9854`), guarda los cambios actuales en un stash, sincroniza `develop`, crea una rama `DEV-9854_nombre_corto_de_la_funcionalidad` a partir de ella, reaplica el stash y hace commit. Requiere el código Jira de antemano; no toca el estado de git sin él.
