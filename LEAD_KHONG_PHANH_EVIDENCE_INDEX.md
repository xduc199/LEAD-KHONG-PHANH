# EVIDENCE INDEX — LEAD KHÔNG PHANH

> Bảng này liên kết nội dung báo cáo đến file/class/method/field. Các line anchor lấy theo method/field name vì số dòng có thể thay đổi; nên cập nhật số dòng khi đóng băng source để nộp. “Scene evidence” xác nhận component serialization chứ không chứng minh runtime thành công.

| Nội dung báo cáo | File | Class / asset | Method / field / scene |
|---|---|---|---|
| Unity version/packages/build scene | `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`, `ProjectSettings/EditorBuildSettings.asset` | Unity project | `m_EditorVersion`, dependency versions, enabled scenes |
| Platform configuration | `ProjectSettings/ProjectSettings.asset`, `QualitySettings.asset` | PlayerSettings | Android/iPhone/Standalone config; not build proof |
| Entry/boot flow | `Assets/Scenes/Boot.unity`, `Assets/Scripts/UI/BOOT/BootController.cs` | BootController | Awake/start/boot logic; Boot scene serialization |
| Login/register | `Assets/Scripts/UI/LOGIN/LoginUIController.cs` | LoginUIController | email/password/register handlers; Firebase readiness checks |
| Google OAuth | `Assets/Scripts/Firebase/GoogleSignInManager.cs` | GoogleSignInManager | `StartGoogleLogin`, PKCE/browser/listener/token exchange; thread/listener path |
| Firebase dependency initialization | `Assets/Scripts/Firebase/FirebaseManager.cs` | FirebaseManager | Firebase dependency check/readiness |
| Auth state and UID | `Assets/Scripts/Firebase/FirebaseAuthManager.cs` | FirebaseAuthManager | InitializeAuth, OnAuthStateChanged, HandleAuthenticatedUser, HandleSignedOutUser |
| Firestore player document | `Assets/Scripts/Firebase/FirestorePlayerDataManager.cs`, `Assets/Scripts/Core/PlayerData.cs` | FirestorePlayerDataManager, PlayerData | `players/{uid}`, CreateOrLoad, mapping, save `SetAsync`/MergeAll; PlayerData fields |
| Logout-labelled UI path | `Assets/Scripts/UI/MAINMENU/MainMenuSettingsUI.cs`, `MainMenuController.cs` | MainMenuSettingsUI | `OnLogoutButtonClicked` invokes `MainMenuController.Quit()`; Quit calls Firebase Auth `SignOut()`, clears current data, then loads Login when configured; runtime pending |
| Run binder | `Assets/Scripts/Core/GameManagerPlayerBinder.cs`, `GameplaySession.cs` | GameManagerPlayerBinder | `TryBind`, GameplayScene check, `BeginNewRun` call |
| Score/run/death state | `Assets/Scripts/Core/GameManager.cs` | GameManager | `UpdateScore`, `BeginNewRun`, `BeginDeathPending`, `TryRevive`, `FinalizeGameOver`; run/death flags/events |
| Player input/movement | `Assets/Scripts/Player/PlayerController.cs` | PlayerController | `Update`, `FixedUpdate`, serialized speed/bounds/input fields |
| Runtime stat pipeline | `Assets/Scripts/Player/PlayerRuntimeStats.cs`, `EquipmentManager.cs` | PlayerRuntimeStats, EquipmentManager | stats rebuild and `OnStatsRebuilt`; player subscription in PlayerController |
| Collision/protection | `Assets/Scripts/Player/PlayerController.cs`, `Assets/Scripts/Pickups/ShieldController.cs`, `PhotonController.cs` | PlayerController, Shield/Photon controllers | `OnCollisionEnter`, `OnTriggerEnter`, protection checks/activation |
| Death/revive restoration | `Assets/Scripts/Player/PlayerController.cs`, `Assets/Scripts/Road/RoadManager.cs` | PlayerController, RoadManager | death pending/revive methods; `ResetWorldForRevive` |
| Road chunk shape | `Assets/Scripts/Road/RoadChunk.cs` | RoadChunk | Length, StartZ, EndZ |
| Road weighted selection/streaming | `Assets/Scripts/Road/RoadManager.cs`, `Assets/Scenes/GameplayScene.unity` | RoadManager | BiomeElement weights; SpawnRoad, EnsureForwardCoverage, despawn/cleanup, serialized lane/count/coverage fields |
| Traffic spawn/safety | `Assets/Scripts/Road/RoadManager.cs` | RoadManager | `SpawnTrafficForChunk`, candidate buffer/lane/Z/gap checks, traffic probabilities |
| Traffic movement | `Assets/Scripts/Traffic/TrafficVehicle.cs` | TrafficVehicle | Rigidbody setup, Update/FixedUpdate speed, knockback coroutine, destroy |
| Traffic AI decisions | `Assets/Scripts/Traffic/TrafficCarBehavior.cs` | TrafficCarBehavior | static `allTraffic`, Update, detect/ahead/safety, TryChangeLane, braking, cooldown, stuck recovery |
| Special vehicles | `Assets/Scripts/Enemy/Exciter*.cs`, `Ambulance*.cs`, `Alley*.cs` | special vehicle controllers/spawners | class methods exist; runtime validation pending |
| Coin collection/spawn | `Assets/Scripts/Pickups/Coin.cs`, `CoinSpawner.cs`, `GameManager.cs` | Coin, CoinSpawner, GameManager | collision/trigger callbacks, spawn calls, coin events/counter |
| Gum/Magnet/Shield/Photon | `Assets/Scripts/Pickups/{Gum,Magnet,Shield,Photon}*.cs`, `ItemManager.cs` | pickup/controllers/spawners/ItemManager | activation, trigger, effect methods; detailed duration not asserted |
| Gameplay HUD | `Assets/Scripts/UI/GAMEPLAY/GameplayItemHUD.cs`, `GameOverUIController.cs`, `GameplayHUDAnimator.cs` | UI controllers | subscriptions and display handlers; GameplayScene components |
| Pause/settings | `Assets/Scripts/UI/GAMEPLAY/PauseMenuController.cs`, `GameplaySettingsUI.cs` | UI controllers | pause/resume and local settings methods |
| Ramp/Heaven | `Assets/Scripts/Player/PlayerController.cs`, `Assets/Scripts/Road/RoadManager.cs`, `HeavenBiomeController.cs`, `CoinSpawner.cs` | PlayerController, RoadManager | RampState, surface/launch/Heaven branches, Heaven coverage/coin spawn |
| Item ID and definition | `Assets/Data/Items/GameItemData.cs`, `GameItemDatabase.cs`, `GameItemEnums.cs`, `Assets/GameData/Items/Definitions/*.asset` | ScriptableObjects | ID/category/price/currency/stats; IDs 1–100 found in data scan |
| Item visual mapping | `Assets/Data/Items/GameItemVisual.cs`, `GameItemVisualDatabase.cs`, `Assets/GameData/Items/Visuals/*.asset` | visual data | visual lookup and 100 visual assets |
| Inventory ownership | `Assets/Scripts/Core/InventoryOwnership.cs`, `Assets/Scripts/UI/Inventory/InventoryUI.cs` | InventoryOwnership, InventoryUI | ownership APIs/events, `EquipSelectedItem` UI path |
| Equipment/visuals | `Assets/Scripts/Core/EquipmentManager.cs`, `Assets/Scripts/Player/VisualBlinder/*.cs` | EquipmentManager/binders | `TryEquip`, equipped category IDs/events, prefab binding |
| Shop purchase | `Assets/Scripts/UI/MAINMENU/MainMenuShopUI.cs`, GameManager/Firestore manager | MainMenuShopUI | `TryBuySelectedItem`, `TryBuyWithGold`, gem branch; client-side balance/ownership path |
| Upgrade | `Assets/Scripts/Core/ItemUpgradeManager.cs`, `Assets/Data/ItemUpgrades/*.asset`, `Assets/Scripts/UI/ITEM/ItemUgradeUI.cs` | ItemUpgradeManager | `CanUpgrade`, cost/affordability, operation/events/data levels |
| Mission | `Assets/Scripts/Core/MissionManager.cs`, `Assets/Scripts/UI/MAINMENU/MissionUIController.cs`, PlayerData.cs | MissionManager | `ClaimMission`, event handlers, mission save fields |
| Progression | `Assets/Scripts/Core/PlayerProgression.cs`, PlayerData.cs | PlayerProgression | exp/level fields and lifecycle; formula/runtime details not asserted |
| Leaderboard read | `Assets/Scripts/Firebase/Leaderboard/LeaderboardManager.cs`, `LeaderboardEntry.cs`, UI files | LeaderboardManager | collection query, limit, loaded/failure events |
| Leaderboard write | `Assets/Scripts/Firebase/FirestorePlayerDataManager.cs` | FirestorePlayerDataManager | leaderboard document UID write methods |
| Lucky wheel | `Assets/Scripts/LuckyWheel/LuckyWheelManager.cs`, UI and reward data | LuckyWheelManager | TrySpinDaily/TrySpinVip and reward events; backend/runtime pending |
| Curve-world controller | `Assets/Effect/Shader/CurveWorld/CurveWorldController.cs` | CurveWorldController | serialized curve/slope settings, shader global properties |
| Curve algorithm | `Assets/Effect/Shader/CurveWorld/CurveWorldCommon.hlsl` | HLSL include | `CurveWorld_Evaluate`, `CurveWorld_EvaluateRigid`, X/Y mapping and preserved base Z |
| Curve shader use | `Assets/Effect/Shader/CurveWorld/CurveWorldLit_DEFORM_V8.shader`, `CurveWorldLit_RIGID_V8.shader` | shader programs | vertex/world position deformation; material usage needs scene/prefab validation |
| Shield/Nitro VFX | `Assets/VFX/*.shader`, `Assets/Scripts/VFX/*.cs`, VFX prefab folders | shaders/scripts/prefabs | visual assets and components present; exact active use partial |
| Audio | `Assets/Effect/Sound`, gameplay/item scripts | audio clips/components | file presence and direct AudioSource calls; no central AudioManager evidence |
| Blender source | `Assets/Model/*.blend` | Blender project files | 17 blend files; specific export/prefab mapping unknown |
| Figma | repository search | — | no Figma file/source found in repository |
| Editor tools | `Assets/Editor/**/*.cs` | setup/migration scripts | UI setup, item system, curve batch/restore, repair scripts; edit-time tooling, not runtime feature proof |
| Unity updater | `Assets/Scripts/Update/**/*.cs`, `Assets/Settings/Update/UpdateConfig.asset` | updater classes/config | manifest/provider/downloader/verifier/UI; release integration unknown |
| Separate delivery tools | `Tools/LeadLauncher`, `Tools/LeadUpdater`, `Tools/FirebaseConfigAudit` | .NET projects | sources and tests exist; not part of gameplay scene; integration/release status unknown |
| Object pooling/Jobs/DOTS/Burst | project-owned runtime scan | — | no pool lifecycle or gameplay ECS/Jobs/Burst evidence found |
| Runtime performance | no profiler capture in repo | — | FPS/GC/CPU/GPU not measured; explicitly UNKNOWN |

## Source inventory pointers

- Full project-owned C# path list: `LEAD_KHONG_PHANH_SOURCE_AUDIT.md` §3.
- Scene and prefab inventories: `LEAD_KHONG_PHANH_SOURCE_AUDIT.md` §§2, 4.
- Reality matrix: `LEAD_KHONG_PHANH_SOURCE_AUDIT.md` §18.
- Use Case IDs and pre/postconditions: `LEAD_KHONG_PHANH_USE_CASE.md` UC01–UC20.

