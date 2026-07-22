# Login Authentication Setup

The current login flow uses the sci-fi PHPLogin scene as the visual shell, then `PhpLoginBridge` takes over the buttons and input fields at runtime.

## Scene

Primary login scene:

- `Assets/Scenes/Login.unity`

Target scene after a successful mock login:

- `Assets/Scenes/MainMenuScene.unity`

The original imported package scene is left alone. Our scene copy is the one wired into the project.

## Main Scripts

- `Assets/Scripts/Auth/PhpLoginBridge.cs`
- `Assets/Scripts/Auth/AuthManager.cs`
- `Assets/Scripts/Auth/AuthApiClient.cs`
- `Assets/Scripts/Auth/AuthModels.cs`
- `Assets/Scripts/Auth/LoginPageController.cs`
- `Assets/Scripts/Auth/AuthUIController.cs`
- `Assets/Scripts/NetworkManager.cs`

`PhpLoginBridge` is the practical runtime bridge for the current UI scene. The older generated-login scripts are kept because they are still useful fallback/reference code, but they are not the main login scene path right now.

## Runtime Behaviour

Mock auth is enabled by default so the Unity scene can be tested without a deployed server.

Mock mode accepts:

- non-empty username/email
- non-empty password
- matching register confirmation fields
- email-looking register input

The bridge disables the imported PHP package's original auth behaviours and replaces their button events with our flow.

## Backend Integration

For the Express backend, use:

- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`

The backend returns a JWT. Unity stores the token in memory through `NetworkManager` and attaches it to future save requests as:

```text
Authorization: Bearer <token>
```

Do not put MongoDB credentials inside Unity. Unity should only talk to backend endpoints over HTTP/HTTPS.

## Backend Local Run

Create `.env` in the repo root:

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

Recommended scene order:

1. `Assets/Scenes/Login.unity`
2. `Assets/Scenes/MainMenuScene.unity`
3. `Assets/Scenes/MainGameplayScene.unity`
4. `Assets/Scenes/Central_Combat.unity`
5. `Assets/Scenes/Demo_Combat.unity`

## Test Checklist

- Open `Assets/Scenes/Login.unity`
- Press Play
- Try login with blank fields and confirm the UI blocks it
- Try register with mismatched confirmation fields
- Try a valid mock login and confirm it enters `MainMenuScene`
- Start the backend and test `/api/v1/health`
- If testing real auth, disable mock auth on the login bridge and set the server URL
- Confirm JWT auth works before testing save/load
