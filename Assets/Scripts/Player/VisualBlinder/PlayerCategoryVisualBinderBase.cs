using System;
using UnityEngine;

/// <summary>
/// Base runtime renderer for one player item category.
///
/// Responsibilities:
/// - Resolve Item/Visual databases.
/// - Subscribe to EquipmentManager category changes.
/// - Resolve equipped ItemId.
/// - Resolve GameItemVisual by stable ItemId.
/// - Instantiate and transform visual prefab.
/// - Keep rendered visual state separate from equipment state.
///
/// IMPORTANT:
/// All category binders inherit this class and coexist
/// on the same PLAYER GameObject.
///
/// Migration behavior:
/// 1. GameItemVisual.Prefab
/// 2. GameItemData.Prefab legacy fallback
/// 3. If no prefab exists -> preserve current/legacy visual.
/// </summary>
public abstract class PlayerCategoryVisualBinderBase : MonoBehaviour
{
    //=========================================================
    // SHARED DATABASE REFERENCES
    //=========================================================

    [Header("Databases")]
    [SerializeField]
    protected GameItemDatabase itemDatabase;

    [SerializeField]
    protected GameItemVisualDatabase visualDatabase;


    //=========================================================
    // VISUAL ROOT
    //=========================================================

    [Header("Visual Root")]
    [SerializeField]
    protected Transform visualRoot;

    [SerializeField]
    protected GameObject[] legacyVisualRoots;


    //=========================================================
    // RUNTIME STATE
    //=========================================================

    private GameObject currentInstance;

    private int currentItemId = -1;

    private bool initialized;

    private bool subscribed;


    //=========================================================
    // CATEGORY CONTRACT
    //=========================================================

    protected abstract GameItemCategory Category { get; }

    protected abstract int StartItemId { get; }

    protected abstract int EndItemId { get; }

    protected abstract int DefaultItemId { get; }

    protected abstract GameItemVisualAnchor RequiredAnchor { get; }

    protected abstract string RootName { get; }

    protected abstract string InstanceName { get; }


    //=========================================================
    // PUBLIC STATE
    //=========================================================

    public int CurrentItemId => currentItemId;

    public GameObject CurrentInstance => currentInstance;

    public Transform VisualRoot => visualRoot;

    public bool HasVisual => currentInstance != null;


    //=========================================================
    // UNITY LIFECYCLE
    //=========================================================

    protected virtual void Awake()
    {
        Initialize();
    }

    protected virtual void OnEnable()
    {
        Initialize();

        SubscribeEquipmentEvents();

        ApplyEquippedItem();
    }

    protected virtual void OnDisable()
    {
        UnsubscribeEquipmentEvents();
    }

    protected virtual void OnDestroy()
    {
        UnsubscribeEquipmentEvents();

        ClearVisual();
    }


    //=========================================================
    // CONFIGURATION
    //=========================================================

    /// <summary>
    /// Injects shared databases from PlayerVisualManager.
    /// </summary>
    public void Configure(
        GameItemDatabase configuredItemDatabase,
        GameItemVisualDatabase configuredVisualDatabase)
    {
        if (configuredItemDatabase == null)
        {
            Debug.LogError(
                "[" + GetType().Name +
                "] Configure failed: GameItemDatabase is null.",
                this);

            return;
        }

        if (configuredVisualDatabase == null)
        {
            Debug.LogError(
                "[" + GetType().Name +
                "] Configure failed: GameItemVisualDatabase is null.",
                this);

            return;
        }

        itemDatabase = configuredItemDatabase;

        visualDatabase = configuredVisualDatabase;

        initialized = false;

        Initialize();

        SubscribeEquipmentEvents();
    }


    //=========================================================
    // INITIALIZATION
    //=========================================================

