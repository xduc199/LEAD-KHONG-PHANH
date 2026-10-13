# ĐẶC TẢ USE CASE — LEAD KHÔNG PHANH

> Đặc tả được dựng từ source và scene đã kiểm kê. Các bước backend/runtime chưa được kiểm chứng được đánh dấu rõ. “Main Flow” mô tả luồng thể hiện trong code, không khẳng định Play Mode đã chạy thành công.

## UC01 — Đăng nhập bằng email/mật khẩu

**Use Case ID:** UC01  
**Use Case Name:** Đăng nhập tài khoản  
**Primary Actor:** Người chơi  
**Supporting Actor:** Firebase Authentication; Firestore player-data manager  
**Goal:** Xác thực người chơi và yêu cầu load/tạo hồ sơ.  
**Trigger:** Người chơi chọn Login trên Login scene.  
**Preconditions:** Login scene có LoginUIController; Firebase dependencies/auth đã sẵn sàng; thông tin đăng nhập hợp lệ.  
**Postconditions:** Firebase Auth có user hiện tại; auth manager điều phối load/create PlayerData theo UID; trạng thái readiness được cập nhật nếu hoàn tất.

**Main Flow:**

1. Người chơi nhập email và mật khẩu.
2. `LoginUIController` kiểm tra field và trạng thái Firestore/auth readiness.
3. Controller gọi Firebase Auth sign-in bất đồng bộ.
4. Khi xác thực thành công, `FirebaseAuthManager` nhận Auth state change và lấy UID.
5. Auth manager yêu cầu `FirestorePlayerDataManager` load hoặc tạo dữ liệu `players/{uid}`.
6. Khi hoàn tất, trạng thái player-data-ready/event được phát cho UI và manager phụ thuộc.

**Alternative Flow:**

- A1. Firebase chưa sẵn sàng: UI đưa trạng thái lỗi/không tiếp tục; người chơi thử lại sau.
- A2. User đã được Firebase khôi phục từ session: auth manager nhận `CurrentUser` khi khởi tạo và vẫn xử lý UID/load data.

**Exception Flow:**

- E1. Firebase Auth trả lỗi credentials/network: controller cập nhật trạng thái lỗi; không có bằng chứng source về thông điệp cụ thể cho mọi mã lỗi.
- E2. Firestore load/create lỗi: player data chưa được xác nhận ready; backend/runtime chưa kiểm chứng.

**Related Systems:** `LoginUIController` (login handler), `FirebaseManager` (dependency check), `FirebaseAuthManager` (`InitializeAuth`, `OnAuthStateChanged`, user handler), `FirestorePlayerDataManager` (Create/Load), `PlayerData`, Login scene.

## UC02 — Đăng ký tài khoản

**Use Case ID:** UC02  
**Use Case Name:** Tạo tài khoản email/mật khẩu  
**Primary Actor:** Người chơi mới  
**Supporting Actor:** Firebase Authentication, Firestore  
**Goal:** Tạo Firebase user và khởi tạo hồ sơ player khi xác thực thành công.  
**Trigger:** Chọn Register trên Login UI.  
**Preconditions:** Login scene và Firebase Auth sẵn sàng; dữ liệu nhập thỏa validation trong UI.  
**Postconditions:** Auth state có thể phát user mới; dữ liệu player được yêu cầu load/create.

**Main Flow:**

1. Người chơi nhập thông tin đăng ký.
2. `LoginUIController` kiểm tra input.
3. UI gọi luồng create user bằng Firebase Auth.
4. Firebase trả user/UID khi thành công.
5. `FirebaseAuthManager` xử lý Auth state và gọi luồng dữ liệu người chơi.
6. Firestore manager tạo document nếu chưa có theo logic CreateOrLoad.

**Alternative Flow:** A1. Email đã tồn tại hoặc validation không đạt: hiển thị lỗi và cho phép sửa input.  
**Exception Flow:** E1. Mạng/Firebase lỗi: không xác nhận tài khoản hay document đã tạo. E2. User tạo thành công nhưng load hồ sơ lỗi: trạng thái gameplay chưa được xác nhận sẵn sàng.  
**Related Systems:** LoginUIController, FirebaseAuthManager, FirestorePlayerDataManager, PlayerData, Login scene.

