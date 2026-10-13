using UnityEngine;

public sealed class InventoryDebugCheat : MonoBehaviour
{
    [Header("TEST OUTFITS")]
    [SerializeField] private int outfitId1 = 40;
    [SerializeField] private int outfitId2 = 41;
    [SerializeField] private int outfitId3 = 42;

    [ContextMenu("CHEAT - Unlock Test Outfits")]
    public void UnlockTestOutfits()
    {
        InventoryOwnership.SetOwned(outfitId1);
        InventoryOwnership.SetOwned(outfitId2);
        InventoryOwnership.SetOwned(outfitId3);

        Debug.Log(
            "[InventoryDebugCheat] Đã mở khóa Outfit ID: "
            + outfitId1 + ", "
            + outfitId2 + ", "
            + outfitId3
        );
    }

    [ContextMenu("CHEAT - Check Ownership")]
    public void CheckOwnership()
    {
        Debug.Log(
            "[InventoryDebugCheat] "
            + "ID " + outfitId1 + " = " + InventoryOwnership.IsOwned(outfitId1)
            + " | ID " + outfitId2 + " = " + InventoryOwnership.IsOwned(outfitId2)
            + " | ID " + outfitId3 + " = " + InventoryOwnership.IsOwned(outfitId3)
        );
    }
}