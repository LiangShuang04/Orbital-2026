# Login Front End

Mode: Offline / Mock by default.

Entry scene: `Assets/Scenes/Login.unity`

Target scene after a successful login: `MainMenuScene`

The scene is copied from the imported PHPLogin demo scene, so the original package scene stays untouched. `PhpLoginBridge` takes over the scene at runtime and wires the login/register buttons to the game flow.

Build order:

1. `Assets/Scenes/Login.unity`
2. `Assets/Scenes/MainMenuScene.unity`

The previous `Assets/Scenes/LoginScene.unity` is still in the project but disabled from Build Settings.

Mock mode accepts any non-empty username and password. Register mode validates username, password confirmation, and email shape locally, then returns to login.

To switch to real PHP later:

1. Select the `PHPLoginBridge` object in the Login scene after opening it in Unity.
2. Turn off `Use Mock Auth`.
3. Set `Server Base Url` to the HTTPS URL where the package PHP files are hosted.
4. Set `Shared Api Key` only if the server scripts require one.
5. Host the PHP files from `Assets/UI_SCIFI_PHP/PHPFiles/`.
6. Create the MySQL tables required by the package PHP files.

Do not put database credentials in Unity. The Unity client should only talk to PHP endpoints over HTTPS. Password hashing must happen server-side before storing credentials.