## UC03 — Đăng nhập Google OAuth

**Use Case ID:** UC03  
**Use Case Name:** Đăng nhập Google  
**Primary Actor:** Người chơi  
**Supporting Actor:** Trình duyệt Google OAuth, localhost callback listener, Firebase Auth  
**Goal:** Nhận credential Google và đăng nhập Firebase.  
**Trigger:** Chọn nút Google Login.  
**Preconditions:** Login scene; cấu hình OAuth/client id/network hợp lệ; platform hỗ trợ listener.  
**Postconditions:** Nếu credential exchange thành công, Firebase Auth state chuyển sang user và load PlayerData.

**Main Flow:**

1. Login UI yêu cầu `GoogleSignInManager.StartGoogleLogin`.
2. Manager tạo state/PKCE verifier và mở authorization flow qua browser.
3. Local HTTP listener nhận callback authorization code.
4. Manager đổi code lấy token theo code path trong source.
5. Tạo Firebase credential và đăng nhập Firebase Auth.
6. Auth manager xử lý UID và yêu cầu Firestore player data.

**Alternative Flow:** A1. Người dùng đóng/hủy browser flow: UI nhận failure callback nếu source path xử lý.  
**Exception Flow:** E1. Port/listener, redirect, HTTP token exchange hoặc Firebase credential lỗi: login không hoàn tất. E2. Thread/callback/platform/browser behavior chưa được runtime kiểm thử.  
**Related Systems:** GoogleSignInManager, LoginUIController, FirebaseAuthManager, FirestorePlayerDataManager; `Assets/Scenes/Login.unity`.

## UC04 — Load hoặc tạo PlayerData

**Use Case ID:** UC04  
**Use Case Name:** Load PlayerData theo UID  
**Primary Actor:** FirebaseAuthManager (system actor)  
**Supporting Actor:** Firebase Firestore  
**Goal:** Có dữ liệu player trong bộ nhớ cho user đã xác thực.  
**Trigger:** Auth state có user hoặc user hiện tại được khôi phục.  
**Preconditions:** Firebase dependency/auth readiness; UID hợp lệ.  
**Postconditions:** `CurrentPlayerData` được set nếu đọc/tạo thành công; lifecycle events được phát.

**Main Flow:**

1. FirebaseAuthManager đọc `FirebaseUser.UserId`.
2. Gọi `FirestorePlayerDataManager` load/create.
3. Manager đọc `players/{uid}` snapshot.
4. Nếu có document, convert và normalize legacy/current fields; nếu thiếu, tạo PlayerData/document theo source logic.
5. Cache data và thông báo lifecycle/readiness.
6. Equipment, inventory, progression/mission và UI consumers nhận data/event.

**Alternative Flow:** A1. Tài khoản chưa có document: khởi tạo document mặc định.  
**Exception Flow:** E1. Snapshot/conversion/network lỗi: dữ liệu chưa ready. E2. Rules/index/network offline chưa biết; không thể mô tả phục hồi offline.  
**Related Systems:** FirebaseAuthManager, FirestorePlayerDataManager (`players/{uid}`, CreateOrLoad/convert/save), PlayerData, EquipmentManager, ItemUpgradeManager, PlayerProgression, MissionManager.

## UC05 — Đăng xuất / thoát từ Settings

**Use Case ID:** UC05  
**Use Case Name:** Chọn nút Logout trong MainMenu Settings  
**Primary Actor:** Người chơi  
**Supporting Actor:** MainMenuController; FirebaseAuthManager  
**Goal:** Hành động của nút được gắn nhãn Logout.  
**Trigger:** Nhấn logout button trong MainMenu Settings.  
**Preconditions:** MainMenu settings UI đã bind button và MainMenuController resolve thành công.  
**Postconditions:** `MainMenuController.Quit()` sign out Firebase nếu Auth manager sẵn sàng, clear CurrentPlayerData và load Login scene nếu tên scene hợp lệ. Play Mode chưa xác minh.

