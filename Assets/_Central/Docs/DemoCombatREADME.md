# Demo Combat

Scene: `Assets/Scenes/Demo_Combat.unity`

Source scene used locally: `Assets/Scenes/demoMainScene.unity`

The original OldIndustry package folders and the main game scene are not edited.

## Framework Notes

Damage interface: `Akila.FPSFramework.IDamageable`

Weapon damage path: Akila `Firearm` raycast calls `IDamageable.Damage(amount, source)`.

Player prefab: `Assets/Akila/FPS Framework/Prefabs/Characters/Player.prefab`

Starter weapons: `Pistol_1` and `Assault Rifle_1`

Spawn support: Akila `SpawnManager` is created at runtime if missing.

Input handling: Project is already set to `Both`.

Render pipeline: URP project with imported OldIndustry assets.

## What Runs In Play Mode

`CentralCombatBootstrapper` activates in `Demo_Combat`.

It disables OldIndustry `FreeCamera`, any old `FirstPersonController`, and extra scene cameras, builds a runtime `NavMeshSurface`, spawns the Akila player, equips starter weapons, places ammo pickups, and starts enemy waves.

## Enemy Archetypes

Rusher: 45 HP, fast melee.

Heavy: 160 HP, slow high-damage melee.

Shooter: 75 HP, ranged projectile and repositioning.

Stalker: 55 HP, fast dark melee unit.

## Test Checklist

Open `Assets/Scenes/Demo_Combat.unity`.

Press Play.

Confirm only the Akila player camera is active.

Move, look, shoot, reload, and switch weapons.

Wait for wave 1.

Shoot enemies and confirm hitmarker plus death/despawn.

Let a melee enemy hit the player and confirm health changes.

Let a Shooter fire and confirm projectiles connect.

Watch Console for NavMesh, input, missing prefab, or material errors.

Use `Tools/OldIndustry/Combat/Rebuild Demo Combat Scene` if you need a fresh copy.
