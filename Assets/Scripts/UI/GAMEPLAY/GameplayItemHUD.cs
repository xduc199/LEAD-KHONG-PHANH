using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameplayItemHUD : MonoBehaviour
{
    // ============================================================
    // CONSTANTS
    // ============================================================

    private const int ItemCount = 4;

    // ============================================================
    // COLORS
    // ============================================================

    private static readonly Color GumColor =
        new Color(0.10f, 1.00f, 0.25f, 1f);

    private static readonly Color PhotonColor =
        new Color(1.00f, 0.88f, 0.00f, 1f);

    private static readonly Color ShieldColor =
        new Color(0.08f, 0.55f, 1.00f, 1f);

    private static readonly Color MagnetColor =
        new Color(1.00f, 0.10f, 0.14f, 1f);

    // ============================================================
    // SLOT DATA
    // ============================================================

    private sealed class Slot
    {
        public UpgradeItemType type;

        public GameObject root;

        public RectTransform fillRect;

        public TMP_Text timeText;

        public Image accentImage;
        public Image iconBackgroundImage;
        public Image barBackgroundImage;
        public Image fillImage;

        public bool active;

        public float maxTime;
    }

    // ============================================================
    // INSPECTOR
    // ============================================================

    [Header("Slots")]
    [SerializeField] private GameObject gumSlot;
    [SerializeField] private GameObject photonSlot;
    [SerializeField] private GameObject shieldSlot;
    [SerializeField] private GameObject magnetSlot;

    // ============================================================
    // RUNTIME
    // ============================================================

    private Slot[] slots;

    /*
     * activationOrder:
     *
     * [0] = item CŨ NHẤT = nằm dưới cùng
     * [1] = item tiếp theo
     * [2] = item tiếp theo
     * [3] = item MỚI NHẤT = nằm trên cùng
     */
    private readonly UpgradeItemType[] activationOrder =
        new UpgradeItemType[ItemCount];

    private int activeCount;

    private ItemManager itemManager;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        itemManager = ItemManager.Instance;

        slots = new Slot[ItemCount];

        CreateSlot(
            0,
            UpgradeItemType.Gum,
            gumSlot
        );

        CreateSlot(
            1,
            UpgradeItemType.Photon,
            photonSlot
        );

        CreateSlot(
            2,
            UpgradeItemType.Shield,
            shieldSlot
        );

        CreateSlot(
            3,
            UpgradeItemType.Magnet,
            magnetSlot
        );

        activeCount = 0;

        HideAllSlotsImmediate();
    }

    private void Update()
    {
        if (itemManager == null)
        {
            itemManager = ItemManager.Instance;

            if (itemManager == null)
                return;
        }

        UpdateItem(
            UpgradeItemType.Gum,
            itemManager.IsGumActive,
            itemManager.GumTimeRemaining
        );

        UpdateItem(
            UpgradeItemType.Photon,
            itemManager.IsPhotonActive,
            itemManager.PhotonTimeRemaining
        );

        UpdateItem(
            UpgradeItemType.Shield,
            itemManager.IsShieldActive,
            itemManager.ShieldTimeRemaining
        );

        UpdateItem(
            UpgradeItemType.Magnet,
            itemManager.IsMagnetActive,
            itemManager.MagnetTimeRemaining
        );
    }

    // ============================================================
    // CREATE SLOT
    // ============================================================

    private void CreateSlot(
        int index,
        UpgradeItemType type,
        GameObject root
    )
    {
        if (root == null)
            return;

        Slot slot = new Slot();

        slot.type = type;
        slot.root = root;

        Transform timeTransform =
            root.transform.Find("Time");

        if (timeTransform != null)
        {
            slot.timeText =
                timeTransform.GetComponent<TMP_Text>();
        }

        Transform fillTransform =
            root.transform.Find("BarBackground/Fill");

        if (fillTransform != null)
        {
            slot.fillRect =
                fillTransform.GetComponent<RectTransform>();

            slot.fillImage =
                fillTransform.GetComponent<Image>();
        }

        Transform accentTransform =
            root.transform.Find("Accent");

        if (accentTransform != null)
        {
            slot.accentImage =
                accentTransform.GetComponent<Image>();
        }

        Transform iconBackgroundTransform =
            root.transform.Find("IconBackground");

        if (iconBackgroundTransform != null)
        {
            slot.iconBackgroundImage =
                iconBackgroundTransform.GetComponent<Image>();
        }

        Transform barBackgroundTransform =
            root.transform.Find("BarBackground");

        if (barBackgroundTransform != null)
        {
            slot.barBackgroundImage =
                barBackgroundTransform.GetComponent<Image>();
        }

        slot.active = false;
        slot.maxTime = 0f;

        slots[index] = slot;

        ConfigureSlotVisual(slot);
    }

    // ============================================================
    // UPDATE ITEM
    // ============================================================

    private void UpdateItem(
        UpgradeItemType type,
        bool isActive,
        float remaining
    )
    {
        Slot slot = GetSlot(type);

        if (slot == null)
            return;

        remaining = Mathf.Max(0f, remaining);

        if (isActive)
        {
            if (!slot.active)
            {
                ActivateSlot(slot, remaining);
            }

            UpdateSlotVisual(
                slot,
                remaining
            );
        }
        else
        {
            if (slot.active)
            {
                DeactivateSlot(slot);
            }
        }
    }

    // ============================================================
    // ACTIVATE
    // ============================================================

    private void ActivateSlot(
        Slot slot,
        float remaining
    )
    {
        slot.active = true;

        /*
         * Nếu slot đã tồn tại trong order thì loại bỏ trước.
         * Sau đó append vào CUỐI.
         *
         * Ví dụ:
         *
         * Photon
         *
         * -> Photon
         *    Magnet
         *
         * -> Photon
         *    Magnet
         *    Shield
         *
         * Item mới luôn nằm trên cùng.
         */

        RemoveFromActivationOrder(slot.type);

        if (activeCount < ItemCount)
        {
            activationOrder[activeCount] =
                slot.type;

            activeCount++;
        }

        /*
         * QUAN TRỌNG:
         *
         * remaining lúc activation chính là
         * thời lượng thực tế mà ItemManager đang có.
         *
         * Không hard-code duration.
         */

        slot.maxTime = remaining;

        if (slot.maxTime <= 0f)
        {
            slot.maxTime = 0.01f;
        }

        slot.root.SetActive(true);

        ConfigureSlotVisual(slot);

        RebuildVisualOrder();
    }

    // ============================================================
    // DEACTIVATE
    // ============================================================

    private void DeactivateSlot(Slot slot)
    {
        slot.active = false;
        slot.maxTime = 0f;

        /*
         * Xóa item khỏi activation order.
         */
        RemoveFromActivationOrder(slot.type);

        /*
         * Ẩn slot.
         */
        slot.root.SetActive(false);

        /*
         * Các item còn lại tự dồn xuống.
         */
        RebuildVisualOrder();
    }

    // ============================================================
    // TIMER / FILL
    // ============================================================

    private void UpdateSlotVisual(
        Slot slot,
        float remaining
    )
    {
        if (slot.maxTime <= 0f)
            return;

        float normalized =
            Mathf.Clamp01(
                remaining / slot.maxTime
            );

        /*
         * Không dùng Image.fillAmount.
         *
         * Fill chạy bằng chiều rộng.
         */
        if (slot.fillRect != null)
        {
            RectTransform parentRect =
                slot.fillRect.parent as RectTransform;

            if (parentRect != null)
            {
                float parentWidth =
                    parentRect.rect.width;

                float width =
                    parentWidth * normalized;

                slot.fillRect.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal,
                    width
                );
            }
        }

        if (slot.timeText != null)
        {
            slot.timeText.SetText(
                "{0:0.0}s",
                remaining
            );
        }
    }

    // ============================================================
    // ORDER
    // ============================================================

    private void RebuildVisualOrder()
    {
        /*
         * activationOrder:
         *
         * [0] = CŨ NHẤT
         * [1] = ...
         * [2] = ...
         * [3] = MỚI NHẤT
         *
         * Ta muốn:
         *
         * CŨ NHẤT = dưới
         * MỚI NHẤT = trên
         */

        for (int i = 0; i < activeCount; i++)
        {
            Slot slot =
                GetSlot(activationOrder[i]);

            if (slot == null)
                continue;

            if (!slot.active)
                continue;

            /*
             * Sibling index tăng dần.
             *
             * LayoutGroup sẽ dùng thứ tự này.
             *
             * Index 0 = thấp nhất
             * Index cuối = cao nhất
             */
            slot.root.transform.SetSiblingIndex(i);
        }

        /*
         * Không cho inactive slot chen vào vùng active.
         *
         * Chỉ thực hiện khi cần đảm bảo thứ tự hierarchy.
         */
        int inactiveIndex = activeCount;

        for (int i = 0; i < ItemCount; i++)
        {
            Slot slot = slots[i];

            if (slot == null)
                continue;

            if (slot.active)
                continue;

            slot.root.transform.SetSiblingIndex(
                inactiveIndex
            );

            inactiveIndex++;
        }
    }

    // ============================================================
    // REMOVE FROM ORDER
    // ============================================================

    private void RemoveFromActivationOrder(
        UpgradeItemType type
    )
    {
        for (int i = 0; i < activeCount; i++)
        {
            if (activationOrder[i] != type)
                continue;

            /*
             * Dồn toàn bộ item phía trên xuống.
             *
             * Ví dụ:
             *
             * Photon
             * Magnet
             * Shield
             *
             * Magnet hết:
             *
             * Photon
             * Shield
             */

            for (
                int j = i;
                j < activeCount - 1;
                j++
            )
            {
                activationOrder[j] =
                    activationOrder[j + 1];
            }

            activeCount--;

            return;
        }
    }

    // ============================================================
    // LOOKUP
    // ============================================================

    private Slot GetSlot(
        UpgradeItemType type
    )
    {
        for (int i = 0; i < ItemCount; i++)
        {
            Slot slot = slots[i];

            if (slot == null)
                continue;

            if (slot.type == type)
                return slot;
        }

        return null;
    }

    // ============================================================
    // VISUAL CONFIGURATION
    // ============================================================

    private void ConfigureSlotVisual(
        Slot slot
    )
    {
        if (slot == null)
            return;

        Color accentColor =
            GetItemColor(slot.type);

        /*
         * Accent
         */
        if (slot.accentImage != null)
        {
            slot.accentImage.color =
                accentColor;
        }

        /*
         * Icon background
         */
        if (slot.iconBackgroundImage != null)
        {
            Color iconBackgroundColor =
                accentColor;

            iconBackgroundColor.a = 0.22f;

            slot.iconBackgroundImage.color =
                iconBackgroundColor;
        }

        /*
         * Bar background
         *
         * Giữ màu xám/trắng.
         * KHÔNG dùng accent làm màu fill background.
         */
        if (slot.barBackgroundImage != null)
        {
            slot.barBackgroundImage.color =
                new Color(
                    0.68f,
                    0.70f,
                    0.74f,
                    1f
                );
        }

        /*
         * FILL
         *
         * Đây là màu thật của thanh thời gian.
         *
         * Runtime luôn ép lại màu tại đây.
         */
        if (slot.fillImage != null)
        {
            slot.fillImage.color =
                accentColor;

            slot.fillImage.type =
                Image.Type.Simple;

            slot.fillImage.raycastTarget =
                false;
        }
    }

    // ============================================================
    // COLOR LOOKUP
    // ============================================================

    private static Color GetItemColor(
        UpgradeItemType type
    )
    {
        switch (type)
        {
            case UpgradeItemType.Gum:
                return GumColor;

            case UpgradeItemType.Photon:
                return PhotonColor;

            case UpgradeItemType.Shield:
                return ShieldColor;

            case UpgradeItemType.Magnet:
                return MagnetColor;

            default:
                return Color.white;
        }
    }

    // ============================================================
    // RESET
    // ============================================================

    private void HideAllSlotsImmediate()
    {
        for (int i = 0; i < ItemCount; i++)
        {
            Slot slot = slots[i];

            if (slot == null)
                continue;

            slot.active = false;
            slot.maxTime = 0f;

            if (slot.root != null)
            {
                slot.root.SetActive(false);
            }
        }

        for (int i = 0; i < ItemCount; i++)
        {
            activationOrder[i] =
                default;
        }

        activeCount = 0;
    }
}