**Main Flow:**

1. MainMenuSettingsUI bind listener cho nút.
2. Người chơi nhấn nút.
3. Handler resolve MainMenuController.
4. Handler gọi `MainMenuController.Quit()`; controller chặn logout nếu đang start game/exiting, đặt timeScale về 1, gọi Firebase Auth `SignOut()` khi có Auth, clear CurrentPlayerData và load Login scene nếu scene name hợp lệ.

**Alternative Flow:** A1. Controller không có: log error và dừng.  
**Exception Flow:** E1. Auth manager thiếu: controller ghi warning nhưng vẫn clear data và thử load Login. E2. Login scene name trống/không load được: reset logging-out flag và dừng. E3. Runtime transition chưa Play Mode verified.  
**Related Systems:** MainMenuSettingsUI (`OnLogoutButtonClicked`), MainMenuController (`Quit`, SignOut, ClearCurrentPlayerData, LoadScene), FirebaseAuthManager (auth state handling), FirestorePlayerDataManager, MainMenu scene.

## UC06 — Bắt đầu run

**Use Case ID:** UC06  
**Use Case Name:** Bắt đầu lượt chơi  
**Primary Actor:** Người chơi / GameManagerPlayerBinder  
**Supporting Actor:** GameManager, PlayerController, RoadManager, HUD  
**Goal:** Reset trạng thái run và bind player trong GameplayScene.  
**Trigger:** Scene gameplay được vào hoặc player được binder tìm thấy.  
**Preconditions:** GameplayScene có player và GameManager/RoadManager liên quan.  
**Postconditions:** GameManager giữ player ref và run counters/state được reset/active theo code.

**Main Flow:**

1. GameplayScene khởi tạo components.
2. `GameManagerPlayerBinder` tìm/bind PlayerController.
3. Binder gọi `BeginNewRun` nếu run chưa active.
4. GameManager reset score, coin/session/death flags theo implementation.
5. PlayerController nhận state/stats và gameplay update tiếp tục.

**Alternative Flow:** A1. Player chưa sẵn sàng: binder thử bind theo cơ chế của script; thứ tự chính xác phụ thuộc scene lifecycle.  
**Exception Flow:** E1. Thiếu GameManager/player reference: run không thể xác nhận bắt đầu.  
**Related Systems:** GameManagerPlayerBinder (`TryBind`), GameManager (`BeginNewRun`, score update), GameplaySession, PlayerController, GameplayScene.

## UC07 — Điều khiển player và chuyển làn

**Use Case ID:** UC07  
**Use Case Name:** Điều khiển xe người chơi  
**Primary Actor:** Người chơi  
**Supporting Actor:** Input Manager, PlayerRuntimeStats  
**Goal:** Điều khiển vị trí ngang và tiến/lên ramp theo logic PlayerController.  
**Trigger:** Input mỗi frame trong gameplay.  
**Preconditions:** Run/player active; component enabled; input axes có giá trị.  
**Postconditions:** Transform/Rigidbody position/velocity được cập nhật theo các nhánh movement hiện có.

**Main Flow:**

1. PlayerController đọc input bằng `Input.GetAxisRaw` (legacy input API trong source).
2. Áp dụng stats cached/base speed và movement tuning.
3. Tính chuyển động tiến và ngang theo delta time/physics callbacks.
4. Clamp X vào giới hạn serialized/default.
5. Nếu đang ramp/airborne, state logic áp dụng movement dọc tương ứng.

**Alternative Flow:** A1. Input 0: movement ngang không đổi theo input. A2. Runtime stats rebuild: PlayerController cập nhật cache qua event.  
**Exception Flow:** E1. PlayerController disabled hoặc dependency/stats thiếu: behavior không xác minh.  
**Related Systems:** PlayerController (`Update`, `FixedUpdate`, RampState), PlayerRuntimeStats (`OnStatsRebuilt`), EquipmentManager, GameplayScene, InputManager settings.

## UC08 — Va chạm traffic/obstacle, bảo vệ và death pending

