using UnityEngine;

[DefaultExecutionOrder(10000)]
public sealed class CurveWorldController : MonoBehaviour
{
    // ============================================================
    // REFERENCE
    // ============================================================

    [Header("Reference")]
    [SerializeField]
    private Transform curveReference;


    // ============================================================
    // MAIN TURN
    // ============================================================

    [Header("Main Turn")]
    [SerializeField]
    [Min(0f)]
    private float curveStart = 120f;

    [SerializeField]
    [Min(1f)]
    private float curveRadius = 900f;

    [SerializeField]
    private float curveDirection = 1f;

    [SerializeField]
    [Range(0f, 45f)]
    private float maxCurveAngle = 16f;

    [SerializeField]
    [Range(0f, 2f)]
    private float curveStrength = 1f;


    // ============================================================
    // CURVE RANGE
    // ============================================================

    [Header("Curve Length")]
    [SerializeField]
    [Min(0f)]
    private float curveRangeOverride = 850f;


    // ============================================================
    // HILL
    // ============================================================

    [Header("Pre-Turn Hill")]
    [Tooltip("Khoảng cách từ Curve Start tới lúc bắt đầu dốc lên.")]
    [SerializeField]
    [Min(0f)]
    private float slopeStart = 70f;


    [Tooltip("Độ dài đoạn dốc lên.")]
    [SerializeField]
    [Min(1f)]
    private float slopeLength = 160f;


    [Tooltip("Độ cao tối đa của đoạn dốc.")]
    [SerializeField]
    [Min(0f)]
    private float slopeHeight = 3f;


    [Tooltip(
        "Độ dài đoạn dốc xuống ngay trước khi bắt đầu ngã rẽ."
    )]
    [SerializeField]
    [Min(1f)]
    private float slopeDropLength = 120f;


    [SerializeField]
    [Range(0f, 1f)]
    private float curveVerticalStrength = 1f;


    // ============================================================
    // SHADER PROPERTY IDS
    // ============================================================

    private static readonly int CurveStartId =
        Shader.PropertyToID("_CurveStart");

    private static readonly int CurveRangeId =
        Shader.PropertyToID("_CurveRange");

    private static readonly int CurveRadiusId =
        Shader.PropertyToID("_CurveRadius");

    private static readonly int CurveMaxAngleRadId =
        Shader.PropertyToID("_CurveMaxAngleRad");

    private static readonly int CurveMaxXOffsetId =
        Shader.PropertyToID("_CurveMaxXOffset");

    private static readonly int CurveDirectionId =
        Shader.PropertyToID("_CurveDirection");

    private static readonly int CurveStrengthId =
        Shader.PropertyToID("_CurveStrength");

    private static readonly int CurveMaxYOffsetId =
        Shader.PropertyToID("_CurveMaxYOffset");

    private static readonly int CurveVerticalStrengthId =
        Shader.PropertyToID("_CurveVerticalStrength");

    private static readonly int CurveReferenceZId =
        Shader.PropertyToID("_CurveReferenceZ");


    private static readonly int CurveSlopeStartId =
        Shader.PropertyToID("_CurveSlopeStart");

    private static readonly int CurveSlopeLengthId =
        Shader.PropertyToID("_CurveSlopeLength");

    private static readonly int CurveSlopeHeightId =
        Shader.PropertyToID("_CurveSlopeHeight");

    private static readonly int CurveSlopeDropLengthId =
        Shader.PropertyToID("_CurveSlopeDropLength");


    // ============================================================
    // STATE
    // ============================================================

    private float lastReferenceZ =
        float.NaN;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        PushCurveParameters(true);
    }


    private void LateUpdate()
    {
        PushCurveParameters(false);
    }


#if UNITY_EDITOR

    private void OnValidate()
    {
        curveStart =
            Mathf.Max(0f, curveStart);

        curveRadius =
            Mathf.Max(1f, curveRadius);

        curveRangeOverride =
            Mathf.Max(0f, curveRangeOverride);

        slopeStart =
            Mathf.Max(0f, slopeStart);

        slopeLength =
            Mathf.Max(1f, slopeLength);

        slopeHeight =
            Mathf.Max(0f, slopeHeight);

        slopeDropLength =
            Mathf.Max(1f, slopeDropLength);

        PushCurveParameters(true);
    }

#endif


    // ============================================================
    // PUSH GLOBAL PARAMETERS
    // ============================================================

    private void PushCurveParameters(
        bool forceReferenceUpdate)
    {
        if (curveReference == null)
        {
            return;
        }


        float referenceZ =
            curveReference.position.z;


        // --------------------------------------------------------
        // TURN ANGLE
        // --------------------------------------------------------

        float angleRad =
            maxCurveAngle *
            Mathf.Deg2Rad;


        // --------------------------------------------------------
        // CURVE RANGE
        // --------------------------------------------------------

        float derivedRange =
            Mathf.Max(
                0.001f,
                curveRadius * angleRad
            );


        float range =
            curveRangeOverride > 0f
                ? curveRangeOverride
                : derivedRange;


        // --------------------------------------------------------
        // X OFFSET
        //
        // Current HLSL uses SmootherStep.
        //
        // Maximum derivative:
        //
        //     max(ds/dt) = 1.875
        //
        // Therefore:
        //
        //     maxSlope =
        //         XOffset * 1.875 / range
        //
        // and:
        //
        //     XOffset =
        //         tan(angle) * range / 1.875
        // --------------------------------------------------------

        float maxXOffset = 0f;


        if (angleRad > 0.000001f &&
            range > 0.001f)
        {
            maxXOffset =
                Mathf.Tan(angleRad) *
                range /
                1.875f;
        }


        // --------------------------------------------------------
        // GLOBAL SHADER PARAMETERS
        // --------------------------------------------------------

        Shader.SetGlobalFloat(
            CurveStartId,
            curveStart
        );


        Shader.SetGlobalFloat(
            CurveRangeId,
            range
        );


        Shader.SetGlobalFloat(
            CurveRadiusId,
            curveRadius
        );


        Shader.SetGlobalFloat(
            CurveMaxAngleRadId,
            angleRad
        );


        Shader.SetGlobalFloat(
            CurveMaxXOffsetId,
            maxXOffset
        );


        Shader.SetGlobalFloat(
            CurveDirectionId,
            curveDirection
        );


        Shader.SetGlobalFloat(
            CurveStrengthId,
            curveStrength
        );


        // --------------------------------------------------------
        // HILL
        // --------------------------------------------------------

        Shader.SetGlobalFloat(
            CurveSlopeStartId,
            slopeStart
        );


        Shader.SetGlobalFloat(
            CurveSlopeLengthId,
            slopeLength
        );


        Shader.SetGlobalFloat(
            CurveSlopeHeightId,
            slopeHeight
        );


        Shader.SetGlobalFloat(
            CurveSlopeDropLengthId,
            slopeDropLength
        );


        Shader.SetGlobalFloat(
            CurveVerticalStrengthId,
            curveVerticalStrength
        );


        // Legacy parameter.
        // Kept so existing shaders/scripts do not break.
        Shader.SetGlobalFloat(
            CurveMaxYOffsetId,
            slopeHeight
        );


        // --------------------------------------------------------
        // REFERENCE Z
        // --------------------------------------------------------

        if (forceReferenceUpdate ||
            !Mathf.Approximately(
                referenceZ,
                lastReferenceZ))
        {
            Shader.SetGlobalFloat(
                CurveReferenceZId,
                referenceZ
            );

            lastReferenceZ =
                referenceZ;
        }
    }
}