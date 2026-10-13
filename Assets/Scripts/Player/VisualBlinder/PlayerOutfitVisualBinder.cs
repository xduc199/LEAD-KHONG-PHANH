using UnityEngine;

/// <summary>
/// Outfit visual binder.
/// Database contract: Outfit ItemId = 41..60.
/// Default Outfit = ItemId 41.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerOutfitVisualBinder : PlayerCategoryVisualBinderBase
{
    private const int OutfitStartId = 41;
    private const int OutfitEndId = 60;
    private const int DefaultOutfitId = 41;

    protected override GameItemCategory Category => GameItemCategory.Outfit;
    protected override int StartItemId => OutfitStartId;
    protected override int EndItemId => OutfitEndId;
    protected override int DefaultItemId => DefaultOutfitId;
    protected override GameItemVisualAnchor RequiredAnchor => GameItemVisualAnchor.OutfitRoot;
    protected override string RootName => "OutfitVisualRoot";
    protected override string InstanceName => "PlayerOutfit";

    public int CurrentOutfitId => CurrentItemId;
    public GameObject CurrentOutfitInstance => CurrentInstance;
    public Transform OutfitVisualRoot => VisualRoot;
    public bool HasOutfitVisual => HasVisual;

    public void ApplyEquippedOutfit()
    {
        ApplyEquippedItem();
    }

    public bool ApplyOutfitById(int outfitId)
    {
        return ApplyItemById(outfitId);
    }

    public void ClearOutfitVisual()
    {
        ClearVisual();
    }

    [ContextMenu("Apply Equipped Outfit")]
    private void DebugApplyEquippedOutfit()
    {
        ApplyEquippedOutfit();
    }

    [ContextMenu("Apply Default Outfit 41")]
    private void DebugApplyDefaultOutfit()
    {
        ApplyOutfitById(DefaultOutfitId);
    }

    [ContextMenu("Clear Outfit Visual")]
    private void DebugClearOutfit()
    {
        ClearOutfitVisual();
    }
}
