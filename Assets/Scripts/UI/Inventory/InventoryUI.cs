using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-80)]
public sealed class InventoryUI : MonoBehaviour
{
    // =========================================================
    // CATEGORY
    // =========================================================

    public enum InventoryCategory
    {
        Outfit = 0,
        Accessory = 1,
        Cosmetic = 2
    }


    // =========================================================
    // AUTHORITATIVE CATEGORY RANGES
    // =========================================================

    private const int OutfitStartId = 41;
    private const int OutfitEndId = 60;

    private const int CosmeticStartId = 61;
    private const int CosmeticEndId = 80;

    private const int AccessoryStartId = 81;
    private const int AccessoryEndId = 100;


    // =========================================================
    // UI REFERENCES
    // =========================================================

    [Header("Panel")]

    [SerializeField]
    private GameObject panelRoot;

    [SerializeField]
    private Button closeButton;


    // =========================================================
    // CATEGORY BUTTONS
    // =========================================================

    [Header("Category Buttons")]

    [SerializeField]
    private Button outfitButton;

    [SerializeField]
    private Button accessoryButton;

    [SerializeField]
    private Button cosmeticButton;


    // =========================================================
    // CATEGORY TEXT
    // =========================================================

    [Header("Category Text")]

    [SerializeField]
    private TMP_Text outfitButtonText;

    [SerializeField]
    private TMP_Text accessoryButtonText;

    [SerializeField]
    private TMP_Text cosmeticButtonText;

    [SerializeField]
    private TMP_Text categoryTitle;


    // =========================================================
    // ITEM GRID
    // =========================================================

    [Header("Item Grid")]

    [SerializeField]
    private Transform itemGrid;

    [SerializeField]
    private InventoryItemCard[] itemCards;

    [SerializeField]
    private ScrollRect itemScrollRect;


    // =========================================================
    // EMPTY STATE
    // =========================================================

    [Header("Empty State")]

    [SerializeField]
    private GameObject emptyStateRoot;

    [SerializeField]
    private TMP_Text emptyStateText;


    // =========================================================
    // SELECTED ITEM DETAIL
    // =========================================================

    [Header("Selected Item")]

    [SerializeField]
    private TMP_Text selectedItemName;

    [SerializeField]
    private TMP_Text selectedItemDescription;

    [SerializeField]
    private TMP_Text selectedItemState;


    // =========================================================
    // EQUIP BUTTON
    // =========================================================

    [Header("Equip Button")]

    [SerializeField]
    private Button equipButton;

    [SerializeField]
    private TMP_Text equipButtonText;


    // =========================================================
    // DATABASE
    // =========================================================

    [Header("Database")]

    [SerializeField]
    private GameItemDatabase itemDatabase;


    // =========================================================
    // EQUIPMENT
    // =========================================================

    [Header("Equipment")]

    [SerializeField]
    private EquipmentManager equipmentManager;


    // =========================================================
    // RUNTIME STATE
    // =========================================================

    private InventoryCategory currentCategory =
        InventoryCategory.Outfit;

    private int selectedItemId = -1;

    private InventoryCategory selectedItemCategory =
        InventoryCategory.Outfit;


    private bool initialized;

    private bool itemCardsResolved;

    private bool cardsInitialized;

    private bool databaseInitialized;

    private GameItemDatabase initializedDatabase;

    private EquipmentManager subscribedEquipmentManager;


    // =========================================================
    // EQUIPMENT SUBSCRIPTION
    // =========================================================

    private bool equipmentSubscribed;

