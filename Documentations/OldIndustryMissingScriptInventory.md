# OldIndustry Missing Script Inventory

## Current Scope

The release scene is `Assets/Scenes/Demo_Combat.unity`.

The project-owned scripts compile and all direct script references authored into `Demo_Combat` resolve correctly. Unity still reports missing-script warnings while loading some OldIndustry prefab dependencies.

## Cause

The warnings come from 27 source prefabs under:

- `Assets/OldIndustry/Prefabs/Decals`
- `Assets/OldIndustry/Prefabs/Furniture/Lamps`

Those prefabs contain legacy HDRP decal projector or additional-light metadata GUIDs. The game uses URP, so the old HDRP behaviours are unavailable. Meshes, materials, standard lights, colliders, and the project combat scripts still load.

This is not a C# compilation error and it does not block Play Mode.

## Current Decision

The OldIndustry source package remains unchanged. Removing scripts directly from those prefabs would make third-party updates and branch management harder.

The safe future cleanup is to create project-owned URP copies of only the affected prefabs, strip the unavailable HDRP metadata from those copies, and replace their instances in `Demo_Combat`.

## Verified State

- Unity C# compiler errors: 0
- Unity EditMode tests: 6 passed
- Unity PlayMode tests: 10 passed
- Node narrative tests: 10 passed
- Demo combat NavMesh agent creation failures: 0
- Demo combat random wave archetypes verified: Rusher, Shooter, Heavy, Stalker
- Demo combat robot model, Animator, attack state, and player damage verified
- Direct project-owned Demo narrative anchors: 9
- Duplicate gameplay AudioListener errors: 0
- Third-party OldIndustry source changes: none
