# LEAD KHÔNG PHANH — SOURCE AUDIT

> Phạm vi: kiểm kê tĩnh repository tại ngày 07/10/2026. Audit chỉ khẳng định những gì thể hiện trong file code, serialization Unity, asset và cấu hình. Không chạy Play Mode/build/profiler hay truy cập Firebase project. Khi chưa có bằng chứng runtime/backend, nội dung được ghi là PARTIAL, UNKNOWN hoặc NOT_FOUND.

## 1. Project inventory

- Repository là project Unity; phiên bản Editor ghi trong `ProjectSettings/ProjectVersion.txt`: **6000.0.82f1**.
- Thư mục chính hiện có: `.git`, `.vscode`, `Assets`, `Builds`, `DefaultCompany`, `GeneratedAssets`, `Library`, `Logs`, `Packages`, `ProjectSettings`, `Temp`, `Tools`, `TutorialInfo`, `UserSettings`.
- Nội dung nguồn game: `Assets/Scripts`, `Assets/Data`, `Assets/GameData`, `Assets/Effect`, `Assets/Editor`, `Assets/Scenes`, `Assets/Prefabs`, `Assets/Settings`, `Assets/Model`, `Assets/VFX`, `Assets/Plugins`, `Assets/Firebase`, `Assets/StreamingAssets`.
- `Packages/manifest.json`: URP 17.0.4, Input System 1.19.0, UGUI 2.0.0, AI Navigation 2.0.14, Test Framework 1.6.0, Timeline 1.8.12; cùng các module Unity. Package có mặt không chứng minh gameplay sử dụng.
- `ProjectSettings/ProjectSettings.asset` có cấu hình Android và iPhone, min Android SDK 23, đồng thời Standalone quality entry. Đây là khai báo cấu hình, **không phải** bằng chứng build chạy thành công/mobile đã được kiểm thử.
- `ProjectSettings/EditorBuildSettings.asset` bật 5 scene: Boot, Login, MainMenu, GameplayScene, Loading. Build order là Boot → Login → MainMenu → GameplayScene → Loading; code có thể load scene theo yêu cầu.
- Inventory đếm tĩnh: 5 `.unity`, 85 `.prefab` dưới Assets, 234 `.asset` gồm 100 định nghĩa item + 100 visual item; có 17 `.blend`, 22 `.fbx`, 22 `.shader` (cộng shadergraph/cginc/hlsl riêng), 21 audio `.mp3`, cùng các loại texture/material khác. Con số đếm theo file extension, bao gồm asset nhập khẩu và test.
- Có `LEAD_KHONG_PHANH_TECHNICAL_PROJECT_DOSSIER.md` từ trước. Nội dung đó chỉ được dùng làm checklist điều tra, không thay thế việc đối chiếu source hiện tại. `Tools` còn chứa các project .NET riêng (LeadLauncher, LeadUpdater, FirebaseConfigAudit và tests), không phải gameplay Unity; liên kết với bản phát hành hiện tại chưa được xác nhận.
- File `.blend` có trong Assets/Model nên chứng minh repository chứa nguồn Blender cho một số model. Không xác minh được ứng dụng nào thực sự tạo mọi model/mesh đã nhập.

## 2. Scene inventory

| Scene | Script components có trong scene YAML | Vai trò xác định từ scripts |
|---|---|---|
| `Assets/Scenes/Boot.unity` | BootController, FirebaseManager, FirebaseAuthManager, FirestorePlayerDataManager, LeaderboardManager, ItemUpgradeManager | Khởi tạo/đợi hệ thống nền và điều phối tiếp tục theo BootController. Thứ tự điều hướng runtime cần xác minh Play Mode. |
| `Assets/Scenes/Login.unity` | LoginUIController, GoogleSignInManager | UI đăng nhập; có email/password/register và luồng Google OAuth trong script. |
| `Assets/Scenes/MainMenu.unity` | GameManager, EquipmentManager, MissionManager, PlayerProgression, LuckyWheelManager/UI, Inventory, shop, garage, profile, leaderboard UI, update UI và các UI khác | Khu vực menu/meta-game. Có cheat/debug components serialized; trạng thái active/build release chưa xác minh. |
| `Assets/Scenes/GameplayScene.unity` | PlayerController, RoadManager, GameManagerPlayerBinder, HUD/pause/settings, Coin/Gum/Magnet/Photon/Shield spawners/controllers, visual binders, camera, Exciter/Ambulance spawners | Scene gameplay và các thành phần chính được gắn trực tiếp. Các nested prefab reference không được kết luận chỉ từ tên. |
| `Assets/Scenes/Loading.unity` | LoadingScreenController | Đọc target scene từ PlayerPrefs và tải scene theo controller. |

Đây là script component inventory giải từ `m_Script` GUID ↔ `.meta` trong YAML; chưa phải bản đồ đầy đủ GameObject hierarchy/UnityEvent cho mọi object.

## 3. Script inventory

Các file runtime/editor do project sở hữu được tổ chức ở `Assets/Scripts/{Core,Player,Road,Traffic,Enemy,Pickups,Firebase,UI,LuckyWheel,Update,VFX}`, `Assets/Data`, `Assets/GameData`, `Assets/Effect`, `Assets/Editor`. Có C# Firebase SDK vendored dưới `Assets/Firebase` và editor/test code; không gộp SDK vào gameplay inventory. Danh sách đầy đủ path của C# dưới `Assets/Scripts`, `Assets/Editor`, `Assets/Data`, `Assets/GameData`, `Assets/Effect` nằm cuối phần này (auto-inventory từ repository).

Nhóm class/runtime chính:

- Core/session/data: `GameManager`, `GameManagerPlayerBinder`, `GameplaySession`, `PlayerData`, `PlayerProgression`, `MissionManager`, `InventoryOwnership`, `EquipmentManager`, `ItemUpgradeManager`, `CameraController`.
- Player/visuals: `PlayerController`, `PlayerRuntimeStats`, `PlayerStatPipelineValidator`, `PlayerLaneSmoke`, visual category binders cho vehicle/outfit/cosmetic/accessory.
- Road/world: `RoadManager`, `RoadChunk`, `HeavenBiomeController`, `AutoDespawnItem`.
- Traffic/enemy: `TrafficVehicle`, `TrafficCarBehavior`, auto-despawn/launch cleanup, Exciter/Ambulance/Alley spawners và controllers.
- Pickup: coin, gum, magnet, shield, photon, spawners/controllers, `ItemManager`, `PhotonCameraEffect`.
- Firebase: `FirebaseManager`, `FirebaseAuthManager`, `GoogleSignInManager`, `FirestorePlayerDataManager`, leaderboard manager/entry/UI.
- UI/meta: Boot/Login/Loading, gameplay HUD/pause/gameover/settings, MainMenu/shop/garage/inventory/profile/missions, upgrade/gem, lucky wheel.
- Effect/editor/update: CurveWorld, parallax/Heaven setup, shield/nitro VFX, update client, Editor setup/migration/repair scripts.

### Full source paths

