# LEAD KHÔNG PHANH — Technical Project Dossier

> Kiểm kê tĩnh workspace Unity hiện có; source không được sửa. IMPLEMENTED nghĩa là logic/asset code hiện diện, không bảo đảm chạy thành công trên mọi target. Tách giá trị code default khỏi scene serialization. Runtime, backend hay profiler chưa xác minh được sẽ ghi KHÔNG ĐỦ BẰNG CHỨNG.

## 1. Project Overview

- Unity endless runner chủ đề xe máy: player chạy theo +Z, điều khiển ngang, collision, coin/power-up, ramp/Heaven, traffic, revive, shop/inventory/equipment và Firebase persistence.
- Build Settings bật 5 scene: Boot, Login, MainMenu, GameplayScene, Loading.
- Workspace có tooling .NET riêng trong Tools (launcher/updater, Firebase config audit, tests), ngoài gameplay Unity.
- Nhiều assets nhập/sinh tự động: prefab, material curve-test, model, VFX, Firebase SDK. Không coi Editor/test/imported SDK là gameplay subsystem.
- Chưa xác minh build/Play Mode, backend data/rules, target deployment hay profiling.

## 2. Technology Stack

| Hạng mục | Bằng chứng |
|---|---|
| Unity | ProjectVersion.txt: 6000.0.82f1 |
| Rendering | URP 17.0.4; pipeline/render assets trong Assets/Settings |
| UI | Unity UI 2.0.0, TextMesh Pro |
| Input | Input System 1.19.0 và InputSystem_Actions.inputactions tồn tại; PlayerController dùng Input.GetAxisRaw (legacy Input Manager). Action asset có được dùng nơi khác chưa xác minh. |
| Firebase | Firebase Unity SDK trong Assets/Firebase và Assets/Plugins; code Auth/Firestore/Google OAuth/leaderboard trong Assets/Scripts/Firebase |
| Async/threading | Task/async cho Firebase/update; Google OAuth có HttpListener + Thread. Không thấy Unity Jobs/NativeArray/BurstCompile trong game scripts. |
| Audio/VFX | AudioSource/AudioClip, ParticleSystem, custom URP shaders; không thấy AudioManager trung tâm |
| Editor | setup/migration/repair scripts trong Assets/Editor |
| Desktop delivery | Tools/LeadLauncher, LeadUpdater, FirebaseConfigAudit và tests; shipped linkage chưa xác minh |

## 3. Project Structure

| Path | Nội dung |
|---|---|
| Assets/Scripts/Core | GameManager, gameplay session, PlayerData/progression, mission, inventory ownership, equipment/upgrades, camera |
| Assets/Scripts/Player | PlayerController, runtime stats, stat validator, lane smoke, visual binders |
| Assets/Scripts/Road | RoadManager, RoadChunk, Heaven biome, auto despawn |
| Assets/Scripts/Traffic, Enemy | Traffic physics/behavior/despawn; Exciter, Ambulance, alley ambush |
| Assets/Scripts/Pickups | Coin, Gum, Magnet, Shield, Photon, ItemManager, spawners/controllers |
| Assets/Scripts/UI | Boot/login/loading, HUD/pause/game-over/settings, menu/shop/inventory/garage, upgrade/gem |
| Assets/Scripts/Firebase | Firebase init/auth/Google sign-in/Firestore/leaderboard |
| Assets/Scripts/Update | manifest/provider/downloader/UI/health reporter |
| Assets/Data, Assets/GameData | item/visual/upgrade ScriptableObjects and stats |
| Assets/Effect/Shader/CurveWorld | curve controller, HLSL, rigid/deform/test shaders |
| Assets/Editor | editor setup/migration/repair/material tools |
| Assets/Scenes | 5 enabled scenes |
| Assets/Prefabs | 127 prefab files across Assets including nested/generated |
| Assets/StreamingAssets | google-services-desktop.json present |
| Tools | separate .NET launcher/updater/audit/test projects |

Project-owned scan found 158 C# files in Scripts/Data/GameData/Effect/Editor (excludes vendored SDK under Assets/Firebase). Inventory by family:

- Managers/coordinators: GameManager, GameManagerPlayerBinder, GameplaySession, RoadManager, MissionManager, PlayerProgression, EquipmentManager, ItemUpgradeManager, ItemManager, FirebaseManager, FirebaseAuthManager, FirestorePlayerDataManager, LeaderboardManager, LuckyWheelManager, UpdateManager.
- Gameplay components: PlayerController, PlayerRuntimeStats, PlayerStatPipelineValidator, PlayerLaneSmoke, TrafficVehicle, TrafficCarBehavior, ExciterController/Spawner, AmbulanceController/Spawner, AlleyBiomeSpawner, AlleyAmbushVehicle, RoadChunk, HeavenBiomeController, Coin/Gum/Magnet/Shield/Photon and controller/spawner classes, auto-despawn/cleanup.
- UI: BootController, LoadingScreenController, LoginUIController, MainMenuController, MainMenuSettingsUI, MainMenuShopUI, MissionUIController/MissionCardUI, CharacterProfileUI, InventoryUI/InventoryItemCard, MainMenuGarageUI, GarageShowcaseController/GaragePreviewRotator, MainMenuCharacterShowcase, GameplayIntroController, GameplayHUDAnimator, GameplayItemHUD, PauseMenuController, GameplaySettingsUI, GameOverUIController, GemHUD, ItemUpgradeUI, LuckyWheelUIController, LeaderboardUI/Row, GameUpdateUI, debug cheats.
- Data: PlayerData, MissionSaveData, MissionManager.Mission, LeaderboardEntry, LuckyWheelReward, GameItemData/Database, GameItemVisual/VisualDatabase, ItemStats/ItemStatModifier, ItemUpgradeData/ItemUpgradeLevel. Enums include item category/rarity/currency, upgrade/mission/camera/lucky wheel/update types and nested ramp/alley states.
- Visuals: CurveWorldController/TestSetup, HeavenBiomeSetup, Infinite2DBackground, FarBackgroundParallax, TimeframeExplosion, CowChunkWalker, Shield VFX and Nitro VFX components.
- Update namespace LEADKhongPhanh.Update: UpdateVersion/Manifest/Config, IUpdateProvider, JsonUpdateProvider, UpdateDownloader/PackageVerifier/Manager/HealthReporter, GameUpdateUI. Other explicit namespaces: LeadKhongPhanh.Environment, LeadKhongPhanh.EditorTools. Most gameplay/data types are global namespace.
- Editor scripts include BootSetup, CleanupMissingScripts, PersistentSystemsSetup, MainMenu/Login/CharacterProfile/Gameplay/Leaderboard/LuckyWheel/Item setup tools, CurveWorld batch/restore, PlayerVisualRootSetup and GameUpdateUISetup.
- Tests exist in Assets/Tests/Editor/Update and Tools/*Tests; not run.

GameplayScene serializes RoadManager and PlayerController. Full scene hierarchy, all nested prefab references and UnityEvent targets: KHÔNG ĐỦ BẰNG CHỨNG.

## 4. Software Architecture

- Component-based: MonoBehaviours, Unity lifecycle, serialized refs.
- Manager-based: cross-system managers; selected classes enforce duplicate guards/static Instance and persist. GameManager specifically destroys duplicate and uses DontDestroyOnLoad.
- Event/delegate use in run/data/equipment/mission/upgrade/UI flows, not universal.
- Data-driven partial: item/visual/upgrade SOs; gameplay also uses serialized/hardcoded values.
- Local state enums exist (ramp, alley, update); no app-wide FSM for Boot/Menu/Gameplay/Pause/Death.
- No evidence of MVC/MVVM/MVP, Clean Architecture, ECS/DOTS, DI container, repository abstraction or formal Service Locator.
- No pool lifecycle found in reviewed project-owned runtime code; not asserting absence in every vendor/import.
- Async Tasks exist. GoogleSignInManager starts a listener Thread; this does not establish multithreaded gameplay or thread safety. IUpdateProvider alone does not prove architecture-wide Strategy.

## 5. Gameplay Architecture

| System | Class | Input → output | Dependencies/runtime |
|---|---|---|---|
| Movement | PlayerController | axes/time/stats → X/Z/Y | Update + FixedUpdate |
| Lane | PlayerController | horizontal input → bounded continuous X | default bounds −4.5/+4.5 |
| Score/run | GameManager | Player Z → score=floor(Z), events | active run/player ref |
| Ramp/Heaven | PlayerController, RoadManager, CoinSpawner, HeavenBiomeController | ramp surface/markers → climb/launch/Heaven/coins | local RampState |
| Collision/death | PlayerController, GameManager | collision/item state → protection, knockback, death pending | shield/Photon/ramp/revive protections |
| Traffic | RoadManager, TrafficVehicle, TrafficCarBehavior | random prefab/lane/Z → movement/decisions | Update + Rigidbody FixedUpdate |
| Special vehicles | Exciter/Ambulance/Alley classes | spawn/proximity/collision → chase/ambush/impact | physics queries/colliders |
| Pickup | Coin/Gum/Magnet/Shield/Photon, ItemManager | trigger/collision → run economy/timed effect | player/controller/HUD |
| UI | UI controllers | events/buttons → panels/scenes/save | serialized refs + runtime lookup |
| Audio/VFX | gameplay/VFX components | impacts/status → audio/particles/materials | some Instantiate/Destroy |

GameManager flags include runActive, isDeathPending, isGameOver, runFinalized, revive count/timer. Death pending uses unscaled time; timeout finalizes. Missing player stops score update. Null dependencies often skip behavior/log.

## 6. Player Architecture

- PlayerController is large and multi-responsibility (line anchors extend to ~8133): input/movement, ramp/vertical, collision/pickup, death/revive, Shield and audio/VFX.
- Defaults: baseSpeed 15, maxSpeed 35, speedIncreaseRate .02, horizontalSpeed 12, GumHorizontalSpeed 30, minX −4.5, maxX +4.5. Scene/prefab can override.
- Explicit stats path: EquipmentManager → PlayerRuntimeStats → PlayerController; OnStatsRebuilt refreshes cached primitives.
- Player uses Input.GetAxisRaw and continuous lateral X bounds, not a proven discrete lane index. Traffic uses laneIndex.
- RampState: None, Climbing, Launching, Heaven, Airborne. Code discovers RampStart/RampEnd and surface collider, uses surface raycasts, ignore/restore collision and launch/Heaven timing. Heaven coins use CoinSpawner.
- Trigger/collision classify Gum/Photon/Shield/ramp/traffic/obstacles. Gum calls ItemManager.ActivateGum; Photon calls ActivatePhoton and removes pickup; Shield goes through TryConsumeShield.
- Shield can consume charge, apply cooldown/push/flash. Photon active speed/status and ramp-specific collision guards are present. Magnet classes exist but full attraction/duration not extracted.
- Death snapshots Rigidbody/collider states, enables death physics/knockback, calls GameManager.BeginDeathPending. Revive restores state and calls RoadManager.ResetWorldForRevive; PlayerController default revive invulnerability 2.5s.
- Revive cost = base + count×increase; code defaults 10 + 10 per use; decision window default 3s. Scene override not exhaustively captured.
- No Coffee class or independent Dodge/Slide class found; equivalent animation behavior is unknown.

Evidence anchors: PlayerController Update ~1103, FixedUpdate ~1161, OnTriggerEnter ~4756, OnCollisionEnter ~5106, CollectGum ~5603, death pending ~7609/~7666, RevivePlayer ~7858. GameManager UpdateScore ~712; GameOverUIController subscriptions ~210–242.

## 7. Endless Road Architecture

- GameplayScene RoadManager has weighted biome prefabs. Ground spawnZ and Heaven heavenSpawnZ are separate.
- Ground uses EnsureForwardCoverage and DespawnRoadsBehindPlayer; Heaven has separate coverage/cleanup. Road/traffic use Instantiate and Destroy.
- Code defaults chunkLength 180, numberOfChunks 3, despawnBehindPlayer 45. Scene values: chunkLength 90.2, chunks 3, despawn 100, initialRoadBehindPlayer 25, forwardCoverage 560.
- RoadChunk may supply endpoints/length; 90.2 is manager serialized/fallback value, not proof every mesh extent.
- ResetWorldForRevive destroys active traffic/road and regenerates initial coverage.
- RoadManager invokes traffic/coin spawners for ground chunks and separate Heaven road/coin methods. Ramp/obstacle placement may be authored inside road prefabs; procedural placement is unverified.

## 8. Procedural Generation

- Road biome prefab selection is weighted; UnityEngine.Random is used, no seed/replay identified.
- GameplayScene traffic count min=4/max=5 per chunk; balancedLaneSpawn=1; minimumTrafficGap=5 (code default 14). Scene overrides matter.
- Z candidates constrained by chunk start/end and spawnEdgePadding, then safety checks against active traffic. Balanced mode rotates a preferred lane and tries safe alternatives/random fallback.
- Traffic coordinate is (GetLaneX(lane), trafficSpawnY, candidateZ). code default trafficSpawnY=.8; active scene override not captured.
- Road lanes serialized −6.5/0/+6.5; chunkLength 90.2. Actual next Z may use RoadChunk endpoints; universal fixed Z delta cannot be asserted.
- RoadManager calls CoinSpawner.SpawnCoins(lane,length); PlayerController/Heaven use separate coin functions. Exact coin coordinates/spacing: KHÔNG ĐỦ BẰNG CHỨNG.
- No Addressables/pooling evidence.

## 9. Traffic & AI

- TrafficVehicle defaults moveSpeed 10, minimum 5, playerSpeedMargin 1; estimates player speed from Z delta, updates Rigidbody in FixedUpdate. Base/temporary speed and Photon/Ambulance knockback; knockback coroutine can Destroy vehicle.
- TrafficCarBehavior defaults lane X −5/0/+5, probability .85, decision distance 20, front/rear safety 18, cooldown 1.2, braking and stuck recovery values. Static List allTraffic updated OnEnable/OnDisable.
- Ahead/safe-lane/recovery searches scan allTraffic. Per query O(N); N traffic vehicles may produce O(N²) aggregate work per frame in worst branch pattern. Lane choice itself checks fixed three lanes.
- FindPlayer uses GameObject.FindGameObjectWithTag("Player") in helper checks; repeated lookups are potential cost.
- Lane change duration selected by GameObject name substrings (bus/bagac/motor/bike/car), not a formal vehicle type data model.
- Ambulance/Alley use overlaps/collider ignores/knockback and may disable/destroy traffic. No NavMesh/path planning identified.

## 10. Spawn & Object Pooling

- RoadManager instantiates road/traffic/Heaven road; AmbulanceSpawner/AlleyBiomeSpawner spawn special vehicles; collision creates VFX; visual binders/showcases instantiate previews.
- Destroy in streaming/reset, traffic knockback/despawn, pickups, collision, Alley/Ambulance, VFX and visual replacement.
- No UnityEngine.Pool/custom pool/prewarm/max size/release/reuse lifecycle found in project-owned runtime scan. Runtime road streaming still Instantiate/Destroy. Performance is not measured.

## 11. Item & Power-up System

| Item | Evidence | Limits |
|---|---|---|
| Coin | Coin/CoinSpawner, GameManager run coin state/event | exact persistent conversion not asserted |
| Gum | Player calls ItemManager.ActivateGum; Gum speed/horizontal state and HUD | duration/audio/VFX values not fully audited |
| Magnet | Magnet/Controller/Spawner and upgrade data | full attraction radius/effect unverified |
| Shield | Shield/Controller/Spawner, ShieldHex VFX, collision consumption | exact charges/duration asset coverage not fully checked |
| Photon | PhotonPowerUp/Controller/Spawner, PhotonCameraEffect; Player activation/speed/UI | exact duration not extracted |
| Coffee | no project-owned implementation found | NOT EVIDENCED |

Power-up state is controller/MonoBehaviour based; upgrades use SO/PlayerData. Not a single unified item definition pipeline.

## 12. Shop / Inventory / Equipment

- GameItemData SO fields: ID/name/category/rarity/icon, legacy prefab, visualId, price/currency, stats and metadata. GameItemDatabase builds Dictionary<int,GameItemData> and validates range/duplicates/category/missing/default outfit.
- Contract: IDs 1–100; Vehicle 1–40; Outfit 41–60; Cosmetic 61–80; Accessory 81–100. Enum integer values Vehicle=0, Outfit=1, Accessory=2, Cosmetic=3.
- Asset scan: 100 definitions, 100 unique IDs with no gaps; 100 GameItemVisual_### assets. Runtime database list and all prefab GUIDs not Unity-import validated.
- GameItemVisualDatabase/Visual assets separate lookups from item definitions. Visual binders subscribe to EquipmentManager and instantiate/replace equipped vehicle/outfit/cosmetic/accessory visuals.
- EquipmentManager defaults vehicle 1, outfit 41, accessory/cosmetic 0; load/save via Firestore; events notify UI/binders. InventoryOwnership handles owned IDs and emits change event; InventoryUI shows categories; Shop UI/card presents purchase.
- PlayerRuntimeStats rebuilds equipment/upgrade stats. ItemUpgradeManager handles Gum/Photon/Shield/Magnet SOs and Firestore; upgrade UI listens to events.
- GameItemData.prefab is explicitly legacy compatibility field; do not imply active runtime use. Purchase atomic/server validation is unknown.

## 13. Player Progression

PlayerProgression subscribes Firestore lifecycle; MissionManager listens to GameManager coin/score/gameover and Firestore. PlayerData contains level/exp/runs/currencies/best score, equipment/ownership/upgrades and mission fields. Exact XP curve, level cap, reward formulas and timezone/server authority: KHÔNG ĐỦ BẰNG CHỨNG. Upgrade/passive stats partly authored in SOs.

## 14. Game State

GameManager run is flag-based, not global enum FSM. GameManagerPlayerBinder checks GameplayScene, starts run if inactive and binds Player. GameManager persists across scenes and clears run state outside gameplay. GameplaySession static helper can reset/destroy players and restore timeScale. Ramp/alley/update have local states only. Boot/Login/MainMenu/Gameplay/Loading controllers exist; full scene navigation and pause path not exhaustively mapped.

## 15. UI Architecture

- Unity UI controllers with serialized refs/listeners and data reads. Editor setup scripts create/wire assets but do not prove each ref resolves.
- Main menu: settings/shop/missions/profile/showcase/garage/inventory. Gameplay: intro/HUD/item HUD/pause/settings/gameover/revive. Other: Boot/Login/Loading/leaderboard/lucky wheel/gem/upgrade.
- Data sources: GameManager events, Firestore CurrentPlayerData/events, EquipmentManager, InventoryOwnership, ItemUpgradeManager, SOs, PlayerPrefs settings.
- Examples: GameOverUI listens death/revive/gameover; GemHUD listens gems; InventoryUI equipment-ready/change; ItemUpgradeUI upgrade results; profile Firestore lifecycle; wheel ticket/reward; leaderboard loaded/failure.
- Runtime FindObjectsByType/GetComponentInChildren lookups exist. Complete navigation and UnityEvent targets: KHÔNG ĐỦ BẰNG CHỨNG.

## 16. Firebase / Firestore Architecture

1. FirebaseManager calls FirebaseApp.CheckAndFixDependenciesAsync and exposes readiness.
2. FirebaseAuthManager waits for FirebaseManager, initializes FirebaseAuth.DefaultInstance and coordinates user/player data readiness.
3. LoginUIController provides auth UI. GoogleSignInManager uses browser OAuth authorization code + PKCE, local HttpListener callback/thread, exchanges code and signs in using Firebase credential.
4. FirestorePlayerDataManager uses FirebaseFirestore.DefaultInstance. Player path players/{uid}; CreateOrLoad gets snapshot, converts/creates PlayerData, normalizes/migrates legacy equipment and emits lifecycle actions. Main save uses SetAsync MergeAll; specialized methods update currencies/progression/missions/equipment/ownership/upgrades/best score.
5. LeaderboardManager reads leaderboard (default max 100); Firestore manager writes leaderboard docs by UID and timestamp fields.
6. Progression/missions/ownership/equipment/upgrades/UI consume cached PlayerData and events.

PlayerData includes uid/displayName/level/exp/totalRuns/coins/gems/bestScore, legacy selectedBike/ownedBikes, ownedItemIds, equipped vehicle/outfit/accessory/cosmetic IDs, four upgrade levels, mission date/selection/daily counters and MissionSaveData{id,progress,claimed}.

Offline persistence, rules/indexes/Cloud Functions, server authority, conflict policy, latency and production setup are KHÔNG ĐỦ BẰNG CHỨNG. StreamingAssets/google-services-desktop.json exists; values omitted.

## 17. Camera / Audio / VFX

CameraController has CameraViewMode; PhotonCameraEffect exists. Active follow/FOV/profile is unverified. Audio is directly triggered by components; no AudioManager found. GameplaySettingsUI uses PlayerPrefs toggles. VFX includes Shield Hex, Nitro flame/heat distortion, lane smoke, impact/explosion particles, Photon camera effect. Some use MaterialPropertyBlock; others Instantiate/Destroy. Full prefab usage/lifecycle not audited.

## 18. Rendering / Visual Curve

- CurveWorldController sets global shader params; CurveWorldCommon.hlsl is shared by rigid/deform V8 shaders; test shader/materials and Editor tools also present.
- Controller defaults: curveStart 120, radius 900, direction +1, max angle 16°, strength 1, range override 850, slopeStart 70. Active scene overrides not exhaustively checked.
- HLSL d = worldPosition.z − referenceZ − curveStart. If d≤0 return original. Otherwise t=saturate(d/max(range,.001)); smootherstep=t³(t(6t−15)+10). X offset = maxXOffset×direction×strength×smootherstep; add to X. Hill profile adds Y rise/hold/fall scaled by slopeHeight×verticalStrength. Base evaluator does not modify Z.
- Rigid path evaluates pivot, yaw=atan(xSlope), rotates local XZ vertex offset about pivot; deform path evaluates each vertex and adjusts normals/tangents. Rendering coordinates can change, but shared base map preserves Z.
- No VisualCurveObject C# class found. Shader deformation is visual, not automatic physics/Transform movement. All road/traffic/coin/item prefab material coverage unknown; no “unified active curve” claim.

## 19. Data Flow

- Scene/prefab → RoadManager → RoadChunk Instantiate → traffic + CoinSpawner.
- PlayerController ↔ PlayerRuntimeStats ← EquipmentManager ← Firestore PlayerData.
- Input/collision → ItemManager/Photon/Shield; Z/death → GameManager → HUD/GameOver UI.
- FirebaseApp → Auth → FirestorePlayerDataManager → players/{uid} → progression/mission/ownership/equipment/UI listeners.
- Firestore leaderboard → LeaderboardManager → LeaderboardUI.
- PlayerPrefs → local settings.
  
This represents observed links, not one synchronous pipeline.

## 20. Event Flow

| Publisher | Event | Subscribers / purpose |
|---|---|---|
| GameManager | score/coin changes | MissionManager/HUD refresh |
| GameManager | death pending/revive/gameover | GameOverUI; PlayerController handles revive |
| GameManager | gem/gold changed | GemHUD/account UI |
| Firestore manager | manager ready | EquipmentManager, ItemUpgradeManager |
| Firestore manager | data loaded/cleared/saved | progression, missions, ownership, equipment, profile |
| EquipmentManager | ready/changed/item equipped | InventoryUI, visual binders, garage |
| PlayerRuntimeStats | OnStatsRebuilt | PlayerController recaches |
| InventoryOwnership | OnOwnershipChanged | inventory UI |
| ItemUpgradeManager | upgrade/currency/result/readiness | ItemUpgradeUI/runtime consumers |
| LuckyWheelManager | ticket/reward | LuckyWheelUIController |
| LeaderboardManager | loaded/failure | LeaderboardUI |

Flows: start = scene load → binder → BeginNewRun/bind → GameManager scores Z. Death = collision/protection → death physics → pending event → UI; revive validates timer/Gems, restores player/world; otherwise finalizes. Load = Firebase deps → auth/user → Firestore get/create/normalize → listeners. Equip = UI validation → equipped ID save → event → visual binder/stat rebuild. Upgrade = UI → async manager validation/cost/save → events. Save is distributed among specialized Firestore calls; full caller graph per field not exhausted.

## 21. UML Model Specification

- Actors: Player, Firebase Auth/Firestore, Google OAuth/browser, OS/update host.
- Systems: Unity scenes/gameplay, inventory/equipment, Firebase data, leaderboard, update client, Editor tools, separate desktop launcher/updater.
- Core links: GameManager↔binder↔PlayerController; PlayerController↔RoadManager/CoinSpawner/ItemManager/Photon/Shield/RuntimeStats; RoadManager↔RoadChunk/TrafficVehicle/TrafficCarBehavior/prefabs; FirebaseManager→Auth→Firestore→PlayerData; EquipmentManager→GameItemDatabase→definitions/visual binders; RuntimeStats→PlayerController.
- Relationships: component composition, serialized references, static Instance, events, SO refs, Instantiate/Destroy, Firestore document mapping. No SQL/FK schema.
- Use cases: sign in/load; start/steer/ramp/collect; revive/finalize/save; missions/leaderboard; purchase/equip/upgrade; settings/update.
- State diagrams should show GameManager flags, local RampState, Alley state, UpdateManager state separately; no global FSM.
- Entities: PlayerData, MissionSaveData, mission progress, item definition/visual mapping, upgrade data, leaderboard entry, lucky reward. Firestore paths players/{uid}, leaderboard/{uid}.
- Deployment topology beyond Unity client, Firebase, Google OAuth browser and separate launcher/updater is unknown.

## 22. Performance Analysis

Static only; no profiler or benchmark.

| Site | Rating | Basis |
|---|---|---|
| Traffic allTraffic scans | HIGH RISK as N grows | O(N) checks per vehicle; aggregate potentially O(N²) |
| FindGameObjectWithTag | POTENTIAL COST | repeated player lookups in traffic helpers |
| Road spawn safety | POTENTIAL COST | candidate positions × active traffic scans/retries |
| Instantiate/Destroy roads/traffic/VFX | POTENTIAL COST | stream/impact paths, no pool evidence |
| Ambulance/Alley overlap/ComputePenetration/FindObjects/GetComponents arrays | HIGH RISK during collision bursts | engine queries and possible arrays |
| Cached player stats/collider snapshots | SAFE relative to hot path by code shape | lifecycle caching exists |
| Particle lifetime GetComponentsInChildren | POTENTIAL COST, event path | impact handling |
| Road candidate buffer, MaterialPropertyBlock reuse | SAFE by code shape | reusable storage/object |
| LINQ/closures/boxing/Camera.main across all code | UNKNOWN | no exhaustive allocation profiler audit |
| GC/zero alloc/thread safety | UNKNOWN | no profiler evidence |

## 23. Algorithm Complexity

| Algorithm | Time | Space | Basis |
|---|---|---|---|
| Traffic ahead/safety scan | O(N) per query, up to O(N²) aggregate | O(1) query | static list scan |
| Three lane candidate selection | O(N) w.r.t traffic | O(1) | fixed three candidates, each scans traffic |
| Road candidate checks | roughly O(P×N) + retries | O(P), reusable buffer | P traffic positions and active-list checks |
| Stream cleanup | O(R+T) | O(1) beyond lists | road/traffic iteration |
| Initial road generation | O(C×(P×N + asset work)) | O(C+N) refs | C chunks; GameplayScene C=3 |
| Item database build | O(I+100); lookup expected O(1) | O(I) | list validation + dictionary |
| Category enumeration | O(I) | O(1) | scans database, max contract 100 |
| Ownership calls | UNKNOWN | stored IDs | set/hash lookup not established globally |
| Firestore | network dominated; conversion proportional to model | model/document size | remote latency not benchmarked |
| Overlap collision | physics query + O(H) handling | O(H) results | H returned colliders; Unity internals not inferred |
| UI refresh | varies, generally item/card count | varies | all loops not inspected |

## 24. Technical Issues Found

| Finding | Evidence | Impact | Severity |
|---|---|---|---|
| Candidate lane mismatch | Scene road −6.5/0/+6.5; traffic defaults −5/0/+5; player bounds −4.5/+4.5; prefab overrides incomplete | possible lane offset; may be intentional | MEDIUM candidate |
| Traffic search scaling | allTraffic scans and repeated Player tag search | CPU/search cost as population grows | MEDIUM potential |
| Spawn/destruction churn | RoadManager/spawners/VFX/binders | possible spikes, unmeasured | MEDIUM potential |
| PlayerController responsibility breadth | movement/ramp/items/collision/death/audio combined | coupling observation only | INFO |
| Firebase authority unknown | client writes, backend rules not inspected | cannot establish security/economy guarantees | UNKNOWN |
| OAuth callback thread state | listener Thread/coroutine share callback state; no explicit synchronization identified in inspected code | race/visibility risk | MEDIUM potential |
| Legacy/new data coexist | prefab/selectedBike and migration fields | requires real data validation | LOW/MEDIUM |
| Visual vs gameplay coordinates | shader bends rendered vertices; does not move physics | could be misdescribed in report | INFO |
| Debug cheats exist | Inventory/Gem/Upgrade cheat components | build/scene activation unknown | LOW |
| Desktop Firebase config packaged | StreamingAssets JSON | config included in client files | INFO |

No source edits or fixes.

## 25. Project Reality Matrix

| Feature | Evidence | Status | Confidence |
|---|---|---|---|
| Multi-scene client | 5 enabled scenes | IMPLEMENTED | HIGH |
| Player movement/lane | PlayerController + GameplayScene | IMPLEMENTED | HIGH |
| Independent Jump/Slide/Dodge | no named separate system; ramp jump exists | UNKNOWN/MISSING as separate systems | MEDIUM |
| Ramp/Heaven | RampState/RoadManager/coin links | IMPLEMENTED | HIGH |
| Road streaming/weighted biome | RoadManager + scene refs | IMPLEMENTED | HIGH |
| Traffic spawn/AI | RoadManager + traffic scripts | IMPLEMENTED | HIGH |
| Exciter/Ambulance/Alley | controllers/spawners exist | PARTIALLY IMPLEMENTED pending runtime | MEDIUM |
| Death/revive | PlayerController/GameManager/UI | IMPLEMENTED | HIGH |
| Coin/Gum/Magnet/Shield/Photon | scripts/controllers/spawners/HUD | PARTIALLY IMPLEMENTED at audit depth | MEDIUM |
| Coffee | none found | MISSING/NOT EVIDENCED | MEDIUM |
| Global FSM | flags/local enums only | MISSING | HIGH |
| Item IDs 1–100 | 100 assets unique/no gaps | IMPLEMENTED | HIGH |
| Inventory/equipment/visual binders | code/SOs/Firestore | IMPLEMENTED; mappings unvalidated | MEDIUM |
| Shop transaction | UI/code, server atomicity unknown | PARTIALLY IMPLEMENTED/UNKNOWN | MEDIUM |
| Upgrades | four SOs, manager/UI/Firestore fields | IMPLEMENTED | HIGH |
| Missions/progression | classes/events/data | PARTIALLY IMPLEMENTED; formula/runtime unknown | MEDIUM |
| Firebase Auth/Firestore/leaderboard | client source/SDK | IMPLEMENTED in client | HIGH |
| Offline data | no proof | UNKNOWN | LOW |
| Object pooling | none in reviewed runtime scan | MISSING/NOT EVIDENCED | MEDIUM |
| Curve rendering | shared HLSL/shaders/controller | IMPLEMENTED; usage coverage unknown | HIGH/MEDIUM |
| AudioManager | no class found | MISSING/NOT EVIDENCED | MEDIUM |
| Updater | scripts/tools exist; live release linkage unknown | PARTIALLY IMPLEMENTED | MEDIUM |
| Multiplayer | packages but no networking gameplay identified | REFERENCED/UNKNOWN | MEDIUM |
| DOTS/Jobs/Burst | no evidence | MISSING/NOT EVIDENCED | HIGH |

## 26. Confirmed Features

Five enabled scenes; Unity 6/URP/UI/TMP; Player movement/ramp/collision/death/revive; persistent GameManager/run binding; ground/Heaven stream and weighted road prefabs; traffic spawn/lane AI; complete item IDs 1–100; Firebase Auth/Firestore client and leaderboard code; inventory/equipment/upgrades/missions UI/data; curve HLSL X/Y vertex transform with base Z unchanged; runtime Instantiate/Destroy.

## 27. Partially Implemented Features

Special vehicle controllers/spawners without runtime validation; power-up class set without complete per-item timing/VFX audit; shop persistence without server transaction proof; progression/missions without formula/runtime validation; updater code without production endpoint/launcher link proof; curve variants without exhaustive prefab material mapping.

## 28. Unknown / Unverified Features

Build/Play Mode/platform support; all prefab refs/UnityEvents; exact coin/item spawn positions and every duration; Firestore rules/offline/server authority; active camera/FOV/audio routing; profiler/frame/GC/device performance; complete UI routes and XP formulas; update release endpoints; multiplayer. Package presence alone does not prove network gameplay.

## 29. Evidence Index

| Conclusion | File/method anchors |
|---|---|
| Unity/version/packages/build scenes | ProjectSettings/ProjectVersion.txt; Packages/manifest.json; ProjectSettings/EditorBuildSettings.asset |
| GameManager lifecycle/score/death | Assets/Scripts/Core/GameManager.cs: Awake/Update, UpdateScore ~712, death/revive/finalize; event declarations ~324–339 |
| Player bind/session | Assets/Scripts/Core/GameManagerPlayerBinder.cs TryBind; GameplaySession.cs EndGameplay |
| Player flow | Assets/Scripts/Player/PlayerController.cs Update ~1103, FixedUpdate ~1161, trigger ~4756, collision ~5106, Gum ~5603, pending ~7609/~7666, RevivePlayer ~7858 |
| Runtime stats | Assets/Scripts/Player/PlayerRuntimeStats.cs; PlayerController subscription ~931 |
| Road generation/config | Assets/Scripts/Road/RoadManager.cs serialized fields ~137–252, coverage ~2048, spawn ~2186 onward |
| Active road values | Assets/Scenes/GameplayScene.unity RoadManager block ~14018–14077 |
| Traffic | TrafficVehicle.cs Update/FixedUpdate ~108–168; TrafficCarBehavior.cs list ~194, Update ~289, searches ~1303, safety/recovery ~795–1873 |
| Item contract/assets | Assets/Data/Items/GameItemData.cs, GameItemDatabase.cs, GameItemEnums.cs; Assets/GameData/Items/Definitions and Visuals |
| Equipment/visual/stats | Assets/Scripts/Core/EquipmentManager.cs; Assets/Scripts/Player/VisualBlinder/*; PlayerRuntimeStats.cs |
| Firebase paths/model | Assets/Scripts/Core/PlayerData.cs; FirestorePlayerDataManager.cs players path ~252, save ~504, MergeAll ~578–590, leaderboard ~1114 |
| Auth/OAuth | FirebaseManager.cs, FirebaseAuthManager.cs, GoogleSignInManager.cs, LoginUIController.cs; SDK/config folders |
| Event examples | MissionManager ~894–921; GameOverUIController ~210–242; InventoryUI ~2059–2102; ItemUgradeUI ~398–441; LuckyWheelUIController ~151–164 |
| Curve formulas | Assets/Effect/Shader/CurveWorld/CurveWorldCommon.hlsl Evaluate ~255+, EvaluateRigid ~358+; CurveWorldController and rigid/deform shaders |
| VFX/editor/tools | Assets/Scripts/VFX/*; Assets/Editor/**/*.cs; Tools/LeadLauncher and Tools/LeadUpdater |

