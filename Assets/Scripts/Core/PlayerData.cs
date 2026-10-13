using System;
using System.Collections.Generic;

[Serializable]
public class MissionSaveData
{
    public string id;
    public int progress;
    public bool claimed;

    public MissionSaveData()
    {
        id = string.Empty;
        progress = 0;
        claimed = false;
    }

    public MissionSaveData(
        string id,
        int progress,
        bool claimed)
    {
        this.id = id;
        this.progress = progress;
        this.claimed = claimed;
    }
}


[Serializable]
public class PlayerData
{
    // =========================================================
    // ACCOUNT
    // =========================================================

    public string uid;
    public string displayName;


    // =========================================================
    // PROGRESSION
    // =========================================================

    public int level;
    public int exp;


    // =========================================================
    // RUN STATISTICS
    // =========================================================

    public int totalRuns;


    // =========================================================
    // ECONOMY
    // =========================================================

    public int coins;
    public int gems;
    public int bestScore;


    // =========================================================
    // LEGACY BIKE
    // =========================================================

    /// <summary>
    /// Legacy vehicle identifier.
    ///
    /// Giữ lại để tương thích hệ thống cũ.
    /// Hệ thống Equipment mới sử dụng equippedVehicleItemId.
    /// </summary>
    public string selectedBike;


    /// <summary>
    /// Legacy danh sách bike.
    ///
    /// Giữ lại để tương thích dữ liệu account cũ.
    /// Không dùng làm nguồn dữ liệu chính cho Inventory mới.
    /// </summary>
    public List<string> ownedBikes =
        new List<string>();


    // =========================================================
    // ITEM OWNERSHIP
    // =========================================================

    /// <summary>
    /// Danh sách Item ID mà player sở hữu.
    ///
    /// Category của Item KHÔNG được suy luận
    /// từ khoảng ID.
    ///
    /// Category phải được lấy từ GameItemDatabase.
    ///
    /// Player mới mặc định sở hữu Item ID 1.
    ///
    /// Không lưu duplicate Item ID.
    /// </summary>
    public List<int> ownedItemIds =
        new List<int>();


    // =========================================================
    // EQUIPMENT
    // =========================================================

    /// <summary>
    /// Vehicle hiện tại đang trang bị.
    ///
    /// Default = 1
    ///
    /// 0 = chưa trang bị.
    /// </summary>
    public int equippedVehicleItemId;


    /// <summary>
    /// Outfit hiện tại đang trang bị.
    ///
    /// Default = 1
    ///
    /// 0 = chưa trang bị.
    /// </summary>
    public int equippedOutfitItemId;


    /// <summary>
    /// Accessory hiện tại đang trang bị.
    ///
    /// Default = 0.
    /// </summary>
    public int equippedAccessoryItemId;


    /// <summary>
    /// Cosmetic hiện tại đang trang bị.
    ///
    /// Default = 0.
    /// </summary>
    public int equippedCosmeticItemId;


    // =========================================================
    // ITEM UPGRADES
    // =========================================================

    public int gumUpgradeLevel;

    public int photonUpgradeLevel;

    public int shieldUpgradeLevel;

    public int magnetUpgradeLevel;


    // =========================================================
    // DAILY MISSIONS
    // =========================================================

    public string missionDate;

    public List<string> missionSelection =
        new List<string>();

    public int missionDailyCoins;

    public int missionDailyDistance;

    public int missionDailyRuns;

    public int missionDailyExp;

    public int missionPreviousTotalExp;

    public List<MissionSaveData> missionStates =
        new List<MissionSaveData>();


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public PlayerData()
    {
        // -----------------------------------------------------
        // ACCOUNT
        // -----------------------------------------------------

        uid = string.Empty;

        displayName = string.Empty;


        // -----------------------------------------------------
        // PROGRESSION
        // -----------------------------------------------------

        level = 1;

        exp = 0;


        // -----------------------------------------------------
        // RUN STATISTICS
        // -----------------------------------------------------

        totalRuns = 0;


        // -----------------------------------------------------
        // ECONOMY
        // -----------------------------------------------------

        coins = 0;

        gems = 0;

        bestScore = 0;


        // -----------------------------------------------------
        // LEGACY BIKE
        // -----------------------------------------------------

        selectedBike = "NinjaLead";

        ownedBikes = new List<string>
        {
            "NinjaLead"
        };


        // -----------------------------------------------------
        // ITEM OWNERSHIP
        // -----------------------------------------------------

        // Player mới mặc định sở hữu Item ID 1.
        //
        // Không thêm duplicate.
        // Không dùng ID range để xác định Category.
        //
        // Category được xác định bởi GameItemDatabase.

        ownedItemIds = new List<int>
        {
            1
        };


        // -----------------------------------------------------
        // EQUIPMENT
        // -----------------------------------------------------

        // Default Vehicle
        equippedVehicleItemId = 1;


        // Default Outfit
        equippedOutfitItemId = 1;


        // Không có Accessory mặc định.
        equippedAccessoryItemId = 0;


        // Không có Cosmetic mặc định.
        equippedCosmeticItemId = 0;


        // -----------------------------------------------------
        // ITEM UPGRADES
        // -----------------------------------------------------

        gumUpgradeLevel = 1;

        photonUpgradeLevel = 1;

        shieldUpgradeLevel = 1;

        magnetUpgradeLevel = 1;


        // -----------------------------------------------------
        // DAILY MISSIONS
        // -----------------------------------------------------

        missionDate = string.Empty;

        missionSelection = new List<string>();

        missionDailyCoins = 0;

        missionDailyDistance = 0;

        missionDailyRuns = 0;

        missionDailyExp = 0;

        missionPreviousTotalExp = 0;

        missionStates = new List<MissionSaveData>();
    }
}