# Don't Die Please

Don't Die Please is a Unity sci-fi survival game set on the toxic planet Yggdrasil. The player explores the Far Kite crash site, survives robot attacks, rebuilds a signal generator and attempts to call for rescue.

## Current Build

Unity version: `6000.4.9f1`

Release flow:

```text
MainMenuScene
  -> Demo_Combat
```

The login page is no longer part of the Unity client. New Game and Continue use local narrative persistence and load the final playable scene directly.

Main scenes:

- `Assets/Scenes/MainMenuScene.unity`
- `Assets/Scenes/Demo_Combat.unity`
- `Assets/Scenes/MainGameplayScene.unity` remains disabled as an aircraft-scene archive

## Demo Combat

`Demo_Combat` contains:

- Liang Shuang's Far Kite spacecraft
- the existing green Yggdrasil terrain
- Akila FPS Framework first-person player
- pistol and assault rifle loadout
- seeded robot waves using the Protofactor robot pack
- narrative encounter and Signal Generator anchors
- first-death and repeat-death recovery dialogue

The player no longer reaches a terminal Game Over at zero health. MIMIR loads the latest branch, then the combat bootstrap creates a fresh Akila player, restores the camera and weapons, and retargets active enemies.

## Gameplay Systems

Survival and persistence:

- health, oxygen, hunger and toxicity data
- inventory and base module state
- objective progress and active timers
- `worldSeed` persistence for repeatable maps and events
- local narrative save used by New Game and Continue

Seeded systems:

- `GameSeedManager` owns deterministic random streams
- `SeededMapGenerator` builds repeatable modular layouts
- `RandomEventManager` triggers seeded storms, patrols and resource drops
- `RandomEventHud` presents event warnings

Combat:

- Akila movement, mouse look, ADS, firing, reload and weapon switching
- four project enemy archetypes with different health and attacks
- Protofactor robot visuals and animation controllers
- runtime NavMesh generation and seeded spawn positions
- story-gated first robot, Warden-K and Signal Generator defence

## Backend

The Express and MongoDB backend remains available for account and cloud-save work, but it is not required to enter the current Unity build.

Install dependencies:

```bash
npm install
```

Create `.env`:

```env
PORT=5000
MONGODB_URI=mongodb+srv://...
JWT_SECRET=replace-this-with-a-long-secret
CORS_ORIGIN=http://localhost:3000
```

Run:

```bash
npm run dev
```

Endpoints:

- `GET /api/v1/health`
- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`
- `POST /api/v1/save`
- `GET /api/v1/save`
- `PUT /api/v1/save`
- `GET /api-docs`

## Repository Setup

Open the repository root in Unity Hub.

Do not commit:

- `Library`
- `Temp`
- `Obj`
- `Logs`
- `UserSettings`

The StarSparrow spacecraft, Protofactor robots, Akila FPS Framework and terrain dependencies used by `Demo_Combat` are tracked in the repository. A complete clone should therefore reproduce the playable scene without relying on a separate local asset folder.

## Controls

- `WASD`: move
- `Mouse`: look
- `Right Mouse`: aim
- `Left Mouse`: fire
- `R`: reload
- `1`, `2` or mouse wheel: switch weapons
- `E`: interact
- `Esc`: pause

## Verification

Before sharing a build:

1. Open `Assets/Scenes/Demo_Combat.unity`.
2. Confirm the Far Kite is visible on the green terrain.
3. Press Play and confirm one Akila player camera and one AudioListener.
4. Confirm movement, look, aim, fire, reload and weapon switching.
5. Let an enemy reduce health to zero.
6. Confirm MIMIR's recovery dialogue appears and the replacement player keeps the FPS camera and weapons.
7. Confirm robot materials render normally and the Console has no compilation errors.
