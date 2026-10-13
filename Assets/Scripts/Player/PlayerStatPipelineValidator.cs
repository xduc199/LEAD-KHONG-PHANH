using UnityEngine;

/// <summary>
/// One-click validation for the complete player stat pipeline.
///
/// Validation flow:
///
/// Equipped Items
///      ↓
/// GameItemDatabase
///      ↓
/// Expected Runtime Stats
///      ↓
/// PlayerRuntimeStats
///      ↓
/// PlayerController
///
/// This validator does NOT modify equipment or save player data.
/// It only calculates expected values and compares them with runtime values.
/// </summary>
public sealed class PlayerStatPipelineValidator : MonoBehaviour
{
    // =====================================================================
    // REFERENCES
    // =====================================================================

    [Header("References")]
    [SerializeField] private PlayerRuntimeStats runtimeStats;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private EquipmentManager equipmentManager;
    [SerializeField] private GameItemDatabase itemDatabase;


    // =====================================================================
    // PLAYER MOVEMENT BASE
    // =====================================================================

    [Header("Player Movement")]
    [Tooltip("PlayerController base Speed Increase Rate.")]
    [SerializeField] private float baseSpeedIncreaseRate = 0.02f;


    // =====================================================================
    // VALIDATION
    // =====================================================================

    [Header("Validation")]
    [SerializeField] private float tolerance = 0.0001f;


    // =====================================================================
    // DIRECT ITEM VALIDATION
    // =====================================================================