**Use Case ID:** UC08  
**Use Case Name:** Xử lý va chạm trong run  
**Primary Actor:** PlayerController  
**Supporting Actor:** GameManager, Shield/Photon, GameOver UI  
**Goal:** Phân loại hit, tiêu thụ bảo vệ nếu có hoặc chuyển sang death flow.  
**Trigger:** Unity collision/trigger callbacks.  
**Preconditions:** Colliders/layers/tags cấu hình đúng; run active.  
**Postconditions:** Có thể bị knockback/protected, hoặc GameManager vào death pending.

**Main Flow:**

1. `OnCollisionEnter`/`OnTriggerEnter` phân loại object/collider.
2. PlayerController kiểm tra ramp/Photon/Shield/protection và nhánh đặc biệt.
3. Nếu không được chặn, player áp dụng death physics/knockback và state cần thiết.
4. PlayerController gọi `GameManager.BeginDeathPending`.
5. GameManager phát event để GameOverUI cập nhật lựa chọn/đếm thời gian.

**Alternative Flow:** A1. Shield còn charge: tiêu thụ charge và áp dụng response thay death theo điều kiện của source. A2. Photon/ramp/invulnerability guard thỏa: collision có thể được bỏ qua/đổi nhánh.  
**Exception Flow:** E1. Thiếu tag/collider reference hoặc object classification không khớp: kết quả runtime không rõ. E2. Timing của collision/scene serialization chưa Play Mode test.  
**Related Systems:** PlayerController (`OnCollisionEnter`, `OnTriggerEnter`, `TryConsumeShield`, death methods), GameManager, ShieldController/PhotonController, GameOverUIController.

## UC09 — Thu thập coin và power-up

**Use Case ID:** UC09  
**Use Case Name:** Nhặt coin/power-up  
**Primary Actor:** Người chơi  
**Supporting Actor:** Coin/Gum/Magnet/Shield/Photon, ItemManager, GameManager, HUD  
**Goal:** Cập nhật coin run hoặc kích hoạt hiệu ứng item tương ứng.  
**Trigger:** Player vào collider/trigger pickup hoặc item activation path.  
**Preconditions:** Pickup/spawner hoạt động, collider và player recognition khớp.  
**Postconditions:** Coin counter/event hoặc trạng thái effect/controller thay đổi theo item code.

**Main Flow:**

1. Road/biome spawner đặt pickup ở vị trí được chọn.
2. Player overlap/collision với pickup.
3. Script item xác thực player/callback.
4. Coin cộng vào run counter; Gum/Photon/Shield/Magnet đi vào ItemManager/controller tương ứng.
5. HUD nhận event/refresh; pickup được deactivate/destroy tùy implementation.

**Alternative Flow:** A1. Pickup không nhận diện Player: bỏ qua. A2. ItemManager/controller missing: item flow không hoàn tất; không khẳng định recovery.  
**Exception Flow:** E1. Spawn/config/collider runtime chưa kiểm chứng. E2. Magnet duration/radius và toàn bộ effect duration không kết luận trong use case tổng quát.  
**Related Systems:** Coin, CoinSpawner, Gum/Photon/Shield/Magnet scripts/controllers/spawners, ItemManager, GameManager, GameplayItemHUD.

## UC10 — Road streaming và traffic AI

**Use Case ID:** UC10  
**Use Case Name:** Sinh đường và điều khiển traffic rule-based  
**Primary Actor:** RoadManager (system actor)  
**Supporting Actor:** TrafficVehicle, TrafficCarBehavior, CoinSpawner, Unity Random  
**Goal:** Duy trì đường phía trước và traffic trong chunk.  
**Trigger:** Gameplay start, forward coverage check, player tiến, cleanup/revive reset.  
**Preconditions:** Road/biome prefabs và serialized settings hợp lệ.  
**Postconditions:** Road/traffic instances active trong vùng stream; cũ phía sau có thể bị destroy.

**Main Flow:**

