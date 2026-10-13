using System;
using UnityEngine;

/// <summary>
/// Runtime gameplay stats generated from base values + equipped item modifiers.
///
/// Stat model:
/// - MaxSpeed       : absolute value
/// - Acceleration   : multiplier, 1.0 = 100% baseline
/// - Handling       : multiplier, 1.0 = 100% baseline
/// - JumpForce      : absolute value
/// - MagnetRange    : absolute value
/// - ShieldDuration : absolute value
///
/// Example:
/// MaxSpeed +20%:
///     35 -> 42
///
/// Acceleration +15%:
///     1.0 -> 1.15
///
/// Architecture:
/// - Base values are the single source of truth.
/// - Runtime values are rebuilt from base values.
/// - Item modifiers are applied deterministically.
/// - All runtime values are sanitized against NaN / Infinity.
/// - No heap allocation is performed by the stat calculation methods.
/// </summary>
public sealed class PlayerRuntimeStats : MonoBehaviour
{
    // =====================================================================
    // BASE STATS
    // =====================================================================

    [Header("Base Stats")]

    [SerializeField]
    private float baseMaxSpeed = 35f;

    [Tooltip("Acceleration multiplier. 1.0 = 100% baseline.")]
    [SerializeField]
    private float baseAcceleration = 1f;

    [Tooltip("Handling multiplier. 1.0 = 100% baseline.")]
    [SerializeField]
    private float baseHandling = 1f;

    [SerializeField]
    private float baseJumpForce = 12f;

    [SerializeField]
    private float baseMagnetRange = 0f;

    [SerializeField]
    private float baseShieldDuration = 8f;


    // =====================================================================
    // RUNTIME VALUES
    // =====================================================================

    private float maxSpeed;
    private float acceleration;
    private float handling;
    private float jumpForce;
    private float magnetRange;
    private float shieldDuration;


    // =====================================================================
    // EVENTS
    // =====================================================================

    public event Action OnStatsRebuilt;


    // =====================================================================
    // BASE STATS — READ ONLY
    // =====================================================================
    //
    // These properties expose the serialized base configuration without
    // allowing external systems to modify it.
    //
    // PlayerRuntimeStats is therefore the single source of truth for
    // runtime-stat base values.
    //
    // Validators / debug systems should read these properties instead of
    // maintaining duplicated base-stat configuration.
    // =====================================================================

    public float BaseMaxSpeed
    {
        get { return baseMaxSpeed; }
    }

    public float BaseAcceleration
    {
        get { return baseAcceleration; }
    }

    public float BaseHandling
    {
        get { return baseHandling; }
    }

    public float BaseJumpForce
    {
        get { return baseJumpForce; }
    }

    public float BaseMagnetRange
    {
        get { return baseMagnetRange; }
    }

    public float BaseShieldDuration
    {
        get { return baseShieldDuration; }
    }


    // =====================================================================
    // RUNTIME STATS — READ ONLY
    // =====================================================================

    public float MaxSpeed
    {
        get { return maxSpeed; }
    }

    /// <summary>
    /// Acceleration multiplier.
    ///
    /// 1.0 = baseline.
    /// 1.15 = +15%.
    /// </summary>
    public float Acceleration
    {
        get { return acceleration; }
    }

    /// <summary>
    /// Handling multiplier.
    ///
    /// 1.0 = baseline.
    /// 1.15 = +15%.
    /// </summary>
    public float Handling
    {
        get { return handling; }
    }

    public float JumpForce
    {
        get { return jumpForce; }
    }

    public float MagnetRange
    {
        get { return magnetRange; }
    }

    public float ShieldDuration
    {
        get { return shieldDuration; }
    }


    // =====================================================================
    // UNITY
    // =====================================================================

    private void Awake()
    {
        RebuildFromBaseStats();
    }


    // =====================================================================
    // REBUILD
    // =====================================================================

    /// <summary>
    /// Resets runtime stats to the serialized base configuration.
    ///
    /// This method does not allocate memory.
    /// </summary>
    public void RebuildFromBaseStats()
    {
        maxSpeed =
            SanitizeAbsoluteValue(
                baseMaxSpeed
            );

        acceleration =
            SanitizeMultiplier(
                baseAcceleration
            );

        handling =
            SanitizeMultiplier(
                baseHandling
            );

        jumpForce =
            SanitizeAbsoluteValue(
                baseJumpForce
            );

        magnetRange =
            SanitizeAbsoluteValue(
                baseMagnetRange
            );

        shieldDuration =
            SanitizeAbsoluteValue(
                baseShieldDuration
            );

        OnStatsRebuilt?.Invoke();
    }


    // =====================================================================
    // APPLY MODIFIER
    // =====================================================================