    [ContextMenu("DEBUG / Validate Limited Ride 38")]
    public void ValidateLimitedRide38()
    {
        const int itemId = 38;

        int pass = 0;
        int warn = 0;
        int fail = 0;

        Debug.Log(
            "\n" +
            "============================================================\n" +
            " LIMITED RIDE ITEM 38 VALIDATOR\n" +
            "============================================================"
        );


        // -------------------------------------------------------------
        // DATABASE
        // -------------------------------------------------------------

        if (itemDatabase == null)
        {
            Fail(
                "GameItemDatabase reference missing.",
                ref fail
            );

            PrintResult(pass, warn, fail);
            return;
        }


        GameItemData item;

        if (!itemDatabase.TryGet(itemId, out item))
        {
            Fail(
                "Item 38 not found in GameItemDatabase.",
                ref fail
            );

            PrintResult(pass, warn, fail);
            return;
        }


        if (item == null)
        {
            Fail(
                "Item 38 resolved to NULL.",
                ref fail
            );

            PrintResult(pass, warn, fail);
            return;
        }


        Pass(
            "Item 38 resolved → " +
            item.ItemName,
            ref pass
        );


        // -------------------------------------------------------------
        // ID / CATEGORY
        // -------------------------------------------------------------

        if (item.ItemId == 38)
        {
            Pass(
                "Item ID = 38",
                ref pass
            );
        }
        else
        {
            Fail(
                "Unexpected Item ID = " +
                item.ItemId,
                ref fail
            );
        }


        if (item.Category == GameItemCategory.Vehicle)
        {
            Pass(
                "Category = Vehicle",
                ref pass
            );
        }
        else
        {
            Fail(
                "Category mismatch. Expected Vehicle, Actual=" +
                item.Category,
                ref fail
            );
        }


        // -------------------------------------------------------------
        // HAS STATS
        // -------------------------------------------------------------

        if (item.HasStats)
        {
            Pass(
                "HasStats = true",
                ref pass
            );
        }
        else
        {
            Fail(
                "HasStats = false. Limited Ride must contain runtime stats.",
                ref fail
            );

            PrintResult(pass, warn, fail);
            return;
        }


        if (item.Stats == null)
        {
            Fail(
                "Item 38 Stats object is NULL.",
                ref fail
            );

            PrintResult(pass, warn, fail);
            return;
        }


        if (item.Stats.modifiers == null)
        {
            Fail(
                "Item 38 modifier list is NULL.",
                ref fail
            );

            PrintResult(pass, warn, fail);
            return;
        }


        // -------------------------------------------------------------
        // FIND EXPECTED MODIFIERS
        // -------------------------------------------------------------

        bool foundMaxSpeed = false;
        bool foundAcceleration = false;

        float maxSpeedValue = 0f;
        bool maxSpeedPercent = false;

        float accelerationValue = 0f;
        bool accelerationPercent = false;


        for (int i = 0;
             i < item.Stats.modifiers.Count;
             i++)
        {
            ItemStatModifier modifier =
                item.Stats.modifiers[i];


            if (string.IsNullOrEmpty(
                modifier.statKey))
            {
                Warn(
                    "Modifier " +
                    i +
                    " has empty statKey.",
                    ref warn
                );

                continue;
            }


            Debug.Log(
                "[MODIFIER] " +
                modifier.statKey +
                " | Value=" +
                modifier.value +
                " | IsPercent=" +
                modifier.isPercent +
                " | Condition=" +
                modifier.condition
            );


            switch (modifier.statKey)
            {
                case "MaxSpeed":

                    if (foundMaxSpeed)
                    {
                        Warn(
                            "Duplicate MaxSpeed modifier detected.",
                            ref warn
                        );
                    }

                    foundMaxSpeed = true;
                    maxSpeedValue = modifier.value;
                    maxSpeedPercent = modifier.isPercent;

                    break;


                case "Acceleration":

                    if (foundAcceleration)
                    {
                        Warn(
                            "Duplicate Acceleration modifier detected.",
                            ref warn
                        );
                    }

                    foundAcceleration = true;
                    accelerationValue = modifier.value;
                    accelerationPercent = modifier.isPercent;

                    break;
            }
        }


        // -------------------------------------------------------------
        // MAX SPEED
        // -------------------------------------------------------------

        if (!foundMaxSpeed)
        {
            Fail(
                "Missing MaxSpeed modifier.",
                ref fail
            );
        }
        else
        {
            if (Mathf.Abs(
                maxSpeedValue - 20f
            ) <= tolerance)
            {
                Pass(
                    "MaxSpeed value = +20",
                    ref pass
                );
            }
            else
            {
                Fail(
                    "MaxSpeed expected +20, actual=" +
                    maxSpeedValue,
                    ref fail
                );
            }


            if (maxSpeedPercent)
            {
                Pass(
                    "MaxSpeed uses percentage modifier.",
                    ref pass
                );
            }
            else
            {
                Fail(
                    "MaxSpeed must use IsPercent=true.",
                    ref fail
                );
            }
        }


        // -------------------------------------------------------------
        // ACCELERATION
        // -------------------------------------------------------------

        if (!foundAcceleration)
        {
            Fail(
                "Missing Acceleration modifier.",
                ref fail
            );
        }
        else
        {
            if (Mathf.Abs(
                accelerationValue - 15f
            ) <= tolerance)
            {
                Pass(
                    "Acceleration value = +15",
                    ref pass
                );
            }
            else
            {
                Fail(
                    "Acceleration expected +15, actual=" +
                    accelerationValue,
                    ref fail
                );
            }


            if (accelerationPercent)
            {
                Pass(
                    "Acceleration uses percentage modifier.",
                    ref pass
                );
            }
            else
            {
                Fail(
                    "Acceleration must use IsPercent=true.",
                    ref fail
                );
            }
        }


        // -------------------------------------------------------------
        // RUNTIME BASE STATS
        // -------------------------------------------------------------

        if (runtimeStats == null)
        {
            Fail(
                "PlayerRuntimeStats reference missing. " +
                "Cannot calculate Item 38 expected runtime values.",
                ref fail
            );

            PrintResult(pass, warn, fail);
            return;
        }


        float expectedMaxSpeed =
            ApplyAbsoluteModifier(
                runtimeStats.BaseMaxSpeed,
                maxSpeedValue,
                maxSpeedPercent
            );


        float expectedAcceleration =
            ApplyMultiplierModifier(
                runtimeStats.BaseAcceleration,
                accelerationValue,
                accelerationPercent
            );


        Debug.Log(
            "\n[EXPECTED RESULT]\n" +
            "Base MaxSpeed = " +
            runtimeStats.BaseMaxSpeed +
            "\nMaxSpeed Modifier = " +
            maxSpeedValue +
            "%"
        );


        Debug.Log(
            "[EXPECTED RESULT] " +
            "MaxSpeed = " +
            expectedMaxSpeed
        );


        Debug.Log(
            "[EXPECTED RESULT] " +
            "Base Acceleration = " +
            runtimeStats.BaseAcceleration +
            "\nAcceleration Modifier = " +
            accelerationValue +
            "%"
        );


        Debug.Log(
            "[EXPECTED RESULT] " +
            "Acceleration = " +
            expectedAcceleration
        );


        // -------------------------------------------------------------
        // EXPECTED NUMBERS
        // -------------------------------------------------------------

        if (Mathf.Abs(
            expectedMaxSpeed - 42f
        ) <= tolerance)
        {
            Pass(
                "Final Expected MaxSpeed = 42",
                ref pass
            );
        }
        else
        {
            Fail(
                "Final Expected MaxSpeed should be 42, actual=" +
                expectedMaxSpeed,
                ref fail
            );
        }


        if (Mathf.Abs(
            expectedAcceleration - 1.15f
        ) <= tolerance)
        {
            Pass(
                "Final Expected Acceleration = 1.15",
                ref pass
            );
        }
        else
        {
            Fail(
                "Final Expected Acceleration should be 1.15, actual=" +
                expectedAcceleration,
                ref fail
            );
        }


        // -------------------------------------------------------------
        // CONDITION CHECK
        // -------------------------------------------------------------

        for (int i = 0;
             i < item.Stats.modifiers.Count;
             i++)
        {
            ItemStatModifier modifier =
                item.Stats.modifiers[i];


            if (modifier.statKey != "MaxSpeed" &&
                modifier.statKey != "Acceleration")
            {
                continue;
            }


            if (!string.IsNullOrEmpty(
                modifier.condition))
            {
                Warn(
                    modifier.statKey +
                    " has condition: " +
                    modifier.condition +
                    " | Current PlayerRuntimeStats applies modifiers unconditionally.",
                    ref warn
                );
            }
            else
            {
                Pass(
                    modifier.statKey +
                    " condition is empty.",
                    ref pass
                );
            }
        }


        // -------------------------------------------------------------
        // LIVE RUNTIME COMPARISON
        // -------------------------------------------------------------

        Debug.Log(
            "\n[LIVE RUNTIME]"
        );


        Debug.Log(
            "Runtime MaxSpeed = " +
            runtimeStats.MaxSpeed
        );


        Debug.Log(
            "Runtime Acceleration = " +
            runtimeStats.Acceleration
        );


        Debug.Log(
            "Expected from Item 38 = " +
            expectedMaxSpeed +
            " / " +
            expectedAcceleration
        );


        /*
         * IMPORTANT:
         *
         * We deliberately do NOT compare runtimeStats against Item 38
         * here because the currently equipped item may not be Item 38.
         *
         * This method validates the Item 38 data itself.
         */

        PrintResult(
            pass,
            warn,
            fail
        );
    }