1. RoadManager chọn biome prefab theo weight/random.
2. SpawnRoad tạo road/chunk và xác định Z/length.
3. EnsureForwardCoverage lặp đến khi đủ coverage.
4. Traffic candidates chọn lane/Z; gap/safety checks so với active traffic.
5. TrafficVehicle di chuyển; TrafficCarBehavior scan traffic trước/safe lanes.
6. AI chọn tiếp tục, brake hoặc đổi lane; cooldown/stuck recovery áp dụng.
7. Despawn cleanup road/traffic phía sau.

**Alternative Flow:** A1. Candidate lane không an toàn: thử lựa chọn khác hoặc fallback theo code. A2. Biome prefab thiếu: fallback road path nếu có.  
**Exception Flow:** E1. Không có prefab hợp lệ: log/lỗi, không thể duy trì road. E2. Random seed/replay không được thiết lập trong source audit. E3. Runtime collision/traffic density chưa kiểm chứng.  
**Related Systems:** RoadManager, RoadChunk, TrafficVehicle, TrafficCarBehavior, CoinSpawner, GameplayScene.

## UC11 — Ramp/Heaven

**Use Case ID:** UC11  
**Use Case Name:** Đi vào ramp và Heaven biome  
**Primary Actor:** Người chơi  
**Supporting Actor:** PlayerController, RoadManager, HeavenBiomeController, CoinSpawner  
**Goal:** Thực hiện ramp state/launch và chuyển tiếp sang vùng Heaven theo điều kiện scene/gameplay.  
**Trigger:** Player gặp marker/surface/ramp trigger.  
**Preconditions:** Ramp geometry/collider/markers và references tồn tại.  
**Postconditions:** Local RampState/vertical movement/Heaven road coverage có thể thay đổi; không khẳng định mọi prefab map đã được kiểm tra.

**Main Flow:**

1. PlayerController nhận marker/trigger và xác định ramp start/end/surface.
2. Chuyển local state từ None sang Climbing/Launching theo logic.
3. Raycast/surface logic điều khiển bám ramp và launch.
4. Heaven entry/transition thay đổi RoadManager state/coverage/visual blend.
5. CoinSpawner có nhánh Heaven coin spawn.
6. Sau transition/landing, state được khôi phục theo code path.

**Alternative Flow:** A1. Thiếu marker hoặc surface: nhánh ramp không khởi động đầy đủ.  
**Exception Flow:** E1. Ramp visuals/collider alignment chưa Play Mode verified. E2. Không đưa ra kết luận về mọi road prefab.  
**Related Systems:** PlayerController (RampState), RoadManager Heaven coverage/transition, HeavenBiomeController, CoinSpawner, scene/prefab road.

## UC12 — Tử vong, revive hoặc Game Over

**Use Case ID:** UC12  
**Use Case Name:** Xử lý death pending và revive  
**Primary Actor:** Người chơi  
**Supporting Actor:** GameManager, PlayerController, RoadManager, GameOverUIController, gem data  
**Goal:** Trong cửa sổ pending, thử revive nếu đủ điều kiện; nếu không kết thúc run.  
**Trigger:** Death pending từ collision hoặc timer/state path.  
**Preconditions:** GameManager/player/UI refs; revive settings và currency state sẵn sàng.  
**Postconditions:** Revive khôi phục player/world/currency nếu thành công; nếu hết thời gian/không revive thì Game Over finalized.

**Main Flow:**

1. GameManager.BeginDeathPending lưu/đặt state và mở thời gian lựa chọn.
2. UI hiển thị trạng thái revive/gameover.
3. Người chơi chọn revive; UI gọi `TryRevive`.
4. GameManager kiểm tra khả năng/cost/count/time/gems theo code.
5. PlayerController phục hồi physics/collider/invulnerability; RoadManager.ResetWorldForRevive tái tạo road/traffic coverage.
6. GameManager cập nhật run state và event.

**Alternative Flow:** A1. Người chơi không revive: hết timer thì FinalizeGameOver. A2. Currency/điều kiện không đủ: TryRevive trả false và run tiếp tục pending/finalize theo timer.  
**Exception Flow:** E1. Road reset không có prefab hợp lệ: revive reset fail. E2. UI/state/currency serialized behavior runtime chưa kiểm tra.  
**Related Systems:** GameManager (`BeginDeathPending`, `TryRevive`, `FinalizeGameOver`), PlayerController (`RevivePlayer`), RoadManager (`ResetWorldForRevive`), GameOverUIController, PlayerData/Firestore gems.