    protected void Initialize()
    {
        if (initialized)
            return;

        ResolveDatabases();

        if (itemDatabase == null)
        {
            Debug.LogError(
                "[" + GetType().Name +
                "] GameItemDatabase is missing.",
                this);

            return;
        }

        if (visualDatabase == null)
        {
            Debug.LogError(
                "[" + GetType().Name +
                "] GameItemVisualDatabase is missing.",
                this);

            return;
        }

        itemDatabase.Initialize();

        visualDatabase.Initialize();

        EnsureVisualRoot();

        //=====================================================
        // IMPORTANT FIX
        //=====================================================
        //
        // KHÔNG disable legacy visual ở đây.
        //
        // Hiện tại project chưa có model cho 100 item.
        // Nếu disable legacy ngay Initialize():
        //
        // Player model cũ
        //      ↓
        // bị SetActive(false)
        //      ↓
        // visual prefab = null
        //      ↓
        // Player biến mất.
        //
        // Legacy chỉ được disable sau khi replacement
        // prefab Instantiate thành công.
        //=====================================================

        initialized = true;
    }


    //=========================================================
    // DATABASE RESOLUTION
    //=========================================================

    private void ResolveDatabases()
    {
        if (itemDatabase == null)
        {
            itemDatabase =
                FindFirstObjectByType<GameItemDatabase>();
        }

        if (visualDatabase == null)
        {
            visualDatabase =
                FindFirstObjectByType<GameItemVisualDatabase>();
        }
    }


    //=========================================================
    // VISUAL ROOT
    //=========================================================

    private void EnsureVisualRoot()
    {
        if (visualRoot != null)
            return;

        Transform existingRoot =
            transform.Find(RootName);

        if (existingRoot != null)
        {
            visualRoot = existingRoot;
            return;
        }

        GameObject rootObject =
            new GameObject(RootName);

        Transform rootTransform =
            rootObject.transform;

        rootTransform.SetParent(
            transform,
            false);

        rootTransform.localPosition =
            Vector3.zero;

        rootTransform.localRotation =
            Quaternion.identity;

        rootTransform.localScale =
            Vector3.one;

        visualRoot =
            rootTransform;
    }


    //=========================================================
    // EQUIPMENT EVENT
    //=========================================================

    private void SubscribeEquipmentEvents()
    {
        if (subscribed)
            return;

        EquipmentManager manager =
            EquipmentManager.Instance;

        if (manager == null)
            return;

        manager.OnEquipmentChanged +=
            HandleEquipmentChanged;

        subscribed = true;
    }

    private void UnsubscribeEquipmentEvents()
    {
        if (!subscribed)
            return;

        EquipmentManager manager =
            EquipmentManager.Instance;

        if (manager != null)
        {
            manager.OnEquipmentChanged -=
                HandleEquipmentChanged;
        }

        subscribed = false;
    }

    private void HandleEquipmentChanged(
        GameItemCategory changedCategory)
    {
        if (changedCategory != Category)
            return;

        ApplyEquippedItem();
    }


    //=========================================================
    // EQUIPMENT
    //=========================================================

    public void ApplyEquippedItem()
    {
        if (!initialized)
        {
            Initialize();

            if (!initialized)
                return;
        }

        EquipmentManager manager =
            EquipmentManager.Instance;

        if (manager == null)
        {
            ApplyItemById(DefaultItemId);
            return;
        }

        int equippedItemId =
            manager.GetEquippedItemId(Category);

        //=====================================================
        // ACCESSORY / COSMETIC
        //=====================================================
        //
        // 0 = unequipped.
        //
        // Vehicle / Outfit không dùng 0.
        //=====================================================

        if ((Category == GameItemCategory.Accessory ||
             Category == GameItemCategory.Cosmetic) &&
            equippedItemId == 0)
        {
            ClearVisual();
            return;
        }

        //=====================================================
        // INVALID ID
        //=====================================================

        if (!IsValidItemId(equippedItemId))
        {
            equippedItemId =
                DefaultItemId;
        }

        ApplyItemById(equippedItemId);
    }


    //=========================================================
    // APPLY ITEM
    //=========================================================

    public bool ApplyItemById(int itemId)
    {
        if (!initialized)
        {
            Initialize();

            if (!initialized)
                return false;
        }

        if (!IsValidItemId(itemId))
            return false;

        if (!TryResolveVisual(
                itemId,
                out GameItemData item,
                out GameItemVisual visual,
                out GameObject prefab))
        {
            //=================================================
            // IMPORTANT:
            //
            // Không ClearVisual().
            //
            // Item có thể chưa có model.
            // Giữ visual cũ / legacy.
            //=================================================

            return false;
        }

        if (prefab == null)
        {
            return false;
        }

        if (!ReplaceVisual(
                itemId,
                visual,
                prefab))
        {
            return false;
        }

        currentItemId =
            itemId;

        return true;
    }