- `Assets/Scripts/Core/CameraController.cs`
- `Assets/Scripts/Core/EquipmentManager.cs`
- `Assets/Scripts/Core/GameManager.cs`
- `Assets/Scripts/Core/GameManagerPlayerBinder.cs`
- `Assets/Scripts/Core/GameplaySession.cs`
- `Assets/Scripts/Core/InventoryOwnership.cs`
- `Assets/Scripts/Core/ItemUpgradeManager.cs`
- `Assets/Scripts/Core/MissionManager.cs`
- `Assets/Scripts/Core/PlayerData.cs`
- `Assets/Scripts/Core/PlayerProgression.cs`
- `Assets/Scripts/Enemy/AlleyAmbushVehicle.cs`
- `Assets/Scripts/Enemy/AlleyBiomeSpawner.cs`
- `Assets/Scripts/Enemy/AmbulanceController.cs`
- `Assets/Scripts/Enemy/AmbulanceSpawner.cs`
- `Assets/Scripts/Enemy/ExciterController.cs`
- `Assets/Scripts/Enemy/ExciterSpawner.cs`
- `Assets/Scripts/Firebase/FirebaseAuthManager.cs`
- `Assets/Scripts/Firebase/FirebaseManager.cs`
- `Assets/Scripts/Firebase/FirestorePlayerDataManager.cs`
- `Assets/Scripts/Firebase/GoogleSignInManager.cs`
- `Assets/Scripts/Firebase/Leaderboard/LeaderboardEntry.cs`
- `Assets/Scripts/Firebase/Leaderboard/LeaderboardManager.cs`
- `Assets/Scripts/Firebase/Leaderboard/LeaderboardRowUI.cs`
- `Assets/Scripts/Firebase/Leaderboard/LeaderboardUI.cs`
- `Assets/Scripts/LuckyWheel/DailyMissionRewardBridge.cs`
- `Assets/Scripts/LuckyWheel/LuckyWheelManager.cs`
- `Assets/Scripts/LuckyWheel/LuckyWheelReward.cs`
- `Assets/Scripts/LuckyWheel/LuckyWheelUIController.cs`
- `Assets/Scripts/Pickups/Coin.cs`
- `Assets/Scripts/Pickups/CoinSpawner.cs`
- `Assets/Scripts/Pickups/Gum.cs`
- `Assets/Scripts/Pickups/GumController.cs`
- `Assets/Scripts/Pickups/GumSpawner.cs`
- `Assets/Scripts/Pickups/ItemManager.cs`
- `Assets/Scripts/Pickups/Magnet.cs`
- `Assets/Scripts/Pickups/MagnetController.cs`
- `Assets/Scripts/Pickups/MagnetSpawner.cs`
- `Assets/Scripts/Pickups/PhotonCameraEffect.cs`
- `Assets/Scripts/Pickups/PhotonController.cs`
- `Assets/Scripts/Pickups/PhotonPowerUp.cs`
- `Assets/Scripts/Pickups/PhotonSpawner.cs`
- `Assets/Scripts/Pickups/Shield.cs`
- `Assets/Scripts/Pickups/ShieldController.cs`
- `Assets/Scripts/Pickups/ShieldSpawner.cs`
- `Assets/Scripts/Player/PlayerController.cs`
- `Assets/Scripts/Player/PlayerLaneSmoke.cs`
- `Assets/Scripts/Player/PlayerRuntimeStats.cs`
- `Assets/Scripts/Player/PlayerStatPipelineValidator.cs`
- `Assets/Scripts/Player/VisualBlinder/PlayerAccessoryVisualBinder.cs`
- `Assets/Scripts/Player/VisualBlinder/PlayerCategoryVisualBinderBase.cs`
- `Assets/Scripts/Player/VisualBlinder/PlayerCosmeticVisualBinder.cs`
- `Assets/Scripts/Player/VisualBlinder/PlayerOutfitVisualBinder.cs`
- `Assets/Scripts/Player/VisualBlinder/PlayerVehicleVisualBinder.cs`
- `Assets/Scripts/Player/VisualBlinder/PlayerVisualManager.cs`
- `Assets/Scripts/Road/AutoDespawnItem.cs`
- `Assets/Scripts/Road/HeavenBiomeController.cs`
- `Assets/Scripts/Road/RoadChunk.cs`
- `Assets/Scripts/Road/RoadManager.cs`
- `Assets/Scripts/Traffic/AutoDespawnTraffic.cs`
- `Assets/Scripts/Traffic/LaunchCleanup.cs`
- `Assets/Scripts/Traffic/TrafficCarBehavior.cs`
- `Assets/Scripts/Traffic/TrafficVehicle.cs`
- `Assets/Scripts/UI/BOOT/BootController.cs`
- `Assets/Scripts/UI/GAMEPLAY/GameOverUIController.cs`
- `Assets/Scripts/UI/GAMEPLAY/GameplayHUDAnimator.cs`
- `Assets/Scripts/UI/GAMEPLAY/GameplayIntroController.cs`
- `Assets/Scripts/UI/GAMEPLAY/GameplayItemHUD.cs`
- `Assets/Scripts/UI/GAMEPLAY/GameplaySettingsUI.cs`
- `Assets/Scripts/UI/GAMEPLAY/PauseMenuController.cs`
- `Assets/Scripts/UI/Garage/GaragePreviewRotator.cs`
- `Assets/Scripts/UI/Garage/GarageShowcaseController.cs`
- `Assets/Scripts/UI/Garage/MainMenuGarageUI.cs`
- `Assets/Scripts/UI/Inventory/InventoryDebugCheat.cs`
- `Assets/Scripts/UI/Inventory/InventoryItemCard.cs`
- `Assets/Scripts/UI/Inventory/InventoryUI.cs`
- `Assets/Scripts/UI/ITEM/GemDebugCheat.cs`
- `Assets/Scripts/UI/ITEM/GemHUD.cs`
- `Assets/Scripts/UI/ITEM/ItemUgradeUI.cs`
- `Assets/Scripts/UI/ITEM/ItemUpgradeDebugCheat.cs`
- `Assets/Scripts/UI/LOADING/LoadingScreenController.cs`
- `Assets/Scripts/UI/LOGIN/LoginUIController.cs`
- `Assets/Scripts/UI/MAINMENU/CharacterProfileUI.cs`
- `Assets/Scripts/UI/MAINMENU/MainMenuCharacterShowcase.cs`
- `Assets/Scripts/UI/MAINMENU/MainMenuController.cs`
- `Assets/Scripts/UI/MAINMENU/MainMenuSettingsUI.cs`
- `Assets/Scripts/UI/MAINMENU/MainMenuShopUI.cs`
- `Assets/Scripts/UI/MAINMENU/MissionCardUI.cs`
- `Assets/Scripts/UI/MAINMENU/MissionUIController.cs`
- `Assets/Scripts/UI/MAINMENU/ShopItemCard.cs`
- `Assets/Scripts/Update/GameUpdateUI.cs`
- `Assets/Scripts/Update/IUpdateProvider.cs`
- `Assets/Scripts/Update/JsonUpdateProvider.cs`
- `Assets/Scripts/Update/UpdateConfig.cs`
- `Assets/Scripts/Update/UpdateDownloader.cs`
- `Assets/Scripts/Update/UpdateHealthReporter.cs`
- `Assets/Scripts/Update/UpdateManager.cs`
- `Assets/Scripts/Update/UpdateManifest.cs`
- `Assets/Scripts/Update/UpdatePackageVerifier.cs`
- `Assets/Scripts/Update/UpdateVersion.cs`
- `Assets/Scripts/VFX/ExhaustNitroVFX.cs`
- `Assets/Scripts/VFX/Nitroflamelayer.cs`
- `Assets/Scripts/VFX/Nitrolightflicker.cs`
- `Assets/Scripts/VFX/NitroVfxModule.cs`
- `Assets/Scripts/VFX/Shieldhexdome.cs`
- `Assets/Scripts/VFX/Shieldhexvfx.cs`
- `Assets/Scripts/VFX/Shieldhitpoint.cs`
- `Assets/Editor/CurveWorldBatchSetup.cs`
- `Assets/Editor/CurveWorldHardRestore.cs`
- `Assets/Editor/CurveWorldRoadBatchSetup.cs`
- `Assets/Editor/GameUpdateUISetup.cs`
- `Assets/Editor/PlayerVisualRootSetup.cs`
- `Assets/Editor/BOOT/BootSetup.cs`
- `Assets/Editor/BOOT/CleanupMissingScripts.cs`
- `Assets/Editor/Character/CharacterProfileMenuUISetup.cs`
- `Assets/Editor/Character/CharacterProfileUISetup.cs`
- `Assets/Editor/GAMEPLAY/GameOverUISetup.cs`
- `Assets/Editor/GAMEPLAY/GameplayHUDVisualSetup.cs`
- `Assets/Editor/GAMEPLAY/GameplayItemHUDSetup.cs`
- `Assets/Editor/GAMEPLAY/GameplaySettingsUISetup.cs`
- `Assets/Editor/GAMEPLAY/PlayerLaneSmokeSetup.cs`
- `Assets/Editor/ITEM/ExhaustNitroVFXSetup.cs`
- `Assets/Editor/ITEM/GemHUDSetup.cs`
- `Assets/Editor/ITEM/ItemSystemSetup.cs`
- `Assets/Editor/ITEM/ItemUpgradeDataGenerator.cs`
- `Assets/Editor/ITEM/ItemUpgradeUISetup.cs`
- `Assets/Editor/ITEM/ItemVisualMigrationTool.cs`
- `Assets/Editor/ITEM/MagnetVFXSetup.cs`
- `Assets/Editor/ITEM/Shieldhexvfxsetup.cs`
- `Assets/Editor/Leaderboard/LeaderboardButtonSetup.cs`
- `Assets/Editor/Leaderboard/LeaderboardUISetup.cs`
- `Assets/Editor/LOGIN/LoginPasswordToggleSetup.cs`
- `Assets/Editor/LOGIN/LoginUISetup.cs`
- `Assets/Editor/LUCKWHEEL/LuckyWheelPanelSetup.cs`
- `Assets/Editor/MAINMENU/MainMenuGarageUISetup.cs`
- `Assets/Editor/MAINMENU/MainMenuInventoryUISetup.cs`
- `Assets/Editor/MAINMENU/MainMenuMissionButtonSetup.cs`
- `Assets/Editor/MAINMENU/MainMenuScreenSetup.cs`
- `Assets/Editor/MAINMENU/MainMenuSettingsUISetup.cs`
- `Assets/Editor/MAINMENU/MainMenuShopUISetup.cs`
- `Assets/Editor/MAINMENU/MainMenuUIInteractionSetup.cs`
- `Assets/Editor/MAINMENU/MissionMenuVisualSetup.cs`
- `Assets/Editor/MAINMENU/PersistentSystemsSetup.cs`
- `Assets/Data/Items/GameItemData.cs`
- `Assets/Data/Items/GameItemDatabase.cs`
- `Assets/Data/Items/GameItemEnums.cs`
- `Assets/Data/Items/GameItemVisual.cs`
- `Assets/Data/Items/GameItemVisualDatabase.cs`
- `Assets/Data/Items/ItemExcelSetup_100.cs`
- `Assets/Data/ItemUpgrades/ItemUpgradeData.cs`
- `Assets/GameData/Items/Visuals/ItemStatModifier.cs`
- `Assets/GameData/Items/Visuals/ItemStats.cs`
- `Assets/Effect/Animation/CowChunkWalker.cs`
- `Assets/Effect/Animation/TimeframeExplosion.cs`
- `Assets/Effect/Shader/FarBackgroundParallax.cs`
- `Assets/Effect/Shader/HeavenBiomeSetup.cs`
- `Assets/Effect/Shader/Infinite2DBackground.cs`
- `Assets/Effect/Shader/CurveWorld/CurveWorldController.cs`
- `Assets/Effect/Shader/CurveWorld/CurveWorldTestSetup.cs`