## UC13 — Xem Game Over và tổng kết run

**Use Case ID:** UC13  
**Use Case Name:** Kết thúc lượt chơi  
**Primary Actor:** GameManager  
**Supporting Actor:** GameOverUIController, MissionManager, PlayerProgression, Firestore data services  
**Goal:** Chốt score/currency/run outcome và trình bày UI.  
**Trigger:** Death pending hết hạn hoặc finalize path.  
**Preconditions:** Active run và GameManager state hợp lệ.  
**Postconditions:** Run finalized; UI được thông báo; persistence phụ thuộc các subscriber/manager và backend thành công.

**Main Flow:**

1. GameManager gọi FinalizeGameOver một lần theo guard flags.
2. Tính/chốt run counters và phát gameover event.
3. UI đọc score/coin/best score state để hiển thị.
4. Mission/progression listeners cập nhật logic theo event.
5. Các manager có thể lưu fields qua Firestore methods.

**Alternative Flow:** A1. Player data/Firebase không sẵn sàng: local UI/state vẫn có thể đổi, nhưng persistence không được xác nhận.  
**Exception Flow:** E1. Listener hoặc write remote lỗi: result persistence unknown.  
**Related Systems:** GameManager, GameOverUIController, MissionManager, PlayerProgression, FirestorePlayerDataManager.

## UC14 — Mua item trong Shop

**Use Case ID:** UC14  
**Use Case Name:** Mua item bằng Gold/Gem  
**Primary Actor:** Người chơi  
**Supporting Actor:** MainMenuShopUI, GameItemDatabase, InventoryOwnership, GameManager/Firestore  
**Goal:** Mua item đủ điều kiện theo giá/currency ở client.  
**Trigger:** Chọn item và nhấn Buy.  
**Preconditions:** Item tồn tại, đúng category/page, purchasable, chưa sở hữu; GameManager/player data khả dụng.  
**Postconditions:** Nếu client path thành công, currency/ownership được cập nhật theo manager; server atomicity chưa xác minh.

**Main Flow:**

1. UI tìm GameItemData cho selected ID.
2. Validate category/page, purchasable và ownership.
3. Chọn nhánh Gold hoặc Gem theo `GameItemData.Currency`.
4. Kiểm tra đủ số dư.
5. Trừ currency và cấp ownership qua client managers/Firestore calls.
6. Refresh UI và quyền equip.

**Alternative Flow:** A1. Item đã sở hữu: chỉ refresh trạng thái owned. A2. Không đủ currency: dừng purchase. A3. Item không purchasable/sai category: dừng.  
**Exception Flow:** E1. GameManager/data manager missing: dừng. E2. Remote write hoặc giao dịch đồng thời thất bại: không có bằng chứng transaction server-side/rollback.  
**Related Systems:** MainMenuShopUI (`TryBuySelectedItem`, currency branch), GameItemData/Database, InventoryOwnership, GameManager, FirestorePlayerDataManager, MainMenu scene.

## UC15 — Xem inventory và equip item

**Use Case ID:** UC15  
**Use Case Name:** Trang bị item sở hữu  
**Primary Actor:** Người chơi  
**Supporting Actor:** InventoryUI, EquipmentManager, PlayerVisualManager/binders, PlayerRuntimeStats  
**Goal:** Trang bị item hợp lệ theo category/ownership và cập nhật visual/stats.  
**Trigger:** Chọn item trong Inventory và nhấn Equip.  
**Preconditions:** PlayerData/equipment manager ready; item database/ownership có ID.  
**Postconditions:** Equipped ID/event/state được lưu; visual/stat update tùy binder subscribers.

**Main Flow:**

1. Inventory UI dựng danh sách từ data và ownership.
2. Người chơi chọn item.
3. UI gọi EquipmentManager.TryEquip.
4. Manager kiểm tra item/category/ownership.
5. Manager cập nhật equipped ID và persistence.
6. Event EquipmentChanged/ItemEquipped báo cho visual binders/garage/stat system.

