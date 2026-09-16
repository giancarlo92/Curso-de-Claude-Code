---
name: create-github-pull-request
description: Use when the user asks to open/create a GitHub pull request for the current branch, push it and fill the PR form via the connected browser.
---

# Crear Pull Request en GitHub

Automatiza de extremo a extremo el flujo diario de abrir un PR en GitHub para la rama actual: push, apertura del formulario de comparación en el navegador del usuario, redacción de título/descripción, asignación y creación del PR. A diferencia de un flujo de solo revisión, este skill **sí pulsa el botón "Create pull request"** — el usuario ya autorizó explícitamente ese clic para este proceso recurrente.

## Requisitos previos

- Repositorio Git con un remoto `origin` en GitHub.
- El usuario debe tener sesión iniciada en GitHub en su navegador conectado (Claude in Chrome). Si las herramientas `mcp__claude-in-chrome__*` no están cargadas, cárgalas primero con `ToolSearch` (`select:mcp__claude-in-chrome__tabs_context_mcp,mcp__claude-in-chrome__navigate,mcp__claude-in-chrome__computer,mcp__claude-in-chrome__read_page,mcp__claude-in-chrome__find,mcp__claude-in-chrome__form_input,mcp__claude-in-chrome__browser_batch,mcp__claude-in-chrome__tabs_create_mcp,mcp__claude-in-chrome__list_connected_browsers`).

## Flujo

1. **Estado local.** `git branch --show-current` y `git status --short`. Si hay cambios sin commitear que claramente pertenecen a la tarea, avisa al usuario antes de continuar; no los commitees por tu cuenta.
2. **Rama base.** Usa `develop` si existe en `origin` (`git ls-remote --heads origin develop`); si no existe, usa la rama por defecto del remoto (`git remote show origin | grep 'HEAD branch'`). No asumas `main` sin comprobarlo.
3. **Sincroniza la rama con origin.**
   ```powershell
   git push -u origin <rama-actual>
   ```
   Si la rama ya tiene upstream, un `git push` normal basta. Este push es parte del flujo autorizado — no pidas confirmación adicional para él.
4. **Resuelve owner/repo** desde `git remote get-url origin` (soporta formato `https://github.com/<owner>/<repo>.git` y `git@github.com:<owner>/<repo>.git`).
5. **Redacta título y descripción** a partir de los commits/diff de la rama (`git log <base>..<rama> --oneline`, `git diff <base>...<rama> --stat` y el contenido relevante):
   - Título: `<tipo>(<código-tarea>): <resumen corto>` si hay un código de tarea identificable en el nombre de rama o commits; si no, un resumen conciso en imperativo.
   - Descripción con este formato:
     ```markdown
     ## Resumen
     <qué cambia y por qué, 1-3 líneas>

     ## Cambios
     - <archivo>: <qué cambió>

     ## Plan de pruebas
     - [ ] <comando o verificación relevante>

     🤖 Generated with [Claude Code](https://claude.com/claude-code)
     ```
6. **Abre el formulario de comparación** en el navegador conectado:
   ```
   https://github.com/<owner>/<repo>/compare/<base>...<rama-actual>?expand=1
   ```
   Si GitHub muestra que ya existe un PR abierto para esa combinación de ramas (en vez del formulario), no crees uno duplicado: reporta el enlace del PR existente y detente.
7. **Rellena el formulario:**
   - Título: usa `find` para localizar el campo y `computer` (`triple_click` + `type`, o `ctrl+a` + `type`) para reemplazarlo.
   - Descripción: usa `find` para obtener el `ref` del textarea y **`form_input` para fijar el valor directamente** (no uses `type` con saltos de línea aquí: el editor Markdown de GitHub auto-inserta `- ` al continuar una lista y duplica los guiones de cada ítem si escribes el `- ` tú mismo).
   - Asignado: clic en "assign yourself" (o equivalente) para asignar el PR al propio usuario, salvo que pida otra persona.
   - Reviewers/labels: solo si el usuario los pidió explícitamente; por defecto no se tocan.
8. **Crea el PR:** localiza el botón "Create pull request" y haz clic. No hace falta confirmación adicional en chat para este clic específico — ya está autorizado para este flujo.
9. **Verifica el resultado:** tras el clic, lee la página (`read_page` o `get_page_text`) para confirmar que se creó y capturar la URL final del PR (`.../pull/<número>`). Repórtasela al usuario.

## Detenciones

- Sin remoto `origin` o remoto que no es GitHub: informa y detente, no lo inventes.
- Sin sesión iniciada en GitHub en el navegador conectado: pide al usuario iniciar sesión y detente.
- Ya existe un PR abierto para `<rama-actual>` contra `<base>`: no crear uno nuevo, reportar el existente.
- Cambios sin commitear relevantes no incluidos en la rama: avisar antes de abrir el PR, ya que quedarían fuera.
- Todo lo demás (redactar el texto, elegir la rama base, rellenar el formulario, pulsar "Create pull request") no requiere pausa adicional: es exactamente el proceso diario que este skill automatiza.
