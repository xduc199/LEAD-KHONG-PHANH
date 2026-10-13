using System;

public enum LuckyWheelType
{
    Daily = 0,
    VIP = 1
}

public enum LuckyRewardType
{
    Gold = 0,
    Exp = 1,
    Gem = 2,
    ShopItem = 3
}

[Serializable]
public sealed class LuckyWheelReward
{
    public string id;
    public string displayName;
    public LuckyRewardType rewardType;
    public int amount;
    public int shopItemId;
    public double weight;

    public LuckyWheelReward(
        string id,
        string displayName,
        LuckyRewardType rewardType,
        int amount,
        double weight,
        int shopItemId = -1)
    {
        this.id = id;
        this.displayName = displayName;
        this.rewardType = rewardType;
        this.amount = amount;
        this.weight = weight;
        this.shopItemId = shopItemId;
    }
}
