using UnityEngine;

/// <summary>
/// Vehicle visual binder.
/// Database contract: Vehicle ItemId = 1..40.
/// Default Vehicle = ItemId 1.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public sealed class PlayerVehicleVisualBinder : PlayerCategoryVisualBinderBase
{
    private const int VehicleStartId = 1;
    private const int VehicleEndId = 40;
    private const int DefaultVehicleId = 1;

    protected override GameItemCategory Category => GameItemCategory.Vehicle;
    protected override int StartItemId => VehicleStartId;
    protected override int EndItemId => VehicleEndId;
    protected override int DefaultItemId => DefaultVehicleId;
    protected override GameItemVisualAnchor RequiredAnchor => GameItemVisualAnchor.VehicleRoot;
    protected override string RootName => "VehicleVisualRoot";
    protected override string InstanceName => "PlayerVehicle";

    public int GetCurrentVehicleId()
    {
        return CurrentItemId;
    }

    public GameObject GetCurrentVehicleInstance()
    {
        return CurrentInstance;
    }

    public void ApplyEquippedVehicle()
    {
        ApplyEquippedItem();
    }

    public bool ApplyVehicleById(int vehicleId)
    {
        return ApplyItemById(vehicleId);
    }

    [ContextMenu("Apply Equipped Vehicle")]
    private void DebugApplyEquippedVehicle()
    {
        ApplyEquippedVehicle();
    }

    [ContextMenu("Clear Vehicle Visual")]
    private void DebugClearVehicle()
    {
        ClearVisual();
    }
}
