# FIGURE CHECKLIST — LEAD KHÔNG PHANH

> Đây là danh sách ảnh cần chụp/chèn khi dàn trang. Không ảnh nào dưới đây được giả định là đã chụp. Nguồn trong repository chỉ chứng minh hệ thống/code/asset; cần mở Unity Editor/Blender/Figma và chụp đúng trạng thái thật. Không dùng ảnh minh họa thay cho bằng chứng scene.

| Figure ID | Tên hình | Vị trí trong báo cáo | Nội dung cần chụp/vẽ | Nguồn và ghi chú |
|---|---|---|---|---|
| Hình 1.1 | Gameplay tổng thể | Chương 1, 1.3.2 | GameplayScene đang chạy: player, đường, traffic, pickup và HUD cùng khung hình. | `Assets/Scenes/GameplayScene.unity`; PlayerController, RoadManager. Chụp Play Mode nếu có build ổn định; nếu chưa, ghi “scene editor view”. |
| Hình 1.2 | Blender environment/asset workflow | Chương 1, 1.4.2 | Blender mở một file `.blend` trong `Assets/Model`, kế bên Unity import/prefab tương ứng chỉ khi GUID/mesh mapping đã xác minh. | Repository có 17 `.blend`; mapping file Blender → FBX/prefab chưa xác minh. Không gán tên asset cụ thể là environment/vehicle nếu chưa mở đối chiếu. |
| Hình 1.3 | Figma UI design | Chương 1, 1.4.3 | Chỉ chèn screenshot Figma thật nếu nhóm cung cấp file/link; có thể đặt cạnh UI Unity tương ứng. | Không tìm thấy file Figma trong repository. Hiện checklist để “chờ nguồn ngoài repo”; không tự tạo hình/mockup rồi gắn nhãn Figma. |
| Hình 2.1 | Kiến trúc tổng thể | Chương 2, 2.1.1 | Sơ đồ component/data/event: GameManager–PlayerController–RoadManager–Traffic, rồi Firebase/data/meta UI. | `GameManager.cs`, `GameManagerPlayerBinder.cs`, `RoadManager.cs`, `TrafficCarBehavior.cs`, `FirestorePlayerDataManager.cs`, `EquipmentManager.cs`. Vẽ sơ đồ bằng chứng, không phải ảnh hierarchy đơn lẻ. |
| Hình 2.2 | Scene flow | Chương 2, 2.1.2 | Flow Login/MainMenu/Gameplay/Loading với Boot entry; đánh dấu tuyến load đã thấy trong code riêng. | `EditorBuildSettings.asset`, 5 scene, `BootController`, `LoginUIController`, `MainMenuController`, `LoadingScreenController`. Chụp Build Settings + sơ đồ. |
| Hình 2.3 | Player, road và traffic | Chương 2, 2.2.2 | Gameplay Hierarchy/Scene view hiển thị player, road chunk, traffic instances. | GameplayScene YAML; PLAYERROOT/road/traffic prefabs. Không hiển thị runtime instance nếu chỉ mở Editor. |
| Hình 2.4 | Inventory và Shop | Chương 2, 2.2.1 | MainMenu UI panels, item list/category, selected item purchase/equip state. | MainMenu scene; InventoryUI, MainMenuShopUI, GameItemData. Có thể cần chạy Play Mode để render đúng. |
| Hình 2.5 | Luồng gameplay | Chương 2, 2.3.1 | Flowchart: bind/start → move/collect/avoid → death pending → revive hoặc Game Over. | GameManager, GameManagerPlayerBinder, PlayerController, GameOverUIController. Dựng diagram từ source. |
| Hình 2.6 | Player movement | Chương 2, 2.3.2 | Sơ đồ input → X/Z → bounds/stats; nếu chụp gameplay, thể hiện lane positions. | PlayerController Update/FixedUpdate; PlayerRuntimeStats; lưu ý input code dùng `Input.GetAxisRaw`; lane movement liên tục, không khẳng định integer lane index cho player. |
| Hình 2.7 | Road generation | Chương 2, 2.3.4 | Sơ đồ RoadManager chọn biome/Instantiate chunk/coverage/traffic+coin spawn/cleanup. | RoadManager, RoadChunk, GameplayScene serialized values. |
| Hình 2.8 | Traffic AI | Chương 2, 2.3.6 | State/decision flow Detect → Evaluate → safe lane → change/brake/continue → cooldown/recovery. | TrafficCarBehavior/TrafficVehicle. Nêu rõ rule-based; không dùng hình mô phỏng ML/neural network. |
| Hình 2.9 | Ramp/Heaven | Chương 2, 2.3.8 | RampState transitions và Heaven road/coin transition; kèm screenshot ramp nếu có. | PlayerController RampState; RoadManager; HeavenBiomeController; CoinSpawner. |
| Hình 2.10 | Death/Revive | Chương 2, 2.3.9 | Flow death pending → revive validation → reset world/restore player hoặc FinalizeGameOver. | GameManager, PlayerController, RoadManager.ResetWorldForRevive, GameOverUIController. |
| Hình 2.11 | Coin/Power-up | Chương 2, 2.3.7 | Pickup prefab và HUD/item controllers; label riêng Gum/Magnet/Shield/Photon. | `Assets/Prefabs/Pickups`, Pickups scripts, GameplayItemHUD. Không đưa Coffee. |
| Hình 2.12 | Firebase data flow | Chương 2, 2.3.11 | Firebase readiness → Auth UID → `players/{uid}` → PlayerData → equipment/mission/runtime. | FirebaseManager, FirebaseAuthManager, FirestorePlayerDataManager, PlayerData, EquipmentManager. Không hiển thị credentials/secret values. |
| Hình 2.13 | Item database và stat pipeline | Chương 2, 2.3.10 | Item definition/visual/upgrades → equipment → runtime stats → PlayerController. | GameItemData/Database, GameItemVisualDatabase, EquipmentManager, ItemUpgradeManager, PlayerRuntimeStats. |
| Hình 3.1 | Unity project structure | Chương 3, 3.1.2 | Project window hiển thị Assets/Scripts, Scenes, Prefabs, Data, Settings, VFX, Packages, ProjectSettings. | Chụp Unity Project window hoặc dùng inventory; tránh workspace temp/Library làm thành cấu trúc sản phẩm. |
| Hình 3.2 | Blender → Unity | Chương 3, 3.1.6 | Một `.blend` mở trong Blender và asset imported/prefab Unity sau khi source mapping đã kiểm chứng. | Có `.blend` trong Assets/Model; mapping cụ thể chưa được xác nhận. Nếu không đối chiếu được, thay bằng bảng “các file Blender có trong repo; export mapping unknown”. |
| Hình 3.3 | Figma → Unity | Chương 3, 3.1.7 | File Figma thật và UI Unity tương ứng, có label bước chuyển giao. | Chưa có Figma source trong repo; chỉ chèn khi nhóm cung cấp. Nếu không có, dùng thông báo không đủ bằng chứng thay cho hình workflow giả định. |
| Hình 3.4 | Gameplay build | Chương 3, 3.2.1 | Build thực tế trên target xác nhận hoặc Play Mode nếu chỉ xác minh Editor. Ghi target/version/cấu hình. | `Builds/` tồn tại nhưng chưa xác minh build file/runtime evidence. Không gắn nhãn “mobile build” nếu chưa chạy trên thiết bị. |
| Hình 3.5 | Unity Profiler | Chương 3, 3.2.3 | Capture CPU/GPU/Memory/GC của scene, kèm editor/build target, thiết bị, phiên chơi và thời lượng. | Chưa có profiler evidence. Không tạo số FPS/GC giả; hình chỉ chèn sau khi đo thật. |
| Hình 3.6 | CurveWorld visual | Chương 2, 2.2.2 hoặc 3.1.3 | Scene/material thể hiện curve/hill shader; có thể kèm X/Y/Z diagram. | CurveWorldController, `CurveWorldCommon.hlsl`, rigid/deform shaders. Cần xác nhận material được gán ở scene/prefab thực. |
| Hình 3.7 | Leaderboard/Mission UI | Chương 2, 2.2.1 | UI thật cùng trạng thái loaded/empty/error rõ ràng. | LeaderboardUI, MissionUIController; runtime/backend chưa xác minh. |

## Trạng thái chèn ảnh

- **Có thể chụp từ Unity ngay:** project structure, Build Settings, source hierarchy, prefab inspector; cần tránh gọi đây là runtime proof.
- **Cần Play Mode/runtime:** gameplay, UI state, revive, ramp, leaderboard live.
- **Cần tool/source ngoài repository:** Figma design; mapping chính xác Blender export; profiler capture.
- Mỗi hình cần caption, nguồn, ngày chụp và trạng thái (Editor/Play Mode/build/device) trước khi đưa vào bản nộp.
