using UnityEngine;

[CreateAssetMenu(
    fileName = "GameItemVisual_",
    menuName = "LEAD KHONG PHANH/Game Data/Item Visual"
)]
public sealed class GameItemVisual : ScriptableObject
{
    // =========================================================
    // IDENTITY
    // =========================================================

    [Header("Identity")]

    [SerializeField]
    private string visualId;

    [SerializeField]
    private int itemId;

    [SerializeField]
    private string characterId;

    // =========================================================
    // ANCHOR
    // =========================================================

    [Header("Anchor")]

    [SerializeField]
    private GameItemVisualAnchor anchor;

    // =========================================================
    // PREFAB
    // =========================================================

    [Header("Prefab")]

    [SerializeField]
    private GameObject prefab;

    // =========================================================
    // TRANSFORM
    // =========================================================

    [Header("Local Transform")]

    [SerializeField]
    private Vector3 localPosition;

    [SerializeField]
    private Vector3 localRotation;

    [SerializeField]
    private Vector3 localScale = Vector3.one;

    // =========================================================
    // STATE
    // =========================================================

    [Header("State")]

    [SerializeField]
    private bool enabled = true;

    // =========================================================
    // PUBLIC API
    // =========================================================

    public string VisualId => visualId;

    public int ItemId => itemId;

    public string CharacterId => characterId;

    public GameItemVisualAnchor Anchor => anchor;

    public GameObject Prefab => prefab;

    public Vector3 LocalPosition => localPosition;

    public Quaternion LocalRotation =>
        Quaternion.Euler(localRotation);

    public Vector3 LocalScale => localScale;

    public bool Enabled => enabled;

#if UNITY_EDITOR

    // =========================================================
    // EDITOR VALIDATION
    // =========================================================

    private void OnValidate()
    {
        // -----------------------------------------------------
        // VISUAL ID
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(visualId))
        {
            visualId = $"VISUAL_ITEM_{itemId:000}";
        }

        // -----------------------------------------------------
        // ITEM ID
        // -----------------------------------------------------

        if (itemId < GameItemDatabase.MinItemId ||
            itemId > GameItemDatabase.MaxItemId)
        {
            Debug.LogError(
                $"[GameItemVisual] Invalid ItemId={itemId}. " +
                $"Expected {GameItemDatabase.MinItemId}-" +
                $"{GameItemDatabase.MaxItemId}.\n" +
                $"Asset={name}",
                this
            );
        }

        // -----------------------------------------------------
        // SCALE
        // -----------------------------------------------------

        if (Mathf.Abs(localScale.x) < 0.0001f ||
            Mathf.Abs(localScale.y) < 0.0001f ||
            Mathf.Abs(localScale.z) < 0.0001f)
        {
            localScale = Vector3.one;
        }

        // -----------------------------------------------------
        // PREFAB
        // -----------------------------------------------------

        if (prefab == null)
        {
            Debug.LogWarning(
                $"[GameItemVisual] ItemId={itemId} " +
                $"has NO prefab.\n" +
                $"Visual={name}",
                this
            );
        }

        // -----------------------------------------------------
        // CHARACTER ID
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(characterId))
        {
            Debug.LogWarning(
                $"[GameItemVisual] ItemId={itemId} " +
                $"has empty CharacterId.\n" +
                $"Visual={name}",
                this
            );
        }

        // -----------------------------------------------------
        // CATEGORY / ANCHOR VALIDATION
        // -----------------------------------------------------

        ValidateAnchorAgainstItemId();
    }

    private void ValidateAnchorAgainstItemId()
    {
        GameItemVisualAnchor expectedAnchor;

        if (itemId >= 1 && itemId <= 40)
        {
            expectedAnchor = GameItemVisualAnchor.VehicleRoot;
        }
        else if (itemId >= 41 && itemId <= 60)
        {
            expectedAnchor = GameItemVisualAnchor.OutfitRoot;
        }
        else if (itemId >= 81 && itemId <= 100)
        {
            expectedAnchor = GameItemVisualAnchor.AccessoryRoot;
        }
        else if (itemId >= 61 && itemId <= 80)
        {
            expectedAnchor = GameItemVisualAnchor.CosmeticRoot;
        }
        else
        {
            return;
        }

        if (anchor != expectedAnchor)
        {
            Debug.LogError(
                $"[GameItemVisual] Anchor mismatch.\n" +
                $"ItemId={itemId}\n" +
                $"Expected={expectedAnchor}\n" +
                $"Actual={anchor}\n" +
                $"Asset={name}",
                this
            );
        }
    }

#endif
}

// =============================================================
// VISUAL ANCHOR
// =============================================================

public enum GameItemVisualAnchor
{
    VehicleRoot = 0,
    OutfitRoot = 1,
    AccessoryRoot = 2,
    CosmeticRoot = 3
}