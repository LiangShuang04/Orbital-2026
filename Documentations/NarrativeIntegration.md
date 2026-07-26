# Narrative Integration

## Release Flow

```text
MainMenuScene
  -> Demo_Combat
```

The Unity login page has been removed. New Game clears the local narrative state, creates a new world seed and enters `Demo_Combat`. Continue restores the local seed and story state before loading the same scene.

`MainGameplayScene` remains disabled in Build Settings as an archive of the earlier aircraft flow. `Central` and `Central_Combat` remain development scenes and are not part of the release path.

## Demo Scene

`Demo_Combat` combines the Far Kite crash site, green Yggdrasil terrain, final combat encounters and the Signal Generator storyline.

The authored scene contains:

- Far Kite spacecraft from Liang Shuang's `cc6adf1` scene
- green terrain from the existing OldIndustry TerrainData
- first robot spawn
- Warden-K spawn
- Signal Generator assembly and placement
- defence centre and four defence enemy spawns

At runtime, `CentralCombatBootstrapper` loads the baked Demo_Combat NavMesh, creates the Akila managers, FPS player, weapons and combat spawner, and falls back to runtime NavMesh generation only when baked data is missing. `NarrativeRuntimeInstaller` creates the dialogue presenter, save adapter, world binder and combat coordinator.

After changing terrain or building colliders, open `Demo_Combat` and run `Tools > Don't Die Please > Combat > Demo > Bake Demo Combat NavMesh`.

## Death Recovery

Akila's default respawn route is disabled for the project player because it does not restore the project loadout and camera setup.

`CentralPlayerRecovery` now owns the project flow:

1. Health reaches zero.
2. Player input and inventory input are locked.
3. First death plays `REACT_FIRST_DEATH`; later deaths play `REACT_REPEAT_DEATH`.
4. Narrative state records `first_death_seen`.
5. `CentralCombatBootstrapper` creates the same Akila player prefab again.
6. Pistol and assault rifle are restored.
7. The new player camera becomes the only active gameplay camera.
8. Active enemies receive the new player target.

This represents MIMIR loading the latest saved branch instead of ending the run.

## Combat

The Demo uses:

- Akila firearm hit processing
- `CentralCombatEnemy` health and death handling
- `CentralCombatEnemyAI` chase, melee and projectile attacks
- Protofactor robot visuals selected by enemy archetype
- seeded wave composition and placement
- runtime material conversion for URP

Enemies are still gated by narrative state. The first robot, Warden-K and defence waves appear at their corresponding story objectives.

## Persistence

The current client flow uses local narrative persistence so no account is required. It stores:

- playthrough ID
- world seed
- current objective
- completed sequences and objectives
- narrative flags
- Signal Generator progress
- defence timer state

The Express authentication and save API remain in the repository for future optional cloud-save work.

## Manual Check

1. Start from `MainMenuScene`.
2. Select New Game and confirm `Demo_Combat` loads.
3. Confirm the Far Kite, green terrain and runtime HUD are visible.
4. Confirm FPS movement, aim, fire, reload and weapon switching.
5. Confirm enemies spawn on reachable ground and attack the player.
6. Trigger a first death and let all three dialogue lines complete.
7. Confirm the replacement player can move, aim and fire.
8. Trigger another death and confirm the shorter repeat line is used.
9. Return to the menu, choose Continue and confirm the same seed and narrative flags load.
