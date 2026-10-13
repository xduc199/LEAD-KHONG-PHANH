using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Item_",
    menuName = "LEAD KHONG PHANH/Game Data/Item"
)]
public sealed class GameItemData : ScriptableObject
{
    // =========================================================
    // ID RANGE
    // =========================================================

    public const int MinItemId = 1;
    public const int MaxItemId = 100;

    // =========================================================
    // IDENTITY
    // =========================================================

    [Header("Identity")]
    [SerializeField]
    private int itemId;

    [SerializeField]
    private string itemName;

    [TextArea(2, 5)]
    [SerializeField]
    private string description;

    // =========================================================
    // CLASSIFICATION
    // =========================================================

    [Header("Classification")]
    [SerializeField]
    private GameItemCategory category;

    [SerializeField]
    private string subCategory;

    [SerializeField]
    private GameItemRarity rarity;

    // =========================================================
    // PRESENTATION
    // =========================================================

    [Header("Presentation")]

    [SerializeField]
    private Sprite icon;

    [Tooltip(
        "Legacy compatibility reference. " +
        "Runtime visual loading should use GameItemVisualDatabase."
    )]
    [SerializeField]
    private GameObject prefab;

    [Tooltip(
        "Stable visual definition key. " +
        "Runtime visual loading should use GameItemVisualDatabase."
    )]
    [SerializeField]
    private string visualId;

    // =========================================================
    // ECONOMY
    // =========================================================

    [Header("Economy")]

    [SerializeField]
    private bool purchasable;

    [Min(0)]
    [SerializeField]
    private int price;

    [SerializeField]
    private GameItemCurrency currency;

    // =========================================================
    // GAMEPLAY STATS
    // =========================================================

    [Header("Gameplay Stats")]

    [SerializeField]
    private bool hasStats;

    [SerializeField]
    private ItemStats stats = new ItemStats();

    // =========================================================
    // SOURCE DATA
    // =========================================================

    [Header("Source Data")]

    [TextArea(2, 5)]
    [SerializeField]
    private string sourceBuffText;

    [TextArea(2, 5)]
    [SerializeField]
    private string passiveDescription;

    // =========================================================
    // PUBLIC API
    // =========================================================

    public int ItemId => itemId;

    public string ItemName => itemName;

    public string Description => description;

    public GameItemCategory Category => category;

    public string SubCategory => subCategory;

    public GameItemRarity Rarity => rarity;

    public Sprite Icon => icon;

    public GameObject Prefab => prefab;

    public string VisualId => visualId;

    public bool Purchasable => purchasable;

    public int Price => price;

    public GameItemCurrency Currency => currency;

    public bool HasStats => hasStats;

    public ItemStats Stats => stats;

    public string SourceBuffText => sourceBuffText;

    public string PassiveDescription => passiveDescription;

#if UNITY_EDITOR

    // =========================================================
    // EDITOR VALIDATION
    // =========================================================

    private void OnValidate()
    {
        // -----------------------------------------------------
        // ID
        // -----------------------------------------------------

        if (itemId < MinItemId || itemId > MaxItemId)
        {
            Debug.LogWarning(
                $"[GameItemData] Invalid ItemId={itemId}. " +
                $"Expected range {MinItemId}-{MaxItemId}. " +
                $"Asset={name}",
                this
            );
        }

        // -----------------------------------------------------
        // PRICE
        // -----------------------------------------------------

        if (price < 0)
        {
            price = 0;
        }

        // -----------------------------------------------------
        // STATS
        // -----------------------------------------------------

        if (stats == null)
        {
            stats = new ItemStats();
        }

        if (stats.modifiers == null)
        {
            stats.modifiers = new List<ItemStatModifier>();
        }

        // -----------------------------------------------------
        // ICON
        // -----------------------------------------------------

        if (icon == null)
        {
            Debug.LogWarning(
                $"[GameItemData] Item {itemId} has no Icon. " +
                $"Inventory card may appear without an icon.\n" +
                $"Asset: {name}",
                this
            );
        }

        // -----------------------------------------------------
        // CATEGORY RANGE VALIDATION
        // -----------------------------------------------------

        if (!IsCategoryRangeValid(itemId, category))
        {
            Debug.LogError(
                $"[GameItemData] CATEGORY MISMATCH\n" +
                $"ItemId={itemId}\n" +
                $"Category={category}\n" +
                $"Asset={name}",
                this
            );
        }

        // -----------------------------------------------------
        // VISUAL ID
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(visualId))
        {
            visualId = $"VISUAL_ITEM_{itemId:000}";
        }
    }

    private static bool IsCategoryRangeValid(
        int id,
        GameItemCategory itemCategory)
    {
        switch (itemCategory)
        {
            case GameItemCategory.Vehicle:
                return id >= 1 && id <= 40;

            case GameItemCategory.Outfit:
                return id >= 41 && id <= 60;

            case GameItemCategory.Cosmetic:
                return id >= 61 && id <= 80;

            case GameItemCategory.Accessory:
                return id >= 81 && id <= 100;

            default:
                return false;
        }
    }

#endif
}