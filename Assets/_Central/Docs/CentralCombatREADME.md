# Central Combat

Scene: `Assets/Scenes/Central_Combat.unity`

Main game scene is not touched. `Central_Combat` is a copy of `Central` with a runtime combat bootstrapper.

## Framework Notes

Damage interface: `Akila.FPSFramework.IDamageable`

Weapon damage path: Akila `Firearm` raycast calls `IDamageable.Damage(amount, source)`.

Player prefab: `Assets/Akila/FPS Framework/Prefabs/Characters/Player.prefab`

Starter weapons: `Pistol_1` and `Assault Rifle_1`

Spawn support: Akila `SpawnManager` is created at runtime if the scene does not already have one.

Input handling: Project is already set to `Both`.

Render pipeline: URP.

## What Runs In Play Mode

`CentralCombatBootstrapper` only activates in a scene named `Central_Combat`.

It disables old `FirstPersonController` objects in that scene, builds a runtime `NavMeshSurface`, spawns the Akila player, sets starter weapons, adds a combat HUD, places ammo pickups, and starts wave spawning.

## Enemy Archetypes

Rusher: 45 HP, fast melee, low damage, group pressure.

Heavy: 160 HP, slow melee, high damage, obvious pressure unit.

Shooter: 75 HP, ranged projectile, repositions if the player gets too close.

Stalker: 55 HP, fast melee, smaller dark silhouette, late-wave pressure.

## Test Checklist

Open `Assets/Scenes/Central_Combat.unity`.

Press Play.

Check that only the Akila player camera is active.

Move, look, shoot, reload, and switch weapons.

Wait for wave 1.

Shoot a Rusher until it dies.

Confirm hitmarker appears and enemy despawns.

Let a melee enemy reach the player and confirm player health drops.

Let a Shooter fire and confirm projectiles damage the player.

Watch Console for missing prefab, NavMesh, or input errors.

If enemies do not move, run `Tools/Central/Combat/Rebuild Central Combat Scene`, reopen `Central_Combat`, and press Play again.