    private bool equipmentReadySubscribed;


    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        Initialize();
    }


    private void OnEnable()
    {
        EnsureReferences();

        SubscribeEquipmentEvents();

        if (
            initialized &&
            IsPanelOpen()
        )
        {
            RefreshItems();
        }
    }


    private void Start()
    {
        EnsureReferences();

        SubscribeEquipmentEvents();

        if (!initialized)
        {
            Initialize();
        }
    }


    private void OnDisable()
    {
        UnsubscribeEquipmentEvents();
    }


    private void OnDestroy()
    {
        UnsubscribeEquipmentEvents();

        UnbindCategoryButtons();

        UnbindCloseButton();
    }


    // =========================================================
    // INITIALIZE
    // =========================================================

    public void Initialize()
    {
        if (initialized)
        {
            return;
        }


        EnsureReferences();

        EnsureDatabase();

        EnsureItemCards();

        InitializeCards();

        EnsureScrollRect();

        PrepareEquipButton();

        BindCategoryButtons();

        BindCloseButton();

        SubscribeEquipmentEvents();


        currentCategory =
            InventoryCategory.Outfit;

        selectedItemId =
            -1;

        selectedItemCategory =
            InventoryCategory.Outfit;


        UpdateCategoryTitle();

        UpdateCategoryVisuals();

        HideAllCards();

        ClearSelection();

        SetEmptyState(false);


        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }


        initialized =
            true;
    }


    // =========================================================
    // ENSURE REFERENCES
    // =========================================================

    private void EnsureReferences()
    {
        if (equipmentManager == null)
        {
            equipmentManager =
                EquipmentManager.Instance;
        }

        PrepareEquipButton();
    }


    // =========================================================
    // DATABASE
    // =========================================================

    private bool EnsureDatabase()
    {
        if (itemDatabase == null)
        {
            Debug.LogError(
                "[InventoryUI] " +
                "GameItemDatabase is NULL.",
                this);

            return false;
        }


        if (
            databaseInitialized &&
            initializedDatabase == itemDatabase
        )
        {
            return true;
        }


        itemDatabase.Initialize();


        initializedDatabase =
            itemDatabase;

        databaseInitialized =
            true;


        return true;
    }


    // =========================================================
    // ITEM CARDS
    // =========================================================

    private void EnsureItemCards()
    {
        if (
            itemCardsResolved &&
            itemCards != null &&
            itemCards.Length > 0
        )
        {
            return;
        }


        if (itemGrid == null)
        {
            Debug.LogError(
                "[InventoryUI] " +
                "Item Grid is NULL.",
                this);

            itemCards =
                new InventoryItemCard[0];

            itemCardsResolved =
                true;

            return;
        }


        itemCards =
            itemGrid.GetComponentsInChildren<
                InventoryItemCard>(
                    true);


        itemCardsResolved =
            true;


        Debug.Log(
            "[InventoryUI] " +
            "Resolved InventoryItemCards = " +
            (
                itemCards != null
                    ? itemCards.Length
                    : 0
            ),
            this);
    }


    // =========================================================
    // INITIALIZE CARDS
    // =========================================================

    private void InitializeCards()
    {
        if (cardsInitialized)
        {
            return;
        }


        if (itemCards == null)
        {
            return;
        }


        int count =
            itemCards.Length;


        for (
            int i = 0;
            i < count;
            i++
        )
        {
            InventoryItemCard card =
                itemCards[i];


            if (card == null)
            {
                continue;
            }


            card.Initialize(this);
        }


        cardsInitialized =
            true;
    }


    // =========================================================
    // OPEN
    // =========================================================

    public void Open()
    {
        if (!initialized)
        {
            Initialize();
        }


        EnsureReferences();

        EnsureDatabase();

        EnsureItemCards();

        InitializeCards();

        SubscribeEquipmentEvents();


        /*
         * Refresh ownership trước khi build card.
         *
         * Điều này đặc biệt quan trọng với:
         *
         * 1  = Default Vehicle
         * 41 = Ninja Lead / Default Outfit
         */
        InventoryOwnership.Refresh();


        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }


        currentCategory =
            InventoryCategory.Outfit;

        selectedItemId =
            -1;

        selectedItemCategory =
            InventoryCategory.Outfit;


        UpdateCategoryTitle();

        UpdateCategoryVisuals();

        ClearSelection();

        RefreshItems();

        ResetScroll();
    }


    // =========================================================
    // CLOSE
    // =========================================================

    public void Close()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        ClearSelection();
    }


    // =========================================================
    // REFRESH
    // =========================================================

    public void Refresh()
    {
        if (!initialized)
        {
            Initialize();
        }

        RefreshItems();
    }


    // =========================================================
    // CATEGORY
    // =========================================================

    public void SelectOutfit()
    {
        SelectCategory(
            InventoryCategory.Outfit);
    }


    public void SelectAccessory()
    {
        SelectCategory(
            InventoryCategory.Accessory);
    }


    public void SelectCosmetic()
    {
        SelectCategory(
            InventoryCategory.Cosmetic);
    }


    public void SelectCategory(
        InventoryCategory category)
    {
        if (!initialized)
        {
            Initialize();
        }


        if (currentCategory == category)
        {
            RefreshItems();
            return;
        }


        currentCategory =
            category;

        selectedItemId =
            -1;

        selectedItemCategory =
            category;


        UpdateCategoryTitle();

        UpdateCategoryVisuals();

        ClearSelection();

        RefreshItems();

        ResetScroll();
    }


    // =========================================================
    // SELECT ITEM
    // =========================================================

    public void SelectItem(
        int itemId,
        InventoryCategory category)
    {
        if (!initialized)
        {
            Initialize();
        }


        // =====================================================
        // VALID ID
        // =====================================================

        if (
            itemId <
            GameItemDatabase.MinItemId ||
            itemId >
            GameItemDatabase.MaxItemId
        )
        {
            Debug.LogWarning(
                "[InventoryUI] " +
                "SelectItem rejected: invalid ItemId = " +
                itemId,
                this);

            return;
        }


        // =====================================================
        // CATEGORY
        // =====================================================

        if (category != currentCategory)
        {
            Debug.LogWarning(
                "[InventoryUI] " +
                "SelectItem rejected: category mismatch.\n" +
                "Current = " +
                currentCategory +
                "\n" +
                "Received = " +
                category,
                this);

            return;
        }


        // =====================================================
        // OWNERSHIP
        // =====================================================

        if (!InventoryOwnership.IsOwned(itemId))
        {
            Debug.LogWarning(
                "[InventoryUI] " +
                "SelectItem rejected: item is not owned.\n" +
                "ItemId = " +
                itemId,
                this);

            return;
        }


        // =====================================================
        // DATABASE
        // =====================================================

        if (!EnsureDatabase())
        {
            return;
        }


        if (
            !itemDatabase.TryGet(
                itemId,
                out GameItemData item
            ) ||
            item == null
        )
        {
            Debug.LogWarning(
                "[InventoryUI] " +
                "SelectItem failed: Item not found.\n" +
                "ItemId = " +
                itemId,
                this);

            return;
        }


        // =====================================================
        // DATABASE CATEGORY
        // =====================================================

        InventoryCategory databaseCategory =
            ConvertCategory(
                item.Category);


        if (databaseCategory != category)
        {
            Debug.LogError(
                "[InventoryUI] " +
                "SelectItem rejected: database category mismatch.\n" +
                "ItemId = " +
                itemId +
                "\n" +
                "UI Category = " +
                category +
                "\n" +
                "Database Category = " +
                databaseCategory,
                this);

            return;
        }


        // =====================================================
        // AUTHORITATIVE SELECTION
        // =====================================================

        selectedItemId =
            itemId;

        selectedItemCategory =
            category;


        // =====================================================
        // UPDATE UI
        // =====================================================

        RefreshSelectedVisual();

        RefreshSelectedDetail();


        Debug.Log(
            "[InventoryUI] " +
            "ITEM SELECTED | " +
            "ItemId = " +
            selectedItemId +
            " | Name = " +
            item.ItemName +
            " | Category = " +
            selectedItemCategory,
            this);
    }


    // =========================================================
    // EQUIP SELECTED ITEM
    // =========================================================

    public void EquipSelectedItem()
    {
        // =====================================================
        // VALID SELECTION
        // =====================================================

        if (
            selectedItemId <
            GameItemDatabase.MinItemId
        )
        {
            Debug.LogWarning(
                "[InventoryUI] " +
                "Equip rejected: no item selected.",
                this);

            return;
        }


        if (
            selectedItemId >
            GameItemDatabase.MaxItemId
        )
        {
            Debug.LogWarning(
                "[InventoryUI] " +
                "Equip rejected: invalid ItemId = " +
                selectedItemId,
                this);

            return;
        }


        // =====================================================
        // OWNERSHIP
        // =====================================================

        if (!InventoryOwnership.IsOwned(
                selectedItemId))
        {
            Debug.LogWarning(
                "[InventoryUI] " +
                "Equip rejected: item is not owned.\n" +
                "ItemId = " +
                selectedItemId,
                this);

            return;
        }


        // =====================================================
        // DATABASE
        // =====================================================

        if (!EnsureDatabase())
        {
            return;
        }


        if (
            !itemDatabase.TryGet(
                selectedItemId,
                out GameItemData item
            ) ||
            item == null
        )
        {
            Debug.LogError(
                "[InventoryUI] " +
                "Equip rejected: ItemId not found.\n" +
                "ItemId = " +
                selectedItemId,
                this);

            return;
        }


        // =====================================================
        // CATEGORY
        // =====================================================

        GameItemCategory expectedCategory =
            ConvertCategoryToDatabase(
                selectedItemCategory);


        if (item.Category != expectedCategory)
        {
            Debug.LogError(
                "[InventoryUI] " +
                "Equip rejected: category mismatch.\n" +
                "ItemId = " +
                selectedItemId +
                "\n" +
                "Database Category = " +
                item.Category +
                "\n" +
                "Selected Category = " +
                expectedCategory,
                this);

            return;
        }


        // =====================================================
        // EQUIPMENT MANAGER
        // =====================================================

        EnsureReferences();


        if (equipmentManager == null)
        {
            equipmentManager =
                EquipmentManager.Instance;
        }


        if (equipmentManager == null)
        {
            Debug.LogError(
                "[InventoryUI] " +
                "EquipmentManager is NULL.",
                this);

            return;
        }


        // =====================================================
        // REQUEST IDENTITY
        // =====================================================

        int requestedItemId =
            selectedItemId;

        string requestedItemName =
            item.ItemName;


        // =====================================================
        // EQUIP
        // =====================================================

        bool success =
            equipmentManager.TryEquip(
                requestedItemId);


        if (!success)
        {
            Debug.LogWarning(
                "[InventoryUI] " +
                "Equip failed.\n" +
                "ItemId = " +
                requestedItemId +
                "\n" +
                "Name = " +
                requestedItemName,
                this);

            RefreshSelectedDetail();

            RefreshSelectedVisual();

            return;
        }


        // =====================================================
        // SUCCESS
        // =====================================================

        Debug.Log(
            "[InventoryUI] " +
            "EQUIPPED SUCCESS.\n" +
            "ItemId = " +
            requestedItemId +
            "\n" +
            "Name = " +
            requestedItemName,
            this);


        /*
         * Không reset selectedItemId.
         *
         * Item vừa equip vẫn là item đang selected.
         */

        RefreshItems();

        RefreshSelectedVisual();

        RefreshSelectedDetail();
    }


    // =========================================================
    // REFRESH ITEMS
    // =========================================================

    public void RefreshItems()
    {
        if (!initialized)
        {
            return;
        }


        EnsureReferences();

        if (!EnsureDatabase())
        {
            SetEmptyState(true);
            return;
        }


        EnsureItemCards();

        InitializeCards();


        if (
            itemCards == null ||
            itemCards.Length == 0
        )
        {
            SetEmptyState(true);
            return;
        }


        HideAllCards();


        // =====================================================
        // CATEGORY RANGE
        // =====================================================

        GetCategoryRange(
            currentCategory,
            out int startId,
            out int endId);


        // =====================================================
        // EQUIPMENT
        // =====================================================

        bool equipmentReady =
            equipmentManager != null &&
            equipmentManager.IsEquipmentReady;


        int cardIndex =
            0;

        int availableCount =
            0;


        // =====================================================
        // BUILD ITEM CARDS
        // =====================================================

        for (
            int id = startId;
            id <= endId;
            id++
        )
        {
            // -------------------------------------------------
            // OWNERSHIP
            // -------------------------------------------------

            if (!InventoryOwnership.IsOwned(id))
            {
                continue;
            }


            // -------------------------------------------------
            // DATABASE
            // -------------------------------------------------

            if (
                !itemDatabase.TryGet(
                    id,
                    out GameItemData item
                ) ||
                item == null
            )
            {
                Debug.LogWarning(
                    "[InventoryUI] " +
                    "Owned item not found in database.\n" +
                    "ItemId = " +
                    id,
                    this);

                continue;
            }


            // -------------------------------------------------
            // DATABASE CATEGORY
            // -------------------------------------------------

            InventoryCategory databaseCategory =
                ConvertCategory(
                    item.Category);


            if (databaseCategory != currentCategory)
            {
                Debug.LogWarning(
                    "[InventoryUI] " +
                    "Item skipped because database category " +
                    "does not match current category.\n" +
                    "ItemId = " +
                    id +
                    "\n" +
                    "Database Category = " +
                    item.Category +
                    "\n" +
                    "Current Category = " +
                    currentCategory,
                    this);

                continue;
            }


            // -------------------------------------------------
            // CARD CAPACITY
            // -------------------------------------------------

            if (cardIndex >= itemCards.Length)
            {
                Debug.LogError(
                    "[InventoryUI] " +
                    "Not enough InventoryItemCard objects.\n" +
                    "Current category = " +
                    currentCategory +
                    "\n" +
                    "ItemId = " +
                    id +
                    "\n" +
                    "Cards = " +
                    itemCards.Length,
                    this);

                break;
            }


            // -------------------------------------------------
            // GET CARD
            // -------------------------------------------------

            InventoryItemCard card =
                itemCards[cardIndex];


            if (card == null)
            {
                Debug.LogError(
                    "[InventoryUI] " +
                    "InventoryItemCard is NULL.\n" +
                    "Index = " +
                    cardIndex +
                    "\n" +
                    "ItemId = " +
                    id,
                    this);

                cardIndex++;
                continue;
            }


            cardIndex++;


            // -------------------------------------------------
            // EQUIPPED
            // -------------------------------------------------

            bool equipped =
                equipmentReady &&
                IsItemEquipped(
                    id,
                    currentCategory);


            // -------------------------------------------------
            // AUTHORITATIVE DATABASE DATA
            // -------------------------------------------------

            int bindItemId =
                item.ItemId;

            string bindItemName =
                item.ItemName;

            Sprite bindIcon =
                item.Icon;


            // -------------------------------------------------
            // DATABASE ID VALIDATION
            // -------------------------------------------------

            if (bindItemId != id)
            {
                Debug.LogError(
                    "[InventoryUI] " +
                    "DATABASE ID MISMATCH.\n" +
                    "Loop ID = " +
                    id +
                    "\n" +
                    "Item.ItemId = " +
                    bindItemId +
                    "\n" +
                    "Name = " +
                    bindItemName,
                    this);

                continue;
            }


            // -------------------------------------------------
            // BIND
            // -------------------------------------------------

            card.Bind(
                bindItemId,
                bindItemName,
                bindIcon,
                currentCategory,
                equipped);


            // -------------------------------------------------
            // SELECTED
            // -------------------------------------------------

            card.SetSelected(
                bindItemId ==
                selectedItemId);


            // -------------------------------------------------
            // ACTIVE
            // -------------------------------------------------

            card.gameObject.SetActive(true);


            availableCount++;
        }


        // =====================================================
        // EMPTY STATE
        // =====================================================

        SetEmptyState(
            availableCount == 0);


        // =====================================================
        // DEBUG
        // =====================================================

        Debug.Log(
            "[InventoryUI] " +
            "REFRESH | " +
            "Category = " +
            currentCategory +
            " | Range = " +
            startId +
            "-" +
            endId +
            " | Cards Used = " +
            availableCount,
            this);


        // =====================================================
        // SELECTION VALIDATION
        // =====================================================

        if (!IsSelectedItemStillValid())
        {
            ClearSelection();
        }
        else
        {
            RefreshSelectedVisual();

            RefreshSelectedDetail();
        }
    }


    // =========================================================
    // HIDE ALL CARDS
    // =========================================================

    private void HideAllCards()
    {
        if (itemCards == null)
        {
            return;
        }


        int count =
            itemCards.Length;


        for (
            int i = 0;
            i < count;
            i++
        )
        {
            InventoryItemCard card =
                itemCards[i];


            if (card == null)
            {
                continue;
            }


            card.Clear();

            card.gameObject.SetActive(false);
        }
    }


    // =========================================================
    // SELECTED VALIDATION
    // =========================================================

    private bool IsSelectedItemStillValid()
    {
        if (
            selectedItemId <
            GameItemDatabase.MinItemId
        )
        {
            return false;
        }


        if (
            selectedItemId >
            GameItemDatabase.MaxItemId
        )
        {
            return false;
        }


        if (
            selectedItemCategory !=
            currentCategory
        )
        {
            return false;
        }


        if (!InventoryOwnership.IsOwned(
                selectedItemId))
        {
            return false;
        }


        if (!EnsureDatabase())
        {
            return false;
        }


        if (
            !itemDatabase.TryGet(
                selectedItemId,
                out GameItemData item
            ) ||
            item == null
        )
        {
            return false;
        }


        InventoryCategory databaseCategory =
            ConvertCategory(
                item.Category);


        return databaseCategory ==
               selectedItemCategory;
    }


    // =========================================================
    // SELECTED VISUAL
    // =========================================================

    private void RefreshSelectedVisual()
    {
        if (itemCards == null)
        {
            return;
        }


        int count =
            itemCards.Length;


        for (
            int i = 0;
            i < count;
            i++
        )
        {
            InventoryItemCard card =
                itemCards[i];


            if (card == null)
            {
                continue;
            }


            if (!card.gameObject.activeSelf)
            {
                continue;
            }


            card.SetSelected(
                card.ItemId ==
                selectedItemId);
        }
    }


    // =========================================================
    // SELECTED DETAIL
    // =========================================================

    private void RefreshSelectedDetail()
    {
        if (!IsSelectedItemStillValid())
        {
            ClearSelectedDetail();
            return;
        }


        if (!EnsureDatabase())
        {
            ClearSelectedDetail();
            return;
        }


        if (
            !itemDatabase.TryGet(
                selectedItemId,
                out GameItemData item
            ) ||
            item == null
        )
        {
            ClearSelectedDetail();
            return;
        }


        // =====================================================
        // NAME
        // =====================================================

        if (selectedItemName != null)
        {
            if (string.IsNullOrWhiteSpace(
                    item.ItemName))
            {
                selectedItemName.text =
                    "ITEM " +
                    selectedItemId.ToString("000");
            }
            else
            {
                selectedItemName.text =
                    item.ItemName;
            }
        }


        // =====================================================
        // DESCRIPTION
        // =====================================================

        if (selectedItemDescription != null)
        {
            selectedItemDescription.text =
                item.Description;
        }


        // =====================================================
        // EQUIPPED
        // =====================================================

        bool equipped =
            IsItemEquipped(
                selectedItemId,
                selectedItemCategory);


        if (selectedItemState != null)
        {
            selectedItemState.text =
                equipped
                    ? "ĐANG TRANG BỊ"
                    : "ĐÃ SỞ HỮU";
        }


        // =====================================================
        // EQUIP BUTTON
        // =====================================================

        PrepareEquipButton();


        if (equipButton != null)
        {
            equipButton.gameObject.SetActive(
                !equipped);

            equipButton.interactable =
                !equipped;
        }


        if (equipButtonText != null)
        {
            equipButtonText.text =
                equipped
                    ? "ĐANG TRANG BỊ"
                    : "TRANG BỊ";
        }
    }


    // =========================================================
    // CLEAR SELECTION
    // =========================================================

    public void ClearSelection()
    {
        selectedItemId =
            -1;

        selectedItemCategory =
            currentCategory;


        ClearSelectedDetail();

        RefreshSelectedVisual();
    }


    // =========================================================
    // CLEAR DETAIL
    // =========================================================

    private void ClearSelectedDetail()
    {
        if (selectedItemName != null)
        {
            selectedItemName.text =
                string.Empty;
        }


        if (selectedItemDescription != null)
        {
            selectedItemDescription.text =
                string.Empty;
        }


        if (selectedItemState != null)
        {
            selectedItemState.text =
                string.Empty;
        }


        if (equipButton != null)
        {
            equipButton.interactable =
                false;

            equipButton.gameObject.SetActive(false);
        }


        if (equipButtonText != null)
        {
            equipButtonText.text =
                "TRANG BỊ";
        }
    }


    // =========================================================
    // EQUIPPED CHECK
    // =========================================================

    private bool IsItemEquipped(
        int itemId,
        InventoryCategory category)
    {
        if (equipmentManager == null)
        {
            return false;
        }


        if (!equipmentManager.IsEquipmentReady)
        {
            return false;
        }


        GameItemCategory databaseCategory =
            ConvertCategoryToDatabase(
                category);


        return
            equipmentManager.GetEquippedItemId(
                databaseCategory) ==
            itemId;
    }


    // =========================================================
    // CATEGORY -> UI
    // =========================================================

    private static InventoryCategory ConvertCategory(
        GameItemCategory category)
    {
        switch (category)
        {
            case GameItemCategory.Outfit:
                return InventoryCategory.Outfit;


            case GameItemCategory.Accessory:
                return InventoryCategory.Accessory;


            case GameItemCategory.Cosmetic:
                return InventoryCategory.Cosmetic;


            default:
                return InventoryCategory.Outfit;
        }
    }


    // =========================================================
    // UI -> DATABASE CATEGORY
    // =========================================================

    private static GameItemCategory ConvertCategoryToDatabase(
        InventoryCategory category)
    {
        switch (category)
        {
            case InventoryCategory.Outfit:
                return GameItemCategory.Outfit;


            case InventoryCategory.Accessory:
                return GameItemCategory.Accessory;


            case InventoryCategory.Cosmetic:
                return GameItemCategory.Cosmetic;


            default:
                return GameItemCategory.Outfit;
        }
    }


    // =========================================================
    // CATEGORY RANGE
    // =========================================================

    private static void GetCategoryRange(
        InventoryCategory category,
        out int startId,
        out int endId)
    {
        switch (category)
        {
            case InventoryCategory.Outfit:

                startId =
                    OutfitStartId;

                endId =
                    OutfitEndId;

                return;


            case InventoryCategory.Accessory:

                startId =
                    AccessoryStartId;

                endId =
                    AccessoryEndId;

                return;


            case InventoryCategory.Cosmetic:

                startId =
                    CosmeticStartId;

                endId =
                    CosmeticEndId;

                return;


            default:

                startId =
                    OutfitStartId;

                endId =
                    OutfitEndId;

                return;
        }
    }


    // =========================================================
    // CATEGORY TITLE
    // =========================================================

    private void UpdateCategoryTitle()
    {
        if (categoryTitle == null)
        {
            return;
        }


        switch (currentCategory)
        {
            case InventoryCategory.Outfit:

                categoryTitle.text =
                    "OUTFIT";

                break;


            case InventoryCategory.Accessory:

                categoryTitle.text =
                    "ACCESSORY";

                break;


            case InventoryCategory.Cosmetic:

                categoryTitle.text =
                    "COSMETIC";

                break;
        }
    }


    // =========================================================
    // CATEGORY VISUAL
    // =========================================================

    private void UpdateCategoryVisuals()
    {
        if (outfitButtonText != null)
        {
            outfitButtonText.text =
                "OUTFIT";
        }


        if (accessoryButtonText != null)
        {
            accessoryButtonText.text =
                "ACCESSORY";
        }


        if (cosmeticButtonText != null)
        {
            cosmeticButtonText.text =
                "COSMETIC";
        }
    }


    // =========================================================
    // EMPTY STATE
    // =========================================================

    private void SetEmptyState(
        bool visible)
    {
        if (emptyStateRoot != null)
        {
            emptyStateRoot.SetActive(
                visible);
        }


        if (emptyStateText != null)
        {
            emptyStateText.text =
                "CHƯA CÓ ITEM";
        }
    }


    // =========================================================
    // EQUIP BUTTON
    // =========================================================

    private void PrepareEquipButton()
    {
        if (equipButton == null)
        {
            return;
        }


        equipButton.enabled =
            true;


        Graphic graphic =
            equipButton.GetComponent<Graphic>();


        if (
            equipButton.targetGraphic == null &&
            graphic != null
        )
        {
            equipButton.targetGraphic =
                graphic;
        }


        if (graphic != null)
        {
            graphic.raycastTarget =
                true;
        }


        CanvasGroup canvasGroup =
            equipButton.GetComponent<CanvasGroup>();


        if (canvasGroup != null)
        {
            canvasGroup.interactable =
                true;

            canvasGroup.blocksRaycasts =
                true;
        }
    }


    // =========================================================
    // CATEGORY BUTTON BINDING
    // =========================================================

    private void BindCategoryButtons()
    {
        if (outfitButton != null)
        {
            outfitButton.onClick.RemoveListener(
                SelectOutfit);

            outfitButton.onClick.AddListener(
                SelectOutfit);
        }


        if (accessoryButton != null)
        {
            accessoryButton.onClick.RemoveListener(
                SelectAccessory);

            accessoryButton.onClick.AddListener(
                SelectAccessory);
        }


        if (cosmeticButton != null)
        {
            cosmeticButton.onClick.RemoveListener(
                SelectCosmetic);

            cosmeticButton.onClick.AddListener(
                SelectCosmetic);
        }
    }


    // =========================================================
    // CLOSE BUTTON
    // =========================================================

    private void BindCloseButton()
    {
        if (closeButton == null)
        {
            return;
        }


        closeButton.onClick.RemoveListener(
            Close);

        closeButton.onClick.AddListener(
            Close);
    }


    // =========================================================
    // UNBIND CATEGORY
    // =========================================================

    private void UnbindCategoryButtons()
    {
        if (outfitButton != null)
        {
            outfitButton.onClick.RemoveListener(
                SelectOutfit);
        }


        if (accessoryButton != null)
        {
            accessoryButton.onClick.RemoveListener(
                SelectAccessory);
        }


        if (cosmeticButton != null)
        {
            cosmeticButton.onClick.RemoveListener(
                SelectCosmetic);
        }
    }


    // =========================================================
    // UNBIND CLOSE
    // =========================================================

    private void UnbindCloseButton()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(
                Close);
        }
    }


    // =========================================================
    // EQUIPMENT EVENTS
    // =========================================================

    private void SubscribeEquipmentEvents()
    {
        EquipmentManager currentManager =
            equipmentManager;


        if (currentManager == null)
        {
            currentManager =
                EquipmentManager.Instance;
        }


        if (currentManager == null)
        {
            return;
        }


        if (
            subscribedEquipmentManager !=
            currentManager
        )
        {
            UnsubscribeEquipmentEvents();


            subscribedEquipmentManager =
                currentManager;

            equipmentManager =
                currentManager;
        }


        if (!equipmentSubscribed)
        {
            equipmentManager.OnEquipmentChanged +=
                HandleEquipmentChanged;

            equipmentSubscribed =
                true;
        }


        if (!equipmentReadySubscribed)
        {
            equipmentManager.OnEquipmentReady +=
                HandleEquipmentReady;

            equipmentReadySubscribed =
                true;
        }
    }


    // =========================================================
    // UNSUBSCRIBE EQUIPMENT
    // =========================================================

    private void UnsubscribeEquipmentEvents()
    {
        if (subscribedEquipmentManager == null)
        {
            return;
        }


        if (equipmentSubscribed)
        {
            subscribedEquipmentManager.OnEquipmentChanged -=
                HandleEquipmentChanged;

            equipmentSubscribed =
                false;
        }


        if (equipmentReadySubscribed)
        {
            subscribedEquipmentManager.OnEquipmentReady -=
                HandleEquipmentReady;

            equipmentReadySubscribed =
                false;
        }


        subscribedEquipmentManager =
            null;
    }


    // =========================================================
    // EQUIPMENT CHANGED
    // =========================================================

    private void HandleEquipmentChanged(
        GameItemCategory category)
    {
        InventoryCategory uiCategory =
            ConvertCategory(category);


        if (
            uiCategory != InventoryCategory.Outfit &&
            uiCategory != InventoryCategory.Accessory &&
            uiCategory != InventoryCategory.Cosmetic
        )
        {
            return;
        }


        if (!IsPanelOpen())
        {
            return;
        }


        RefreshItems();
    }


    // =========================================================
    // EQUIPMENT READY
    // =========================================================

    private void HandleEquipmentReady()
    {
        if (!IsPanelOpen())
        {
            return;
        }


        RefreshItems();
    }


    // =========================================================
    // SCROLL
    // =========================================================

    private void EnsureScrollRect()
    {
        if (itemScrollRect == null)
        {
            return;
        }


        if (itemGrid == null)
        {
            return;
        }


        RectTransform content =
            itemGrid as RectTransform;


        if (content == null)
        {
            return;
        }


        itemScrollRect.content =
            content;
    }


    // =========================================================
    // RESET SCROLL
    // =========================================================

    private void ResetScroll()
    {
        if (itemScrollRect == null)
        {
            return;
        }


        itemScrollRect.StopMovement();


        itemScrollRect.verticalNormalizedPosition =
            1f;


        itemScrollRect.horizontalNormalizedPosition =
            0f;
    }


    // =========================================================
    // PANEL STATE
    // =========================================================

    private bool IsPanelOpen()
    {
        return
            panelRoot != null &&
            panelRoot.activeInHierarchy;
    }


    // =========================================================
    // PUBLIC PROPERTIES
    // =========================================================

    public int SelectedItemId =>
        selectedItemId;


    public InventoryCategory CurrentCategory =>
        currentCategory;


    public InventoryCategory SelectedItemCategory =>
        selectedItemCategory;


    public bool HasSelectedItem =>
        selectedItemId >=
        GameItemDatabase.MinItemId;
}