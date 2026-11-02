# MIJUEGO2D

Juego de plataformas 2D hecho en **Unity 6.3 LTS (6000.3.11f1)** con URP 2D.

Nico debe atravesar tres niveles (Bosque, Montañas y Castillo), derrotar a enemigos
y al jefe **Maldrak**, y rescatar a la **Princesa Elena**.

## Equipo

| Integrante | Área |
|---|---|
| Daner | Base del proyecto, núcleo (Core) y herramientas de editor |
| Cesar | Jugador (Nico) |
| Castillo | Enemigos, jefe y Nivel 3 (Castillo) |
| Condor | Mundo, fondos y Niveles 1 y 2 |
| Inés | Interfaz (UI), audio, objetos y escenas de menú/victoria |

## Cómo abrir el proyecto

1. Instalar **Unity Hub** y la versión **6000.3.11f1** (Unity 6.3 LTS).
2. Clonar este repositorio: `git clone <URL-del-repo>`
3. En Unity Hub: *Projects → Add → Add project from disk* y elegir la carpeta clonada.
4. Esperar a que Unity importe todo (la primera vez tarda varios minutos y necesita internet).
5. Abrir `Assets/Game/Scenes/MainMenu` y pulsar Play.

> Nota: `Packages/manifest.json` incluye dos paquetes descargados desde GitHub por URL
> (`com.coplaydev.unity-mcp` y `com.ramenvr.aura-unity`, herramientas de asistencia en el editor).
> El código del juego no depende de ellos. Si Unity muestra errores de paquetes (por ejemplo, si
> no tienen Git instalado), pueden quitar esas dos líneas de `Packages/manifest.json`.

## Estructura

```
Assets/Game/
├── Scripts/   Core · Player · Enemies · World · UI · Editor
├── Scenes/    MainMenu · Level1_Bosque · Level2_Montanas · Level3_Castillo · Victoria
├── Prefabs/   jugador, enemigos, jefe, objetos, checkpoints
├── Sprites/   personajes, fondos, parallax, plataformas, props
└── Resources/ audio y efectos
```