## 4. Prefab inventory

Có 85 prefab dưới `Assets` (đếm file). Các nhóm sử dụng gồm `Assets/Prefabs/Player`, `Traffic`, `Road`, `Pickups`, `Obstacles`, `Enemy`, `UI`; prefab VFX nằm dưới `Assets/VFX`. Prefab nguồn tiêu biểu đọc được từ cây file: `PLAYERROOT`, `PlayerBike`, `Ex150`; traffic xe máy/ô tô/bus/ba gác; `RoadChunk_Base 2 1`, `mountainbiome`, `HeavenBiomeGenerator`; Coin/Gum/Photon/shield; `Obstacle_Barrier`, `WarningLineSystem`; shield/nitro/magnet VFX. Danh sách tên/đường dẫn đầy đủ:

- `Assets/Effect/Animation/PhotonSpeedEffect.prefab`
- `Assets/Effect/Animation/PhotonVisual.prefab`
- `Assets/GameData/DATAPRELAF/TRANG PHỤC/EXCITER OUFIT 1.prefab`
- `Assets/GameData/DATAPRELAF/TRANG PHỤC/EXCITER OUFIT 2.prefab`
- `Assets/GameData/DATAPRELAF/TRANG PHỤC/LEAD OUTFIT 1.prefab`
- `Assets/GameData/DATAPRELAF/TRANG PHỤC/LEAD OUTFIT 10.prefab`
- `Assets/GameData/DATAPRELAF/TRANG PHỤC/LEAD OUTFIT 11.prefab`
- `Assets/GameData/DATAPRELAF/TRANG PHỤC/LEAD OUTFIT 12.prefab`
- `Assets/GameData/DATAPRELAF/TRANG PHỤC/LEAD OUTFIT 13.prefab`
- `Assets/GameData/DATAPRELAF/TRANG PHỤC/LEAD OUTFIT 14.prefab`
- `Assets/GameData/DATAPRELAF/TRANG PHỤC/LEAD OUTFIT 15.prefab`
- `Assets/GameData/DATAPRELAF/TRANG PHỤC/LEAD OUTFIT 2.prefab`
- `Assets/GameData/DATAPRELAF/TRANG PHỤC/LEAD OUTFIT 3.prefab`
- `Assets/GameData/DATAPRELAF/TRANG PHỤC/LEAD OUTFIT 4.prefab`
- `Assets/GameData/DATAPRELAF/TRANG PHỤC/LEAD OUTFIT 5.prefab`
- `Assets/GameData/DATAPRELAF/TRANG PHỤC/LEAD OUTFIT 6.prefab`
- `Assets/GameData/DATAPRELAF/TRANG PHỤC/LEAD OUTFIT 7.prefab`
- `Assets/GameData/DATAPRELAF/TRANG PHỤC/LEAD OUTFIT 8.prefab`
- `Assets/GameData/DATAPRELAF/TRANG PHỤC/LEAD OUTFIT 9.prefab`
- `Assets/GameData/DATAPRELAF/VEHICLE/EXCITER.prefab`
- `Assets/GameData/DATAPRELAF/VEHICLE/Lead Hỏa Long.prefab`
- `Assets/GameData/DATAPRELAF/VEHICLE/LEAD MẶC ĐỊNH 1.prefab`
- `Assets/GameData/DATAPRELAF/VEHICLE/LEAD MẶC ĐỊNH 10.prefab`
- `Assets/GameData/DATAPRELAF/VEHICLE/LEAD MẶC ĐỊNH 11.prefab`
- `Assets/GameData/DATAPRELAF/VEHICLE/LEAD MẶC ĐỊNH 12.prefab`
- `Assets/GameData/DATAPRELAF/VEHICLE/LEAD MẶC ĐỊNH 13.prefab`
- `Assets/GameData/DATAPRELAF/VEHICLE/LEAD MẶC ĐỊNH 14.prefab`
- `Assets/GameData/DATAPRELAF/VEHICLE/LEAD MẶC ĐỊNH 15.prefab`
- `Assets/GameData/DATAPRELAF/VEHICLE/LEAD MẶC ĐỊNH 2.prefab`
- `Assets/GameData/DATAPRELAF/VEHICLE/LEAD MẶC ĐỊNH 3.prefab`
- `Assets/GameData/DATAPRELAF/VEHICLE/LEAD MẶC ĐỊNH 4.prefab`
- `Assets/GameData/DATAPRELAF/VEHICLE/LEAD MẶC ĐỊNH 5.prefab`
- `Assets/GameData/DATAPRELAF/VEHICLE/LEAD MẶC ĐỊNH 6.prefab`
- `Assets/GameData/DATAPRELAF/VEHICLE/LEAD MẶC ĐỊNH 7.prefab`
- `Assets/GameData/DATAPRELAF/VEHICLE/LEAD MẶC ĐỊNH 8.prefab`
- `Assets/GameData/DATAPRELAF/VEHICLE/LEAD MẶC ĐỊNH 9.prefab`
- `Assets/GameData/DATAPRELAF/VEHICLE/LỌ VƯƠNG CHIẾN MÃ.prefab`
- `Assets/Prefabs/Enemy/ambu.prefab`
- `Assets/Prefabs/Enemy/ex cai tơ.prefab`
- `Assets/Prefabs/Enemy/TimeframeExplosion_Prefab.prefab`
- `Assets/Prefabs/Enemy/trẻ trâu.prefab`
- `Assets/Prefabs/Obstacles/Obstacle_Barrier.prefab`
- `Assets/Prefabs/Obstacles/WarningLineSystem.prefab`
- `Assets/Prefabs/Pickups/Coin.prefab`
- `Assets/Prefabs/Pickups/default.prefab`
- `Assets/Prefabs/Pickups/Gum.prefab`
- `Assets/Prefabs/Pickups/MagnetEffect.prefab`
- `Assets/Prefabs/Pickups/Photon.prefab`
- `Assets/Prefabs/Pickups/PhotonEffectPoint.prefab`
- `Assets/Prefabs/Pickups/shield.prefab`
- `Assets/Prefabs/Pickups/ShieldEffect.prefab`
- `Assets/Prefabs/Pickups/ShieldEffectPoint.prefab`
- `Assets/Prefabs/Player/DriftSmoke.prefab`
- `Assets/Prefabs/Player/Ex150.prefab`
- `Assets/Prefabs/Player/Photon.prefab`
- `Assets/Prefabs/Player/PlayerBike.prefab`
- `Assets/Prefabs/Player/PLAYERROOT.prefab`
- `Assets/Prefabs/Road/Cube (1).prefab`
- `Assets/Prefabs/Road/Cube (2).prefab`
- `Assets/Prefabs/Road/Cube 1.prefab`
- `Assets/Prefabs/Road/Cube 2.prefab`
- `Assets/Prefabs/Road/Cube.prefab`
- `Assets/Prefabs/Road/HeavenBiomeGenerator.prefab`
- `Assets/Prefabs/Road/landmark.prefab`
- `Assets/Prefabs/Road/mountainbiome.prefab`
- `Assets/Prefabs/Road/Road (2).prefab`
- `Assets/Prefabs/Road/RoadChunk_Base 2 1.prefab`
- `Assets/Prefabs/Road/THÀNH PHỐ.prefab`
- `Assets/Prefabs/Traffic/BaGac_Vehicle.prefab`
- `Assets/Prefabs/Traffic/BUS (1).prefab`
- `Assets/Prefabs/Traffic/O TO.prefab`
- `Assets/Prefabs/Traffic/Oncoming_Car.prefab`
- `Assets/Prefabs/Traffic/OTO 1.prefab`
- `Assets/Prefabs/Traffic/OTO.prefab`
- `Assets/Prefabs/Traffic/XE MÁY 2.prefab`
- `Assets/Prefabs/Traffic/XE MÁY.prefab`
- `Assets/Prefabs/Traffic/XE TÔN.prefab`
- `Assets/Prefabs/Traffic/XETON.prefab`
- `Assets/Prefabs/UI/Leaderboard/LeaderboardRow.prefab`
- `Assets/VFX/Magnet/Prefabs/MagnetVFX.prefab`
- `Assets/VFX/NitroExhaust/Prefabs/PF_CinematicNitroExhaust.prefab`
- `Assets/VFX/NitroExhaust/Prefabs/photonef.prefab`
- `Assets/VFX/NitroExhaust/Prefabs/PHOTONEFFECT.prefab`
- `Assets/VFX/ShieldHex/Prefabs/ShieldHexVFX 1.prefab`
- `Assets/VFX/ShieldHex/Prefabs/ShieldHexVFX.prefab`

