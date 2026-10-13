using UnityEngine;

public sealed class GemDebugCheat : MonoBehaviour
{
    // ============================================================
    // DEBUG SETTINGS
    // ============================================================

    [Header("Debug Settings")]
    [SerializeField, Min(1)]
    private int addGemsAmount = 100;

    [SerializeField, Min(0)]
    private int setGemsAmount = 1000;

    // ============================================================
    // KEYBOARD
    // ============================================================

    [Header("Keyboard")]
    [SerializeField]
    private bool enableKeyboardCheat = true;

    [SerializeField]
    private KeyCode addGemsKey = KeyCode.F9;

    [SerializeField]
    private KeyCode setGemsKey = KeyCode.F10;

    // ============================================================
    // UPDATE
    // ============================================================

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

        if (!enableKeyboardCheat)
            return;

        if (Input.GetKeyDown(addGemsKey))
        {
            AddGems();
        }

        if (Input.GetKeyDown(setGemsKey))
        {
            SetGems();
        }

#endif
    }

    // ============================================================
    // ADD GEMS
    // ============================================================

    [ContextMenu("Add Gems")]
    public void AddGems()
    {
        FirestorePlayerDataManager manager =
            FirestorePlayerDataManager.Instance;

        if (manager == null)
        {
            Debug.LogError(
                "[GemDebugCheat] FirestorePlayerDataManager chưa tồn tại.");
            return;
        }

        if (!manager.HasPlayerData ||
            manager.CurrentPlayerData == null)
        {
            Debug.LogError(
                "[GemDebugCheat] Chưa có PlayerData. Hãy đăng nhập trước.");
            return;
        }

        if (addGemsAmount <= 0)
        {
            Debug.LogError(
                "[GemDebugCheat] addGemsAmount phải > 0.");
            return;
        }

        GameManager gameManager = GameManager.Instance;

        if (gameManager == null)
        {
            Debug.LogError(
                "[GemDebugCheat] GameManager chưa tồn tại.");
            return;
        }

        int before = manager.CurrentPlayerData.gems;

        // ========================================================
        // DÙNG API CHÍNH THỐNG
        // API này sẽ:
        // 1. Cộng GEM
        // 2. Save Firestore
        // 3. Update PlayerData
        // 4. Invoke OnGemChanged
        // ========================================================

        gameManager.AddGem(addGemsAmount);

        Debug.Log(
            $"[GemDebugCheat] Gửi yêu cầu +{addGemsAmount} GEM. " +
            $"Before = {before}");
    }

    // ============================================================
    // SET GEMS
    // ============================================================

    [ContextMenu("Set Gems")]
    public void SetGems()
    {
        FirestorePlayerDataManager manager =
            FirestorePlayerDataManager.Instance;

        if (manager == null)
        {
            Debug.LogError(
                "[GemDebugCheat] FirestorePlayerDataManager chưa tồn tại.");
            return;
        }

        if (!manager.HasPlayerData ||
            manager.CurrentPlayerData == null)
        {
            Debug.LogError(
                "[GemDebugCheat] Chưa có PlayerData. Hãy đăng nhập trước.");
            return;
        }

        GameManager gameManager = GameManager.Instance;

        if (gameManager == null)
        {
            Debug.LogError(
                "[GemDebugCheat] GameManager chưa tồn tại.");
            return;
        }

        int target = Mathf.Max(0, setGemsAmount);

        manager.CurrentPlayerData.gems = target;

        Debug.Log(
            $"[GemDebugCheat] GEM set local = {target}");

        // ========================================================
        // SAVE FIRESTORE + UPDATE HUD
        // ========================================================

        gameManager.SaveGemDebugValue(target);
    }
}