    // =====================================================================
    // COMPLETE PIPELINE VALIDATION
    // =====================================================================

    [ContextMenu("DEBUG / Validate Complete Player Stat Pipeline")]
    public void ValidateCompletePipeline()
    {
        int pass = 0;
        int warn = 0;
        int fail = 0;


        Debug.Log(
            "\n" +
            "============================================================\n" +
            " PLAYER STAT PIPELINE VALIDATOR\n" +
            "============================================================"
        );


        // -------------------------------------------------------------
        // STEP 1
        // -------------------------------------------------------------

        if (!ResolveReferences(
            ref pass,
            ref fail))
        {
            PrintResult(
                pass,
                warn,
                fail
            );

            return;
        }


        // -------------------------------------------------------------
        // STEP 2
        // -------------------------------------------------------------

        ExpectedStats expected =
            CreateBaseExpectedStats();


        // -------------------------------------------------------------
        // STEP 3
        // -------------------------------------------------------------

        Debug.Log(
            "\n[1] EQUIPMENT"
        );


        int vehicleId =
            equipmentManager.GetEquippedItemId(
                GameItemCategory.Vehicle
            );


        int outfitId =
            equipmentManager.GetEquippedItemId(
                GameItemCategory.Outfit
            );


        int accessoryId =
            equipmentManager.GetEquippedItemId(
                GameItemCategory.Accessory
            );


        int cosmeticId =
            equipmentManager.GetEquippedItemId(
                GameItemCategory.Cosmetic
            );


        Debug.Log(
            "[EQUIPPED] " +
            "Vehicle=" +
            vehicleId +
            " | Outfit=" +
            outfitId +
            " | Accessory=" +
            accessoryId +
            " | Cosmetic=" +
            cosmeticId
        );


        ValidateAndApplyItem(
            "Vehicle",
            vehicleId,
            ref expected,
            ref pass,
            ref warn,
            ref fail
        );


        ValidateAndApplyItem(
            "Outfit",
            outfitId,
            ref expected,
            ref pass,
            ref warn,
            ref fail
        );


        ValidateAndApplyItem(
            "Accessory",
            accessoryId,
            ref expected,
            ref pass,
            ref warn,
            ref fail
        );


        ValidateAndApplyItem(
            "Cosmetic",
            cosmeticId,
            ref expected,
            ref pass,
            ref warn,
            ref fail
        );


        // -------------------------------------------------------------
        // STEP 4
        // -------------------------------------------------------------

        Debug.Log(
            "\n[2] EXPECTED STATS"
        );


        LogExpectedStats(
            in expected
        );


        // -------------------------------------------------------------
        // STEP 5
        // -------------------------------------------------------------

        Debug.Log(
            "\n[3] PLAYER RUNTIME STATS"
        );


        ValidateRuntimeStat(
            "MaxSpeed",
            expected.maxSpeed,
            runtimeStats.MaxSpeed,
            ref pass,
            ref fail
        );


        ValidateRuntimeStat(
            "Acceleration",
            expected.acceleration,
            runtimeStats.Acceleration,
            ref pass,
            ref fail
        );


        ValidateRuntimeStat(
            "Handling",
            expected.handling,
            runtimeStats.Handling,
            ref pass,
            ref fail
        );


        ValidateRuntimeStat(
            "JumpForce",
            expected.jumpForce,
            runtimeStats.JumpForce,
            ref pass,
            ref fail
        );


        ValidateRuntimeStat(
            "MagnetRange",
            expected.magnetRange,
            runtimeStats.MagnetRange,
            ref pass,
            ref fail
        );


        ValidateRuntimeStat(
            "ShieldDuration",
            expected.shieldDuration,
            runtimeStats.ShieldDuration,
            ref pass,
            ref fail
        );


        // -------------------------------------------------------------
        // STEP 6
        // -------------------------------------------------------------

        Debug.Log(
            "\n[4] PLAYER CONTROLLER"
        );


        float playerMaxSpeed =
            playerController.MaxSpeed;


        ValidateRuntimeStat(
            "PlayerController.MaxSpeed",
            expected.maxSpeed,
            playerMaxSpeed,
            ref pass,
            ref fail
        );


        float expectedRuntimeSpeedIncreaseRate =
            baseSpeedIncreaseRate *
            expected.acceleration;


        Debug.Log(
            "[EXPECTED MOVEMENT] " +
            baseSpeedIncreaseRate +
            " × " +
            expected.acceleration +
            " = " +
            expectedRuntimeSpeedIncreaseRate
        );


        float actualRuntimeSpeedIncreaseRate =
            playerController.RuntimeSpeedIncreaseRate;


        ValidateRuntimeStat(
            "PlayerController.RuntimeSpeedIncreaseRate",
            expectedRuntimeSpeedIncreaseRate,
            actualRuntimeSpeedIncreaseRate,
            ref pass,
            ref fail
        );


        // -------------------------------------------------------------
        // STEP 7
        // -------------------------------------------------------------

        Debug.Log(
            "\n[5] RUNTIME SANITY"
        );


        ValidateFinite(
            "Runtime MaxSpeed",
            runtimeStats.MaxSpeed,
            ref pass,
            ref fail
        );


        ValidateFinite(
            "Runtime Acceleration",
            runtimeStats.Acceleration,
            ref pass,
            ref fail
        );


        ValidateFinite(
            "Runtime Handling",
            runtimeStats.Handling,
            ref pass,
            ref fail
        );


        ValidateFinite(
            "Runtime JumpForce",
            runtimeStats.JumpForce,
            ref pass,
            ref fail
        );


        ValidateFinite(
            "Runtime MagnetRange",
            runtimeStats.MagnetRange,
            ref pass,
            ref fail
        );


        ValidateFinite(
            "Runtime ShieldDuration",
            runtimeStats.ShieldDuration,
            ref pass,
            ref fail
        );


        ValidateFinite(
            "PlayerController.MaxSpeed",
            playerController.MaxSpeed,
            ref pass,
            ref fail
        );


        ValidateFinite(
            "PlayerController.RuntimeSpeedIncreaseRate",
            playerController.RuntimeSpeedIncreaseRate,
            ref pass,
            ref fail
        );


        if (runtimeStats.Acceleration > 0f)
        {
            Pass(
                "Acceleration > 0",
                ref pass
            );
        }
        else
        {
            Fail(
                "Acceleration <= 0",
                ref fail
            );
        }


        if (runtimeStats.MaxSpeed > 0f)
        {
            Pass(
                "MaxSpeed > 0",
                ref pass
            );
        }
        else
        {
            Fail(
                "MaxSpeed <= 0",
                ref fail
            );
        }


        if (playerController.RuntimeSpeedIncreaseRate >= 0f)
        {
            Pass(
                "RuntimeSpeedIncreaseRate >= 0",
                ref pass
            );
        }
        else
        {
            Fail(
                "RuntimeSpeedIncreaseRate < 0",
                ref fail
            );
        }


        // -------------------------------------------------------------
        // FINAL
        // -------------------------------------------------------------

        PrintResult(
            pass,
            warn,
            fail
        );
    }

// =====================================================================
// REFERENCES
// =====================================================================

private bool ResolveReferences(
    ref int pass,
    ref int fail)
{
    // =================================================================
    // PLAYER RUNTIME STATS
    // =================================================================

    if (runtimeStats == null)
    {
        runtimeStats =
            GetComponent<PlayerRuntimeStats>();
    }

    if (runtimeStats == null)
    {
        Fail(
            "PlayerRuntimeStats reference missing.",
            ref fail
        );
    }
    else
    {
        Pass(
            "PlayerRuntimeStats reference OK.",
            ref pass
        );
    }


    // =================================================================
    // PLAYER CONTROLLER
    // =================================================================

    if (playerController == null)
    {
        playerController =
            GetComponent<PlayerController>();
    }

    if (playerController == null)
    {
        Fail(
            "PlayerController reference missing.",
            ref fail
        );
    }
    else
    {
        Pass(
            "PlayerController reference OK.",
            ref pass
        );
    }


    // =================================================================
    // EQUIPMENT MANAGER
    // =================================================================
    //
    // EquipmentManager là Persistent Singleton.
    //
    // KHÔNG tìm bằng GetComponent<Player>().
    // KHÔNG cần gắn EquipmentManager vào PLAYER.
    //
    // Resolve:
    //
    //     EquipmentManager.Instance
    //
    // =================================================================

    if (equipmentManager == null)
    {
        equipmentManager =
            EquipmentManager.Instance;
    }

    if (equipmentManager == null)
    {
        Fail(
            "EquipmentManager instance missing. " +
            "Ensure PersistentSystems contains EquipmentManager " +
            "and EquipmentManager.Awake() has executed.",
            ref fail
        );
    }
    else
    {
        Pass(
            "EquipmentManager reference OK.",
            ref pass
        );
    }


    // =================================================================
    // GAME ITEM DATABASE
    // =================================================================

    if (itemDatabase == null)
    {
        Fail(
            "GameItemDatabase reference missing.",
            ref fail
        );
    }
    else
    {
        Pass(
            "GameItemDatabase reference OK.",
            ref pass
        );
    }


    // =================================================================
    // FINAL
    // =================================================================

    return fail == 0;
}