## 5. ScriptableObject/data inventory

- `Assets/Data/Items`: `GameItemData`, database/visual database, item visual definition, enums và Excel setup helper.
- `Assets/Data/ItemUpgrades`: bốn data asset Gum, Magnet, Photon, Shield (và class `ItemUpgradeData`).
- `Assets/GameData/Items/Definitions`: 100 definition assets; IDs 1–100 trong data được scan là liên tục/duy nhất. Category contract/enum nằm trong source; không đồng nghĩa toàn bộ visual đã được xem trong Editor.
- `Assets/GameData/Items/Visuals`: 100 `GameItemVisual` assets; `ItemStats` và `ItemStatModifier` bổ sung stat data.
- Project settings/pipeline assets trong `Assets/Settings`: PC/Mobile URP assets/renderers, default volume profile, update config, global settings.
- Có các curve generated/test materials, TMP settings/font assets và nhiều imported mesh/material. Chưa tuyên bố tất cả đều được sử dụng trong scene đang chơi.

## 6. Shader inventory

Project shader do gameplay/effect sở hữu gồm `Assets/Effect/Shader/CurveWorld/CurveWorldCommon.hlsl`, rigid/deform/test curve shaders, `LEAD_ProceduralSky.shader`, `PhotonVideoBlackKey.shader`, cùng `Assets/VFX/ShieldHex.shader`, `NitroFlame.shader`, `NitroHeatDistortion.shader`. Các shader TextMesh Pro và ShaderGraph đi kèm package/assets là dependency UI; không phải shader gameplay mới.

`CurveWorldController` gửi global shader properties. `CurveWorldCommon.hlsl` đánh giá vị trí theo Z so với reference/start, tính smootherstep trong curve range, offset X theo hướng/strength và Y theo profile dốc; base evaluator giữ nguyên Z. Rigid shader đánh giá pivot và xoay local offsets theo slope; deform shader đánh giá từng vertex và cập nhật normal/tangent. Đây là biến dạng render; không thấy code tương ứng di chuyển Transform/physics collider.

## 7. UI inventory

Scene/component scan xác nhận Login, MainMenu (settings/shop/mission/profile/garage/inventory/leaderboard/lucky wheel/upgrade/gem/update), Gameplay (intro/HUD/item HUD/pause/gameplay settings/gameover), Boot và Loading. Button/text/image hierarchy phần lớn là Unity YAML + TMP/UGUI, nhiều Editor setup script tạo/wire UI. Chưa xác minh từng UnityEvent/route bằng Play Mode. Không tìm thấy scene độc lập tên Shop/Inventory/Profile; các UI này ở MainMenu.

## 8. Firebase inventory

- Project có Firebase Unity SDK assemblies/plugins cho Auth, Firestore, App; native plugin cho iOS/tvOS và x86_64 assets; `Assets/StreamingAssets/google-services-desktop.json` và `Assets/GoogleService-Info.plist`.
- `FirebaseManager` kiểm tra dependency; `FirebaseAuthManager` quản lý auth state; `LoginUIController` email/password và registration; `GoogleSignInManager` triển khai browser OAuth/PKCE với localhost callback listener/thread.
- `FirestorePlayerDataManager` đọc/tạo `players/{uid}`, chuyển đổi `PlayerData`, lưu/cập nhật trường; `SetAsync` MergeAll xuất hiện trong nhánh lưu. Leaderboard path là `leaderboard/{uid}` và collection read trong `LeaderboardManager`.
- `PlayerData` giữ uid/displayName, level/exp/runs, coins/gems/best score, legacy bike fields, owned/equipped IDs, upgrade levels, mission date/selection/counters và saved mission progress.
- Không có file rules/firestore indexes/server/cloud functions rõ ràng trong các đường dẫn đã scan. Do đó không suy luận security rules, authority, atomic shop, offline behavior, production project hay tính đúng đắn backend. Không trích/đưa giá trị nhạy cảm từ config vào báo cáo.

## 9. Gameplay architecture

