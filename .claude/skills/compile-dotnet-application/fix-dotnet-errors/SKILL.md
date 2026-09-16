---
name: fix-dotnet-errors
description: Use when a .NET project has compilation errors, runtime exceptions, failing tests, restore failures, or other diagnostics that must be corrected and revalidated.
---

# Fix .NET Errors

Skill anidada a `compile-dotnet-application`. Corrige los errores del proyecto y repite la validación hasta que no queden errores conocidos.

Esta skill autoriza modificar el código para corregir errores; prevalece sobre la instrucción de solo diagnosticar del skill padre.

## Aviso inicial obligatorio

Antes de tocar archivos, informa:

> Ejecutaré el diagnóstico, corregiré los errores y repetiré las pruebas hasta que compilación, tests y ejecución queden correctos. No solicitaré decisiones durante el proceso; podrás revisar los cambios al final.

No hagas preguntas durante el ciclo. Decide usando el código, los errores reproducidos, las convenciones del proyecto y la documentación disponible.

## Bucle obligatorio

1. Usa `compile-dotnet-application` para descubrir la solución/proyectos y ejecutar `restore`, `build`, `test` y `run` cuando corresponda. Registra salida y código de cada comando.
2. Investiga la causa raíz de cada error: archivo, línea, dependencia y cambio que lo provoca. No corrijas por intuición.
3. Corrige primero compilación y restore; después errores de ejecución; finalmente tests unitarios y otros tests. Corrige una causa relacionada por iteración.
4. Repite inmediatamente la comprobación que evidenció el error. Si pasa, ejecuta la siguiente fase; si falla, vuelve al diagnóstico.
5. En tests, conserva el comportamiento esperado: no elimines tests, no los omitas y no cambies aserciones solo para obtener verde. Corrige producción cuando el código está mal y el test cuando el test está mal.
6. Tras cada cambio ejecuta de nuevo el ciclo completo. Continúa hasta que restore/build/test/run terminen correctamente y no haya errores `CS`, `MSB`, excepciones, `error`, `failed` o `FAIL` en sus salidas.

## Código limpio y arquitectura

- No agregues comentarios cuando el código sea claro y evidente.
- Si una corrección tiene complejidad significativa, añade como máximo una línea que explique brevemente el error o la solución.
- Separa responsabilidades en métodos, clases o archivos pequeños cuando la funcionalidad lo requiera; no concentres todo en un único archivo.
- Respeta la arquitectura existente y aplica código limpio, principios SOLID y patrones de diseño adecuados sin sobreingeniería.

## Límites

- No ocultes errores, desactives validaciones, rebajes cobertura ni introduzcas cambios no relacionados.
- Si el mismo error reaparece, vuelve a investigar su causa raíz; no repitas el mismo parche.
- Si el bloqueo depende de credenciales, servicios externos, permisos o requisitos ausentes, realiza todas las correcciones locales posibles y termina informando el bloqueo exacto. No lo presentes como solucionado ni formules una pregunta durante el ciclo.
- Prohibido hacer commits durante el ciclo.
- No crear documentación durante el ciclo.

## Resultado final

Entrega un resumen de archivos modificados, errores corregidos y comandos finales ejecutados. Si quedó un bloqueo externo, indica su causa y la acción necesaria para continuar después de la revisión del usuario.
