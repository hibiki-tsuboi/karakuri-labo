# Repository Guidelines

## Project Structure & Module Organization

KarakuriLabo is a Unity **6000.6.4f1** URP starter for an iPhone physics puzzle. Read `spec.md` for requirements; its proposed `docs/SPEC.md` location does not yet exist.

- `Assets/Scenes/SampleScene.unity`: current scene and enabled build scene.
- `Assets/Settings/`: rendering assets; `Assets/InputSystem_Actions.inputactions`: input configuration.
- `Assets/TutorialInfo/`: template documentation and editor scripts, not gameplay code.
- `Packages/manifest.json` and `packages-lock.json`: dependencies; `ProjectSettings/`: project configuration.
- Add gameplay code under `Assets/Scripts/{Game,Physics,Placement,UI,Stage}/` and reusable parts under `Assets/Prefabs/`, following the specification as features arrive.

## Build, Test, and Development Commands

Use the editor version recorded in `ProjectSettings/ProjectVersion.txt`. From the repository root on macOS:

```bash
UNITY_EDITOR="/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/MacOS/Unity"
"$UNITY_EDITOR" -projectPath "$PWD"
```

Open `SampleScene` and press Play for local checks. For iOS builds, install iOS Build Support, select iOS in **File > Build Profiles**, and export to `Builds/iOS/` for Xcode. No custom build script exists.

Once test assemblies exist, close this project's editor before running batch tests:

```bash
"$UNITY_EDITOR" -batchmode -projectPath "$PWD" -runTests -testPlatform EditMode -testResults /tmp/karakuri-editmode.xml
```

Use `-testPlatform PlayMode` and a separate results path for runtime tests.

## Coding Style & Naming Conventions

Follow existing C# style: four-space indentation, braces on separate lines, PascalCase types/methods, and camelCase fields/locals. Match component filenames to class names, such as `BallController.cs`. Keep editor-only scripts in `Editor/` folders and separate physics, placement, and UI responsibilities. No formatter or linter is configured.

## Testing Guidelines

Unity Test Framework 1.8.0 is installed; no project tests or coverage threshold exist yet. Add NUnit test assemblies under `Assets/Tests/EditMode/` and `Assets/Tests/PlayMode/`, with `*Tests.cs` filenames. Run them through **Window > General > Test Runner** or the command above. Prioritize goal detection, state transitions, and reset restoration as implemented. Check Console errors and manually verify touch controls and landscape safe areas.

## Commit & Pull Request Guidelines

This checkout has no Git metadata or PR template, so historical conventions are unavailable. Use concise imperative commit subjects, e.g., `Add ball goal detection`. PRs should describe scope, link relevant issues or specification phases, record validation, and include screenshots for visual changes.

## Asset and Agent Workflow

Preserve asset `.meta` files. Exclude generated `Library/`, `Temp/`, `Logs/`, `Obj/`, `Build/`, and `Builds/` from version control. Inspect the current scene before changes; prefer Unity MCP when available. Follow specification phases without advancing scope automatically.