    //=========================================================
    // RESOLVE VISUAL
    //=========================================================

    private bool TryResolveVisual(
        int itemId,
        out GameItemData item,
        out GameItemVisual visual,
        out GameObject prefab)
    {
        item = null;

        visual = null;

        prefab = null;

        //=====================================================
        // ITEM
        //=====================================================

        if (itemDatabase == null)
            return false;

        if (!itemDatabase.TryGet(
                itemId,
                out item))
        {
            return false;
        }

        if (item == null)
            return false;

        //=====================================================
        // CATEGORY
        //=====================================================

        if (item.Category != Category)
        {
            Debug.LogWarning(
                "[" + GetType().Name +
                "] Item category mismatch. ItemId=" +
                itemId +
                " Expected=" +
                Category +
                " Actual=" +
                item.Category,
                this);

            return false;
        }

        //=====================================================
        // VISUAL DATABASE
        //=====================================================

        if (visualDatabase != null &&
            visualDatabase.TryGet(
                itemId,
                out visual))
        {
            if (visual == null)
                return false;

            //=================================================
            // ENABLED
            //=================================================

            if (!visual.Enabled)
            {
                prefab =
                    item.Prefab;

                return prefab != null;
            }

            //=================================================
            // ITEM ID
            //=================================================

            if (visual.ItemId != itemId)
            {
                Debug.LogError(
                    "[" + GetType().Name +
                    "] Visual ItemId mismatch. " +
                    "Requested=" + itemId +
                    " Visual=" + visual.ItemId,
                    visual);

                prefab =
                    item.Prefab;

                return prefab != null;
            }

            //=================================================
            // VISUAL ID
            //=================================================

            if (string.IsNullOrEmpty(
                    visual.VisualId))
            {
                prefab =
                    item.Prefab;

                return prefab != null;
            }

            //=================================================
            // ANCHOR
            //=================================================

            if (visual.Anchor != RequiredAnchor)
            {
                Debug.LogError(
                    "[" + GetType().Name +
                    "] Visual Anchor mismatch. " +
                    "ItemId=" + itemId +
                    " Expected=" + RequiredAnchor +
                    " Actual=" + visual.Anchor,
                    visual);

                prefab =
                    item.Prefab;

                return prefab != null;
            }

            //=================================================
            // PRIMARY PREFAB
            //=================================================

            prefab =
                visual.Prefab;
        }

        //=====================================================
        // LEGACY FALLBACK
        //=====================================================

        if (prefab == null)
        {
            prefab =
                item.Prefab;
        }

        return prefab != null;
    }


    //=========================================================
    // REPLACE VISUAL
    //=========================================================

    private bool ReplaceVisual(
        int itemId,
        GameItemVisual visual,
        GameObject prefab)
    {
        if (prefab == null)
            return false;

        if (visualRoot == null)
        {
            EnsureVisualRoot();

            if (visualRoot == null)
                return false;
        }

        //=====================================================
        // INSTANTIATE FIRST
        //=====================================================
        //
        // Không Destroy currentInstance trước.
        // Nếu prefab lỗi -> Player cũ vẫn còn.
        //=====================================================

        GameObject newInstance;

        try
        {
            newInstance =
                Instantiate(
                    prefab,
                    visualRoot);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "[" + GetType().Name +
                "] Failed to instantiate visual. " +
                "ItemId=" + itemId +
                " Prefab=" + prefab.name +
                "\n" + exception,
                this);

            return false;
        }

        if (newInstance == null)
            return false;

        newInstance.name =
            InstanceName;

        //=====================================================
        // TRANSFORM
        //=====================================================

        Transform newTransform =
            newInstance.transform;

        if (visual != null)
        {
            newTransform.localPosition =
                visual.LocalPosition;

            newTransform.localRotation =
                visual.LocalRotation;

            newTransform.localScale =
                SanitizeScale(
                    visual.LocalScale);
        }
        else
        {
            newTransform.localPosition =
                Vector3.zero;

            newTransform.localRotation =
                Quaternion.identity;

            newTransform.localScale =
                Vector3.one;
        }

