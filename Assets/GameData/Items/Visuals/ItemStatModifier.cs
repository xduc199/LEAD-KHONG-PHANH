using System;
using UnityEngine;

[Serializable]
public struct ItemStatModifier
{
    // =====================================================================
    // RUNTIME STAT KEYS
    //
    // MUST MATCH PlayerRuntimeStats EXACTLY.
    // =====================================================================

    public const string MaxSpeed = "MaxSpeed";
    public const string Acceleration = "Acceleration";
    public const string Handling = "Handling";
    public const string JumpForce = "JumpForce";
    public const string MagnetRange = "MagnetRange";
    public const string ShieldDuration = "ShieldDuration";


    // =====================================================================
    // SERIALIZED DATA
    // =====================================================================

    [Tooltip(
        "Runtime stat key. " +
        "Supported: MaxSpeed, Acceleration, Handling, " +
        "JumpForce, MagnetRange, ShieldDuration."
    )]
    [SerializeField]
    public string statKey;


    [Tooltip(
        "Modifier value. " +
        "Example: 5 = +5% when Is Percent is enabled."
    )]
    [SerializeField]
    public float value;


    [Tooltip(
        "True = value is percentage based. " +
        "False = value is an absolute value."
    )]
    [SerializeField]
    public bool isPercent;


    [Tooltip(
        "Optional gameplay condition. " +
        "Leave empty for unconditional modifier."
    )]
    [TextArea(1, 3)]
    [SerializeField]
    public string condition;


    // =====================================================================
    // CONSTRUCTORS
    // =====================================================================

    public ItemStatModifier(
        string statKey,
        float value,
        bool isPercent,
        string condition = "")
    {
        this.statKey = statKey;
        this.value = value;
        this.isPercent = isPercent;
        this.condition = condition;
    }


    // =====================================================================
    // FACTORIES
    // =====================================================================

    public static ItemStatModifier CreateAbsolute(
        string statKey,
        float value)
    {
        return new ItemStatModifier(
            statKey,
            value,
            false,
            string.Empty);
    }


    public static ItemStatModifier CreatePercent(
        string statKey,
        float percent)
    {
        return new ItemStatModifier(
            statKey,
            percent,
            true,
            string.Empty);
    }


    // =====================================================================
    // VALIDATION
    // =====================================================================

    public bool IsValid()
    {
        if (string.IsNullOrEmpty(statKey))
        {
            return false;
        }

        if (float.IsNaN(value) ||
            float.IsInfinity(value))
        {
            return false;
        }

        return IsSupportedStat(statKey);
    }


    public bool IsSupportedStat()
    {
        return IsSupportedStat(statKey);
    }


    public static bool IsSupportedStat(
        string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        switch (key)
        {
            case MaxSpeed:
            case Acceleration:
            case Handling:
            case JumpForce:
            case MagnetRange:
            case ShieldDuration:
                return true;

            default:
                return false;
        }
    }


    // =====================================================================
    // SEMANTIC HELPERS
    // =====================================================================

    public bool IsMultiplierStat()
    {
        switch (statKey)
        {
            case Acceleration:
            case Handling:
                return true;

            default:
                return false;
        }
    }


    public bool IsAbsoluteStat()
    {
        switch (statKey)
        {
            case MaxSpeed:
            case JumpForce:
            case MagnetRange:
            case ShieldDuration:
                return true;

            default:
                return false;
        }
    }
}