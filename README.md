# LEAD KHÔNG PHANH

> **Đường đông, xe đông, khe hở thì ít — né được bao lâu tùy tay lái của bạn.**

[![Unity](https://img.shields.io/badge/Unity-6000.0.82f1-black?logo=unity)](ProjectSettings/ProjectVersion.txt)
[![C%23](https://img.shields.io/badge/C%23-L%E1%BA%ADp%20tr%C3%ACnh%20Unity-239120?logo=csharp)](Assets/Scripts)
[![URP](https://img.shields.io/badge/Render%20Pipeline-URP-5C2D91)](Packages/manifest.json)

**LEAD KHÔNG PHANH** là game arcade né giao thông 3D do nhóm sinh viên phát triển bằng Unity 6 (Editor `6000.0.82f1`). Người chơi điều khiển xe len qua dòng phương tiện trên đường phố Việt Nam, nhặt xu và cố gắng kéo dài quãng chạy. Dự án đang trong quá trình phát triển; README này không xác nhận game đã chạy ổn định hoặc đã build thành công.

## Giới thiệu

Mục tiêu rất dễ hiểu: chạy qua dòng xe hỗn loạn, tránh va chạm và lập điểm số cao hơn. Source hiện có điều khiển theo làn, tạo đường liên tục, xe giao thông có thể đổi làn, vật phẩm nhặt được và tuyến đường qua dốc lên “Heaven”. Đường có thể uốn cong trên màn hình; còn khoảng trống tiếp theo thì người chơi phải tự tìm.

## Tính năng gameplay

### Đã có trong source

- **Điều khiển và va chạm người chơi** — `PlayerController` đọc input ngang, di chuyển người chơi và xử lý va chạm, bảo vệ, trạng thái tử nạn và tương tác hồi sinh.
- **Tạo đường liên tục** — `RoadManager` sinh và dọn các đoạn đường, chọn thành phần đường theo trọng số và duy trì phần đường phía trước.
- **Sinh xe và hành vi giao thông** — xe được sinh theo đoạn đường; `TrafficVehicle` điều khiển chuyển động, còn `TrafficCarBehavior` kiểm tra xe xung quanh, đổi làn và xử lý khi bị kẹt.
- **Điểm số và trạng thái lượt chơi** — `GameManager` quản lý lượt chơi, điểm, trạng thái chờ tử nạn, hồi sinh và kết thúc lượt.
- **Xu và khung vật phẩm** — có code và thành phần scene cho xu, Gum, Magnet, Shield và Photon. Mức độ hoàn thiện từng hiệu ứng và hành vi runtime chưa được xác minh đầy đủ.
- **Đường dốc và tuyến Heaven** — code người chơi và đường có xử lý phóng xe qua dốc và biome Heaven.
- **Menu và giao diện trong game** — Build Settings có các scene boot, đăng nhập, menu chính, gameplay và loading; script gameplay có HUD, tạm dừng/cài đặt, game over và hồi sinh.
- **Tích hợp Firebase phía client** — có khởi tạo dependency Firebase, xác thực, dữ liệu người chơi trên Firestore và code client cho bảng xếp hạng. Chưa xác minh backend Firebase đang hoạt động.

### Đang phát triển hoặc chưa xác minh runtime

Inventory, trang bị và hình ảnh trang bị, nâng cấp vật phẩm, shop, nhiệm vụ, tiến trình người chơi, vòng quay may mắn, xe giao thông đặc biệt và các luồng Firebase có code hoặc dữ liệu trong repository. Việc nối các hệ thống từ đầu đến cuối, lưu dữ liệu, an toàn giao dịch và hoạt động backend chưa được xác nhận khi chạy game. Hãy xem đây là các phần đang phát triển, không phải tính năng đã được xác nhận cho bản phát hành.

## Ảnh và video gameplay

Chưa tìm thấy ảnh chụp hoặc video gameplay phù hợp để giới thiệu trong README. **TODO:** bổ sung ảnh chụp hoặc video gameplay thật vào repository rồi liên kết tại đây.

## Công nghệ

- Unity Editor `6000.0.82f1` (Unity 6)
- C# cho gameplay và công cụ Editor
- Universal Render Pipeline (URP) `17.0.4`
- Unity Input System package `1.19.0`; điều khiển người chơi hiện đọc trục `Horizontal` của Input Manager cũ. Cả hai backend input đều được bật trong Project Settings.
- Firebase Unity SDK cho Firebase App, Authentication và Cloud Firestore
- Unity UI (uGUI), TextMesh Pro và Unity Test Framework

## Cấu trúc dự án

```text
Assets/
├── Scenes/                 # Boot, Login, MainMenu, GameplayScene, Loading
├── Scripts/
│   ├── Core/               # GameManager, trạng thái lượt chơi, dữ liệu người chơi
│   ├── Player/             # Điều khiển, chỉ số và hiển thị người chơi
│   ├── Road/               # Đoạn đường, tạo đường liên tục, biome và dốc
│   ├── Traffic/            # Chuyển động và hành vi đổi làn của xe
│   ├── Enemy/              # Bộ điều khiển và bộ sinh xe đặc biệt
│   ├── Pickups/            # Xu và bộ điều khiển/bộ sinh vật phẩm
│   ├── UI/                 # Boot, đăng nhập, menu, gameplay và loading
│   ├── Firebase/           # Client xác thực, Firestore và bảng xếp hạng
│   └── LuckyWheel/, VFX/, Update/ # Gameplay và công cụ hỗ trợ
├── Data/, GameData/        # ScriptableObject và dữ liệu game/vật phẩm
├── Prefabs/, Model/        # Prefab và model nguồn/đã import
├── Materials/, UI/, VFX/   # Vật liệu, giao diện và hiệu ứng hình ảnh
├── Effect/                 # Shader, âm thanh và hiệu ứng khác
├── Tests/                  # Unity Editor tests, gồm test liên quan updater
└── Editor/                 # Công cụ thiết lập và kết nối asset trong Editor
Packages/                   # Manifest và lockfile của Unity packages
ProjectSettings/            # Phiên bản Unity, scene, input, quality và player settings
Tools/                      # Tiện ích .NET launcher/updater riêng và các bài test
```

`GameManagerPlayerBinder` kết nối gameplay scene với `GameManager`; `RoadManager` tạo đường và sinh xe; chỉ số người chơi có thể được dựng lại từ dữ liệu trang bị. Các client Firebase kết nối trạng thái đăng nhập với hồ sơ Firestore và truy vấn bảng xếp hạng. Đây là các luồng có thể thấy trong code, không phải xác nhận rằng mọi tham chiếu scene hoặc dịch vụ từ xa đều hoạt động khi chạy.

## Bắt đầu

1. Cài [Git LFS](https://git-lfs.com/) và khởi tạo một lần:

   ```bash
   git lfs install
   ```

2. Clone repository và tải các asset được quản lý bằng LFS:

   ```bash
   git clone https://github.com/xduc199/LEAD-KHONG-PHANH.git
   cd LEAD-KHONG-PHANH
   git lfs pull
   ```

3. Cài Unity Editor **6000.0.82f1**. Mở thư mục project vừa clone bằng Unity Hub và chờ Unity import asset, tải package.
4. Scene đầu tiên được bật trong `ProjectSettings/EditorBuildSettings.asset` là `Assets/Scenes/Boot.unity`. Mở scene này và nhấn Play để đi theo luồng khởi động được cấu hình.
5. Repository có Firebase client và file cấu hình Firebase cho desktop. Các tính năng đăng nhập và Firestore cần một Firebase project có cấu hình client/provider và backend rules phù hợp. Repository không có Firebase security rules đã được xác minh hoặc hướng dẫn cấu hình production; clone source không đảm bảo dịch vụ từ xa hoạt động.

Dự án dùng Git LFS cho các loại binary asset như model, ảnh, âm thanh và một số binary Firebase. Nếu clone mà chưa tải object LFS, một số file có thể chỉ là pointer thay vì asset dùng được.

## Điều khiển

- **Rẽ trái/phải:** phím mũi tên trái/phải hoặc A/D theo trục `Horizontal` đã cấu hình trong Unity.
- **Tạm dừng/tiếp tục:** Escape (được xử lý bởi pause controller trong gameplay).

Đây là các phím được xác minh từ code và cấu hình. Chưa xác minh điều khiển cảm ứng hoặc tay cầm. `R` là phím debug hồi sinh được khai báo trong `GameManager`, không được giới thiệu như điều khiển thông thường.

## Build

`ProjectSettings` có cấu hình player cho Android, iOS và Standalone; cả năm scene đều được bật trong Build Settings. Điều này chỉ xác nhận cấu hình dự án, không xác nhận build hoặc bản phát hành đã chạy thành công. README này không ghi nhận artifact build hay kiểm thử runtime nào.

Để tạo bản build cục bộ, mở **File → Build Profiles** (hoặc giao diện Build Settings tương ứng trong Unity 6), chọn target đã cấu hình, kiểm tra player settings và danh sách scene rồi build. Tùy target, có thể cần SDK nền tảng, thông tin ký ứng dụng và cấu hình Firebase/provider/backend. Chỉ dựa vào settings thì chưa thể xem target nào là sẵn sàng phát hành.

## Trạng thái phát triển

**Đang phát triển.** Code dự án có các hệ thống cốt lõi về lượt chơi, tạo đường, giao thông, va chạm và trạng thái run. Vật phẩm, tiến trình, inventory/shop, nhiệm vụ, xe đặc biệt và luồng online mới được triển khai một phần hoặc chưa xác minh đầy đủ khi chạy. Chưa có tuyên bố về phát hành công khai, benchmark hiệu năng hay build nền tảng thành công.

## Nhóm phát triển

Dự án được phát triển theo nhóm sinh viên. Repository và tài liệu hiện có chưa cung cấp tên thành viên hoặc phân công vai trò đã xác minh để công khai.

## Đóng góp

Hãy tạo branch riêng và pull request với phạm vi thay đổi rõ ràng. Trước khi gửi, kiểm tra scene và tham chiếu prefab bị ảnh hưởng trong Unity, xác nhận package cần thiết, đồng thời tuân thủ `.gitignore`, `.gitattributes` và quy tắc Git LFS. Không commit các thư mục Unity tự sinh như `Library`, `Temp`, `Obj`, `Logs` hoặc `UserSettings` cục bộ.

## Giấy phép

Repository chưa có file LICENSE ở cấp dự án, vì vậy giấy phép của project chưa được chỉ định. Hãy liên hệ người quản lý repository trước khi phân phối lại hoặc sử dụng nội dung của dự án. Giấy phép đi kèm asset bên thứ ba chỉ áp dụng cho các asset đó, không xác lập giấy phép cho toàn bộ project.