- Component-based Unity: MonoBehaviour lifecycle và serialized references.
- Manager-based ở một số hệ thống; `GameManager`/một số manager dùng static Instance và duplicate guard/persistence. Không phải mọi class singleton.
- Event-driven có chọn lọc: C# events/Actions giữa GameManager, UI, equipment, stats, Firestore, missions/upgrades. Chưa phải event-only architecture.
- ScriptableObject/data-driven partial: item/visual/upgrade/update config. Player movement, AI thresholds, rules cũng có serialized/hard-coded fields.
- State: `GameManager` run/death/gameover dùng nhiều flags; Player ramp và enemy alley có state cục bộ; không thấy global state machine.
- `FOUND`: component/manager pattern; static singleton instances ở một số manager; SO data; local state enums; events.
- `PARTIAL`: interface-based provider trong updater, không đủ để khẳng định project-wide Strategy/Repository.
- `NOT_FOUND` trong project-owned gameplay: ECS/DOTS/Jobs/Burst, DI container, MVC/MVVM, formal Service Locator, global FSM, ML AI, explicit reusable object pool lifecycle. Package presence hay vendored code không đổi kết luận cho gameplay.

## 10. Data flow

1. Auth flow: Boot/scene setup → Firebase dependency readiness → Firebase Auth user UID → `FirestorePlayerDataManager.CreateOrLoad...` → `players/{uid}` → cached `PlayerData` → events đến progression/missions/ownership/equipment/upgrades/UI.
2. Stats: PlayerData/equipped IDs + `GameItemDatabase`/upgrade data → `EquipmentManager`/`ItemUpgradeManager` → `PlayerRuntimeStats` rebuild + `OnStatsRebuilt` → `PlayerController` refresh giá trị gameplay.
3. Run: `GameManagerPlayerBinder` bind player trong GameplayScene → GameManager start/reset run → score từ Z và coin/death events → HUD, missions, gameover; các nhánh chuyên biệt lưu progression/best score dùng Firestore.
4. Road: scene `RoadManager` → chọn biome road prefab weighted → Instantiate road/`RoadChunk` → spawn traffic/coins theo chunk → cleanup road/traffic bằng Destroy; Heaven có coverage/cleanup riêng.
5. Equip: UI item selection → validate ownership/category và save qua manager → event equipment changed → visual binders cập nhật prefab visual; stat manager rebuild khi cần.

Đây là phụ thuộc quan sát được trong source, không cam kết mọi luồng sync/không lỗi ở runtime.

## 11. Event flow

- `GameManager`: score/coin/gold/gem, death pending, revive, gameover; UI/HUD và MissionManager subscribe theo lifecycle.
- `FirestorePlayerDataManager`: ready/data-loaded/data-cleared/data-saved; progression, mission, equipment, ownership, upgrades/profile consume.
- `EquipmentManager`: ready/equipped/category changed; Inventory UI, garage/showcase, visual binders/stat pipeline.
- `PlayerRuntimeStats.OnStatsRebuilt` → PlayerController cache refresh.
- `InventoryOwnership.OnOwnershipChanged` → UI refresh.
- `ItemUpgradeManager` result/readiness/level/currency events → upgrade UI/runtime consumers.
- Leaderboard/lucky wheel events → UI.

Event subscriptions thường được thêm/bỏ theo OnEnable/OnDisable hoặc lifecycle tương đương; không khẳng định toàn bộ code không leak listener.

## 12. Traffic AI analysis

### TrafficVehicle

`Assets/Scripts/Traffic/TrafficVehicle.cs`, class `TrafficVehicle`: lấy/cache Rigidbody và colliders; chuyển động dựa trên speed trong FixedUpdate; theo dõi player speed/điều chỉnh target/min speed; nhánh knockback dùng force/torque và coroutine; cleanup có thể Destroy. Thông số là field serializable, scene/prefab có thể override default. Không phải server/network-authoritative movement.

### TrafficCarBehavior

`Assets/Scripts/Traffic/TrafficCarBehavior.cs`, class `TrafficCarBehavior`: đăng ký instance trong static `allTraffic` ở OnEnable/OnDisable; theo dõi lane/X, phát hiện xe ahead, tính front/rear/lateral safety; ưu tiên đổi lane theo điều kiện, xác suất và cooldown; nếu không có phương án thì soft/strong/hard brake; khi bị kẹt đủ điều kiện thì thử recovery lane. Có player safety distance. Các lane kiểm tra hữu hạn (3 lane theo fields). Một số helper tìm Player bằng `FindGameObjectWithTag`.

Flow được xác nhận bằng code: **Detect → Evaluate khoảng cách/lane/player → kiểm tra lane an toàn → chọn lane/brake/continue → cooldown và stuck recovery**. Đây là rule-based AI, không có ML training/inference.

Complexity: một truy vấn scan `allTraffic` là O(N) theo active traffic. Mỗi xe có thể thực hiện nhiều truy vấn; aggregate toàn frame có thể tiệm cận O(N²) trong tình huống mọi xe kiểm tra danh sách. Lane options là hằng số 3, nhưng kiểm tra mỗi lane vẫn scan traffic. Đây là phân tích từ cấu trúc vòng lặp, không phải benchmark.

Đặc trưng fixed threshold/probability/cooldown khiến hành vi có quy tắc, dễ kiểm soát nhưng có tính cơ học. Tên object được dùng ở một số nơi để chọn lane-change time theo loại xe; chưa có xác nhận data-driven type model. Tọa độ mặc định khác nhau: RoadManager lane X −6.5/0/+6.5; TrafficCarBehavior −5/0/+5; PlayerController biên X −4.5/+4.5. Đây là điểm cần kiểm tra transform/prefab/runtime, chưa kết luận là lỗi.

## 13. Road generation analysis

`Assets/Scripts/Road/RoadManager.cs`: biome elements có prefab/weight; chọn weighted ngẫu nhiên bằng `UnityEngine.Random`; spawn road, xác định chiều dài từ RoadChunk/fallback, nối chunk theo endpoints; đảm bảo forward coverage, despawn road phía sau, Heaven có state/coverage/cleanup riêng. Spawn traffic theo giới hạn count/min-max, lane, Z trong chunk, padding và gap/safety so với traffic đang hoạt động; gọi CoinSpawner theo chunk. Traffic/pickup/road không có pool được xác nhận, có nhánh Instantiate/Destroy. `ResetWorldForRevive` dọn traffic/road, reset Heaven state, respawn coverage.

`RoadChunk` cung cấp Length/StartZ/EndZ với pivot giữa chunk. GameplayScene serialized values quan sát được trong YAML và cần ưu tiên hơn default source fields khi mô tả scene: chunkLength khoảng 90.2, numberOfChunks 3, forwardCoverage 560, despawnBehindPlayer 100, initialRoadBehindPlayer 25; traffic min/max 4/5, minimum gap 5, lane centers −6.5/0/+6.5. RoadChunk prefab-specific length có thể tham gia nên không khẳng định mọi đoạn dài cùng giá trị. Không có bằng chứng deterministic seed/replay.

Tính gần đúng: spawn road C chunks, traffic P/chunk và quét N traffic để kiểm tra candidate ⇒ O(C×P×N) cộng chi phí retries/asset Instantiate; cleanup O(R+T) cho road/traffic active. Danh sách candidate được tái dùng trong source ở một số đường đi; điều đó không chứng minh zero allocation toàn frame.

## 14. Performance analysis

**Chỉ phân tích source tĩnh; chưa có Unity Profiler, build runtime, frame-time/GC/device measurement.**

