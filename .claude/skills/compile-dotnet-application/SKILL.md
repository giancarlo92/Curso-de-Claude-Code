---
name: compile-dotnet-application
description: Use when validating, compiling, testing, running, or diagnosing a .NET project or C# file.
---

# Validación .NET

Aplica este skill solo a proyectos con `.sln`, `.slnx`, `.csproj`, `.fsproj` o archivos C# ejecutables. No asumas nombres ni rutas.

## Secuencia obligatoria

1. Descubre el objetivo:

   ```powershell
   rg --files -g '*.sln' -g '*.slnx' -g '*.csproj' -g '*.fsproj' -g '*.cs'
   ```

2. Respeta `global.json` y comprueba `dotnet --version`.
3. Ejecuta:

   ```powershell
   dotnet restore <objetivo> --nologo
   dotnet build <objetivo> --nologo --no-restore
   dotnet test <objetivo> --nologo --no-build
   ```

4. Si el build termina correctamente y existe una aplicación ejecutable, ejecuta siempre:

   ```powershell
   dotnet run --project <proyecto-ejecutable> --no-build -- <argumentos>
   ```

   Para un script C# independiente usa `dotnet run --file <archivo.cs>`.

No omitas `dotnet run` porque el usuario no lo haya mencionado: es necesario para detectar errores de ejecución. Si hay varios ejecutables, ejecuta el que corresponda al objetivo o pide elegir.

## Resultado y errores

Registra código de salida y salida completa de cada comando. Un código `0` no basta: busca también `CSxxxx`, `MSBxxxx`, excepciones, `error`, `failed` y `FAIL`.

Para cada problema informa brevemente:

```text
Fase: build | test | run
Error: código o excepción
Ubicación: archivo:línea:columna
Causa: explicación
Solución: cambio o comando concreto
```

Si `dotnet test` no descubre tests, informa `SIN TESTS`; no lo presentes como tests aprobados. No ocultes errores ni modifiques código automáticamente.