## 30. Final Technical Summary

PROJECT CONFIDENCE: MEDIUM

TOTAL SYSTEMS IDENTIFIED: 16 grouped families (application/scenes, player, run manager, road, traffic, special vehicles, pickups, economy, progression/missions, item database, inventory/equipment, Firebase, leaderboard, UI, rendering/VFX/audio, updater/tooling).

CORE SYSTEMS: PlayerController + PlayerRuntimeStats; GameManager + binder; RoadManager + RoadChunk; TrafficVehicle + TrafficCarBehavior; Firestore/Auth + PlayerData; GameItemDatabase + Equipment/Inventory + visual binders.

IMPORTANT FINDINGS: GameplayScene road lanes −6.5/0/+6.5; PlayerController defaults bounds −4.5/+4.5 and TrafficCarBehavior defaults −5/0/+5. Scene chunk length 90.2, forward coverage 560, traffic 4–5/chunk and gap 5. No pooling found in reviewed runtime scripts. Traffic scans static list. Curve is render-side, not game logic Z transform.

CRITICAL INCONSISTENCIES: none confirmed CRITICAL. Medium-confidence lane-coordinate candidate needs prefab/collider validation. Item IDs 1–100 are unique/gap-free in scanned assets.

UNVERIFIED AREAS: Play Mode/build; every prefab reference/UnityEvent; exact coin/item coordinates and durations; Firebase rules/offline/server authority; camera setup; profiler; production update linkage.

REPORT-READY: YES for source-backed system/design description with caveats; NO for runtime/profiler/backend claims.

### Final validation

- [x] Scanned project tree and major Unity/config folders.
- [x] Enumerated source groups, scenes, shaders, SO groups, Firebase and external tools.
- [x] Read core sections and traced representative calls/events/data paths.
- [x] Cross-checked RoadManager vs GameplayScene and IDs vs item assets.
- [x] Inspected curve HLSL/shader/controller.
- [x] Scanned instantiate/destroy/pooling/jobs/Burst/thread markers.
- [ ] Exhaustively documented every method in 158 owned C# files, every prefab link and every UnityEvent; marked unverified rather than inferred.
- [ ] Build/Play Mode/backend/Profiler (not performed).