| Site | Chi phí/rủi ro từ source | Kết luận đúng mức |
|---|---|---|
| Traffic `allTraffic` scans | O(N) mỗi search, có thể O(N²) tổng nhiều agent | CPU cost có thể tăng theo traffic; chưa benchmark |
| Player tag find helper | FindGameObjectWithTag ở helper | có thể tốn nếu gọi lặp; tần suất thực tế phải đo |
| Road safety placement | candidate/retry × danh sách traffic | cost phụ thuộc số candidate/traffic |
| Road/traffic/VFX/visuals Instantiate/Destroy | churn trên spawn, cleanup, impact, preview replacement | nguy cơ spike/GC/native work; chưa đo |
| Collision queries | overlap/ComputePenetration/GetComponents ở nhánh enemy/collision | cost burst phụ thuộc collider/hit count; chưa đo |
| Database | Dictionary<GameItemData> được dựng từ list; lookup theo ID dự kiến O(1) trung bình; enumerate category scan list O(I) | source structure, không benchmark |
| Firebase | async network calls | latency/network dominated; runtime/backend chưa đo |
| GPU/render | URP/custom shader/particles/materials hiện diện | không có GPU profiling nên UNKNOWN |
| Allocations/LINQ/boxing/string closures toàn dự án | chưa hoàn tất profiler-backed audit toàn đường code | chưa được xác minh bằng profiling/runtime test |

Không tìm thấy UnityEngine.Pool/custom pool lifecycle trong project-owned runtime scripts được scan. Điều này là NOT_FOUND trong phạm vi source game, không phải khẳng định SDK không có pool. Không có bằng chứng ECS/DOTS/Jobs/Burst hoặc zero-GC.

## 15. Complexity analysis

| Thuật toán | Time | Space | Bằng chứng/giới hạn |
|---|---:|---:|---|
| Vehicle ahead/safety scan | O(N) mỗi query | O(1) ngoài danh sách | `TrafficCarBehavior` quét static `allTraffic` |
| Một agent chọn trong 3 lane | O(3N)=O(N) | O(1) ngoài refs | số lane cố định, mỗi lane check traffic |
| AI tổng thể N agent, mỗi agent scan danh sách | tối đa xấp xỉ O(N²) mỗi lần cập nhật toàn bộ | O(N) static refs | upper-shape, branch/query count runtime-dependent |
| Spawn P traffic candidates/chunk safety check | gần O(P×N) + số retry | buffer/list candidate | `RoadManager`; retries phụ thuộc scene |
| Road forward coverage/cleanup | O(R) scan/list operations | O(R) refs | số road active R |
| Item database build + lookup | O(I) build; dictionary lookup kỳ vọng O(1) | O(I) | `GameItemDatabase`; I data definitions |
| Category enumeration UI | O(I) | O(1) bổ sung | scan definitions |
| Collider overlap processing | physics query + O(H) handling | O(H) result capacity/data | H số collider trả về; không suy ra chi phí nội bộ engine |
| Firestore | network + serialize theo kích thước document/model | proportional payload/cache | chưa đo latency |

## 16. Missing/partial features

- **NOT_FOUND:** Coffee-specific gameplay; ML AI; multiplayer gameplay/network authority; ECS/DOTS/Jobs/Burst; object pool lifecycle ở project-owned runtime; global FSM; project-owned Blender/Figma scene source beyond assets (Figma files not found); Firebase security rules/cloud code trong repository scan.
- **PARTIAL:** special traffic (Exciter/Ambulance/Alley classes/spawners exist, runtime validation absent); item power-up systems (classes, controllers/spawners/assets exist, per-item full timing/behavior not all validated); progression/missions (code/data/events exist, formulas and runtime persistence not fully verified); shop (UI/client persistence exists, atomic/server purchase proof absent); updater (Unity scripts and .NET tools exist, release endpoint/integration absent); shader coverage across live materials; project UI routes/events.
- **UNKNOWN:** actual frame rate/GC/CPU/GPU; live Firebase rules/auth project; device/build compatibility; scene route correctness/runtime visuals; Figma use outside repository; exact Blender origin for every imported asset; every prefab nested reference; audio mix/live clip routing; deterministic replay.

## 17. Evidence index

| Nội dung | File | Class | Method/field |
|---|---|---|---|
| Player movement/stats | `Assets/Scripts/Player/PlayerController.cs`, `PlayerRuntimeStats.cs` | PlayerController, PlayerRuntimeStats | Update, FixedUpdate, stat rebuild event, serialized movement fields |
| Collision/death/revive | `Assets/Scripts/Player/PlayerController.cs`, `Assets/Scripts/Core/GameManager.cs` | PlayerController, GameManager | OnTriggerEnter, OnCollisionEnter, BeginDeathPending, RevivePlayer, FinalizeGameOver |
| Score/run lifecycle | `Assets/Scripts/Core/GameManager.cs`, `GameManagerPlayerBinder.cs` | GameManager, GameManagerPlayerBinder | UpdateScore, Begin/Reset run, TryBind |
| Road streaming | `Assets/Scripts/Road/RoadManager.cs`, `RoadChunk.cs` | RoadManager, RoadChunk | SpawnRoad, EnsureForwardCoverage, cleanup, ResetWorldForRevive; StartZ/EndZ |
| Traffic physics | `Assets/Scripts/Traffic/TrafficVehicle.cs` | TrafficVehicle | Awake/Update/FixedUpdate/knockback |
| Traffic AI | `Assets/Scripts/Traffic/TrafficCarBehavior.cs` | TrafficCarBehavior | allTraffic, Update, TryChangeLane, safety scans, braking, TryStuckRecovery |
| Coin/power-up | `Assets/Scripts/Pickups/*.cs`, `Assets/Scripts/Core/GameManager.cs` | Coin/Gum/Magnet/Shield/Photon/ItemManager | collision/trigger and activate methods; exact entrypoints vary by item |
| Account/data | `Assets/Scripts/Firebase/FirebaseManager.cs`, `FirebaseAuthManager.cs`, `FirestorePlayerDataManager.cs`, `Assets/Scripts/Core/PlayerData.cs` | respective managers, PlayerData | dependency/auth lifecycle, CreateOrLoad, `players/{uid}`, SetAsync save |
| Google OAuth | `Assets/Scripts/Firebase/GoogleSignInManager.cs` | GoogleSignInManager | OAuth/PKCE listener and credential flow |
| Leaderboard | `Assets/Scripts/Firebase/Leaderboard/LeaderboardManager.cs`, Firestore manager | LeaderboardManager | collection query; leaderboard document writes |
| Item/inventory/equipment | `Assets/Data/Items/*.cs`, `Assets/Scripts/Core/{EquipmentManager,InventoryOwnership,ItemUpgradeManager}.cs` | data/manager classes | ID/category lookup, ownership/equip/upgrade events |
| UI | `Assets/Scripts/UI/**/*.cs`, `Assets/Scenes/*.unity` | controllers | subscriptions, click handlers, serialized scene components |
| Rendering curve | `Assets/Effect/Shader/CurveWorld/*` | CurveWorldController/common HLSL/shaders | global property update, Evaluate/EvaluateRigid, vertex deformation |
| Blender | `Assets/Model/*.blend` | source files | existence only; exported asset mapping unknown |
| Figma | repository-wide file inventory | — | Figma source document not found |
| Build/package | ProjectSettings, Packages | — | ProjectVersion, manifest, EditorBuildSettings, ProjectSettings |

## 18. Reality matrix

