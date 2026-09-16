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
    // ECONOMY
    // =========================================================

    public int coins;
    public int bestScore;


    // =========================================================
    // BIKE
    // =========================================================

    public string selectedBike;

    public List<string> ownedBikes =
        new List<string>();


    // =========================================================
    // ITEM UPGRADES
    // =========================================================

    /// <summary>
    /// Level nâng cấp Gum.
    /// Mặc định Level 1.
    /// </summary>
    public int gumUpgradeLevel;

    /// <summary>
    /// Level nâng cấp Photon.
    /// Mặc định Level 1.
    /// </summary>
    public int photonUpgradeLevel;

    /// <summary>
    /// Level nâng cấp Shield.
    /// Mặc định Level 1.
    /// </summary>
    public int shieldUpgradeLevel;

    /// <summary>
    /// Level nâng cấp Magnet.
    /// Mặc định Level 1.
    /// </summary>
    public int magnetUpgradeLevel;


    // =========================================================
    // DAILY MISSIONS
    // =========================================================

    /// <summary>
    /// Ngày của Daily Mission hiện tại.
    /// Format: yyyyMMdd
    /// </summary>
    public string missionDate;


    /// <summary>
    /// Danh sách ID template được chọn trong ngày.
    /// Ví dụ:
    /// coin_30, distance_1000, runs_3
    /// </summary>
    public List<string> missionSelection =
        new List<string>();


    /// <summary>
    /// Tổng coin dùng cho Daily Mission trong ngày.
    /// </summary>
    public int missionDailyCoins;


    /// <summary>
    /// Tổng distance dùng cho Daily Mission trong ngày.
    /// </summary>
    public int missionDailyDistance;


    /// <summary>
    /// Tổng số run hoàn thành trong ngày.
    /// </summary>
    public int missionDailyRuns;


    /// <summary>
    /// Tổng EXP kiếm được trong ngày.
    /// </summary>
    public int missionDailyExp;


    /// <summary>
    /// Total EXP cuối cùng MissionManager đã biết.
    /// Dùng để tính delta EXP.
    /// </summary>
    public int missionPreviousTotalExp;


    /// <summary>
    /// Progress + claimed của từng Daily Mission.
    /// </summary>
    public List<MissionSaveData> missionStates =
        new List<MissionSaveData>();


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public PlayerData()
    {
        // -----------------------------------------------------
        // PROGRESSION
        // -----------------------------------------------------

        level = 1;
        exp = 0;


        // -----------------------------------------------------
        // ECONOMY
        // -----------------------------------------------------

        coins = 0;
        bestScore = 0;


        // -----------------------------------------------------
        // BIKE
        // -----------------------------------------------------

        selectedBike = "NinjaLead";

        ownedBikes = new List<string>
        {
            "NinjaLead"
        };


        // -----------------------------------------------------
        // ITEM UPGRADES
        // -----------------------------------------------------

        gumUpgradeLevel = 1;

        photonUpgradeLevel = 1;

        shieldUpgradeLevel = 1;

        magnetUpgradeLevel = 1;


        // -----------------------------------------------------
        // MISSIONS
        // -----------------------------------------------------

        missionDate = string.Empty;

        missionSelection =
            new List<string>();

        missionDailyCoins = 0;

        missionDailyDistance = 0;

        missionDailyRuns = 0;

        missionDailyExp = 0;

        // Giá trị 0 vẫn tương thích với dữ liệu cũ.
        // MissionManager sẽ tự đồng bộ lại khi load account.
        missionPreviousTotalExp = 0;

        missionStates =
            new List<MissionSaveData>();
    }
}