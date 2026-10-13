using UnityEngine;

/// <summary>
/// Cosmetic visual binder.
/// Database contract: Cosmetic ItemId = 61..80.
/// Default fallback = ItemId 61.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerCosmeticVisualBinder : PlayerCategoryVisualBinderBase
{
    private const int CosmeticStartId = 61;
    private const int CosmeticEndId = 80;
    private const int DefaultCosmeticId = 61;

    protected override GameItemCategory Category => GameItemCategory.Cosmetic;
    protected override int StartItemId => CosmeticStartId;
    protected override int EndItemId => CosmeticEndId;
    protected override int DefaultItemId => DefaultCosmeticId;
    protected override GameItemVisualAnchor RequiredAnchor => GameItemVisualAnchor.CosmeticRoot;
    protected override string RootName => "CosmeticVisualRoot";
    protected override string InstanceName => "PlayerCosmetic";

    public int CurrentCosmeticId => CurrentItemId;
    public GameObject CurrentCosmeticInstance => CurrentInstance;
    public Transform CosmeticVisualRoot => VisualRoot;
    public bool HasCosmeticVisual => HasVisual;

    public void ApplyEquippedCosmetic()
    {
        ApplyEquippedItem();
    }

    public bool ApplyCosmeticById(int cosmeticId)
    {
        return ApplyItemById(cosmeticId);
    }

    public void ClearCosmeticVisual()
    {
        ClearVisual();
    }

    [ContextMenu("Apply Equipped Cosmetic")]
    private void DebugApplyEquippedCosmetic()
    {
        ApplyEquippedCosmetic();
    }

    [ContextMenu("Apply Default Cosmetic 61")]
    private void DebugApplyDefaultCosmetic()
    {
        ApplyCosmeticById(DefaultCosmeticId);
    }

    [ContextMenu("Clear Cosmetic Visual")]
    private void DebugClearCosmetic()
    {
        ClearCosmeticVisual();
    }
}
