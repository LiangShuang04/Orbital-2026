# Don't Die Please — Session Handoff

Paste this into a new Claude Code session to continue with full context. It captures the project state, every script built/modified, the (messy) git situation, required Unity wiring, and outstanding work.

---

## 1. Project overview

- **Game:** *Don't Die Please* — first-person sci-fi survival game. Player crash-lands on toxic alien planet **Yggdrasil** (once home to the vanished Type-3 **Omphalos** civilisation), survives (oxygen/hunger/toxicity/health), gathers resources, crafts, fights robots, builds a signal generator for rescue.
- **Engine:** Unity 6000.4.9f1, **URP**. Language: C#.
- **Program:** NUS Orbital 2026, "Apollo" level. Team: **LiangShuang** (this user — gameplay/FPS/enemies/crafting/UI) + **Xiaoyan** (backend/auth/save-load/seeded map/combat).
- **Project path:** `/Users/liang/Documents/GitHub/Orbital-2026`
- **Deadline:** ~2 days out at time of writing (late July 2026). Priority is a **working, finishable demo**, not more features.

---

## 2. ⚠️ Git state — READ THIS FIRST

The repo has been through heavy branch churn this session, which repeatedly caused files to "disappear" (branch switches / **detached HEAD** reverting the working tree).

**Current good state:**
- Branch **`backup-detached-work`** @ commit **`bbd167cc`** = the **complete source of truth**: most-recent MainGameplayScene + seed system + ALL session scripts + correct `.gitignore`.
- This branch **diverged** from `origin/third-party-packages` (@ `5ea247ad`), so a plain `git push` is rejected.

**Other states (safe, still in git):**
- `origin/third-party-packages` @ `5ea247ad` = the session scripts, older scene (pushed).
- `97f25b54` = the newest scene, older scripts (was local-only).
- `origin/main` @ `2025c607` = Xiaoyan's Demo Combat work.

**To get the combined state onto GitHub (user runs these — user prefers to drive git themselves):**
```bash
git branch -m backup-detached-work third-party-packages-combined
git push -u origin third-party-packages-combined   # non-destructive: new branch
```

**RULES to stop the churn (this caused most problems this session):**
1. **Never `git checkout <commit-hash>`** — that causes detached HEAD and reverts files. Only `git checkout <branch-name>`.
2. If `git branch --show-current` prints nothing → you're detached → `git checkout <branch>` immediately.
3. **Stay on ONE branch and commit often**: `git add -A && git commit -m "..." && git push`.
4. Big asset packs are **gitignored** (`VattalusAssets`, `_DLNK`, `SineVFX`, `Health Pack`) — kept locally, imported per-teammate. Do NOT re-track them (a 161 MB PSD + ~2.5 GB broke the push earlier). Keep those `.gitignore` exclusions.

**Git commit trailer convention:** end commit messages with `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`. NOTE the AI-policy caveat in §8.

---

## 3. Player setup (Akila FPS)

The player is the **Akila FPS Framework** player (`AkilaFPSFrameworkPlayer`). Decision was made to use Akila for movement + guns (it has a full firearm/viewmodel/animation system). Vattalus frigate is used only as **environment/model**, not its demo simulation.

On the **player root**, these must be present:
- Akila's `FirstPersonController`, `CharacterInput`, `CharacterManager`, `CameraManager` (movement/camera — from the prefab)
- **`PlayerStats`** (survival health/oxygen/hunger/toxicity + death/respawn)
- **`Inventory`**
- **Tag = `Player`**
- Optional (for future Akila-gun enemies): **`AkilaDamageBridge`**