    // =====================================================================
    // BASE EXPECTED STATS
    // =====================================================================

    private ExpectedStats CreateBaseExpectedStats()
    {
        ExpectedStats result;


        /*
         * PlayerRuntimeStats is the authoritative source
         * for base runtime stats.
         *
         * This prevents the validator from maintaining
         * a second duplicate set of base values.
         */

        result.maxSpeed =
            SanitizeAbsolute(
                runtimeStats.BaseMaxSpeed
            );


        result.acceleration =
            SanitizeMultiplier(
                runtimeStats.BaseAcceleration
            );


        result.handling =
            SanitizeMultiplier(
                runtimeStats.BaseHandling
            );


        result.jumpForce =
            SanitizeAbsolute(
                runtimeStats.BaseJumpForce
            );


        result.magnetRange =
            SanitizeAbsolute(
                runtimeStats.BaseMagnetRange
            );


        result.shieldDuration =
            SanitizeAbsolute(
                runtimeStats.BaseShieldDuration
            );


        return result;
    }


    // =====================================================================
    // ITEM VALIDATION + EXPECTED STAT CALCULATION
    // =====================================================================

    private void ValidateAndApplyItem(
        string slotName,
        int itemId,
        ref ExpectedStats expected,
        ref int pass,
        ref int warn,
        ref int fail)
    {
        if (itemId == 0)
        {
            Pass(
                slotName +
                " empty.",
                ref pass
            );

            return;
        }


        if (itemId < 20 || itemId > 99)
        {
            Fail(
                slotName +
                " invalid Item ID = " +
                itemId,
                ref fail
            );

            return;
        }


        GameItemData item;


        if (!itemDatabase.TryGet(
            itemId,
            out item))
        {
            Fail(
                slotName +
                " Item ID " +
                itemId +
                " not found in database.",
                ref fail
            );

            return;
        }


        if (item == null)
        {
            Fail(
                slotName +
                " Item ID " +
                itemId +
                " resolved NULL.",
                ref fail
            );

            return;
        }


        Pass(
            slotName +
            " → " +
            item.ItemName +
            " (" +
            item.ItemId +
            ")",
            ref pass
        );


        if (!item.HasStats)
        {
            Debug.Log(
                "[ITEM] " +
                item.ItemName +
                " has no runtime stats."
            );

            return;
        }


        if (item.Stats == null ||
            item.Stats.modifiers == null)
        {
            Warn(
                item.ItemName +
                " HasStats=true but modifiers are missing.",
                ref warn
            );

            return;
        }


        for (int i = 0;
             i < item.Stats.modifiers.Count;
             i++)
        {
            ItemStatModifier modifier =
                item.Stats.modifiers[i];


            if (string.IsNullOrEmpty(
                modifier.statKey))
            {
                Warn(
                    "Item " +
                    item.ItemId +
                    " contains empty statKey.",
                    ref warn
                );

                continue;
            }


            if (!IsFinite(
                modifier.value))
            {
                Fail(
                    "Item " +
                    item.ItemId +
                    " stat " +
                    modifier.statKey +
                    " contains NaN/Infinity.",
                    ref fail
                );

                continue;
            }


            if (!IsRuntimePlayerStat(
                modifier.statKey))
            {
                /*
                 * Other item systems may contain stats such as:
                 *
                 * Armor
                 * GoldReward
                 * ExpReward
                 * NitroCharge
                 * etc.
                 *
                 * They are not PlayerRuntimeStats.
                 */

                Debug.Log(
                    "[SKIP] Item " +
                    item.ItemId +
                    " | Non-player-runtime stat: " +
                    modifier.statKey
                );

                continue;
            }


            ApplyExpectedModifier(
                ref expected,
                in modifier
            );


            Debug.Log(
                "[APPLY] Item " +
                item.ItemId +
                " | " +
                modifier.statKey +
                " | Value=" +
                modifier.value +
                (modifier.isPercent
                    ? "%"
                    : "")
            );
        }
    }


