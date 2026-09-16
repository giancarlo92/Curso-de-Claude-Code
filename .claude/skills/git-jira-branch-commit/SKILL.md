---
name: git-jira-branch-commit
description: Use when the user asks to inspect current Git changes, create a Jira-named branch from develop, and commit those changes.
---

# Git Jira Branch Commit

Gestiona los cambios actuales en una rama nueva basada en el último `develop`.

## Regla obligatoria

El usuario debe proporcionar un código Jira, por ejemplo `DEV-9854`, o un enlace que lo contenga. Si falta, solicita el código y termina. No ejecutes ningún comando Git, no leas archivos y no modifiques el repositorio antes de recibirlo. No inventes el código.

## Flujo

1. Extrae y normaliza la clave `DEV-<número>` desde el texto o enlace. Usa esa clave en mayúsculas.
2. Lee los cambios activos antes de preparar el commit:
   - `git status --short`
   - `git diff` y `git diff --cached`
   - contenido de todos los archivos modificados, creados o eliminados que sean legibles.
   Incluye cambios staged, unstaged y no rastreados. No incluyas archivos ignorados ni binarios sin inspeccionarlos como texto.
3. Deduce un nombre corto de la funcionalidad a partir de los cambios. Usa 2–5 palabras en minúsculas separadas por `_`, sin espacios, `/` ni caracteres especiales.
4. Comprueba la rama actual y, si hay cambios locales, guárdalos inmediatamente —incluidos los no rastreados— antes de cambiar de rama:
   `git stash push --include-untracked -m "pre-<jira>"`
   Conserva la referencia exacta del stash y comprueba con `git stash show --include-untracked --name-status <referencia>` que contenga todos los archivos detectados en el paso 2. Si la verificación falla, no cambies de rama.
   `--include-untracked` retira temporalmente los archivos no rastreados del directorio de trabajo; no continúes hasta confirmar que están guardados. Si no hay cambios, no crees un stash. El árbol queda limpio para que el cambio a `develop`, su actualización y la creación de la rama no arrastren conflictos locales.
5. Con el árbol limpio, cambia a `develop` y actualízalo desde `origin/develop` con avance rápido:
   `git switch develop`
   `git pull --ff-only origin develop`
   Si `develop`, el remoto o la actualización no están disponibles, detente e informa el problema; no uses otra rama como base.
6. Crea la rama exactamente con este formato:
   `DEV-9854_nombre_corto_de_la_funcionalidad`
   Sustituye `DEV-9854` por la clave recibida.
7. Recupera el stash únicamente en la rama nueva usando `git stash apply <referencia>`. Este es el único punto donde podría aparecer un conflicto: solo ocurriría si `develop` cambió las mismas líneas. Si sucede, detente, informa los archivos afectados y conserva el stash.
   Después de aplicarlo, comprueba `git status --short --untracked-files=all` y confirma que todos los archivos guardados en el paso 4 reaparecieron. No uses `git stash pop` porque elimina el respaldo automáticamente.
8. Revisa de nuevo todos los cambios recuperados. Agrega todos los cambios activos con `git add -A`, valida con `git diff --cached --check` y crea un commit cuyo mensaje describa el cambio:
   `<tipo>(DEV-9854): <verbo y resumen corto>`
   Usa `feat`, `fix`, `refactor`, `docs`, `test`, `build` o `chore` según corresponda.
9. Verifica `git status --short`, `git branch --show-current` y `git log -1 --oneline`. No afirmes éxito sin revisar sus salidas.

## Detenciones

- Sin código Jira: solicitarlo y no hacer nada más.
- Sin cambios después de recuperar el stash: no crear un commit vacío; informar que no hay cambios para confirmar.
- Conflicto al aplicar el stash, `develop` desactualizable o validación fallida: detenerse y explicar la solución concreta.
- Mantén el stash como respaldo hasta confirmar que la nueva rama contiene correctamente los cambios.
- `git add -A` solo actualiza el índice; no elimina archivos del disco. Una eliminación (`D`) indica que el archivo ya estaba eliminado antes de agregarlo.
- No uses `git clean`, `git reset --hard`, `git restore`, `git checkout --` ni `git stash drop` durante este flujo.
