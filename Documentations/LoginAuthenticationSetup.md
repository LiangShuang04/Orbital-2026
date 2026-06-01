# Login Authentication Setup

## Current Repository Status

This checkout is a complete Unity project root.

A complete Unity project root should contain:

- `Assets`
- `Packages`
- `ProjectSettings`

Current status:

- `Assets`: present
- `Packages`: present
- `ProjectSettings`: present

Keep these Unity project folders under version control:

- `Packages/manifest.json`
- `Packages/packages-lock.json` if Unity generated it
- the full `ProjectSettings` folder

Do not push `Library`, `Temp`, `Obj`, `Build`, or `Logs`.

## Dependencies

The login screen uses standard Unity UI:

- `UnityEngine`
- `UnityEngine.UI`
- `UnityEngine.EventSystems`
- `UnityEngine.SceneManagement`
- `UnityEngine.Networking`

TextMeshPro is not used, so TextMeshPro Essentials are not required for this implementation.

In modern Unity versions, standard UI is provided by the built-in Unity UI package. If Unity reports missing `UnityEngine.UI` types, install or enable `com.unity.ugui` through Package Manager.

The runtime UI first tries clearer OS UI fonts such as Bahnschrift, Segoe UI, Arial, Verdana, Arial Black, Segoe UI Black, Bahnschrift SemiBold, and Segoe UI Semibold, then optional sci-fi display fonts such as Agency FB, Orbitron, Oxanium, and Audiowide. If those cannot be resolved, it falls back to Unity's `LegacyRuntime.ttf`. It does not use the obsolete built-in `Arial.ttf` path.

## Files

- `Assets/Scripts/Auth/AuthModels.cs`: login/register request models, auth response model, and session data.
- `Assets/Scripts/Auth/AuthApiClient.cs`: mock authentication by default, with configurable `Base Api Url`, `/auth/login`, and `/auth/register` endpoints for real backend integration later.
- `Assets/Scripts/Auth/AuthManager.cs`: persistent auth state, session storage, and login/register coordination.
- `Assets/Scripts/Auth/LoginPageController.cs`: UI input handling, validation, mode switching, loading state, error messages, and scene transition.
- `Assets/Scripts/Auth/LoginSceneBootstrapper.cs`: builds a complete dark sci-fi survival login screen at runtime, using generated Unity UI shapes and no external image assets.
- `Assets/Scripts/Auth/LoginSciFiAnimator.cs`: provides restrained ambient motion and button hover/press feedback for the generated login UI.

## Exact Unity Editor Setup

1. Open the complete Unity project root in Unity.
2. In the Project window, create a scene named `LoginScene`.
3. Open `LoginScene`.
4. In the Hierarchy, create an empty GameObject named `LoginSceneBootstrapper`.
5. Select the GameObject and attach `LoginSceneBootstrapper.cs`.
6. In the inspector, set `Target Scene Name` to the exact name of your main menu or game scene, for example `MainMenu` or `GameScene`.
7. Open `File > Build Settings`.
8. Click `Add Open Scenes` while `LoginScene` is open.
9. Open the target main menu/game scene and add it to Build Settings too.
10. Make sure both scenes are enabled in Build Settings.
11. Put `LoginScene` first if it should be the first screen in a build.
12. Open `LoginScene` again and press Play.

You do not need to manually create a Canvas, panel, input fields, buttons, decorative HUD objects, scanline, status meters, or EventSystem. `LoginSceneBootstrapper` creates them at runtime if no `LoginPageController` already exists.

The visual direction is a dark sci-fi survival login menu: layered star/nebula background, moon and distant colony skyline, apocalyptic foreground silhouettes, thick central armored login panel, warning ticks, bevels, corner brackets, scanline detail, left-side system status panel, right-side profile/lore/terminal panel, bottom sync strip, glowing cyan text, and high-contrast form fields. It does not depend on external art assets.

## Built-In Safeguards

- Missing Canvas: `LoginSceneBootstrapper` creates `LoginCanvas`.
- Missing EventSystem: `LoginSceneBootstrapper` creates an `EventSystem` with `StandaloneInputModule`.
- Missing UI objects: `LoginSceneBootstrapper` creates the full login panel, text, input fields, and buttons.
- Unclear/default font: `LoginSceneBootstrapper` first tries clear OS fonts such as Bahnschrift, Segoe UI, Arial, and Verdana, then falls back to `LegacyRuntime.ttf`.
- Invalid built-in font path: the implementation does not call Unity's obsolete built-in `Arial.ttf` path.
- Missing target scene name: login succeeds, then the UI shows a setup error instead of crashing.
- Target scene not added to Build Settings: login succeeds, then the UI shows that the scene is missing from Build Settings.

## Demo Authentication

Mock authentication is enabled by default:

- Any non-empty email/username is accepted.
- Password must be at least 6 characters.
- Register mode requires matching password confirmation.
- Successful login/register stores only token, user ID, username, and login status through `PlayerPrefs`.
- Raw passwords are not stored.

For backend integration, disable `Use Mock Authentication` on `AuthApiClient` and set `Base Api Url`. The expected response shape is:

```json
{
  "success": true,
  "token": "mock-or-real-jwt-token",
  "userId": "user-id",
  "username": "player-name",
  "errorMessage": ""
}
```

## Manual Test Checklist

- Press Login with empty fields and confirm `Email cannot be empty` appears.
- Enter an email/username and a password shorter than 6 characters and confirm validation appears.
- Switch to Register mode and confirm the confirm-password field appears.
- Enter mismatched passwords and confirm `Passwords do not match` appears.
- Enter a valid email/username and matching 6+ character password.
- Confirm buttons and input fields disable briefly while `Authenticating...` is shown.
- Confirm successful authentication loads the configured target scene.
- If the scene does not load, confirm the target scene name exactly matches a scene enabled in Build Settings.