    // =====================================================================
    // EXPECTED MODIFIER CALCULATION
    // =====================================================================

    private static void ApplyExpectedModifier(
        ref ExpectedStats stats,
        in ItemStatModifier modifier)
    {
        switch (modifier.statKey)
        {
            case "MaxSpeed":

                stats.maxSpeed =
                    ApplyAbsoluteModifier(
                        stats.maxSpeed,
                        modifier.value,
                        modifier.isPercent
                    );

                break;


            case "Acceleration":

                stats.acceleration =
                    ApplyMultiplierModifier(
                        stats.acceleration,
                        modifier.value,
                        modifier.isPercent
                    );

                break;


            case "Handling":

                stats.handling =
                    ApplyMultiplierModifier(
                        stats.handling,
                        modifier.value,
                        modifier.isPercent
                    );

                break;


            case "JumpForce":

                stats.jumpForce =
                    ApplyAbsoluteModifier(
                        stats.jumpForce,
                        modifier.value,
                        modifier.isPercent
                    );

                break;


            case "MagnetRange":

                stats.magnetRange =
                    ApplyAbsoluteModifier(
                        stats.magnetRange,
                        modifier.value,
                        modifier.isPercent
                    );

                break;


            case "ShieldDuration":

                stats.shieldDuration =
                    ApplyAbsoluteModifier(
                        stats.shieldDuration,
                        modifier.value,
                        modifier.isPercent
                    );

                break;
        }
    }


