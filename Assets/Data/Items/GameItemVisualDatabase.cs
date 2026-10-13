using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "GameItemVisualDatabase",
    menuName = "LEAD KHONG PHANH/Game Data/Item Visual Database"
)]
public sealed class GameItemVisualDatabase : ScriptableObject
{
    // =========================================================
    // SERIALIZED DATA
    // =========================================================

    [Header("Visual Definitions")]
    [SerializeField]
    private List<GameItemVisual> visuals =
        new List<GameItemVisual>();

    // =========================================================
    // RUNTIME LOOKUP
    // =========================================================

    private Dictionary<int, GameItemVisual> byItemId;

    private Dictionary<string, GameItemVisual> byVisualId;

    private bool initialized;

    // =========================================================
    // CONSTANTS
    // =========================================================

    private const int MinimumExpectedItemId =
        GameItemDatabase.MinItemId;

    private const int MaximumExpectedItemId =
        GameItemDatabase.MaxItemId;

    // =========================================================
    // PUBLIC API
    // =========================================================

    public int Count
    {
        get
        {
            return visuals != null
                ? visuals.Count
                : 0;
        }
    }

    // =========================================================
    // INITIALIZATION
    // =========================================================

    public void Initialize()
    {
        if (initialized)
        {
            return;
        }

        int capacity =
            visuals != null
                ? visuals.Count
                : 0;

        if (capacity < 1)
        {
            capacity = 1;
        }

        byItemId =
            new Dictionary<int, GameItemVisual>(capacity);

        byVisualId =
            new Dictionary<string, GameItemVisual>(capacity);

        if (visuals != null)
        {
            BuildLookup();
        }

        initialized = true;
    }

    // =========================================================
    // LOOKUP BUILD
    // =========================================================

    private void BuildLookup()
    {
        int count = visuals.Count;

        for (int i = 0; i < count; i++)
        {
            GameItemVisual visual = visuals[i];

            if (visual == null)
            {
                Debug.LogError(
                    $"[GameItemVisualDatabase] " +
                    $"NULL visual at index {i}.",
                    this
                );

                continue;
            }

            int itemId = visual.ItemId;

            // -------------------------------------------------
            // ITEM ID RANGE
            // -------------------------------------------------

            if (itemId < MinimumExpectedItemId ||
                itemId > MaximumExpectedItemId)
            {
                Debug.LogError(
                    "[GameItemVisualDatabase] Invalid ItemId.\n" +
                    $"ItemId = {itemId}\n" +
                    $"Expected = " +
                    $"{MinimumExpectedItemId}-" +
                    $"{MaximumExpectedItemId}\n" +
                    $"Visual = {visual.name}",
                    visual
                );

                continue;
            }

            // -------------------------------------------------
            // DUPLICATE ITEM ID
            // -------------------------------------------------

            if (byItemId.ContainsKey(itemId))
            {
                Debug.LogError(
                    "[GameItemVisualDatabase] " +
                    $"Duplicate ItemId = {itemId}\n" +
                    $"Visual = {visual.name}",
                    visual
                );

                continue;
            }

            // -------------------------------------------------
            // CATEGORY / ANCHOR VALIDATION
            // -------------------------------------------------

            ValidateVisualAnchor(visual);

            // -------------------------------------------------
            // REGISTER ITEM ID
            // -------------------------------------------------

            byItemId.Add(itemId, visual);

            // -------------------------------------------------
            // VISUAL ID
            // -------------------------------------------------

            string visualId = visual.VisualId;

            if (string.IsNullOrWhiteSpace(visualId))
            {
                Debug.LogError(
                    "[GameItemVisualDatabase] " +
                    $"VisualId is empty.\n" +
                    $"ItemId = {itemId}\n" +
                    $"Visual = {visual.name}",
                    visual
                );

                continue;
            }

            // -------------------------------------------------
            // DUPLICATE VISUAL ID
            // -------------------------------------------------

            if (byVisualId.ContainsKey(visualId))
            {
                Debug.LogError(
                    "[GameItemVisualDatabase] " +
                    $"Duplicate VisualId = {visualId}\n" +
                    $"ItemId = {itemId}\n" +
                    $"Visual = {visual.name}",
                    visual
                );

                continue;
            }

            byVisualId.Add(
                visualId,
                visual
            );
        }
    }

    // =========================================================
    // ITEM ID LOOKUP
    // =========================================================

    public bool TryGet(
        int itemId,
        out GameItemVisual visual)
    {
        Initialize();

        visual = null;

        if (itemId < MinimumExpectedItemId ||
            itemId > MaximumExpectedItemId)
        {
            return false;
        }

        if (byItemId == null)
        {
            return false;
        }

        if (!byItemId.TryGetValue(
                itemId,
                out visual))
        {
            visual = null;
            return false;
        }

        if (visual == null)
        {
            visual = null;
            return false;
        }

        if (!visual.Enabled)
        {
            visual = null;
            return false;
        }

        return true;
    }

