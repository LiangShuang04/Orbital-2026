# Narrative Integration

## Release Flow

```text
Login
  -> MainGameplayScene
  -> Demo_Combat
```

`MainGameplayScene` is the aircraft introduction and tutorial. `Demo_Combat` is the final playable map and contains the ruins storyline, combat encounters, Warden-K, Signal Generator assembly, and defence.

`Central` and `Central_Combat` are not part of the release flow. `MainMenuScene` remains available for standalone menu tests but authenticated login routes directly through `AuthenticatedGameFlow`.

## Login and Resume

`PhpLoginBridge` connects the imported sci-fi login UI to the Express endpoints through `NetworkManager`.

After successful registration or login:

1. Unity stores the JWT in memory.
2. Unity loads `GET /api/v1/save`.
3. A missing profile is created with a new `worldSeed` and `ACT1_WAKE`.
4. `AuthenticatedGameFlow` chooses the resume scene from `objectiveState`.
5. Early progression loads the aircraft scene.
6. Ruins and later progression load `Demo_Combat`.

Narrative progress is saved in MongoDB through `SaveProfileService` and `NarrativeSaveAdapter`. The stored data includes the world seed, current quest, completed story entries, Signal Generator progress, and active timers.

## Aircraft Scene

`MainGameplayScene` handles:

- wake-up and aircraft introduction
- survival and interaction tutorial
- initial objective presentation
- the unknown transmission
- transition into `Demo_Combat`

Combat encounters and final Signal Generator assembly are not spawned in this scene.

## Demo Scene

`Demo_Combat` handles:

- ruins entry
- first robot encounter
- Warden-K encounter
- component core reward
- Signal Generator assembly and placement
- seeded defence waves
- final rescue progression

`NarrativeAnchorSceneAuthoring` authors nine typed scene anchors:

- first robot spawn
- Warden-K spawn
- Signal Generator assembly
- Signal Generator placement
- defence centre
- four defence enemy spawns

At runtime, `CentralCombatBootstrapper` builds the Demo NavMesh and configures the Akila player and combat spawner. `NarrativeCombatCoordinator` waits for that setup before aligning encounter anchors and enabling narrative combat.

## Combat

The Demo scene uses one framework FPS player, one gameplay camera, and one enabled AudioListener.

The combat path supports:

- Akila pistol firearm hit processing
- `EnemyHealthDamageAdapter` for the first robot and defence robots
- `CentralCombatEnemy` damage for Warden-K
- Warden-K health set to 320
- automatic wave spawning disabled outside the Signal Generator defence
- seeded defence placement with unique reachable NavMesh positions

Enemies are intentionally gated by story state. The first robot is hidden until its story event, Warden-K appears at its objective, and defence robots spawn only while `signalDefenseActive` is true.

## Narrative UI

`DialoguePresenter` builds the runtime HUD and dialogue interface.

Current readability settings:

- Liberation Sans SDF
- high-contrast dark panels and bright text
- 30 pt full dialogue text
- 28 pt subtitle text
- 24 pt notifications
- larger objective and choice text
- 36 character-per-second typewriter speed
- at least 5.5 seconds of automatic reading time with extra time for longer lines

The login UI also replaces package fonts at runtime, increases input and button sizes, and blocks repeated input while an API request is active.

## Scene Transitions

`NarrativeWorldBinder` creates the aircraft-to-Demo route after the transmission step. `NarrativeSceneBridge` raises scene-specific story events and never starts Demo combat while the player is still in the aircraft.

When the player logs in again, scene routing comes from the backend save rather than always restarting the aircraft sequence.

## Automated Verification

Current passing checks:

- Node narrative tests: 10 passed
- Unity EditMode tests: 6 passed
- Unity PlayMode tests: 9 passed

The PlayMode suite verifies:

- new game and continue state handling
- authored Demo anchors are unique and NavMesh reachable
- the first robot remains gated until its event
- the first robot progresses only from its own death
- restored Signal Generator defence spawns capped enemies and respects pause
- the Akila pistol damages project enemies through the framework hit path
- Demo creates one FPS player, one gameplay camera, and one AudioListener
- Warden-K can be killed through Akila firearm hits
- Warden-K completion creates the Signal Generator assembly console

## Manual Release Check

Before a build:

1. Run the Express backend and connect to the intended MongoDB database.
2. Register a fresh account in `Login`.
3. Confirm the first load enters `MainGameplayScene`.
4. Play the aircraft tutorial and verify dialogue timing at the target resolution.
5. Enter `Demo_Combat` and manually verify movement, aiming, firing, enemy visibility, and HUD readability.
6. Exit and log in again to verify resume routing.
7. Confirm the Console has no compilation errors, duplicate AudioListeners, or authentication failures.
8. Treat the documented OldIndustry HDRP metadata messages as package warnings, not project compilation failures.