    // =====================================================================
    // STAT TYPES
    // =====================================================================

    private static bool IsRuntimePlayerStat(
        string statKey)
    {
        switch (statKey)
        {
            case "MaxSpeed":
            case "Acceleration":
            case "Handling":
            case "JumpForce":
            case "MagnetRange":
            case "ShieldDuration":
                return true;

            default:
                return false;
        }
    }


    private static float ApplyAbsoluteModifier(
        float current,
        float modifier,
        bool isPercent)
    {
        if (isPercent)
        {
            current +=
                current *
                (modifier * 0.01f);
        }
        else
        {
            current += modifier;
        }


        return SanitizeAbsolute(
            current
        );
    }


    private static float ApplyMultiplierModifier(
        float current,
        float modifier,
        bool isPercent)
    {
        if (isPercent)
        {
            current *=
                1f +
                (modifier * 0.01f);
        }
        else
        {
            current += modifier;
        }


        return SanitizeMultiplier(
            current
        );
    }


    // =====================================================================
    // RUNTIME VALIDATION
    // =====================================================================

    private void ValidateRuntimeStat(
        string statName,
        float expected,
        float actual,
        ref int pass,
        ref int fail)
    {
        if (!IsFinite(actual))
        {
            Fail(
                statName +
                " = NaN/Infinity.",
                ref fail
            );

            return;
        }


        if (Mathf.Abs(
            expected - actual
        ) <= tolerance)
        {
            Pass(
                statName +
                " = " +
                actual +
                " | Expected=" +
                expected,
                ref pass
            );
        }
        else
        {
            Fail(
                statName +
                " MISMATCH | Expected=" +
                expected +
                " | Actual=" +
                actual,
                ref fail
            );
        }
    }


