using UnityEngine;

/// <summary>
/// Accessory visual binder.
/// Database contract: Accessory ItemId = 81..100.
/// Default fallback = ItemId 81.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerAccessoryVisualBinder : PlayerCategoryVisualBinderBase
{
    private const int AccessoryStartId = 81;
    private const int AccessoryEndId = 100;
    private const int DefaultAccessoryId = 81;

    protected override GameItemCategory Category => GameItemCategory.Accessory;
    protected override int StartItemId => AccessoryStartId;
    protected override int EndItemId => AccessoryEndId;
    protected override int DefaultItemId => DefaultAccessoryId;
    protected override GameItemVisualAnchor RequiredAnchor => GameItemVisualAnchor.AccessoryRoot;
    protected override string RootName => "AccessoryVisualRoot";
    protected override string InstanceName => "PlayerAccessory";

    public int CurrentAccessoryId => CurrentItemId;
    public GameObject CurrentAccessoryInstance => CurrentInstance;
    public Transform AccessoryVisualRoot => VisualRoot;
    public bool HasAccessoryVisual => HasVisual;

    public void ApplyEquippedAccessory()
    {
        ApplyEquippedItem();
    }

    public bool ApplyAccessoryById(int accessoryId)
    {
        return ApplyItemById(accessoryId);
    }

    public void ClearAccessoryVisual()
    {
        ClearVisual();
    }

    [ContextMenu("Apply Equipped Accessory")]
    private void DebugApplyEquippedAccessory()
    {
        ApplyEquippedAccessory();
    }

    [ContextMenu("Apply Default Accessory 81")]
    private void DebugApplyDefaultAccessory()
    {
        ApplyAccessoryById(DefaultAccessoryId);
    }

    [ContextMenu("Clear Accessory Visual")]
    private void DebugClearAccessory()
    {
        ClearAccessoryVisual();
    }
}
