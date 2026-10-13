using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public sealed class LuckyWheelManager : MonoBehaviour
{
    //=====================================================================
    // SINGLETON
    //=====================================================================

    public static LuckyWheelManager Instance { get; private set; }


    //=====================================================================
    // VIP CONFIG
    //=====================================================================

    public const int VipSpinCostGem = 500;


    //=====================================================================
    // PLAYER PREFS KEYS
    //=====================================================================

    private const string DailyTicketKeyPrefix =
        "LKP_DAILY_WHEEL_TICKETS_";

    private const string DailyCompletionGrantKeyPrefix =
        "LKP_DAILY_WHEEL_GRANTED_";


    //=====================================================================
    // WEIGHT CONFIG
    //=====================================================================

    private const double DailyTotalWeight = 100.0;
    private const double VipTotalWeight = 100.0;


    //=====================================================================
    // REWARD TABLES
    //=====================================================================

    private readonly List<LuckyWheelReward> dailyRewards =
        new List<LuckyWheelReward>(12);

    private readonly List<LuckyWheelReward> vipRewards =
        new List<LuckyWheelReward>(15);


    //=====================================================================
    // SPIN LOCKS
    //=====================================================================

    private bool dailySpinLocked;
    private bool vipSpinLocked;


    //=====================================================================
    // EVENTS
    //=====================================================================

    public event Action<LuckyWheelType, int> OnTicketsChanged;

    public event Action<LuckyWheelType, LuckyWheelReward> OnRewardGranted;


    //=====================================================================
    // PUBLIC READ-ONLY REWARD TABLES
    //=====================================================================

    public IReadOnlyList<LuckyWheelReward> DailyRewards =>
        dailyRewards;

    public IReadOnlyList<LuckyWheelReward> VipRewards =>
        vipRewards;


    //=====================================================================
    // CURRENT ACCOUNT
    //=====================================================================

    private string CurrentAccountKey
    {
        get
        {
            FirestorePlayerDataManager firestore =
                FirestorePlayerDataManager.Instance;

            if (
                firestore != null &&
                !string.IsNullOrEmpty(
                    firestore.CurrentPlayerUID)
            )
            {
                return firestore.CurrentPlayerUID;
            }

            return "LOCAL";
        }
    }


    //=====================================================================
    // UNITY LIFECYCLE
    //=====================================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        BuildDailyRewards();
        BuildVipRewards();
    }


    //=====================================================================
    // DAILY TICKETS
    //=====================================================================

    public int GetDailyTickets()
    {
        return Mathf.Max(
            0,
            PlayerPrefs.GetInt(
                DailyTicketKeyPrefix + CurrentAccountKey,
                0));
    }


    //=====================================================================
    // VIP TICKETS
    //=====================================================================

    public int GetVipTickets()
    {
        // VIP không tích ticket.
        // Mỗi lượt quay tiêu trực tiếp 500 Gem.
        return 0;
    }


    //=====================================================================
    // ADD DAILY TICKET
    //=====================================================================

    public bool AddDailyTicket(int amount = 1)
    {
        if (amount <= 0)
            return false;

        int current =
            GetDailyTickets();

        int next =
            SafeAdd(current, amount);

        SaveDailyTickets(next);

        OnTicketsChanged?.Invoke(
            LuckyWheelType.Daily,
            next);

        return true;
    }


    //=====================================================================
    // CAN SPIN - DAILY
    //=====================================================================

    public bool CanSpinDaily()
    {
        return
            !dailySpinLocked &&
            GetDailyTickets() > 0;
    }


    //=====================================================================
    // CAN SPIN - VIP
    //=====================================================================

    public bool CanSpinVip()
    {
        GameManager gameManager =
            GameManager.Instance;

        return
            !vipSpinLocked &&
            gameManager != null &&
            gameManager.CanAffordGem(
                VipSpinCostGem);
    }


    //=====================================================================
    // SPIN DAILY
    //=====================================================================

    public bool TrySpinDaily(
        out LuckyWheelReward reward)
    {
        reward = null;

        //-------------------------------------------------------------
        // PREVENT DOUBLE SPIN
        //-------------------------------------------------------------

        if (dailySpinLocked)
            return false;


        //-------------------------------------------------------------
        // CHECK TICKET
        //-------------------------------------------------------------

        int tickets =
            GetDailyTickets();

        if (tickets <= 0)
            return false;


        //-------------------------------------------------------------
        // LOCK
        //-------------------------------------------------------------

        dailySpinLocked = true;

        try
        {
            //---------------------------------------------------------
            // CONSUME TICKET
            //---------------------------------------------------------

            int remainingTickets =
                tickets - 1;

            SaveDailyTickets(
                remainingTickets);

            OnTicketsChanged?.Invoke(
                LuckyWheelType.Daily,
                remainingTickets);


            //---------------------------------------------------------
            // ROLL
            //---------------------------------------------------------

            reward =
                Roll(
                    dailyRewards,
                    DailyTotalWeight);

            if (reward == null)
                return false;


            //---------------------------------------------------------
            // GRANT
            //---------------------------------------------------------

            if (!GrantReward(reward))
                return false;


            //---------------------------------------------------------
            // EVENT
            //---------------------------------------------------------

            OnRewardGranted?.Invoke(
                LuckyWheelType.Daily,
                reward);


            return true;
        }
        finally
        {
            //---------------------------------------------------------
            // ALWAYS UNLOCK
            //---------------------------------------------------------

            dailySpinLocked = false;
        }
    }


    //=====================================================================
    // SPIN VIP
    //=====================================================================

    public bool TrySpinVip(
        out LuckyWheelReward reward)
    {
        reward = null;

        //-------------------------------------------------------------
        // PREVENT DOUBLE SPIN
        //-------------------------------------------------------------

        if (vipSpinLocked)
            return false;


        //-------------------------------------------------------------
        // GET GAME MANAGER
        //-------------------------------------------------------------

        GameManager gameManager =
            GameManager.Instance;

        if (gameManager == null)
            return false;


        //-------------------------------------------------------------
        // LOCK BEFORE ECONOMY TRANSACTION
        //
        // IMPORTANT:
        // Lock MUST happen before SpendGem().
        //-------------------------------------------------------------

        vipSpinLocked = true;

        try
        {
            //---------------------------------------------------------
            // SPEND 500 GEM
            //---------------------------------------------------------

            if (!gameManager.SpendGem(
                    VipSpinCostGem))
            {
                return false;
            }


            //---------------------------------------------------------
            // ROLL
            //---------------------------------------------------------

            reward =
                Roll(
                    vipRewards,
                    VipTotalWeight);

            if (reward == null)
                return false;


            //---------------------------------------------------------
            // GRANT
            //---------------------------------------------------------

            if (!GrantReward(reward))
                return false;


            //---------------------------------------------------------
            // EVENT
            //---------------------------------------------------------

            OnRewardGranted?.Invoke(
                LuckyWheelType.VIP,
                reward);


            return true;
        }
        finally
        {
            //---------------------------------------------------------
            // ALWAYS UNLOCK
            //---------------------------------------------------------

            vipSpinLocked = false;
        }
    }


    //=====================================================================
    // DAILY COMPLETION MARKER
    //=====================================================================

    public bool HasAlreadyGrantedDailyTicketForDate(
        string dateKey)
    {
        if (string.IsNullOrEmpty(dateKey))
            return false;

        string key =
            DailyCompletionGrantKeyPrefix +
            CurrentAccountKey +
            "_" +
            dateKey;

        return PlayerPrefs.GetInt(
            key,
            0) == 1;
    }


    //=====================================================================
    // MARK DAILY TICKET GRANTED
    //=====================================================================

    public bool TryMarkDailyTicketGranted(
        string dateKey)
    {
        if (string.IsNullOrEmpty(dateKey))
            return false;

        string key =
            DailyCompletionGrantKeyPrefix +
            CurrentAccountKey +
            "_" +
            dateKey;


        //-------------------------------------------------------------
        // ALREADY GRANTED
        //-------------------------------------------------------------

        if (PlayerPrefs.GetInt(
                key,
                0) == 1)
        {
            return false;
        }


        //-------------------------------------------------------------
        // MARK
        //-------------------------------------------------------------

        PlayerPrefs.SetInt(
            key,
            1);

        PlayerPrefs.Save();

        return true;
    }


    //=====================================================================
    // GRANT SHOP ITEM
    //=====================================================================

    private bool GrantShopItemReward(
        LuckyWheelReward reward)
    {
        FirestorePlayerDataManager firestore =
            FirestorePlayerDataManager.Instance;


        //-------------------------------------------------------------
        // PLAYER DATA CHECK
        //-------------------------------------------------------------

        if (
            firestore == null ||
            !firestore.HasPlayerData ||
            firestore.CurrentPlayerData == null
        )
        {
            Debug.LogError(
                "[LuckyWheel] Không có PlayerData để grant Shop Item.");

            return false;
        }


        //-------------------------------------------------------------
        // ITEM ID CHECK
        //-------------------------------------------------------------

        if (reward.shopItemId < 0)
            return false;


        //-------------------------------------------------------------
        // PLAYER DATA
        //-------------------------------------------------------------

        PlayerData data =
            firestore.CurrentPlayerData;


        //-------------------------------------------------------------
        // SAFETY INITIALIZATION
        //-------------------------------------------------------------

        if (data.ownedItemIds == null)
        {
            data.ownedItemIds =
                new List<int>();
        }


        //-------------------------------------------------------------
        // CHECK OWNERSHIP
        //-------------------------------------------------------------

        bool alreadyOwned =
            data.ownedItemIds.Contains(
                reward.shopItemId);


        //-------------------------------------------------------------
        // DUPLICATE → GEM COMPENSATION
        //-------------------------------------------------------------

        if (alreadyOwned)
        {
            int compensation =
                GetDuplicateCompensationGem(
                    reward.shopItemId);

            if (compensation <= 0)
                return false;


            GameManager gameManager =
                GameManager.Instance;

            if (gameManager == null)
                return false;


            gameManager.AddGem(
                compensation);


            Debug.Log(
                $"[LuckyWheel] Duplicate Item " +
                $"{reward.displayName} " +
                $"→ +{compensation} Gem compensation.");

            return true;
        }


        //-------------------------------------------------------------
        // ADD NEW ITEM
        //-------------------------------------------------------------

        data.ownedItemIds.Add(
            reward.shopItemId);


        //-------------------------------------------------------------
        // SAVE ASYNC
        //-------------------------------------------------------------

        _ = SaveShopItemGrantAsync(
            firestore,
            reward);


        return true;
    }


    //=====================================================================
    // SAVE SHOP ITEM
    //=====================================================================

    private async Task SaveShopItemGrantAsync(
        FirestorePlayerDataManager firestore,
        LuckyWheelReward reward)
    {
        bool success =
            await firestore.SavePlayerDataAsync();

        if (!success)
        {
            Debug.LogError(
                $"[LuckyWheel] Không thể lưu Shop Item: " +
                $"{reward.displayName} " +
                $"(ID {reward.shopItemId}).");
        }
    }


    //=====================================================================
    // DUPLICATE ITEM COMPENSATION
    //=====================================================================

    private static int GetDuplicateCompensationGem(
        int itemId)
    {
        switch (itemId)
        {
            case 65:
                return 50;

            case 76:
                return 100;

            case 78:
                return 100;

            case 79:
                return 500;

            case 25:
                return 100;

            case 30:
                return 150;

            case 38:
                return 300;

            case 59:
                return 1000;

            case 99:
                return 2000;

            default:
                return 100;
        }
    }


    //=====================================================================
    // GRANT REWARD
    //=====================================================================

    private bool GrantReward(
        LuckyWheelReward reward)
    {
        if (reward == null)
            return false;


        //-------------------------------------------------------------
        // GAME MANAGER
        //-------------------------------------------------------------

        GameManager gameManager =
            GameManager.Instance;

        if (gameManager == null)
            return false;


        //-------------------------------------------------------------
        // REWARD TYPE
        //-------------------------------------------------------------

        switch (reward.rewardType)
        {
            //=========================================================
            // GOLD
            //=========================================================

            case LuckyRewardType.Gold:

                gameManager.AddGold(
                    reward.amount);

                return true;


            //=========================================================
            // GEM
            //=========================================================

            case LuckyRewardType.Gem:

                gameManager.AddGem(
                    reward.amount);

                return true;


            //=========================================================
            // EXP
            //=========================================================

            case LuckyRewardType.Exp:
            {
                PlayerProgression progression =
                    FindFirstObjectByType<PlayerProgression>();

                if (progression == null)
                {
                    Debug.LogError(
                        "[LuckyWheel] PlayerProgression không tồn tại.");

                    return false;
                }


                _ = progression.AddExpAsync(
                    reward.amount);

                return true;
            }


            //=========================================================
            // SHOP ITEM
            //=========================================================

            case LuckyRewardType.ShopItem:

                return GrantShopItemReward(
                    reward);


            //=========================================================
            // UNKNOWN
            //=========================================================

            default:

                return false;
        }
    }


    //=====================================================================
    // WEIGHTED RANDOM ROLL
    //=====================================================================

    private LuckyWheelReward Roll(
        List<LuckyWheelReward> rewards,
        double totalWeight)
    {
        //-------------------------------------------------------------
        // SAFETY CHECK
        //-------------------------------------------------------------

        if (
            rewards == null ||
            rewards.Count == 0
        )
        {
            return null;
        }


        //-------------------------------------------------------------
        // RANDOM
        //-------------------------------------------------------------

        double randomValue =
            UnityEngine.Random.value *
            totalWeight;


        //-------------------------------------------------------------
        // CUMULATIVE WEIGHT
        //-------------------------------------------------------------

        double cumulative =
            0.0;


        //-------------------------------------------------------------
        // SEARCH
        //-------------------------------------------------------------

        for (int i = 0; i < rewards.Count; i++)
        {
            LuckyWheelReward reward =
                rewards[i];

            if (
                reward == null ||
                reward.weight <= 0.0
            )
            {
                continue;
            }


            cumulative +=
                reward.weight;


            if (randomValue < cumulative)
            {
                return reward;
            }
        }


        //-------------------------------------------------------------
        // FALLBACK
        //-------------------------------------------------------------

        return rewards[
            rewards.Count - 1];
    }


    //=====================================================================
    // SAVE DAILY TICKETS
    //=====================================================================

    private void SaveDailyTickets(
        int value)
    {
        PlayerPrefs.SetInt(
            DailyTicketKeyPrefix +
            CurrentAccountKey,
            Mathf.Max(
                0,
                value));

        PlayerPrefs.Save();
    }


    //=====================================================================
    // SAFE INTEGER ADD
    //=====================================================================

    private static int SafeAdd(
        int a,
        int b)
    {
        long value =
            (long)a + b;


        if (value > int.MaxValue)
            return int.MaxValue;


        if (value < 0)
            return 0;


        return (int)value;
    }


    //=====================================================================
    // BUILD DAILY REWARDS
    //=====================================================================

    private void BuildDailyRewards()
    {
        dailyRewards.Clear();


        //-------------------------------------------------------------
        // 100 GOLD
        //-------------------------------------------------------------

        dailyRewards.Add(
            new LuckyWheelReward(
                "daily_gold_100",
                "100 Gold",
                LuckyRewardType.Gold,
                100,
                25.0));


        //-------------------------------------------------------------
        // 200 GOLD
        //-------------------------------------------------------------

        dailyRewards.Add(
            new LuckyWheelReward(
                "daily_gold_200",
                "200 Gold",
                LuckyRewardType.Gold,
                200,
                20.0));


        //-------------------------------------------------------------
        // 500 GOLD
        //-------------------------------------------------------------

        dailyRewards.Add(
            new LuckyWheelReward(
                "daily_gold_500",
                "500 Gold",
                LuckyRewardType.Gold,
                500,
                12.0));


        //-------------------------------------------------------------
        // 50 EXP
        //-------------------------------------------------------------

        dailyRewards.Add(
            new LuckyWheelReward(
                "daily_exp_50",
                "50 EXP",
                LuckyRewardType.Exp,
                50,
                10.0));


        //-------------------------------------------------------------
        // 100 EXP
        //-------------------------------------------------------------

        dailyRewards.Add(
            new LuckyWheelReward(
                "daily_exp_100",
                "100 EXP",
                LuckyRewardType.Exp,
                100,
                8.0));


        //-------------------------------------------------------------
        // 10 GEM
        //-------------------------------------------------------------

        dailyRewards.Add(
            new LuckyWheelReward(
                "daily_gem_10",
                "10 Gem",
                LuckyRewardType.Gem,
                10,
                7.0));


        //-------------------------------------------------------------
        // Ơ KÌA
        //-------------------------------------------------------------

        dailyRewards.Add(
            new LuckyWheelReward(
                "daily_o_ki_a",
                "Ơ KÌA",
                LuckyRewardType.ShopItem,
                0,
                6.0,
                65));


        //-------------------------------------------------------------
        // GOLD TRAIL
        //-------------------------------------------------------------

        dailyRewards.Add(
            new LuckyWheelReward(
                "daily_gold_trail",
                "Gold Trail",
                LuckyRewardType.ShopItem,
                0,
                4.0,
                76));


        //-------------------------------------------------------------
        // LIGHTNING TRAIL
        //-------------------------------------------------------------

        dailyRewards.Add(
            new LuckyWheelReward(
                "daily_lightning_trail",
                "Lightning Trail",
                LuckyRewardType.ShopItem,
                0,
                3.0,
                78));


        //-------------------------------------------------------------
        // 1,000 GOLD
        //-------------------------------------------------------------

        dailyRewards.Add(
            new LuckyWheelReward(
                "daily_gold_1000",
                "1.000 Gold",
                LuckyRewardType.Gold,
                1000,
                3.0));


        //-------------------------------------------------------------
        // LEGEND TRAIL
        //-------------------------------------------------------------

        dailyRewards.Add(
            new LuckyWheelReward(
                "daily_legend_trail",
                "Legend Trail",
                LuckyRewardType.ShopItem,
                0,
                1.9,
                79));


        //-------------------------------------------------------------
        // 9,999 GEM JACKPOT
        //-------------------------------------------------------------

        dailyRewards.Add(
            new LuckyWheelReward(
                "daily_jackpot",
                "9.999 Gem JACKPOT",
                LuckyRewardType.Gem,
                9999,
                0.1));
    }


    //=====================================================================
    // BUILD VIP REWARDS
    //=====================================================================

    private void BuildVipRewards()
    {
        vipRewards.Clear();


        //-------------------------------------------------------------
        // 1,000 GOLD
        //-------------------------------------------------------------

        vipRewards.Add(
            new LuckyWheelReward(
                "vip_gold_1000",
                "1.000 Gold",
                LuckyRewardType.Gold,
                1000,
                20.0));


        //-------------------------------------------------------------
        // 2,500 GOLD
        //-------------------------------------------------------------

        vipRewards.Add(
            new LuckyWheelReward(
                "vip_gold_2500",
                "2.500 Gold",
                LuckyRewardType.Gold,
                2500,
                12.0));


        //-------------------------------------------------------------
        // 5,000 GOLD
        //-------------------------------------------------------------

        vipRewards.Add(
            new LuckyWheelReward(
                "vip_gold_5000",
                "5.000 Gold",
                LuckyRewardType.Gold,
                5000,
                8.0));


        //-------------------------------------------------------------
        // 100 GEM
        //-------------------------------------------------------------

        vipRewards.Add(
            new LuckyWheelReward(
                "vip_gem_100",
                "100 Gem",
                LuckyRewardType.Gem,
                100,
                10.0));


        //-------------------------------------------------------------
        // 250 GEM
        //-------------------------------------------------------------

        vipRewards.Add(
            new LuckyWheelReward(
                "vip_gem_250",
                "250 Gem",
                LuckyRewardType.Gem,
                250,
                7.0));


        //-------------------------------------------------------------
        // 500 EXP
        //-------------------------------------------------------------

        vipRewards.Add(
            new LuckyWheelReward(
                "vip_exp_500",
                "500 EXP",
                LuckyRewardType.Exp,
                500,
                8.0));


        //-------------------------------------------------------------
        // 1,000 EXP
        //-------------------------------------------------------------

        vipRewards.Add(
            new LuckyWheelReward(
                "vip_exp_1000",
                "1.000 EXP",
                LuckyRewardType.Exp,
                1000,
                5.0));


        //-------------------------------------------------------------
        // LEAD GOLD
        //-------------------------------------------------------------

        vipRewards.Add(
            new LuckyWheelReward(
                "vip_lead_gold",
                "LEAD Gold",
                LuckyRewardType.ShopItem,
                0,
                8.0,
                25));


        //-------------------------------------------------------------
        // NINJA LEAD
        //-------------------------------------------------------------

        vipRewards.Add(
            new LuckyWheelReward(
                "vip_ninja_lead",
                "Ninja Lead",
                LuckyRewardType.ShopItem,
                0,
                6.0,
                30));


        //-------------------------------------------------------------
        // LIMITED RIDE
        //-------------------------------------------------------------

        vipRewards.Add(
            new LuckyWheelReward(
                "vip_limited_ride",
                "Limited Ride",
                LuckyRewardType.ShopItem,
                0,
                5.0,
                38));


        //-------------------------------------------------------------
        // GOLD TRAIL
        //-------------------------------------------------------------

        vipRewards.Add(
            new LuckyWheelReward(
                "vip_gold_trail",
                "Gold Trail",
                LuckyRewardType.ShopItem,
                0,
                4.0,
                76));


        //-------------------------------------------------------------
        // LEGEND TRAIL
        //-------------------------------------------------------------

        vipRewards.Add(
            new LuckyWheelReward(
                "vip_legend_trail",
                "Legend Trail",
                LuckyRewardType.ShopItem,
                0,
                3.0,
                79));


        //-------------------------------------------------------------
        // LEGEND STREET SET
        //-------------------------------------------------------------

        vipRewards.Add(
            new LuckyWheelReward(
                "vip_legend_street",
                "Legend Street Set",
                LuckyRewardType.ShopItem,
                0,
                2.0,
                59));


        //-------------------------------------------------------------
        // HUY HIỆU LEGEND
        //-------------------------------------------------------------

        vipRewards.Add(
            new LuckyWheelReward(
                "vip_legend_badge",
                "Huy Hiệu Legend",
                LuckyRewardType.ShopItem,
                0,
                1.9999,
                99));


        //-------------------------------------------------------------
        // 999,999 GEM JACKPOT
        //-------------------------------------------------------------

        vipRewards.Add(
            new LuckyWheelReward(
                "vip_jackpot",
                "999.999 Gem JACKPOT",
                LuckyRewardType.Gem,
                999999,
                0.0001));
    }
}