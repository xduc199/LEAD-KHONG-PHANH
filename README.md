# LEAD KHÔNG PHANH

> **A Vietnamese arcade traffic runner where every gap in the road is a decision.**

[![Unity](https://img.shields.io/badge/Unity-6000.0.82f1-black?logo=unity)](ProjectSettings/ProjectVersion.txt)
[![C%23](https://img.shields.io/badge/C%23-Unity%20scripting-239120?logo=csharp)](Assets/Scripts)
[![URP](https://img.shields.io/badge/Render%20Pipeline-URP-5C2D91)](Packages/manifest.json)

**LEAD KHÔNG PHANH** is a student-developed 3D arcade traffic runner built with Unity 6 (Editor `6000.0.82f1`). Steer through Vietnamese street traffic, collect coins, and keep a run alive for as long as you can. The project is under active development; runtime and build status have not been independently verified for this README.

## About the game

The premise is simple: ride through chaotic traffic, dodge vehicles, and chase a longer run and a higher score. The codebase includes lane-based player movement, streamed road sections, traffic that can change lanes, pickups, and a ramp-to-“Heaven” route. The road may curve on screen; your next gap in traffic still needs your attention.

## Gameplay features

### Implemented in source

- **Lane-based riding and collision handling** — `PlayerController` reads horizontal input, moves the player, and handles collisions, protection, death, and revive interactions.
- **Endless road streaming** — `RoadManager` creates and removes road chunks, selects road elements with weights, and fills forward coverage.
- **Traffic and rule-based driving behavior** — traffic is spawned for road chunks; `TrafficVehicle` manages vehicle motion while `TrafficCarBehavior` checks nearby traffic and can change lanes or recover when stuck.
- **Run score and session state** — `GameManager` tracks run state, score, death-pending, revive, and game-over events.
- **Coins and pickup framework** — coin collection and Gum, Magnet, Shield, and Photon code and scene components are present. Individual effect completeness and runtime behavior have not all been validated.
- **Ramp and Heaven route** — player and road code includes ramp launch and Heaven biome handling.
- **Menus and gameplay UI** — build scenes include boot, login, main menu, gameplay, and loading; gameplay scripts provide HUD, pause/settings, game-over, and revive flows.
- **Firebase client integration** — Firebase dependency initialization, authentication, Firestore player data, and leaderboard client code are present. A working Firebase backend is not verified here.

### Partial or not runtime-verified

Inventory, equipment visuals, item upgrades, shop, missions, progression, lucky wheel, special traffic, and Firebase-backed flows have code and/or data assets. Their end-to-end wiring, persistence, transaction safety, and backend behavior have not been confirmed in a running build. Treat them as in development rather than as a verified release feature set.

## Screenshots

No gameplay screenshots or recordings suitable for a README were identified. **TODO:** add a real gameplay screenshot or short capture under a repository media folder and link it here.

## Tech stack

- Unity Editor `6000.0.82f1` (Unity 6)
- C# gameplay and editor scripts
- Universal Render Pipeline (URP) `17.0.4`
- Unity Input System package `1.19.0`; player movement currently reads the legacy `Horizontal` Input Manager axis. Both input backends are enabled in Project Settings.
- Firebase Unity SDK assets for Firebase App, Authentication, and Cloud Firestore
- Unity UI (uGUI), TextMesh Pro, and Unity Test Framework packages

## Project architecture

```text
Assets/
├── Scenes/                 # Boot, Login, MainMenu, GameplayScene, Loading
├── Scripts/
│   ├── Core/               # GameManager, run state, player data, progression/inventory
│   ├── Player/             # Player movement, stats, visual binders
│   ├── Road/               # Road chunks, streaming, biome/ramp logic
│   ├── Traffic/            # Traffic movement and lane-change behavior
│   ├── Enemy/              # Special vehicle controllers and spawners
│   ├── Pickups/            # Coins and power-up controllers/spawners
│   ├── UI/                 # Boot, login, menus, gameplay and loading UI
│   ├── Firebase/           # Auth, Firestore player data, leaderboard clients
│   └── LuckyWheel/, VFX/, Update/ # Supporting gameplay and tools
├── Data/, GameData/        # ScriptableObject definitions and item/game data
├── Prefabs/, Model/        # Prefabs and source/imported models
├── Materials/, UI/, VFX/   # Rendering, interface, and visual effects assets
├── Effect/                 # Shader, audio, and other effect assets
├── Tests/                  # Unity editor tests, including updater-related tests
└── Editor/                 # Editor setup and asset wiring utilities
Packages/                   # Unity package manifest and lockfile
ProjectSettings/            # Unity version, scenes, input, quality and player settings
Tools/                      # Separate .NET launcher/updater utilities and tests
```

`GameManagerPlayerBinder` connects the gameplay scene to `GameManager`; `RoadManager` streams chunks and spawns traffic; player stats can be rebuilt from equipment data. The Firebase clients connect authentication state to Firestore player records and leaderboard queries. These are code-level relationships, not confirmation that all scene references or remote services work at runtime.

## Getting started

1. Install [Git LFS](https://git-lfs.com/) and initialize it once:

   ```bash
   git lfs install
   ```

2. Clone the repository and download LFS-managed assets:

   ```bash
   git clone https://github.com/xduc199/LEAD-KHONG-PHANH.git
   cd LEAD-KHONG-PHANH
   git lfs pull
   ```

3. Install Unity Editor **6000.0.82f1**. Open the cloned project directory in Unity Hub and allow Unity to import assets and resolve packages.
4. The enabled scene list in `ProjectSettings/EditorBuildSettings.asset` starts with `Assets/Scenes/Boot.unity`. Open that scene and enter Play Mode to follow the configured entry flow.
5. Firebase client assets and a desktop Firebase configuration file are present. Authentication and Firestore features require an accessible Firebase project with appropriate client/provider configuration and backend rules. The repository does not include verified Firebase security rules or a documented production setup; remote-service operation is not guaranteed by cloning alone.

The project uses Git LFS for binary asset patterns, including models, images, audio, and selected Firebase binaries. A clone without LFS objects may contain pointer files instead of usable assets.

## Controls

- **Steer:** Left/Right Arrow or A/D through Unity's configured `Horizontal` axis.
- **Pause/resume:** Escape (handled by the gameplay pause controller).

These describe code/configured keyboard input. Touch controls and controller bindings were not verified. `R` is a serialized debug revive key in `GameManager`, not presented as a normal player control.

## Build

`ProjectSettings` contains Android, iOS, and Standalone player settings, and the five scenes are enabled in Build Settings. This indicates project configuration, not a successful platform build or tested release. No verified build artifact or runtime validation is documented here.

To create a local build, open **File → Build Profiles** (or the equivalent Build Settings interface in Unity 6), select a configured target, review its player settings and scene list, then build. Platform SDKs, signing credentials, and Firebase provider/backend setup may be required depending on the chosen target. No target should be considered release-ready based on settings alone.

## Development status

**In active development.** The core run, road streaming, traffic, collision, and run-state systems are present in project code. Items, progression, inventory/shop, missions, special traffic, and online flows have partial implementations whose full runtime behavior is unverified. No public release, performance benchmark, or successful platform build is claimed.


## Team

A student team is developing the project. Public member names and role assignments were not found in the repository materials reviewed for this README.

## Contributing

Contributions should use a focused branch and pull request. Before submitting, check affected scenes and prefab references in Unity, confirm required packages, and keep `.gitignore` and `.gitattributes`/Git LFS rules in place. Do not commit generated Unity folders such as `Library`, `Temp`, `Obj`, `Logs`, or local `UserSettings`.

## License

No repository-level license was found. The project license has not been specified; please contact the repository maintainers before redistributing or reusing project content. Licenses included with third-party assets apply to those assets and do not establish a license for the project as a whole.
