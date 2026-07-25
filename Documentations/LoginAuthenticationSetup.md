# Login Authentication Setup

The `Login` scene uses the imported sci-fi interface as its visual shell. `PhpLoginBridge` replaces the package's PHP behaviours at runtime and connects the form to the Express API.

## Game Flow

The release flow is:

```text
Login
  -> MainGameplayScene for the aircraft introduction and tutorial
  -> Demo_Combat for the final playable map, enemies, Warden-K, and Signal Generator defence
```

After login, Unity retrieves the authenticated player's save profile:

- A new account or missing save starts in `MainGameplayScene` with `ACT1_WAKE`.
- An early aircraft/tutorial save resumes in `MainGameplayScene`.
- A save that has reached the ruins, robot encounter, or later objectives resumes in `Demo_Combat`.

`MainMenuScene` remains available for manual testing but is not part of the authenticated release flow.

## Main Scripts

- `Assets/Scripts/Auth/PhpLoginBridge.cs`
- `Assets/Scripts/Auth/AuthenticatedGameFlow.cs`
- `Assets/Scripts/NetworkManager.cs`
- `Assets/Scripts/Systems/SaveProfileService.cs`
- `Assets/Scripts/Systems/SaveProfileModels.cs`

## Authentication

Unity calls:

- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`

The backend writes the account to MongoDB and returns a JWT. Unity keeps the JWT in memory and sends it on save requests:

```text
Authorization: Bearer <token>
```

The Unity client never connects to MongoDB directly and must not contain Atlas credentials.

## Save Routing

After authentication, Unity calls:

- `GET /api/v1/save`
- `POST /api/v1/save` when the account has no save profile
- `PUT /api/v1/save` for seed and narrative progress updates

The save profile supplies `worldSeed` and `objectiveState`. `AuthenticatedGameFlow` uses those fields to select the resume scene.

## Login UI

The runtime bridge:

- uses Liberation Sans SDF for readable text
- increases input, status, and button text sizes
- applies high-contrast input and button colours
- disables controls while a request is running
- shows backend validation and connection failures in the scene
- supports Enter and Tab keyboard navigation

## Backend Local Run

Create `.env` in the repository root:

```env
PORT=5000
MONGODB_URI=mongodb+srv://...
JWT_SECRET=replace-this-with-a-long-secret
```

Run:

```bash
npm install
npm run dev
```

Check:

```text
GET http://127.0.0.1:5000/api/v1/health
GET http://127.0.0.1:5000/api-docs
```

## Build Settings

Required enabled scenes:

1. `Assets/Scenes/Login.unity`
2. `Assets/Scenes/MainGameplayScene.unity`
3. `Assets/Scenes/Demo_Combat.unity`

`MainMenuScene` can stay enabled for its standalone tests. `Central` and `Central_Combat` are not part of the release flow.

## Manual Test

1. Start the Express backend.
2. Open `Assets/Scenes/Login.unity`.
3. Press Play and register a new account.
4. Confirm the account receives a save profile and loads `MainGameplayScene`.
5. Complete or advance the aircraft tutorial until the ruins transition.
6. Confirm the game loads `Demo_Combat`.
7. Exit, log in again, and confirm the saved objective resumes in the correct scene.
8. Confirm the Console has no authentication, scene-loading, missing-script, or duplicate AudioListener errors.