| Feature | Status | Evidence | Confidence |
|---|---|---|---|
| 5 Unity scenes/build entries | IMPLEMENTED | EditorBuildSettings + scene YAML | HIGH |
| Player movement/lane/speed | IMPLEMENTED | PlayerController + GameplayScene component | HIGH |
| Score/run manager | IMPLEMENTED | GameManager and binder | HIGH |
| Collision/death/revive | IMPLEMENTED | PlayerController, GameManager, GameOver UI | HIGH source; runtime UNKNOWN |
| Road streaming/weighted biome | IMPLEMENTED | RoadManager + GameplayScene | HIGH source |
| Traffic spawn | IMPLEMENTED | RoadManager, scene config | HIGH source |
| Traffic AI | IMPLEMENTED, rule-based | TrafficCarBehavior, TrafficVehicle | HIGH source |
| Coin/Gum/Magnet/Shield/Photon | PARTIAL | scripts/spawners/controllers; runtime/item completeness not validated | MEDIUM |
| Ramp/Heaven | IMPLEMENTED | PlayerController/RoadManager/Heaven scripts | HIGH source |
| Inventory/equipment/items 1–100 | IMPLEMENTED in code/data | item definitions/visual assets/managers | HIGH data; runtime wiring MEDIUM |
| Shop transaction/server validation | PARTIAL/UNKNOWN | shop UI/client data; no backend transaction proof | MEDIUM |
| Missions/progression | PARTIAL | managers/model/events/UI | MEDIUM |
| Firebase client auth/Firestore | IMPLEMENTED in client source | managers, SDK/plugins/config presence | HIGH source; backend operation UNKNOWN |
| Firebase rules/security | UNKNOWN / NOT_FOUND in repository | repository scan | HIGH for repo scope only |
| Leaderboard client | IMPLEMENTED in source | manager/UI and Firestore calls | HIGH source; backend/runtime UNKNOWN |
| Blender source models | IMPLEMENTED as repository assets | 17 `.blend` files under Assets/Model | HIGH existence; export mapping UNKNOWN |
| Figma source | NOT_FOUND | repository file scan | HIGH for repository scope |
| Curve rendering | IMPLEMENTED in shader code | controller/HLSL/rigid+deform shaders | HIGH code; scene material use PARTIAL |
| Object pooling | NOT_FOUND in project-owned runtime | source scan | MEDIUM/HIGH scope-limited |
| ML AI / multiplayer gameplay | NOT_FOUND | code/package usage scan | HIGH for inspected project scripts |
| ECS/DOTS/Jobs/Burst | NOT_FOUND in gameplay source | source scan | HIGH |
| Performance benchmark/FPS/GC | UNKNOWN | no profiler/runtime record | HIGH |
| Android/iOS/Standalone build success | UNKNOWN | settings only, no build evidence | HIGH |
| Coffee | NOT_FOUND | gameplay source and asset scan | HIGH |

## 19. Audit checklist and limits

- [x] Enumerated repository top-level structure and major Assets/Packages/ProjectSettings/Tools directories.
- [x] Enumerated script families, 5 scenes, 85 prefabs, 234 `.asset` files; inspected scene script GUIDs and build settings.
- [x] Inspected source-backed gameplay, traffic, road, data/Firebase, UI, rendering, packages and project settings; scanned full project-owned script path inventory included above.
- [x] Cross-checked scene YAML values versus default fields where discussed; distinguished scene serialized values from source defaults.
- [x] Checked for random seed, pool/Jobs/Burst/ECS markers, Blender/Figma evidence; marked absence only within inspected project scope.
- [ ] No Play Mode/build, device compatibility, Firebase backend/rules, or profiler execution; those remain unverified.
- [ ] No exhaustive visual review of every nested prefab, material assignment or every UnityEvent target; avoid claims that depend on these.

## 20. Full `.asset` path inventory




Có 234 file `.asset` trong Assets; danh sách dưới đây để audit có thể truy vết. File extension không chứng minh tất cả đều là ScriptableObject; mesh và imported data cũng có thể dùng `.asset`.