    /// <summary>
    /// Applies one item stat modifier to the current runtime stats.
    ///
    /// Unsupported stat keys are ignored safely.
    ///
    /// No heap allocation is performed here.
    /// </summary>
    public void ApplyModifier(
        in ItemStatModifier modifier)
    {
        if (string.IsNullOrEmpty(modifier.statKey))
            return;

        float value = modifier.value;

        if (!IsFinite(value))
            return;

        switch (modifier.statKey)
        {
            // -------------------------------------------------------------
            // MAX SPEED
            // -------------------------------------------------------------

            case "MaxSpeed":

                maxSpeed =
                    ApplyAbsoluteModifier(
                        maxSpeed,
                        value,
                        modifier.isPercent
                    );

                break;


            // -------------------------------------------------------------
            // ACCELERATION
            // -------------------------------------------------------------

            case "Acceleration":

                acceleration =
                    ApplyMultiplierModifier(
                        acceleration,
                        value,
                        modifier.isPercent
                    );

                break;


            // -------------------------------------------------------------
            // HANDLING
            // -------------------------------------------------------------

            case "Handling":

                handling =
                    ApplyMultiplierModifier(
                        handling,
                        value,
                        modifier.isPercent
                    );

                break;


            // -------------------------------------------------------------
            // JUMP FORCE
            // -------------------------------------------------------------

            case "JumpForce":

                jumpForce =
                    ApplyAbsoluteModifier(
                        jumpForce,
                        value,
                        modifier.isPercent
                    );

                break;


            // -------------------------------------------------------------
            // MAGNET RANGE
            // -------------------------------------------------------------

            case "MagnetRange":

                magnetRange =
                    ApplyAbsoluteModifier(
                        magnetRange,
                        value,
                        modifier.isPercent
                    );

                break;


            // -------------------------------------------------------------
            // SHIELD DURATION
            // -------------------------------------------------------------

            case "ShieldDuration":

                shieldDuration =
                    ApplyAbsoluteModifier(
                        shieldDuration,
                        value,
                        modifier.isPercent
                    );

                break;
        }
    }


    // =====================================================================
    // FINALIZE
    // =====================================================================

    /// <summary>
    /// Sanitizes all runtime values after all modifiers have been applied,
    /// then notifies consumers that the runtime stat set is ready.
    ///
    /// This should normally be called once after the complete equipment
    /// modifier pipeline has finished.
    /// </summary>
    public void FinalizeStats()
    {
        maxSpeed =
            SanitizeAbsoluteValue(
                maxSpeed
            );

        acceleration =
            SanitizeMultiplier(
                acceleration
            );

        handling =
            SanitizeMultiplier(
                handling
            );

        jumpForce =
            SanitizeAbsoluteValue(
                jumpForce
            );

        magnetRange =
            SanitizeAbsoluteValue(
                magnetRange
            );

        shieldDuration =
            SanitizeAbsoluteValue(
                shieldDuration
            );

        OnStatsRebuilt?.Invoke();
    }


    // =====================================================================
    // QUERY
    // =====================================================================

    /// <summary>
    /// Gets a runtime stat by its canonical stat key.
    ///
    /// Supported keys:
    /// - MaxSpeed
    /// - Acceleration
    /// - Handling
    /// - JumpForce
    /// - MagnetRange
    /// - ShieldDuration
    ///
    /// Returns false for unsupported or empty keys.
    /// </summary>
    public bool TryGetStat(
        string statKey,
        out float value)
    {
        value = 0f;

        if (string.IsNullOrEmpty(statKey))
            return false;

        switch (statKey)
        {
            case "MaxSpeed":

                value = maxSpeed;
                return true;


            case "Acceleration":

                value = acceleration;
                return true;


            case "Handling":

                value = handling;
                return true;


            case "JumpForce":

                value = jumpForce;
                return true;


            case "MagnetRange":

                value = magnetRange;
                return true;


            case "ShieldDuration":

                value = shieldDuration;
                return true;


            default:

                return false;
        }
    }


    // =====================================================================
    // MODIFIER MATH
    // =====================================================================

    /// <summary>
    /// Applies an absolute stat modifier.
    ///
    /// Examples:
    ///
    /// +10:
    ///     current + 10
    ///
    /// +20%:
    ///     current * 1.20
    /// </summary>
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

        return current;
    }


    /// <summary>
    /// Applies a multiplier stat modifier.
    ///
    /// Base:
    ///     1.0
    ///
    /// +15%:
    ///     1.0 * 1.15 = 1.15
    ///
    /// Another +10%:
    ///     1.15 * 1.10 = 1.265
    ///
    /// This means percentage modifiers stack multiplicatively.
    /// </summary>
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

        return current;
    }


    // =====================================================================
    // SANITIZATION
    // =====================================================================

    /// <summary>
    /// Sanitizes an absolute stat.
    ///
    /// Invalid floating-point values become 0.
    /// Negative values are clamped to 0.
    /// </summary>
    private static float SanitizeAbsoluteValue(
        float value)
    {
        if (!IsFinite(value))
            return 0f;

        return Mathf.Max(
            0f,
            value
        );
    }


    /// <summary>
    /// Sanitizes a multiplier stat.
    ///
    /// Invalid floating-point values become 1.
    /// Negative values are clamped to 0.
    /// </summary>
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


    /// <summary>
    /// Checks whether a float is a valid finite number.
    /// </summary>
    private static bool IsFinite(
        float value)
    {
        return
            !float.IsNaN(value) &&
            !float.IsInfinity(value);
    }
}