        //=====================================================
        // SANITIZE
        //=====================================================

        SanitizeVisualOnly(
            newInstance);

        //=====================================================
        // SWAP
        //=====================================================

        GameObject previousInstance =
            currentInstance;

        currentInstance =
            newInstance;

        if (previousInstance != null)
        {
            Destroy(
                previousInstance);
        }

        //=====================================================
        // ONLY NOW disable legacy
        //=====================================================

        DisableLegacyVisualRootsAfterReplacement();

        return true;
    }


    //=========================================================
    // SCALE SANITIZATION
    //=========================================================

    private static Vector3 SanitizeScale(
        Vector3 scale)
    {
        if (float.IsNaN(scale.x) ||
            float.IsInfinity(scale.x) ||
            float.IsNaN(scale.y) ||
            float.IsInfinity(scale.y) ||
            float.IsNaN(scale.z) ||
            float.IsInfinity(scale.z))
        {
            return Vector3.one;
        }

        if (Mathf.Abs(scale.x) < 0.000001f ||
            Mathf.Abs(scale.y) < 0.000001f ||
            Mathf.Abs(scale.z) < 0.000001f)
        {
            return Vector3.one;
        }

        return scale;
    }


    //=========================================================
    // SANITIZE VISUAL ONLY
    //=========================================================

    protected virtual void SanitizeVisualOnly(
        GameObject instance)
    {
        if (instance == null)
            return;

        Rigidbody[] rigidbodies =
            instance.GetComponentsInChildren<Rigidbody>(
                true);

        for (int i = 0;
             i < rigidbodies.Length;
             i++)
        {
            Rigidbody rb =
                rigidbodies[i];

            if (rb == null)
                continue;

            rb.isKinematic =
                true;

            rb.useGravity =
                false;

            rb.linearVelocity =
                Vector3.zero;

            rb.angularVelocity =
                Vector3.zero;
        }

        Rigidbody2D[] rigidbodies2D =
            instance.GetComponentsInChildren<Rigidbody2D>(
                true);

        for (int i = 0;
             i < rigidbodies2D.Length;
             i++)
        {
            Rigidbody2D rb =
                rigidbodies2D[i];

            if (rb == null)
                continue;

            rb.bodyType =
                RigidbodyType2D.Kinematic;

            rb.linearVelocity =
                Vector2.zero;

            rb.angularVelocity =
                0f;
        }

        CharacterController[] controllers =
            instance.GetComponentsInChildren<CharacterController>(
                true);

        for (int i = 0;
             i < controllers.Length;
             i++)
        {
            CharacterController controller =
                controllers[i];

            if (controller != null)
            {
                controller.enabled =
                    false;
            }
        }
    }


    //=========================================================
    // CLEAR
    //=========================================================

    /// <summary>
    /// Clears only the runtime visual created by this binder.
    ///
    /// It does NOT destroy legacy visual roots.
    /// </summary>
    public void ClearVisual()
    {
        if (currentInstance != null)
        {
            Destroy(currentInstance);

            currentInstance = null;
        }

        currentItemId = -1;
    }


    //=========================================================
    // LEGACY VISUALS
    //=========================================================

    private void DisableLegacyVisualRootsAfterReplacement()
    {
        if (legacyVisualRoots == null)
            return;

        int count =
            legacyVisualRoots.Length;

        for (int i = 0;
             i < count;
             i++)
        {
            GameObject root =
                legacyVisualRoots[i];

            if (root == null)
                continue;

            Transform rootTransform =
                root.transform;

            //=================================================
            // NEVER disable visualRoot itself.
            //=================================================

            if (visualRoot != null)
            {
                if (rootTransform == visualRoot)
                    continue;

                // Nếu legacy root là parent của visualRoot,
                // disable nó sẽ làm luôn runtime visual biến mất.
                if (visualRoot.IsChildOf(
                        rootTransform))
                {
                    continue;
                }
            }

            root.SetActive(false);
        }
    }


    //=========================================================
    // ITEM RANGE
    //=========================================================

    protected bool IsValidItemId(
        int itemId)
    {
        return itemId >= StartItemId &&
               itemId <= EndItemId;
    }
}