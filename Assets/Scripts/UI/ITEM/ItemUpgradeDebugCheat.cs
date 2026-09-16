using UnityEngine;

public sealed class ItemUpgradeDebugCheat : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField, Min(1)]
    private int addCoinsAmount = 10000;

    [SerializeField, Min(0)]
    private int setCoinsAmount = 100000;

    [Header("Keyboard")]
    [SerializeField]
    private bool enableKeyboardCheat = true;

    [SerializeField]
    private KeyCode addCoinsKey = KeyCode.F6;

    [SerializeField]
    private KeyCode setCoinsKey = KeyCode.F7;

    [SerializeField]
    private KeyCode maxAllUpgradeKey = KeyCode.F8;

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!enableKeyboardCheat)
            return;

        if (Input.GetKeyDown(addCoinsKey))
        {
            AddCoins();
        }

        if (Input.GetKeyDown(setCoinsKey))
        {
            SetCoins();
        }

        if (Input.GetKeyDown(maxAllUpgradeKey))
        {
            MaxAllUpgrades();
        }
#endif
    }

    [ContextMenu("Add Coins")]
    public void AddCoins()
    {
        FirestorePlayerDataManager manager = FirestorePlayerDataManager.Instance;

        if (manager == null)
        {
            Debug.LogError(
                "[ItemUpgradeDebugCheat] FirestorePlayerDataManager chưa tồn tại.");
            return;
        }

        if (!manager.HasPlayerData || manager.CurrentPlayerData == null)
        {
            Debug.LogError(
                "[ItemUpgradeDebugCheat] Chưa có PlayerData. Hãy đăng nhập trước.");
            return;
        }

        manager.CurrentPlayerData.coins += addCoinsAmount;

        Debug.Log(
            $"[ItemUpgradeDebugCheat] +{addCoinsAmount} coins. " +
            $"Current = {manager.CurrentPlayerData.coins}");
    }

    [ContextMenu("Set Coins")]
    public void SetCoins()
    {
        FirestorePlayerDataManager manager = FirestorePlayerDataManager.Instance;

        if (manager == null)
        {
            Debug.LogError(
                "[ItemUpgradeDebugCheat] FirestorePlayerDataManager chưa tồn tại.");
            return;
        }

        if (!manager.HasPlayerData || manager.CurrentPlayerData == null)
        {
            Debug.LogError(
                "[ItemUpgradeDebugCheat] Chưa có PlayerData. Hãy đăng nhập trước.");
            return;
        }

        manager.CurrentPlayerData.coins = setCoinsAmount;

        Debug.Log(
            $"[ItemUpgradeDebugCheat] Coins = {manager.CurrentPlayerData.coins}");
    }

    [ContextMenu("Max All Item Upgrades")]
    public void MaxAllUpgrades()
    {
        FirestorePlayerDataManager manager = FirestorePlayerDataManager.Instance;

        if (manager == null)
        {
            Debug.LogError(
                "[ItemUpgradeDebugCheat] FirestorePlayerDataManager chưa tồn tại.");
            return;
        }

        if (!manager.HasPlayerData || manager.CurrentPlayerData == null)
        {
            Debug.LogError(
                "[ItemUpgradeDebugCheat] Chưa có PlayerData. Hãy đăng nhập trước.");
            return;
        }

        ItemUpgradeManager upgradeManager = ItemUpgradeManager.Instance;

        if (upgradeManager == null)
        {
            Debug.LogError(
                "[ItemUpgradeDebugCheat] ItemUpgradeManager chưa tồn tại.");
            return;
        }

        int gumMax = upgradeManager.GetMaxLevel(UpgradeItemType.Gum);
        int photonMax = upgradeManager.GetMaxLevel(UpgradeItemType.Photon);
        int shieldMax = upgradeManager.GetMaxLevel(UpgradeItemType.Shield);
        int magnetMax = upgradeManager.GetMaxLevel(UpgradeItemType.Magnet);

        manager.CurrentPlayerData.gumUpgradeLevel = gumMax;
        manager.CurrentPlayerData.photonUpgradeLevel = photonMax;
        manager.CurrentPlayerData.shieldUpgradeLevel = shieldMax;
        manager.CurrentPlayerData.magnetUpgradeLevel = magnetMax;

        Debug.Log(
            $"[ItemUpgradeDebugCheat] MAX ALL: " +
            $"Gum={gumMax}, " +
            $"Photon={photonMax}, " +
            $"Shield={shieldMax}, " +
            $"Magnet={magnetMax}");
    }
}