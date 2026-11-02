# Guía para colaborar

## Ramas

- `main`: versión estable. Solo se actualiza al cerrar un ciclo, por Daner.
- `develop`: integración del equipo.
- `feature/<tarea>`: una rama por tarea o por persona. Siempre se crea desde `develop`.

## Flujo

1. `git checkout develop` y `git pull`
2. `git checkout -b feature/nombre-de-la-tarea`
3. Trabajar y hacer commits pequeños con mensajes claros (`Player: agregar salto doble`).
4. `git push -u origin feature/nombre-de-la-tarea`
5. Abrir un Pull Request hacia `develop`. Lo revisa otra persona, nunca quien lo escribió.
6. Tras mezclarlo, todos hacen `git checkout develop` y `git pull`.

## Reglas de Unity

- Todos usan la misma versión: **6000.3.11f1**.
- Siempre se suben los archivos `.meta` junto a su archivo.
- Una sola persona edita cada escena (`.unity`) a la vez; cada escena tiene su responsable.
- Antes de subir, abrir Unity y comprobar que no hay errores rojos en la consola.
- No subir `Library`, `Temp`, `Logs`, `UserSettings` ni archivos de más de 100 MB.

## Identidad en Git

Cada integrante configura su nombre y el correo de su cuenta de GitHub:

    git config --global user.name "Nombre Apellido"
    git config --global user.email "correo-de-su-cuenta-github"

## Revisiones cruzadas

| Pull Request de | Lo revisa |
|---|---|
| Cesar | Castillo |
| Castillo | Cesar |
| Condor | Inés |
| Inés | Condor |