    // =========================================================
    // VISUAL ID LOOKUP
    // =========================================================

    public bool TryGet(
        string visualId,
        out GameItemVisual visual)
    {
        Initialize();

        visual = null;

        if (string.IsNullOrWhiteSpace(visualId))
        {
            return false;
        }

        if (byVisualId == null)
        {
            return false;
        }

        if (!byVisualId.TryGetValue(
                visualId,
                out visual))
        {
            visual = null;
            return false;
        }

        if (visual == null)
        {
            visual = null;
            return false;
        }

        if (!visual.Enabled)
        {
            visual = null;
            return false;
        }

        return true;
    }

    // =========================================================
    // EXISTENCE CHECK
    // =========================================================

    public bool Contains(int itemId)
    {
        Initialize();

        if (byItemId == null)
        {
            return false;
        }

        return byItemId.ContainsKey(itemId);
    }

    // =========================================================
    // ENABLED VISUAL CHECK
    // =========================================================

    public bool HasEnabledVisual(int itemId)
    {
        return TryGet(
            itemId,
            out GameItemVisual visual
        );
    }

    // =========================================================
    // CATEGORY RANGE CHECK
    // =========================================================

    public bool IsVisualCategoryCompatible(
        int itemId,
        GameItemVisual visual)
    {
        if (visual == null)
        {
            return false;
        }

        if (itemId != visual.ItemId)
        {
            return false;
        }

        GameItemVisualAnchor expectedAnchor;

        if (itemId >= 1 &&
            itemId <= 40)
        {
            expectedAnchor =
                GameItemVisualAnchor.VehicleRoot;
        }
        else if (itemId >= 41 &&
                 itemId <= 60)
        {
            expectedAnchor =
                GameItemVisualAnchor.OutfitRoot;
        }
        else if (itemId >= 61 &&
                 itemId <= 80)
        {
            expectedAnchor =
                GameItemVisualAnchor.CosmeticRoot;
        }
        else if (itemId >= 81 &&
                 itemId <= 100)
        {
            expectedAnchor =
                GameItemVisualAnchor.AccessoryRoot;
        }
        else
        {
            return false;
        }

        return visual.Anchor == expectedAnchor;
    }

    // =========================================================
    // VALIDATE SINGLE VISUAL
    // =========================================================

    private void ValidateVisualAnchor(
        GameItemVisual visual)
    {
        if (visual == null)
        {
            return;
        }

        int itemId = visual.ItemId;

        GameItemVisualAnchor expectedAnchor;

        if (itemId >= 1 &&
            itemId <= 40)
        {
            expectedAnchor =
                GameItemVisualAnchor.VehicleRoot;
        }
        else if (itemId >= 41 &&
                 itemId <= 60)
        {
            expectedAnchor =
                GameItemVisualAnchor.OutfitRoot;
        }
        else if (itemId >= 61 &&
                 itemId <= 80)
        {
            expectedAnchor =
                GameItemVisualAnchor.CosmeticRoot;
        }
        else if (itemId >= 81 &&
                 itemId <= 100)
        {
            expectedAnchor =
                GameItemVisualAnchor.AccessoryRoot;
        }
        else
        {
            return;
        }

        if (visual.Anchor != expectedAnchor)
        {
            Debug.LogError(
                "[GameItemVisualDatabase] " +
                "Visual Anchor mismatch.\n" +
                $"ItemId = {itemId}\n" +
                $"Expected Anchor = {expectedAnchor}\n" +
                $"Actual Anchor = {visual.Anchor}\n" +
                $"Visual = {visual.name}",
                visual
            );
        }

        if (visual.Prefab == null)
        {
            Debug.LogWarning(
                "[GameItemVisualDatabase] " +
                $"ItemId={itemId} has no Prefab.\n" +
                $"Visual={visual.name}",
                visual
            );
        }
    }

    // =========================================================
    // REBUILD
    // =========================================================

    public void RebuildLookup()
    {
        initialized = false;

        if (byItemId != null)
        {
            byItemId.Clear();
        }

        if (byVisualId != null)
        {
            byVisualId.Clear();
        }

        byItemId = null;
        byVisualId = null;

        Initialize();
    }

    // =========================================================
    // EDITOR VALIDATION
    // =========================================================

#if UNITY_EDITOR

    private void OnValidate()
    {
        initialized = false;

        byItemId = null;
        byVisualId = null;

        ValidateSerializedVisuals();
    }

    private void ValidateSerializedVisuals()
    {
        if (visuals == null)
        {
            return;
        }

        int count = visuals.Count;

        for (int i = 0; i < count; i++)
        {
            GameItemVisual visual = visuals[i];

            if (visual == null)
            {
                Debug.LogError(
                    $"[GameItemVisualDatabase] " +
                    $"NULL visual at index {i}.",
                    this
                );

                continue;
            }

            ValidateVisualAnchor(visual);
        }
    }

#endif
}