Camera: exactly **one** camera tagged **`MainCamera`** (Akila's main camera). Akila also uses an **overlay camera** for the weapon viewmodel — do NOT delete it. Exactly one `AudioListener` in the scene.

**Akila's `Damageable` component was removed** from the player (it needs Akila's `UIManager`/`DamageableEffectsVisualizer` singletons that aren't in the scene → spammed errors). Player damage now flows through `PlayerStats` instead. See `AkilaDamageBridge` for the future Akila-gun path.

---

## 4. Scripts inventory (all in `Assets/Scripts/`)

All the custom UI/HUD scripts are **code-generated and auto-bootstrap** (`[RuntimeInitializeOnLoadMethod]`) — they build themselves at runtime, need no editor wiring, and are **immune to asset imports overwriting scene UI** (which happened repeatedly).

### Interaction / inventory
- **`IInteractable.cs`** — interface: `GetDisplayName()`, `Interact(GameObject interactor)`.
- **`InteractableObject.cs`** — named non-pickup interactable (generator, etc.), implements `IInteractable`.
- **`ItemData.cs`** — ScriptableObject: `itemName`, `description`, `icon`, `isStackable`, `maxStackSize`, `type` (enum Resource/Consumable/Tool/Key), **`worldPrefab`** (spawned when dropped). Create via `Create ▸ Inventory ▸ Item`.
- **`Inventory.cs`** — in-memory model: `InventorySlot` (item+quantity), stacking, `AddItem`/`RemoveItem`/`GetCount`, `OnInventoryChanged` event, `maxSlots` (24).
- **`ItemPickup.cs`** — world pickup (`IInteractable`): references `ItemData`+quantity, adds to inventory + destroys self.
- **`SelectionManager.cs`** — crosshair raycast (`Camera.main`, center-screen, 3.5 m, ignores triggers). Handles BOTH `IInteractable` AND **`VattalusInteractable`** (ship doors). Interact key **E**. Auto-builds its own prompt UI if `interaction_Info_UI` not assigned. Goes on the player.
- **`InventoryUI.cs`** — simple text inventory panel (Tab toggle). Older/simpler; superseded by graphical one.
- **`GraphicalInventoryUI.cs`** — code-built grid inventory. **Tab** opens/closes + unlocks cursor; **click** a slot to select; **Q** drops selected item (spawns `worldPrefab` or a cube via `ItemPickup`). Auto-builds; needs an EventSystem (creates one if missing).

### Survival / HUD / death
- **`PlayerStats.cs`** — health/oxygen/saturation/toxicity, drains based on `isInsideShip`. `TakeDamage`, `Heal`, `RestoreOxygen/Saturation`, `ReduceToxicity`. **`Die()`** (fires `OnDied` event, auto-disables Akila `FirstPersonController`+`CharacterInput` by name, plus a `disableOnDeath` Behaviour[] list), **`Respawn()`** (restores stats, re-enables movement, optional `respawnPoint`), `IsDead`.
- **`SurvivalHUD.cs`** — auto-built 4-bar HUD (health/oxygen/food/toxicity), top-left, colour warns near danger. Reads `PlayerStats`.
- **`HUDDisplay.cs`** — older slider-based HUD (namespace `UI`); the SurvivalHUD replaces its role. May coexist.
- **`DeathScreen.cs`** — auto-built full-screen "YOU DIED" overlay with **RESPAWN** and **RETURN TO MAIN MENU** buttons. Listens to `PlayerStats.OnDied`. Main-menu button loads scene named `MainMenuScene` (const at top — must be in Build Settings).
- **`InsideShip.cs`** — trigger that sets `PlayerStats.isInsideShip` on enter/exit.

### Combat / enemies
- **`Enemies/EnemyStats.cs`** — ScriptableObject: maxHealth, moveSpeed, stoppingDistance, detectionRange, attackRange, attackDamage, attackCooldown. Create via `Create ▸ Enemies ▸ Enemy Stats`.
- **`Enemies/EnemyHealth.cs`** — health + `OnDied` event; `TakeDamage`.
- **`Enemies/EnemyController.cs`** — FSM (Patrol/Chase/Attack/Dead) on a `NavMeshAgent`. Chases player, attacks via `PlayerStats.TakeDamage`. Data-driven via `EnemyStats`. Animator params (`Speed`/`Attack`/`Dead`) set only if they exist (no error spam). Uses parent/child rig (agent on root, model as child with a facing offset).
- **`Weapon.cs`** — self-contained hitscan gun (fires from `Camera.main`). Left-mouse fire, R reload, ammo/reload/fire-rate. Damages `EnemyHealth`. Works with any controller. NOTE: does NOT render arms/viewmodel (Akila's guns do that).
- **`AkilaDamageBridge.cs`** — implements Akila's `IDamageable` and forwards `Damage()` to `PlayerStats.TakeDamage`. Add to player when future enemies use Akila firearms, so bullets hurt the player through one health value (no need for Akila's buggy `Damageable`). Has `using Akila.FPSFramework;` (needs Akila present).

### Crafting
- **`Crafting/CraftingRecipe.cs`** — ScriptableObject: `ItemStack` list of ingredients + results, `CanCraft(Inventory)`, `TryCraft(Inventory)`. Results is a list → also covers dematerialising. Create via `Create ▸ Crafting ▸ Recipe`.
- **`Crafting/CraftingStation.cs`** — world interactable (`IInteractable`) holding a recipe list; opens the crafting UI.
- **`Crafting/CraftingUI.cs`** — text crafting panel; number keys 1-9 craft, E closes. Attach to Canvas (not the panel).

### World / doors / spaceship
- **`DoorController.cs`** — interactable door (`IInteractable`), toggles an Animator `IsOpen` bool. Has `isLocked` + `Unlock()` for power-gated story doors.
- **`RandomSpawner.cs`** — spawns prefabs (enemies or items) at random NavMesh-snapped points around itself, max-alive cap, interval, gizmo radius. Reusable for both enemies and items.
- **`HyperspaceAutoStart.cs`** — put on the Vattalus `HyperspaceEffectController`; calls `StartTransitionIn()` on load so the hyperspace shell shows (for the main-menu background). Use the **`_URP`** hyperspace prefab.

### Not mine / leave alone
`Systems/*` (seed map, save profiles, random events, pause menu — Xiaoyan's), `Auth/*` (login/backend), `NetworkManager`, `CameraController`/`PlayerMovement` (old custom controller, unused now), `Editor/*`.

---

## 5. Systems status

| System | Status |
|---|---|
| First-person movement + guns | Working (Akila) |
| Survival stats + auto HUD | Working |
| Death screen + respawn/main-menu | Working (movement auto-freezes on death) |
| Pickups + interaction (own + ship doors) | Working |
| Graphical inventory (drop/click/cursor) | Working |
| FSM + NavMesh enemies | Built; needs enemy prefab placed + NavMesh baked |
| Weapon (hitscan) | Built; needs adding to player, hitMask set |
| Crafting (recipes/station/UI) | Built; needs recipe assets + station in scene |
| Random spawners | Built; needs prefabs assigned + NavMesh |
| Main-menu hyperspace background | Built; needs URP hyperspace prefab + frigate placed |
| Signal generator / win condition | **NOT built** — the missing core-loop keystone |
| Seeded map / save-load / auth | Xiaoyan's, working |

---

## 6. Unity wiring checklist (manual steps still needed)

- **Bake the NavMesh** (AI Navigation → `NavMesh Surface` → Bake). Required for enemies AND `RandomSpawner`. Re-bake after any scene/map merge.
- Player: `PlayerStats` + `Inventory` + Tag `Player` on the root; camera tagged `MainCamera`.
- Enemies: build `Enemy_Root` (NavMeshAgent + `EnemyController` + `EnemyHealth`, `EnemyStats` asset in both slots, model as child), make it a prefab, feed to a `RandomSpawner`.
- Crafting: create `ItemData` assets + `CraftingRecipe` assets, put `CraftingStation` on the dematerialiser, build a crafting panel + `CraftingUI` on the Canvas.
- Weapon: add `Weapon` to player, set `hitMask` to enemy+environment layers.
- Icons: assign `icon` sprite on each `ItemData` for the graphical inventory.
- Death screen main-menu button loads scene `MainMenuScene` (must be in Build Settings).

---

## 7. Known gotchas encountered

- **Vattalus demo controllers null-crash** with the Akila player — remove ALL `Vattalus*Controller`/`*Manager` demo scripts (`VattalusSceneController`, `VattalusSpaceshipController`, `VattalusInteriorEnvManager`, `VattalusRoomController`, orbit/first-person cameras, thrusters, engine sound, hyperspace demo UI). **Keep** `VattalusInteractable`, `VattalusDoorController`, post-process, lights, `VattalusOneShotSFX`. They're interdependent (a removed one nulls the next). The **ramp** is a `VattalusInteractable` — deploy it via `SelectionManager` (look + E) or set its `defaultState = true`; you do NOT need `VattalusSpaceshipController`.
- **Purple materials** = shader not URP. Vattalus/BetterCrystals: use the `_URP` prefabs; Vattalus frigate materials use a **custom Built-in-only shader** with no URP version (don't sink time into it — it's set dressing).
- **Player falls through floor** with Akila: check Layer Collision Matrix (player layer × floor layer) and spawn position; add a big Box Collider safety floor if needed.
- **Walking anim moves the whole robot** → it has root motion; make it in-place (remove root position curves / Bake Into Pose) and move via NavMesh.
- **Robot faces sideways** → rotate the child model's Y so its face aligns with the parent's +Z.
- **NavMesh doorway** → doors need the mesh to run through; put a `NavMeshObstacle (Carve)` on the moving door panel if you want it to block when closed.
- **Two interaction systems** → if `VattalusSceneController` is present it double-triggers doors with `SelectionManager`; remove it (SelectionManager now handles ship doors).

---

## 8. ⚠️ AI-usage policy (important for submission)

The user said Orbital's AI policy is **strict**. Much of the code in this session was AI-assisted/AI-written (Claude). The honest path: **read the actual policy** and either (a) disclose AI use if allowed, or (b) genuinely understand/rewrite the code so the user can explain and defend it. **Do NOT** scrub `Co-Authored-By` trailers to hide AI involvement — that's misrepresentation and doesn't actually help (the real signal is whether the user can explain the code in a viva). Suggest confirming the policy with the advisor.

---

## 9. Next steps (recommended priority)

1. **Verify the combined `bbd167cc` state compiles in Unity** and the scene loads clean.
2. **Push the combined branch** (§2 commands) so it's safe on GitHub. Then work on ONE branch, commit often.
3. **Crafting is the current focus.** The user switched from Earth ores to **BetterCrystals** (crystal materials). Turn the crystal names (§10) into `ItemData` assets + `CraftingRecipe` assets, place a `CraftingStation`, test the loop.
4. **Build the signal-generator + win/lose condition** — the missing keystone that makes it a finishable game (an interactable that checks inventory for crafted parts → win; game-over already exists via death screen).
5. Wire weapon→enemy and place enemy spawners so the damage → death → respawn loop is demoable.
6. README: add sections for enemy AI (FSM/NavMesh), interaction (`IInteractable`), crafting, data-driven `ScriptableObject`s (good SOLID evidence); update work log; add AI disclosure.

---

## 10. Fictitious crystal material names (crafting, replacing Earth ores)

Using BetterCrystals for the "ores". Consistent mineral suffixes; tied to the lore.

**Raw (mined):** Ashenite (grey, base/scrap), Aetherite (blue, power), Emberglass (red, heat), Verdenite (green, bio), Rimecrystal (cyan, cold), Gloamshard (purple/black, armour/tools).
**Rare (deep/enemy drops):** Omphalite (iridescent, endgame), Voidglass (black), Corelith (from robot cores, power), Ionvein (yellow, wiring).
**Bio/environmental:** Blightbloom (toxic), Condensate (water).

**Example recipes:** Oxygen Filter = Verdenite + Blightbloom + Ionvein; Thermal Wrap = Emberglass + Ashenite + Rimecrystal; Nutrient Paste = Verdenite + Condensate; Crystal Blade = Gloamshard + Ashenite; Signal Generator Core = Omphalite + Aetherite + Ionvein + Corelith.

---

## 11. Working-style notes for the assistant

- The user prefers to **run git commands themselves** — give exact commands to run rather than executing destructive git ops (they've interrupted commit/push tool calls to run them personally).
- The user is learning Unity/C# — explanations of *why* are valued.
- Prefer **code-generated, auto-bootstrapping UI** (import-proof) over editor-wired UI, since asset imports repeatedly wiped scene UI this session.
- Keep the user on ONE git branch; watch for detached HEAD.