- `Assets/Readme.asset`
- `Assets/Data/ItemUpgrades/Gum_ItemUpgradeData.asset`
- `Assets/Data/ItemUpgrades/Magnet_ItemUpgradeData.asset`
- `Assets/Data/ItemUpgrades/Photon_ItemUpgradeData.asset`
- `Assets/Data/ItemUpgrades/Shield_ItemUpgradeData.asset`
- `Assets/GameData/Items/Database/GameItemDatabase.asset`
- `Assets/GameData/Items/Database/GameItemVisualDatabase.asset`
- `Assets/GameData/Items/Definitions/Item_001_Ninja_Lead.asset`
- `Assets/GameData/Items/Definitions/Item_002_Lead_02.asset`
- `Assets/GameData/Items/Definitions/Item_003_Lead_03.asset`
- `Assets/GameData/Items/Definitions/Item_004_Lead_04.asset`
- `Assets/GameData/Items/Definitions/Item_005_Lead_05.asset`
- `Assets/GameData/Items/Definitions/Item_006_Lead_06.asset`
- `Assets/GameData/Items/Definitions/Item_007_Lead_07.asset`
- `Assets/GameData/Items/Definitions/Item_008_Lead_08.asset`
- `Assets/GameData/Items/Definitions/Item_009_Lead_09.asset`
- `Assets/GameData/Items/Definitions/Item_010_Lead_10.asset`
- `Assets/GameData/Items/Definitions/Item_011_Lead_11.asset`
- `Assets/GameData/Items/Definitions/Item_012_Lead_12.asset`
- `Assets/GameData/Items/Definitions/Item_013_Lead_13.asset`
- `Assets/GameData/Items/Definitions/Item_014_Lead_14.asset`
- `Assets/GameData/Items/Definitions/Item_015_Lead_15.asset`
- `Assets/GameData/Items/Definitions/Item_016_Exciter_01.asset`
- `Assets/GameData/Items/Definitions/Item_017_Exciter_02.asset`
- `Assets/GameData/Items/Definitions/Item_018_Exciter_03.asset`
- `Assets/GameData/Items/Definitions/Item_019_Exciter_04.asset`
- `Assets/GameData/Items/Definitions/Item_020_LEAD_Factory.asset`
- `Assets/GameData/Items/Definitions/Item_021_LEAD_Street.asset`
- `Assets/GameData/Items/Definitions/Item_022_LEAD_Racing.asset`
- `Assets/GameData/Items/Definitions/Item_023_LEAD_Night.asset`
- `Assets/GameData/Items/Definitions/Item_024_LEAD_Neon.asset`
- `Assets/GameData/Items/Definitions/Item_025_LEAD_Gold.asset`
- `Assets/GameData/Items/Definitions/Item_026_EXCITER_Street.asset`
- `Assets/GameData/Items/Definitions/Item_027_EXCITER_Racing.asset`
- `Assets/GameData/Items/Definitions/Item_028_EXCITER_Red.asset`
- `Assets/GameData/Items/Definitions/Item_029_EXCITER_Carbon.asset`
- `Assets/GameData/Items/Definitions/Item_030_Ninja_Lead.asset`
- `Assets/GameData/Items/Definitions/Item_031_Ninja_Lead_Black.asset`
- `Assets/GameData/Items/Definitions/Item_032_Ninja_Lead_Red.asset`
- `Assets/GameData/Items/Definitions/Item_033_Ninja_Lead_Night.asset`
- `Assets/GameData/Items/Definitions/Item_034_Street_Beast.asset`
- `Assets/GameData/Items/Definitions/Item_035_Traffic_King.asset`
- `Assets/GameData/Items/Definitions/Item_036_Full_Ga.asset`
- `Assets/GameData/Items/Definitions/Item_037_Vietnam_Street.asset`
- `Assets/GameData/Items/Definitions/Item_038_Limited_Ride.asset`
- `Assets/GameData/Items/Definitions/Item_039_Legend_Ride.asset`
- `Assets/GameData/Items/Definitions/Item_040_hoavuong.asset`
- `Assets/GameData/Items/Definitions/Item_041_ninjalead.asset`
- `Assets/GameData/Items/Definitions/Item_042_MƯA BÃO.asset`
- `Assets/GameData/Items/Definitions/Item_043_SAIGONDEM.asset`
- `Assets/GameData/Items/Definitions/Item_044_HOATOC.asset`
- `Assets/GameData/Items/Definitions/Item_045_TETVIET.asset`
- `Assets/GameData/Items/Definitions/Item_046_Trung Thu.asset`
- `Assets/GameData/Items/Definitions/Item_047_Bánh Mì.asset`
- `Assets/GameData/Items/Definitions/Item_048_Cyber Việt.asset`
- `Assets/GameData/Items/Definitions/Item_049_Chaos.asset`
- `Assets/GameData/Items/Definitions/Item_050_Hỏa Long.asset`
- `Assets/GameData/Items/Definitions/Item_051_Kim Long.asset`
- `Assets/GameData/Items/Definitions/Item_052_Bão Tố.asset`
- `Assets/GameData/Items/Definitions/Item_053_Địa Ngục.asset`
- `Assets/GameData/Items/Definitions/Item_054_Thiên Hà.asset`
- `Assets/GameData/Items/Definitions/Item_055_Thần Long.asset`
- `Assets/GameData/Items/Definitions/Item_056_Racing_Shoes.asset`
- `Assets/GameData/Items/Definitions/Item_057_Mechanic_Shoes.asset`
- `Assets/GameData/Items/Definitions/Item_058_Deadline_Set.asset`
- `Assets/GameData/Items/Definitions/Item_059_Legend_Street_Set.asset`
- `Assets/GameData/Items/Definitions/Item_060_KHÔNG_PHANH.asset`
- `Assets/GameData/Items/Definitions/Item_061_ĐỪNG_CẢN.asset`
- `Assets/GameData/Items/Definitions/Item_062_CHẠY_ĐI.asset`
- `Assets/GameData/Items/Definitions/Item_063_FULL_GA.asset`
- `Assets/GameData/Items/Definitions/Item_064_BÃO_ĐÊM.asset`
- `Assets/GameData/Items/Definitions/Item_065_Ơ_KÌA.asset`
- `Assets/GameData/Items/Definitions/Item_066_NHÌN_GÌ_.asset`
- `Assets/GameData/Items/Definitions/Item_067_TRÁNH_RA.asset`
- `Assets/GameData/Items/Definitions/Item_068_TOANG.asset`
- `Assets/GameData/Items/Definitions/Item_069_NINJA.asset`
- `Assets/GameData/Items/Definitions/Item_070_Plate_37.asset`
- `Assets/GameData/Items/Definitions/Item_071_Plate_29.asset`
- `Assets/GameData/Items/Definitions/Item_072_Plate_43.asset`
- `Assets/GameData/Items/Definitions/Item_073_Carbon_Plate.asset`
- `Assets/GameData/Items/Definitions/Item_074_Red_Trail.asset`
- `Assets/GameData/Items/Definitions/Item_075_Cyan_Trail.asset`
- `Assets/GameData/Items/Definitions/Item_076_Gold_Trail.asset`
- `Assets/GameData/Items/Definitions/Item_077_Neon_Trail.asset`
- `Assets/GameData/Items/Definitions/Item_078_Lightning_Trail.asset`
- `Assets/GameData/Items/Definitions/Item_079_Legend_Trail.asset`
- `Assets/GameData/Items/Definitions/Item_080_Kính_Đen.asset`
- `Assets/GameData/Items/Definitions/Item_081_Kính_Neon.asset`
- `Assets/GameData/Items/Definitions/Item_082_Kính_Racing.asset`
- `Assets/GameData/Items/Definitions/Item_083_Khẩu_Trang_Đen.asset`
- `Assets/GameData/Items/Definitions/Item_084_Khẩu_Trang_Neon.asset`
- `Assets/GameData/Items/Definitions/Item_085_Túi_Đeo_Chéo.asset`
- `Assets/GameData/Items/Definitions/Item_086_Ba_Lô_Street.asset`
- `Assets/GameData/Items/Definitions/Item_087_Ba_Lô_Racing.asset`
- `Assets/GameData/Items/Definitions/Item_088_Găng_Tay_Đen.asset`
- `Assets/GameData/Items/Definitions/Item_089_Găng_Tay_Racing.asset`
- `Assets/GameData/Items/Definitions/Item_090_Găng_Tay_Neon.asset`
- `Assets/GameData/Items/Definitions/Item_091_Dây_Chuyền.asset`
- `Assets/GameData/Items/Definitions/Item_092_Vòng_Tay_Street.asset`
- `Assets/GameData/Items/Definitions/Item_093_Vòng_Tay_Neon.asset`
- `Assets/GameData/Items/Definitions/Item_094_Tai_Nghe_Street.asset`
- `Assets/GameData/Items/Definitions/Item_095_Tai_Nghe_Neon.asset`
- `Assets/GameData/Items/Definitions/Item_096_Móc_Khóa_Lead.asset`
- `Assets/GameData/Items/Definitions/Item_097_Móc_Khóa_Ninja.asset`
- `Assets/GameData/Items/Definitions/Item_098_Huy_Hiệu_Street.asset`
- `Assets/GameData/Items/Definitions/Item_099_Huy_Hiệu_Legend.asset`
- `Assets/GameData/Items/Definitions/Item_100_Phụ_Kiện_20.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_001.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_002.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_003.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_004.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_005.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_006.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_007.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_008.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_009.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_010.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_011.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_012.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_013.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_014.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_015.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_016.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_017.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_018.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_019.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_020.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_021.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_022.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_023.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_024.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_025.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_026.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_027.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_028.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_029.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_030.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_031.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_032.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_033.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_034.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_035.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_036.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_037.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_038.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_039.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_040.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_041.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_042.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_043.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_044.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_045.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_046.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_047.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_048.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_049.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_050.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_051.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_052.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_053.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_054.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_055.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_056.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_057.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_058.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_059.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_060.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_061.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_062.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_063.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_064.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_065.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_066.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_067.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_068.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_069.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_070.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_071.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_072.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_073.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_074.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_075.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_076.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_077.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_078.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_079.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_080.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_081.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_082.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_083.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_084.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_085.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_086.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_087.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_088.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_089.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_090.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_091.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_092.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_093.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_094.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_095.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_096.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_097.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_098.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_099.asset`
- `Assets/GameData/Items/Visuals/GameItemVisual_100.asset`
- `Assets/Settings/DefaultVolumeProfile.asset`
- `Assets/Settings/Mobile_Renderer.asset`
- `Assets/Settings/Mobile_RPAsset.asset`
- `Assets/Settings/PC_Renderer.asset`
- `Assets/Settings/PC_RPAsset.asset`
- `Assets/Settings/SampleSceneProfile.asset`
- `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`
- `Assets/Settings/Update/UpdateConfig.asset`
- `Assets/TextMesh Pro/Resources/TMP Settings.asset`
- `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset`
- `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset`
- `Assets/TextMesh Pro/Resources/Sprite Assets/EmojiOne.asset`
- `Assets/TextMesh Pro/Resources/Style Sheets/Default Style Sheet.asset`
- `Assets/VFX/Magnet/Meshes/SM_Magnet_Core.asset`
- `Assets/VFX/Magnet/Meshes/SM_Magnet_Node.asset`
- `Assets/VFX/NitroExhaust/Meshes/SM_Nitro_Core.asset`
- `Assets/VFX/NitroExhaust/Meshes/SM_Nitro_Flame_Main.asset`
- `Assets/VFX/NitroExhaust/Meshes/SM_Nitro_Flame_Second.asset`
- `Assets/VFX/NitroExhaust/Meshes/SM_Nitro_Tongue_C.asset`
- `Assets/VFX/NitroExhaust/Meshes/SM_Nitro_Tongue_L.asset`
- `Assets/VFX/NitroExhaust/Meshes/SM_Nitro_Tongue_R.asset`
- `Assets/VFX/ShieldHex/Meshes/SM_Shield_Core.asset`
- `Assets/VFX/ShieldHex/Meshes/SM_Shield_Dome.asset`
- `Assets/VFX/ShieldHex/Meshes/SM_Shield_EnergyArc.asset`
- `Assets/VFX/ShieldHex/Meshes/SM_Shield_EnergyShard.asset`
- `Assets/VFX/ShieldHex/Meshes/SM_Shield_HexCell.asset`
- `Assets/VFX/ShieldHex/Meshes/SM_Shield_HexRing.asset`



