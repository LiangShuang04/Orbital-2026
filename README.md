# Don't Die Please

Don't Die Please is a Unity sci-fi survival prototype about staying alive on the toxic planet Yggdrasil long enough to rebuild a signal generator and call for rescue.

The repo currently contains both sides of the project:

- Unity client gameplay scenes, UI, seeded map/event systems, and combat prototypes
- Node.js/Express backend for account auth, MongoDB save data, and Swagger API docs

## Current Build

Unity version: `6000.4.9f1`

Main scenes:

- `Assets/Scenes/Login.unity`
- `Assets/Scenes/MainMenuScene.unity`
- `Assets/Scenes/MainGameplayScene.unity`
- `Assets/Scenes/Central.unity`
- `Assets/Scenes/Central_Combat.unity`
- `Assets/Scenes/demoMainScene.unity`
- `Assets/Scenes/Demo_Combat.unity`

The main project branch keeps our gameplay/backend work. Large imported packages are kept on the `third-party-packages` branch so `main` stays easier to review.

## Gameplay Systems

Login:

- Sci-fi login scene based on the imported PHPLogin UI
- Mock auth by default for Unity testing
- Backend-ready auth client for Express `/api/v1/auth`

Survival/save data:

- Health, oxygen, hunger, and toxicity
- Inventory items like `metal_scrap` and `filter_fibre`
- Base modules such as oxygen station, storage unit, power generator, and signal generator
- Objective progress and active timers
- `worldSeed` stored with save profiles so seeded systems can be restored later

Seeded world features:

- `GameSeedManager` owns deterministic random streams
- `SeededMapGenerator` builds a repeatable modular map layout from the same seed
- `RandomEventManager` triggers seeded toxic storm, robot patrol, and resource drop events
- `RandomEventHud` shows warnings in-game
- `PauseSettingsMenuController` handles pause/resume/restart/settings/menu/quit plus PlayerPrefs settings

Combat:

- `Central_Combat` and `Demo_Combat` are test scenes for FPS combat
- Akila FPS Framework player prefab is used for first-person movement, aiming, firing, reload, and weapon switching
- Enemy prototype includes movement, stats, health, FSM, and robot bundle assets from the enemy branch

## Backend

Install dependencies:

```bash
npm install
```

Create a local `.env` file:

```env
PORT=5000
MONGODB_URI=mongodb+srv://...
JWT_SECRET=replace-this-with-a-long-secret
CORS_ORIGIN=http://localhost:3000
```

Run the API:

```bash
npm run dev
```

Useful endpoints:

- `GET /api/v1/health`
- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`
- `POST /api/v1/save`
- `GET /api/v1/save`
- `PUT /api/v1/save`
- `GET /api-docs`

The Unity client should never talk to MongoDB directly. It talks to the API with a JWT.

## Unity Setup

Open the repo root in Unity Hub, not just the `Assets` folder.

Do not commit these generated folders:

- `Library`
- `Temp`
- `Obj`
- `Logs`
- `UserSettings`

The most useful editor tools are under Unity's top menu:

- `Tools/Central/Combat/Rebuild Central Combat Scene`
- `Tools/OldIndustry/Combat/Rebuild Demo Combat Scene`
- `Tools/Old Industry/Setup First Person Walker In Demo Scene`
- `Tools/Don't Die Please/Apply Toxic Morandi Style`
- `Tools/Don't Die Please/Batch Assign Seeded Variant`

## Docs

- `Documentations/LoginAuthenticationSetup.md`
- `Documentations/SeededEventsAndMenuChecklist.md`
- `Assets/_Central/Docs/CentralCombatREADME.md`
- `Assets/_Central/Docs/DemoCombatREADME.md`
- `Assets/_Central/Docs/OldIndustryFirstPersonControls.md`
- `Assets/_Login/README.md`

## Current Gaps

- Random events trigger and display HUD warnings, but toxic storm still needs to affect real oxygen/toxicity gameplay.
- Some map/resource/event prefabs are still prototype-level and need final art hookup.
- Unity play-mode testing should be repeated after pulling large third-party assets.
- Imported package delivery should stay on the separate branch unless we decide to move to Git LFS or release downloads.
