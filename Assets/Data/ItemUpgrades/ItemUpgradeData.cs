using UnityEngine;

public enum UpgradeItemType
{
    Gum,
    Photon,
    Shield,
    Magnet
}

[System.Serializable]
public class ItemUpgradeLevel
{
    [Header("Level")]
    [Min(1)]
    public int level = 1;

    [Header("Common")]
    [Min(0f)]
    public float duration = 0f;

    [Header("Gum")]
    [Min(0f)]
    public float horizontalSpeed = 0f;

    [Header("Photon")]
    [Min(0f)]
    public float speedMultiplier = 0f;

    [Header("Shield")]
    [Min(1)]
    public int maxHits = 1;

    [Header("Magnet")]
    [Min(0f)]
    public float magnetRadius = 0f;

    [Min(0f)]
    public float pullSpeed = 0f;

    [Min(0f)]
    public float collectDistance = 0f;

    [Header("Upgrade Cost")]
    [Min(0)]
    public int upgradeCost = 0;
}


[CreateAssetMenu(
    fileName = "ItemUpgradeData",
    menuName = "Lead Khong Phanh/Item Upgrade Data"
)]
public class ItemUpgradeData : ScriptableObject
{
    [Header("Item")]
    public UpgradeItemType itemType;

    [Header("Upgrade Settings")]
    [Min(1)]
    public int maxLevel = 20;

    [Header("Upgrade Levels")]
    public ItemUpgradeLevel[] levels;


    // =========================================================
    // GET LEVEL
    // =========================================================

    public bool TryGetLevel(
        int targetLevel,
        out ItemUpgradeLevel result)
    {
        result = null;

        if (levels == null ||
            levels.Length == 0)
        {
            return false;
        }

        if (targetLevel < 1 ||
            targetLevel > maxLevel)
        {
            return false;
        }

        int index = targetLevel - 1;

        if (index < 0 ||
            index >= levels.Length)
        {
            return false;
        }

        result = levels[index];

        return result != null;
    }


    public ItemUpgradeLevel GetLevel(
        int targetLevel)
    {
        if (levels == null ||
            levels.Length == 0)
        {
            return null;
        }

        if (targetLevel < 1 ||
            targetLevel > maxLevel)
        {
            return null;
        }

        int index = targetLevel - 1;

        if (index < 0 ||
            index >= levels.Length)
        {
            return null;
        }

        return levels[index];
    }


    // =========================================================
    // MAX LEVEL
    // =========================================================

    public bool IsMaxLevel(
        int currentLevel)
    {
        return currentLevel >= maxLevel;
    }


    // =========================================================
    // NEXT UPGRADE COST
    // =========================================================

    public bool TryGetNextUpgradeCost(
        int currentLevel,
        out int cost)
    {
        cost = 0;

        if (currentLevel < 1 ||
            currentLevel >= maxLevel)
        {
            return false;
        }

        ItemUpgradeLevel nextLevel;

        if (!TryGetLevel(
                currentLevel + 1,
                out nextLevel))
        {
            return false;
        }

        cost = Mathf.Max(
            0,
            nextLevel.upgradeCost
        );

        return true;
    }


#if UNITY_EDITOR

    private void OnValidate()
    {
        if (maxLevel < 1)
        {
            maxLevel = 1;
        }

        if (levels == null)
        {
            return;
        }

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] == null)
            {
                continue;
            }

            levels[i].level = i + 1;

            if (levels[i].duration < 0f)
            {
                levels[i].duration = 0f;
            }

            if (levels[i].horizontalSpeed < 0f)
            {
                levels[i].horizontalSpeed = 0f;
            }

            if (levels[i].speedMultiplier < 0f)
            {
                levels[i].speedMultiplier = 0f;
            }

            if (levels[i].maxHits < 1)
            {
                levels[i].maxHits = 1;
            }

            if (levels[i].magnetRadius < 0f)
            {
                levels[i].magnetRadius = 0f;
            }

            if (levels[i].pullSpeed < 0f)
            {
                levels[i].pullSpeed = 0f;
            }

            if (levels[i].collectDistance < 0f)
            {
                levels[i].collectDistance = 0f;
            }

            if (levels[i].upgradeCost < 0)
            {
                levels[i].upgradeCost = 0;
            }
        }
    }

#endif
}