    private static void ValidateFinite(
        string name,
        float value,
        ref int pass,
        ref int fail)
    {
        if (IsFinite(value))
        {
            Pass(
                name +
                " finite = " +
                value,
                ref pass
            );
        }
        else
        {
            Fail(
                name +
                " = NaN/Infinity.",
                ref fail
            );
        }
    }


    // =====================================================================
    // LOG EXPECTED
    // =====================================================================

    private static void LogExpectedStats(
        in ExpectedStats stats)
    {
        Debug.Log(
            "[EXPECTED] " +
            "MaxSpeed=" +
            stats.maxSpeed +
            " | Acceleration=" +
            stats.acceleration +
            " | Handling=" +
            stats.handling +
            " | JumpForce=" +
            stats.jumpForce +
            " | MagnetRange=" +
            stats.magnetRange +
            " | ShieldDuration=" +
            stats.shieldDuration
        );
    }


    // =====================================================================
    // SANITIZE
    // =====================================================================

    private static float SanitizeAbsolute(
        float value)
    {
        if (!IsFinite(value))
            return 0f;


        return Mathf.Max(
            0f,
            value
        );
    }


    private static float SanitizeMultiplier(
        float value)
    {
        if (!IsFinite(value))
            return 1f;


        return Mathf.Max(
            0f,
            value
        );
    }


    private static bool IsFinite(
        float value)
    {
        return
            !float.IsNaN(value) &&
            !float.IsInfinity(value);
    }


    // =====================================================================
    // LOGGING
    // =====================================================================

    private static void Pass(
        string message,
        ref int counter)
    {
        counter++;


        Debug.Log(
            "[PASS] " +
            message
        );
    }


    private static void Warn(
        string message,
        ref int counter)
    {
        counter++;


        Debug.LogWarning(
            "[WARN] " +
            message
        );
    }


    private static void Fail(
        string message,
        ref int counter)
    {
        counter++;


        Debug.LogError(
            "[FAIL] " +
            message
        );
    }


    private static void PrintResult(
        int pass,
        int warn,
        int fail)
    {
        Debug.Log(
            "\n" +
            "============================================================\n" +
            " PLAYER STAT VALIDATION RESULT\n" +
            " PASS = " +
            pass +
            " | WARN = " +
            warn +
            " | FAIL = " +
            fail +
            "\n" +
            "============================================================"
        );


        if (fail == 0)
        {
            if (warn == 0)
            {
                Debug.Log(
                    "[PLAYER STAT PIPELINE] " +
                    "✅ ALL TESTS PASSED"
                );
            }
            else
            {
                Debug.LogWarning(
                    "[PLAYER STAT PIPELINE] " +
                    "⚠ PASS WITH WARNINGS"
                );
            }
        }
        else
        {
            Debug.LogError(
                "[PLAYER STAT PIPELINE] " +
                "❌ VALIDATION FAILED"
            );
        }
    }


    // =====================================================================
    // DATA
    // =====================================================================

    private struct ExpectedStats
    {
        public float maxSpeed;
        public float acceleration;
        public float handling;
        public float jumpForce;
        public float magnetRange;
        public float shieldDuration;
    }
}