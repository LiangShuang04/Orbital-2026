# Seeded Events And Menu Checklist

## Goal

Make the game feel less like a static prototype by adding seeded world behaviour, visible event feedback, and a usable pause/settings menu.

## Checklist

- [x] Add seeded random event manager
- [x] Add random seed generation
- [x] Add manual seed support for testing
- [x] Add seeded toxic storm event
- [x] Add seeded robot patrol spawn event
- [x] Add seeded resource drop event
- [x] Add event HUD warning messages
- [x] Add `worldSeed` to backend save profile
- [x] Add backend validation for saving `worldSeed`
- [x] Add Unity save DTOs for save profile data
- [x] Add Unity save service for storing/loading the seed
- [x] Add pause menu controller
- [x] Add runtime-generated menu bar UI
- [x] Polish menu bar with sci-fi HUD styling
- [x] Add settings panel support
- [x] Save mouse sensitivity using `PlayerPrefs`
- [x] Save master volume using `PlayerPrefs`
- [x] Save fullscreen setting using `PlayerPrefs`
- [ ] Test in Unity Editor
- [ ] Connect toxic storm to real player toxicity/oxygen logic
- [ ] Replace placeholder event cubes with final robot/resource prefabs
- [ ] Connect menu sensitivity value to the actual camera controller
- [ ] Add modular pack assets into one polished map area

## Notes For GitHub Issue

Implemented the first proper seeded gameplay system. The seed now controls random event order, event timing, spawn point selection, and optional map decoration variation. Backend save data now stores `worldSeed`, so the same save can restore the same random behaviour later. Also added visible event warnings and a sci-fi styled pause/settings menu so the gameplay scene feels more complete.

## Testing Plan

1. Start Unity.
2. Add `GameSeedManager`, `RandomEventManager`, `RandomEventHud`, `SaveProfileService`, and `PauseMenuBootstrapper` to the gameplay scene.
3. Set `GameSeedManager` to manual seed `2026`.
4. Enter Play Mode and confirm the console logs the seed and event preview.
5. Stop and replay with seed `2026`.
6. Confirm the preview order stays the same.
7. Trigger random events and confirm HUD warnings appear.
8. Login through backend flow and call `SaveCurrentSeed`.
9. Confirm MongoDB save profile contains `worldSeed`.
10. Confirm the top HUD menu bar appears.
11. Press `ESC` or click `MENU`.
12. Test pause, resume, restart, main menu, quit, mouse sensitivity, volume, and fullscreen settings.