**Alternative Flow:** A1. Item chưa sở hữu hoặc sai category: TryEquip từ chối. A2. Manager chưa ready: UI chờ/không thực hiện.  
**Exception Flow:** E1. Visual prefab mapping thiếu: equipped data có thể đổi nhưng render mapping chưa đảm bảo. E2. Firestore write failure chưa runtime verified.  
**Related Systems:** InventoryUI/InventoryItemCard, EquipmentManager, InventoryOwnership, GameItemDatabase/VisualDatabase, PlayerVisualManager/category binders, PlayerRuntimeStats.

## UC16 — Nâng cấp item

**Use Case ID:** UC16  
**Use Case Name:** Nâng cấp Gum/Photon/Shield/Magnet  
**Primary Actor:** Người chơi  
**Supporting Actor:** ItemUpgradeUI, ItemUpgradeManager, ItemUpgradeData, Firestore, PlayerRuntimeStats  
**Goal:** Nâng level một loại upgrade nếu đủ currency/level constraints.  
**Trigger:** Chọn Upgrade trên UI.  
**Preconditions:** Upgrade manager/data ready; cấp còn dưới max; currency đủ.  
**Postconditions:** Level/currency được ghi qua manager; event refresh UI/runtime.

**Main Flow:**

1. UI đọc upgrade level, cost/max từ ItemUpgradeManager/data.
2. Manager kiểm tra `CanUpgrade` và `CanAffordUpgrade`.
3. UI gửi request upgrade.
4. Manager trừ currency, tăng level và lưu fields qua Firestore logic.
5. Event kết quả/level/currency cập nhật UI và runtime stats.

**Alternative Flow:** A1. Không đủ tiền: manager từ chối. A2. Max level hoặc operation đang chạy: không nhận request.  
**Exception Flow:** E1. Remote save fail: kết quả persistence cần xác minh; không mô tả transaction rollback chưa được chứng minh.  
**Related Systems:** ItemUgradeUI, ItemUpgradeManager, ItemUpgradeData assets, PlayerData, FirestorePlayerDataManager, PlayerRuntimeStats.

## UC17 — Mission và claim reward

**Use Case ID:** UC17  
**Use Case Name:** Theo dõi và nhận thưởng nhiệm vụ  
**Primary Actor:** Người chơi  
**Supporting Actor:** MissionUIController, MissionManager, GameManager, FirestorePlayerDataManager  
**Goal:** Cập nhật progress theo sự kiện gameplay và claim khi hoàn thành.  
**Trigger:** Gameplay event (score/coin/run/gameover) hoặc nhấn Claim.  
**Preconditions:** Mission data được khởi tạo; player data ready cho persistence.  
**Postconditions:** MissionSaveData progress/claimed được cập nhật theo manager; reward persistence phụ thuộc write thành công.

**Main Flow:**

1. MissionManager load/chọn daily mission data.
2. GameManager events được MissionManager nhận.
3. Progress được cập nhật theo mission rule.
4. UI hiển thị trạng thái hoàn thành/claimable.
5. Người chơi claim; MissionManager kiểm tra điều kiện và xử lý reward/save.

**Alternative Flow:** A1. Mission chưa hoàn thành: claim từ chối. A2. Đã claim: không cấp lần nữa theo claimed state.  
**Exception Flow:** E1. Date/timezone reset semantics và backend authority chưa kiểm chứng. E2. Mission configuration/runtime behavior chưa Play Mode verified.  
**Related Systems:** MissionManager (`ClaimMission`, event handlers), MissionSaveData/PlayerData, MissionUIController/MissionCardUI, GameManager, FirestorePlayerDataManager.

## UC18 — Xem leaderboard

**Use Case ID:** UC18  
**Use Case Name:** Tải leaderboard  
**Primary Actor:** Người chơi  
**Supporting Actor:** LeaderboardUI, LeaderboardManager, Firestore  
**Goal:** Đọc các entry leaderboard từ collection client-side.  
**Trigger:** Mở leaderboard UI/load action.  
**Preconditions:** Firebase/Firestore initialized và UI references có mặt.  
**Postconditions:** UI nhận loaded rows hoặc failure event.

