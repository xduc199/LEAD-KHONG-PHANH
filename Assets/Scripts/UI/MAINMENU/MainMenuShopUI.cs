using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuShopUI : MonoBehaviour
{
    //==============================================================
    // ENUMS
    //==============================================================

    public enum ShopCategory
    {
        Vehicle = 0,
        Outfit = 1,
        Accessory = 2,
        Cosmetic = 3
    }

    public enum CurrencyType
    {
        Gold = 0,
        Gem = 1
    }


    //==============================================================
    // DATABASE
    //==============================================================

    [Header("Database")]
    [SerializeField]
    private GameItemDatabase itemDatabase;


    //==============================================================
    // SHOP PANEL
    //==============================================================

    [Header("Shop Panel")]
    [SerializeField]
    private GameObject shopPanel;


    //==============================================================
    // CATEGORY
    //==============================================================

    [Header("Category")]
    [SerializeField]
    private Button[] categoryButtons;

    [SerializeField]
    private TMP_Text[] categoryButtonTexts;


    //==============================================================
    // ITEM GRID
    //==============================================================

    [Header("Item Grid")]
    [SerializeField]
    private Transform itemGrid;

    [SerializeField]
    private ShopItemCard[] itemCards;

    [SerializeField]
    private ScrollRect itemScrollRect;


    //==============================================================
    // PAGINATION
    //==============================================================

    [Header("Pagination")]
    [SerializeField]
    private Button previousPageButton;

    [SerializeField]
    private Button nextPageButton;

    [SerializeField]
    private TMP_Text pageText;


    //==============================================================
    // SELECTED ITEM
    //==============================================================

    [Header("Selected Item")]
    [SerializeField]
    private Image selectedPreview;

    [SerializeField]
    private TMP_Text selectedNameText;

    [SerializeField]
    private TMP_Text selectedDescriptionText;

    [SerializeField]
    private TMP_Text selectedPriceText;

    [SerializeField]
    private TMP_Text selectedCategoryText;


    //==============================================================
    // BUY
    //==============================================================

    [Header("Buy")]
    [SerializeField]
    private Button buyButton;

    [SerializeField]
    private TMP_Text buyButtonText;


    //==============================================================
    // CURRENCY
    //==============================================================

    [Header("Currency")]
    [SerializeField]
    private TMP_Text goldAmountText;

    [SerializeField]
    private TMP_Text gemAmountText;


    //==============================================================
    // CATEGORY COLORS
    //==============================================================

    [Header("Category Colors")]
    [SerializeField]
    private Color selectedCategoryColor =
        new Color(1f, 0.75f, 0.1f, 1f);

    [SerializeField]
    private Color normalCategoryColor =
        new Color(0.18f, 0.18f, 0.18f, 1f);


    //==============================================================
    // TEXT COLORS
    //==============================================================

    [Header("Text Colors")]
    [SerializeField]
    private Color whiteColor =
        Color.white;

    [SerializeField]
    private Color goldColor =
        new Color(1f, 0.75f, 0.1f, 1f);

    [SerializeField]
    private Color gemColor =
        new Color(0.3f, 0.9f, 1f, 1f);

    [SerializeField]
    private Color ownedColor =
        new Color(0.3f, 1f, 0.45f, 1f);


    //==============================================================
    // PREVIEW COLORS
    //==============================================================

    [Header("Preview Colors")]
    [SerializeField]
    private Color vehiclePreviewColor =
        new Color(0.95f, 0.2f, 0.15f, 1f);

    [SerializeField]
    private Color outfitPreviewColor =
        new Color(0.2f, 0.65f, 1f, 1f);

    [SerializeField]
    private Color accessoryPreviewColor =
        new Color(1f, 0.55f, 0.1f, 1f);

    [SerializeField]
    private Color cosmeticPreviewColor =
        new Color(0.75f, 0.3f, 1f, 1f);


    //==============================================================
    // CONSTANTS
    //==============================================================

    private const int CategoryCount = 4;

    /*
     * Shop sử dụng tối đa 20 card UI.
     *
     * Vehicle:
     *   1 - 40
     *   Page 1 = 1  - 20
     *   Page 2 = 21 - 40
     *
     * Outfit:
     *   41 - 60
     *
     * Cosmetic:
     *   61 - 80
     *
     * Accessory:
     *   81 - 100
     */

    private const int ItemsPerPage = 20;

    private const int MaxVisibleItemCount =
        GameItemDatabase.MaxItemId;


    //==============================================================
    // RUNTIME DATA
    //==============================================================

    /*
     * Buffer chứa ItemId thực tế.
     *
     * KHÔNG dùng index làm ItemId.
     */
    private readonly int[] visibleItemIds =
        new int[MaxVisibleItemCount];

    private int visibleItemCount;

    private ShopCategory currentCategory;

    private int selectedItemId = -1;

    private int currentPage;

    private int totalPages = 1;

    private bool isInitialized;


    //==============================================================
    // UNITY
    //==============================================================

    private void Awake()
    {
        Initialize();
    }


    private void OnEnable()
    {
        if (!isInitialized)
        {
            Initialize();
        }

        if (!isInitialized)
            return;

        RefreshCurrency();

        SelectCategory(
            ShopCategory.Vehicle
        );
    }


    //==============================================================
    // INITIALIZATION
    //==============================================================

    private void Initialize()
    {
        if (isInitialized)
            return;

        if (itemDatabase == null)
        {
            Debug.LogError(
                "[MainMenuShopUI] GameItemDatabase chưa được gán.",
                this
            );

            return;
        }

        itemDatabase.Initialize();

        BindCategoryButtons();

        BindPaginationButtons();

        BindBuyButton();

        isInitialized = true;
    }


    //==============================================================
    // CATEGORY BUTTONS
    //==============================================================

    private void BindCategoryButtons()
{
    if (categoryButtons == null)
    {
        Debug.LogError(
            "[MainMenuShopUI] categoryButtons == NULL.",
            this
        );

        return;
    }

    int count = Mathf.Min(
        categoryButtons.Length,
        CategoryCount
    );

    if (count < CategoryCount)
    {
        Debug.LogWarning(
            "[MainMenuShopUI] " +
            $"Chỉ có {count}/4 Category Buttons được gán.",
            this
        );
    }

    for (int i = 0; i < count; i++)
    {
        Button button = categoryButtons[i];

        if (button == null)
        {
            Debug.LogWarning(
                "[MainMenuShopUI] " +
                $"categoryButtons[{i}] == NULL.",
                this
            );

            continue;
        }

        /*
         * Không dùng lambda.
         * Bind trực tiếp method tương ứng.
         */
        button.onClick.RemoveAllListeners();

        switch (i)
        {
            case 0:
                button.onClick.AddListener(
                    SelectVehicleCategory
                );
                break;

            case 1:
                button.onClick.AddListener(
                    SelectOutfitCategory
                );
                break;

            case 2:
                button.onClick.AddListener(
                    SelectAccessoryCategory
                );
                break;

            case 3:
                button.onClick.AddListener(
                    SelectCosmeticCategory
                );
                break;
        }

        button.interactable = true;
    }
}


    //==============================================================
    // PAGINATION BUTTONS
    //==============================================================

   private void BindPaginationButtons()
{
    if (previousPageButton != null)
    {
        previousPageButton.onClick.RemoveListener(
            PreviousPage
        );

        previousPageButton.onClick.AddListener(
            PreviousPage
        );

        previousPageButton.interactable = true;
    }

    if (nextPageButton != null)
    {
        nextPageButton.onClick.RemoveListener(
            NextPage
        );

        nextPageButton.onClick.AddListener(
            NextPage
        );

        nextPageButton.interactable = true;
    }
}


    //==============================================================
    // BUY BUTTON
    //==============================================================

    private void BindBuyButton()
    {
        if (buyButton == null)
            return;

        buyButton.onClick.RemoveListener(
            TryBuySelectedItem
        );

        buyButton.onClick.AddListener(
            TryBuySelectedItem
        );
    }


    //==============================================================
    // CATEGORY API
    //==============================================================

    public void SelectVehicleCategory()
    {
        SelectCategory(
            ShopCategory.Vehicle
        );
    }


    public void SelectOutfitCategory()
    {
        SelectCategory(
            ShopCategory.Outfit
        );
    }


    public void SelectAccessoryCategory()
    {
        SelectCategory(
            ShopCategory.Accessory
        );
    }


    public void SelectCosmeticCategory()
    {
        SelectCategory(
            ShopCategory.Cosmetic
        );
    }


    public void SelectCategory(
        int categoryIndex)
    {
        if (
            categoryIndex < 0 ||
            categoryIndex >= CategoryCount
        )
        {
            return;
        }

        SelectCategory(
            (ShopCategory)categoryIndex
        );
    }


    public void SelectCategory(
        ShopCategory category)
    {
        if (!isInitialized)
            return;

        if (
            category < ShopCategory.Vehicle ||
            category > ShopCategory.Cosmetic
        )
        {
            return;
        }

        currentCategory = category;

        visibleItemCount = 0;

        selectedItemId = -1;

        //==========================================================
        // BUILD CATEGORY ITEMS
        //==========================================================

        BuildVisibleItems();

        //==========================================================
        // RESET PAGINATION
        //==========================================================

        currentPage = 0;

        RecalculatePageCount();

        //==========================================================
        // BIND CURRENT PAGE
        //==========================================================

        BindVisibleCards();

        UpdatePaginationUI();

        //==========================================================
        // CATEGORY UI
        //==========================================================

        UpdateCategoryButtons();

        ResetScroll();

        //==========================================================
        // AUTO SELECT FIRST ITEM
        //==========================================================

        if (visibleItemCount > 0)
        {
            SelectItem(
                visibleItemIds[0]
            );
        }
        else
        {
            ClearSelectedItem();
        }
    }


    //==============================================================
    // PAGINATION API
    //==============================================================

    public void PreviousPage()
    {
        if (!isInitialized)
            return;

        if (currentPage <= 0)
            return;

        currentPage--;

        RefreshCurrentPage();

        ResetScroll();
    }


    public void NextPage()
    {
        if (!isInitialized)
            return;

        if (currentPage >= totalPages - 1)
            return;

        currentPage++;

        RefreshCurrentPage();

        ResetScroll();
    }


    //==============================================================
    // RECALCULATE PAGE COUNT
    //==============================================================

    private void RecalculatePageCount()
    {
        if (visibleItemCount <= 0)
        {
            totalPages = 1;
            currentPage = 0;

            return;
        }

        totalPages =
            (visibleItemCount + ItemsPerPage - 1)
            / ItemsPerPage;

        if (currentPage < 0)
        {
            currentPage = 0;
        }

        if (currentPage >= totalPages)
        {
            currentPage =
                totalPages - 1;
        }
    }


    //==============================================================
    // REFRESH CURRENT PAGE
    //==============================================================

    private void RefreshCurrentPage()
    {
        RecalculatePageCount();

        BindVisibleCards();

        UpdatePaginationUI();
    }


    //==============================================================
    // PAGINATION UI
    //==============================================================

    private void UpdatePaginationUI()
    {
        if (pageText != null)
        {
            pageText.text =
                $"{currentPage + 1} / {totalPages}";
        }

        if (previousPageButton != null)
        {
            previousPageButton.interactable =
                currentPage > 0;
        }

        if (nextPageButton != null)
        {
            nextPageButton.interactable =
                currentPage < totalPages - 1;
        }
    }


    //==============================================================
    // BUILD VISIBLE ITEMS
    //==============================================================

    private void BuildVisibleItems()
    {
        visibleItemCount = 0;

        if (itemDatabase == null)
            return;

        IReadOnlyList<GameItemData> databaseItems =
            itemDatabase.Items;

        if (databaseItems == null)
            return;

        for (
            int i = 0;
            i < databaseItems.Count;
            i++)
        {
            GameItemData item =
                databaseItems[i];

            if (item == null)
                continue;

            if (!IsMatchingCategory(item))
                continue;

            if (
                visibleItemCount >=
                visibleItemIds.Length
            )
            {
                break;
            }

            visibleItemIds[
                visibleItemCount
            ] = item.ItemId;

            visibleItemCount++;
        }
    }


    //==============================================================
    // CATEGORY CHECK
    //==============================================================

    private bool IsMatchingCategory(
        GameItemData item)
    {
        if (item == null)
            return false;

        switch (currentCategory)
        {
            case ShopCategory.Vehicle:

                return item.Category ==
                       GameItemCategory.Vehicle;

            case ShopCategory.Outfit:

                return item.Category ==
                       GameItemCategory.Outfit;

            case ShopCategory.Accessory:

                return item.Category ==
                       GameItemCategory.Accessory;

            case ShopCategory.Cosmetic:

                return item.Category ==
                       GameItemCategory.Cosmetic;

            default:

                return false;
        }
    }


    //==============================================================
    // BIND CARDS
    //==============================================================

    private void BindVisibleCards()
    {
        if (itemCards == null)
            return;

        int cardCount =
            itemCards.Length;

        for (
            int i = 0;
            i < cardCount;
            i++)
        {
            ShopItemCard card =
                itemCards[i];

            if (card == null)
                continue;

            /*
             * Page 0:
             *   visibleIndex = 0 + i
             *
             * Page 1:
             *   visibleIndex = 20 + i
             */
            int visibleIndex =
                currentPage *
                ItemsPerPage +
                i;

            if (
                visibleIndex < 0 ||
                visibleIndex >= visibleItemCount
            )
            {
                card.Hide();

                continue;
            }

            int itemId =
                visibleItemIds[
                    visibleIndex
                ];

            if (
                !TryGetItem(
                    itemId,
                    out GameItemData item
                )
            )
            {
                card.Hide();

                continue;
            }

            CurrencyType currency =
                ToCurrencyType(
                    item.Currency
                );

            card.Bind(
                this,
                item.ItemId,
                item.ItemName,
                item.Price,
                currency,
                GetCurrencyColor(currency)
            );

            card.SetSelected(
                card.ItemId ==
                selectedItemId
            );
        }
    }


    //==============================================================
    // CATEGORY VISUAL
    //==============================================================

    private void UpdateCategoryButtons()
    {
        if (categoryButtons == null)
            return;

        int count =
            Mathf.Min(
                categoryButtons.Length,
                CategoryCount
            );

        for (
            int i = 0;
            i < count;
            i++)
        {
            Button button =
                categoryButtons[i];

            if (button == null)
                continue;

            Image image =
                button.targetGraphic as Image;

            if (image == null)
            {
                image =
                    button.GetComponent<Image>();
            }

            bool selected =
                i == (int)currentCategory;

            if (image != null)
            {
                image.color =
                    selected
                        ? selectedCategoryColor
                        : normalCategoryColor;
            }

            if (
                categoryButtonTexts != null &&
                i < categoryButtonTexts.Length
            )
            {
                TMP_Text text =
                    categoryButtonTexts[i];

                if (text != null)
                {
                    text.color =
                        selected
                            ? Color.black
                            : whiteColor;
                }
            }
        }
    }


    //==============================================================
    // SCROLL
    //==============================================================

    private void ResetScroll()
    {
        if (itemScrollRect == null)
            return;

        itemScrollRect.StopMovement();

        itemScrollRect.velocity =
            Vector2.zero;

        Canvas.ForceUpdateCanvases();

        itemScrollRect.verticalNormalizedPosition =
            1f;

        itemScrollRect.horizontalNormalizedPosition =
            0f;

        Canvas.ForceUpdateCanvases();
    }


    //==============================================================
    // CARD CALLBACK
    //==============================================================

    public void SelectItemFromCard(
        ShopItemCard card)
    {
        if (card == null)
            return;

        int itemId =
            card.ItemId;

        if (itemId < 0)
            return;

        if (
            !TryGetItem(
                itemId,
                out GameItemData item
            )
        )
        {
            return;
        }

        if (!IsMatchingCategory(item))
            return;

        /*
         * Safety:
         *
         * Card phải thuộc page hiện tại.
         */
        if (!IsItemOnCurrentPage(itemId))
            return;

        SelectItem(itemId);
    }


    //==============================================================
    // CURRENT PAGE CHECK
    //==============================================================

    private bool IsItemOnCurrentPage(
        int itemId)
    {
        int startIndex =
            currentPage *
            ItemsPerPage;

        int endIndex =
            Mathf.Min(
                startIndex + ItemsPerPage,
                visibleItemCount
            );

        for (
            int i = startIndex;
            i < endIndex;
            i++)
        {
            if (
                visibleItemIds[i] ==
                itemId
            )
            {
                return true;
            }
        }

        return false;
    }


    //==============================================================
    // SELECT ITEM
    //==============================================================

    private void SelectItem(
        int itemId)
    {
        if (
            !TryGetItem(
                itemId,
                out GameItemData item
            )
        )
        {
            return;
        }

        if (!IsMatchingCategory(item))
            return;

        selectedItemId =
            item.ItemId;

        if (selectedNameText != null)
        {
            selectedNameText.text =
                item.ItemName;
        }

        if (selectedDescriptionText != null)
        {
            selectedDescriptionText.text =
                item.Description;
        }

        if (selectedCategoryText != null)
        {
            selectedCategoryText.text =
                GetCategoryName(
                    item.Category
                );
        }

        bool owned =
            InventoryOwnership.IsOwned(
                item.ItemId
            );

        if (selectedPriceText != null)
        {
            if (owned)
            {
                selectedPriceText.text =
                    "ĐÃ SỞ HỮU";

                selectedPriceText.color =
                    ownedColor;
            }
            else
            {
                selectedPriceText.text =
                    FormatPrice(
                        item.Price,
                        item.Currency
                    );

                selectedPriceText.color =
                    GetCurrencyColor(
                        item.Currency
                    );
            }
        }

        if (selectedPreview != null)
        {
            selectedPreview.color =
                GetPreviewColor(
                    item.Category
                );
        }

        if (buyButton != null)
        {
            buyButton.interactable =
                !owned &&
                item.Purchasable;
        }

        if (buyButtonText != null)
        {
            buyButtonText.text =
                owned
                    ? "ĐÃ SỞ HỮU"
                    : "MUA NGAY";
        }

        RefreshCardSelection();
    }


    //==============================================================
    // REFRESH CARD SELECTION
    //==============================================================

    private void RefreshCardSelection()
    {
        if (itemCards == null)
            return;

        for (
            int i = 0;
            i < itemCards.Length;
            i++)
        {
            ShopItemCard card =
                itemCards[i];

            if (card == null)
                continue;

            card.SetSelected(
                card.ItemId ==
                selectedItemId
            );
        }
    }


    //==============================================================
    // BUY
    //==============================================================

    public void TryBuySelectedItem()
    {
        if (selectedItemId < 0)
            return;

        if (
            !TryGetItem(
                selectedItemId,
                out GameItemData item
            )
        )
        {
            return;
        }

        if (!IsMatchingCategory(item))
            return;

        if (!IsItemOnCurrentPage(item.ItemId))
            return;

        if (!item.Purchasable)
        {
            Debug.Log(
                "[MainMenuShopUI] " +
                $"Item không bán: {item.ItemName}"
            );

            return;
        }

        if (
            InventoryOwnership.IsOwned(
                item.ItemId
            )
        )
        {
            UpdateSelectedOwnedState();

            return;
        }

        if (
            item.Currency ==
            GameItemCurrency.Gold
        )
        {
            TryBuyWithGold(item);

            return;
        }

        if (
            item.Currency ==
            GameItemCurrency.Gem
        )
        {
            TryBuyWithGem(item);

            return;
        }
    }


    //==============================================================
    // BUY GOLD
    //==============================================================

    private void TryBuyWithGold(
        GameItemData item)
    {
        if (item == null)
            return;

        GameManager gameManager =
            GameManager.Instance;

        if (gameManager == null)
        {
            Debug.LogWarning(
                "[MainMenuShopUI] " +
                "Không tìm thấy GameManager."
            );

            return;
        }

        if (
            !gameManager.CanAffordGold(
                item.Price
            )
        )
        {
            Debug.Log(
                "[MainMenuShopUI] " +
                $"Không đủ GOLD để mua {item.ItemName}."
            );

            return;
        }

        bool success =
            gameManager.SpendGold(
                item.Price
            );

        if (!success)
        {
            Debug.LogWarning(
                "[MainMenuShopUI] " +
                $"SpendGold thất bại: {item.ItemName}"
            );

            return;
        }

        InventoryOwnership.SetOwned(
            item.ItemId
        );

        RefreshCurrency();

        RefreshVisibleCards();

        UpdateSelectedOwnedState();

        Debug.Log(
            "[MainMenuShopUI] " +
            $"Mua thành công: {item.ItemName} " +
            $"(-{item.Price} GOLD)"
        );
    }


    //==============================================================
    // BUY GEM
    //==============================================================

    private void TryBuyWithGem(
        GameItemData item)
    {
        if (item == null)
            return;

        GameManager gameManager =
            GameManager.Instance;

        if (gameManager == null)
        {
            Debug.LogWarning(
                "[MainMenuShopUI] " +
                "Không tìm thấy GameManager."
            );

            return;
        }

        if (
            !gameManager.CanAffordGem(
                item.Price
            )
        )
        {
            Debug.Log(
                "[MainMenuShopUI] " +
                $"Không đủ GEM để mua {item.ItemName}."
            );

            return;
        }

        bool success =
            gameManager.SpendGem(
                item.Price
            );

        if (!success)
        {
            Debug.LogWarning(
                "[MainMenuShopUI] " +
                $"SpendGem thất bại: {item.ItemName}"
            );

            return;
        }

        InventoryOwnership.SetOwned(
            item.ItemId
        );

        RefreshCurrency();

        RefreshVisibleCards();

        UpdateSelectedOwnedState();

        Debug.Log(
            "[MainMenuShopUI] " +
            $"Mua thành công: {item.ItemName} " +
            $"(-{item.Price} GEM)"
        );
    }


    //==============================================================
    // REFRESH VISIBLE CARDS
    //==============================================================

    private void RefreshVisibleCards()
    {
        if (itemCards == null)
            return;

        for (
            int i = 0;
            i < itemCards.Length;
            i++)
        {
            ShopItemCard card =
                itemCards[i];

            if (card == null)
                continue;

            if (!card.gameObject.activeSelf)
                continue;

            card.RefreshOwnership();
        }
    }


    //==============================================================
    // SELECTED OWNERSHIP STATE
    //==============================================================

    private void UpdateSelectedOwnedState()
    {
        if (selectedItemId < 0)
            return;

        if (
            !TryGetItem(
                selectedItemId,
                out GameItemData item
            )
        )
        {
            return;
        }

        bool owned =
            InventoryOwnership.IsOwned(
                item.ItemId
            );

        if (selectedPriceText != null)
        {
            if (owned)
            {
                selectedPriceText.text =
                    "ĐÃ SỞ HỮU";

                selectedPriceText.color =
                    ownedColor;
            }
            else
            {
                selectedPriceText.text =
                    FormatPrice(
                        item.Price,
                        item.Currency
                    );

                selectedPriceText.color =
                    GetCurrencyColor(
                        item.Currency
                    );
            }
        }

        if (buyButton != null)
        {
            buyButton.interactable =
                !owned &&
                item.Purchasable;
        }

        if (buyButtonText != null)
        {
            buyButtonText.text =
                owned
                    ? "ĐÃ SỞ HỮU"
                    : "MUA NGAY";
        }

        RefreshCardSelection();
    }


    //==============================================================
    // CLEAR SELECTION
    //==============================================================

    private void ClearSelectedItem()
    {
        selectedItemId = -1;

        if (selectedNameText != null)
        {
            selectedNameText.text =
                string.Empty;
        }

        if (selectedDescriptionText != null)
        {
            selectedDescriptionText.text =
                string.Empty;
        }

        if (selectedPriceText != null)
        {
            selectedPriceText.text =
                string.Empty;
        }

        if (selectedCategoryText != null)
        {
            selectedCategoryText.text =
                string.Empty;
        }

        if (selectedPreview != null)
        {
            selectedPreview.color =
                whiteColor;
        }

        if (buyButton != null)
        {
            buyButton.interactable =
                false;
        }

        if (buyButtonText != null)
        {
            buyButtonText.text =
                "MUA NGAY";
        }

        RefreshCardSelection();
    }


    //==============================================================
    // CURRENCY UI
    //==============================================================

    private void RefreshCurrency()
    {
        GameManager gameManager =
            GameManager.Instance;

        if (gameManager == null)
            return;

        if (goldAmountText != null)
        {
            goldAmountText.text =
                gameManager
                    .GetGold()
                    .ToString("N0");
        }

        if (gemAmountText != null)
        {
            gemAmountText.text =
                gameManager
                    .GetGem()
                    .ToString("N0");
        }
    }


    //==============================================================
    // FORMAT PRICE
    //==============================================================

    private string FormatPrice(
        int price,
        GameItemCurrency currency)
    {
        if (
            currency ==
            GameItemCurrency.Gem
        )
        {
            return
                "♦ " +
                price.ToString("N0") +
                " GEM";
        }

        return
            "● " +
            price.ToString("N0") +
            " GOLD";
    }


    //==============================================================
    // CATEGORY NAME
    //==============================================================

    private string GetCategoryName(
        GameItemCategory category)
    {
        switch (category)
        {
            case GameItemCategory.Vehicle:

                return "XE";

            case GameItemCategory.Outfit:

                return "TRANG PHỤC";

            case GameItemCategory.Accessory:

                return "PHỤ KIỆN";

            case GameItemCategory.Cosmetic:

                return "COSMETIC";

            default:

                return string.Empty;
        }
    }


    //==============================================================
    // CURRENCY COLOR - DATABASE
    //==============================================================

    private Color GetCurrencyColor(
        GameItemCurrency currency)
    {
        return currency ==
               GameItemCurrency.Gem
            ? gemColor
            : goldColor;
    }


    //==============================================================
    // CURRENCY COLOR - UI
    //==============================================================

    private Color GetCurrencyColor(
        CurrencyType currency)
    {
        return currency ==
               CurrencyType.Gem
            ? gemColor
            : goldColor;
    }


    //==============================================================
    // CURRENCY CONVERSION
    //==============================================================

    private CurrencyType ToCurrencyType(
        GameItemCurrency currency)
    {
        return currency ==
               GameItemCurrency.Gem
            ? CurrencyType.Gem
            : CurrencyType.Gold;
    }


    //==============================================================
    // PREVIEW COLOR
    //==============================================================

    private Color GetPreviewColor(
        GameItemCategory category)
    {
        switch (category)
        {
            case GameItemCategory.Vehicle:

                return vehiclePreviewColor;

            case GameItemCategory.Outfit:

                return outfitPreviewColor;

            case GameItemCategory.Accessory:

                return accessoryPreviewColor;

            case GameItemCategory.Cosmetic:

                return cosmeticPreviewColor;

            default:

                return whiteColor;
        }
    }


    //==============================================================
    // DATABASE LOOKUP
    //==============================================================

    private bool TryGetItem(
        int itemId,
        out GameItemData item)
    {
        if (itemDatabase == null)
        {
            item = null;

            return false;
        }

        return itemDatabase.TryGet(
            itemId,
            out item
        );
    }


    //==============================================================
    // OPEN
    //==============================================================

    public void Open()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
        }

        RefreshCurrency();

        if (isInitialized)
        {
            SelectCategory(
                ShopCategory.Vehicle
            );
        }
    }


    //==============================================================
    // CLOSE
    //==============================================================

    public void Close()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
    }
    
}