**Main Flow:**

1. LeaderboardUI yêu cầu LeaderboardManager load.
2. Manager tạo query collection `leaderboard` với giới hạn cấu hình (default source value 100).
3. Firestore trả snapshot.
4. Manager chuyển document thành `LeaderboardEntry`.
5. OnLeaderboardLoaded báo UI dựng rows.

**Alternative Flow:** A1. Snapshot rỗng: UI hiển thị trạng thái không có kết quả theo logic UI.  
**Exception Flow:** E1. Network/rules/query lỗi: OnLeaderboardLoadFailed; ranking backend consistency chưa xác minh.  
**Related Systems:** LeaderboardManager, LeaderboardEntry, LeaderboardUI/RowUI, FirestorePlayerDataManager writes, Firestore collection.

## UC19 — Thay đổi Settings

**Use Case ID:** UC19  
**Use Case Name:** Cấu hình âm thanh/gameplay settings  
**Primary Actor:** Người chơi  
**Supporting Actor:** MainMenuSettingsUI, GameplaySettingsUI, PlayerPrefs  
**Goal:** Thay đổi tùy chọn cục bộ có trong UI.  
**Trigger:** Toggle setting.  
**Preconditions:** Settings UI active.  
**Postconditions:** Một số tùy chọn được lưu qua PlayerPrefs; phạm vi cài đặt chính xác theo controller.

**Main Flow:**

1. UI đọc/khởi tạo giá trị toggle.
2. Người chơi bật/tắt master/music/SFX hoặc setting gameplay hiện có.
3. Controller cập nhật local behavior/value.
4. Các giá trị được ghi PlayerPrefs ở những nhánh source có persistence.

**Alternative Flow:** A1. Không có PlayerPrefs key: dùng default trong code/UI.  
**Exception Flow:** E1. Không khẳng định mọi setting đồng bộ giữa menu/gameplay hoặc lưu cloud.  
**Related Systems:** MainMenuSettingsUI, GameplaySettingsUI, PlayerPrefs, MainMenu/Gameplay scenes.

## UC20 — Kiểm tra/tải game update

**Use Case ID:** UC20  
**Use Case Name:** Kiểm tra update qua client updater  
**Primary Actor:** GameUpdateUI/UpdateManager  
**Supporting Actor:** IUpdateProvider, JsonUpdateProvider, UpdateDownloader, package verifier  
**Goal:** Đọc manifest/tải và xác minh package theo code được cài đặt.  
**Trigger:** Update UI/manager check theo cấu hình.  
**Preconditions:** UpdateConfig endpoint có giá trị, mạng và filesystem hợp lệ.  
**Postconditions:** Manager có thể báo trạng thái/update; release endpoint/production deployment chưa xác minh.

**Main Flow:**

1. UI/UpdateManager lấy config/provider.
2. Provider đọc manifest JSON.
3. Version/package metadata được so sánh.
4. Downloader tải package nếu flow yêu cầu.
5. Verifier kiểm tra manifest/hash/giới hạn theo source.
6. UI nhận kết quả.

**Alternative Flow:** A1. Không có update mới: UI báo hiện hành theo logic manager.  
**Exception Flow:** E1. Endpoint/network/hash/package lỗi: dừng và báo failure. E2. Cài đặt thực tế phụ thuộc Tools launcher/updater; integration với build phát hành hiện tại chưa xác nhận.  
**Related Systems:** UpdateManager, IUpdateProvider, JsonUpdateProvider, UpdateDownloader, UpdateManifest/PackageVerifier, GameUpdateUI, Assets/Settings/Update.

## Quy ước cho kiểm thử

Test case trong báo cáo chính là kế hoạch kiểm thử dựa trên use case. `Actual Result` ghi “Chưa xác minh bằng Play Mode.” và `Status` là `Pending` trừ khi có runtime evidence được cung cấp. Audit này không chạy test.

