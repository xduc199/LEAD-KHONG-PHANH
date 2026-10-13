#ifndef LEAD_KHONG_PHANH_CURVE_WORLD_COMMON_INCLUDED
#define LEAD_KHONG_PHANH_CURVE_WORLD_COMMON_INCLUDED

// ============================================================
// LEAD KHONG PHANH - CURVE WORLD COMMON
//
// Gameplay Z: KHÔNG BAO GIỜ thay đổi
// Gameplay X: KHÔNG BAO GIỜ thay đổi
//
// Visual only:
//   X = horizontal turn
//   Y = gentle hill / slope
//
// Profile:
//   FLAT
//      -> GENTLE UP
//      -> HOLD
//      -> GENTLE DOWN
//      -> TURN
// ============================================================


// ============================================================
// GLOBAL PARAMETERS
// ============================================================

float _CurveStart;
float _CurveRange;
float _CurveRadius;
float _CurveMaxAngleRad;
float _CurveMaxXOffset;

float _CurveDirection;
float _CurveStrength;

float _CurveMaxYOffset;
float _CurveVerticalStrength;

float _CurveReferenceZ;


// ============================================================
// HILL / SLOPE PARAMETERS
// ============================================================

// Distance from CurveStart where the uphill begins.
float _CurveSlopeStart;

// Length of the uphill section.
float _CurveSlopeLength;

// Height gained at the top of the hill.
float _CurveSlopeHeight;

// Distance from CurveStart where the downhill ENDS.
// The downhill begins before this point.
float _CurveSlopeDropLength;


// ============================================================
// DATA
// ============================================================

struct CurveWorldData
{
    float3 positionWS;

    // dX / dZ
    float xSlope;

    // dY / dZ
    float ySlope;
};


// ============================================================
// SMOOTHER STEP
// ============================================================

float CurveWorld_SmootherStep(float t)
{
    t = saturate(t);

    return
        t * t * t *
        (t * (t * 6.0 - 15.0) + 10.0);
}


// ============================================================
// SMOOTHER STEP DERIVATIVE
// ============================================================

float CurveWorld_SmootherStepDerivative(float t)
{
    t = saturate(t);

    return
        30.0 *
        t * t *
        (t - 1.0) *
        (t - 1.0);
}


// ============================================================
// HILL PROFILE
//
// Returns:
//     height
//
// out slope:
//     dY / dZ
//
// Profile:
//
//                  ______
//                /        \
//              /            \__
// ____________/                 \________
//
// ============================================================

float CurveWorld_EvaluateHill(
    float distanceFromCurveStart,
    out float ySlope)
{
    ySlope = 0.0;

    float slopeStart =
        max(_CurveSlopeStart, 0.0);

    float slopeLength =
        max(_CurveSlopeLength, 0.001);

    float slopeEnd =
        slopeStart + slopeLength;

    float dropLength =
        max(_CurveSlopeDropLength, 0.001);

    // --------------------------------------------------------
    // 1. UPHILL
    // --------------------------------------------------------

    if (distanceFromCurveStart <= slopeStart)
    {
        return 0.0;
    }

    if (distanceFromCurveStart < slopeEnd)
    {
        float t =
            (distanceFromCurveStart - slopeStart)
            / slopeLength;

        float s =
            CurveWorld_SmootherStep(t);

        float ds =
            CurveWorld_SmootherStepDerivative(t)
            / slopeLength;

        ySlope =
            _CurveSlopeHeight *
            _CurveVerticalStrength *
            ds;

        return
            _CurveSlopeHeight *
            _CurveVerticalStrength *
            s;
    }


    // --------------------------------------------------------
    // 2. DOWNHILL
    //
    // We derive the drop end from the turn position:
    //
    // dropEnd = CurveRangeStart
    //          - dropLength
    //
    // Since CurveRange currently begins at CurveStart,
    // use the curve range as the end of the pre-turn setup.
    //
    // --------------------------------------------------------

    float turnDistance =
        max(_CurveRange, slopeEnd + dropLength);

    float dropEnd =
        turnDistance;

    float dropStart =
        max(
            slopeEnd,
            dropEnd - dropLength
        );


    // --------------------------------------------------------
    // 3. HOLD THE TOP
    // --------------------------------------------------------

    if (distanceFromCurveStart <= dropStart)
    {
        return
            _CurveSlopeHeight *
            _CurveVerticalStrength;
    }


    // --------------------------------------------------------
    // 4. GENTLE DOWNHILL
    // --------------------------------------------------------

    if (distanceFromCurveStart < dropEnd)
    {
        float t =
            (distanceFromCurveStart - dropStart)
            / dropLength;

        float s =
            CurveWorld_SmootherStep(t);

        float ds =
            CurveWorld_SmootherStepDerivative(t)
            / dropLength;

        ySlope =
            -_CurveSlopeHeight *
            _CurveVerticalStrength *
            ds;

        return
            _CurveSlopeHeight *
            _CurveVerticalStrength *
            (1.0 - s);
    }


    // --------------------------------------------------------
    // 5. AFTER DOWNHILL
    // --------------------------------------------------------

    return 0.0;
}


// ============================================================
// MAIN CURVE EVALUATION
// ============================================================

CurveWorldData CurveWorld_Evaluate(
    float3 worldPosition)
{
    CurveWorldData result;

    result.positionWS = worldPosition;
    result.xSlope = 0.0;
    result.ySlope = 0.0;


    // Distance from curve reference.
    float d =
        worldPosition.z
        - _CurveReferenceZ
        - _CurveStart;


    // --------------------------------------------------------
    // BEFORE CURVE
    // --------------------------------------------------------

    if (d <= 0.0)
    {
        return result;
    }


    // ========================================================
    // X CURVE
    // ========================================================

    float range =
        max(_CurveRange, 0.001);

    float t =
        saturate(d / range);

    float s =
        CurveWorld_SmootherStep(t);

    float dsdd =
        CurveWorld_SmootherStepDerivative(t)
        / range;


    float xOffset =
        _CurveMaxXOffset *
        _CurveDirection *
        _CurveStrength *
        s;


    result.positionWS.x += xOffset;


    result.xSlope =
        _CurveMaxXOffset *
        _CurveDirection *
        _CurveStrength *
        dsdd;


    // ========================================================
    // Y HILL
    // ========================================================

    float ySlope;

    float yOffset =
        CurveWorld_EvaluateHill(
            d,
            ySlope
        );


    result.positionWS.y += yOffset;

    result.ySlope = ySlope;


    // ========================================================
    // IMPORTANT
    //
    // Z IS NEVER MODIFIED.
    // ========================================================

    return result;
}


// ============================================================
// RIGID OBJECT CURVE
//
// Used for:
// Traffic
// Coins
// Items
// Buildings
// Houses
//
// Prevents mesh shear.
// ============================================================

CurveWorldData CurveWorld_EvaluateRigid(
    float3 worldPosition,
    float3 objectPivotWS)
{
    CurveWorldData pivotData =
        CurveWorld_Evaluate(objectPivotWS);


    float yaw =
        atan(pivotData.xSlope);


    float c =
        cos(yaw);

    float s =
        sin(yaw);


    float3 localOffset =
        worldPosition - objectPivotWS;


    float3 rotatedOffset;


    rotatedOffset.x =
        localOffset.x * c +
        localOffset.z * s;


    rotatedOffset.y =
        localOffset.y;


    rotatedOffset.z =
        -localOffset.x * s +
        localOffset.z * c;


    CurveWorldData result;


    result.positionWS =
        pivotData.positionWS +
        rotatedOffset;


    result.xSlope =
        pivotData.xSlope;


    result.ySlope =
        pivotData.ySlope;


    return result;
}


// ============================================================
// ROTATE NORMAL AROUND Y
// ============================================================

float3 CurveWorld_RotateNormalY(
    float3 normalWS,
    float yaw)
{
    float c =
        cos(yaw);

    float s =
        sin(yaw);


    float3 rotated;


    rotated.x =
        normalWS.x * c +
        normalWS.z * s;


    rotated.y =
        normalWS.y;


    rotated.z =
        -normalWS.x * s +
        normalWS.z * c;


    return normalize(rotated);
}


// ============================================================
// NORMAL CORRECTION
// ============================================================

float3 CurveWorld_ApplyNormalFromData(
    float3 normalWS,
    CurveWorldData data)
{
    float3 corrected;


    corrected.x =
        normalWS.x;


    corrected.y =
        normalWS.y;


    corrected.z =
        normalWS.z
        - data.xSlope * normalWS.x
        - data.ySlope * normalWS.y;


    return normalize(corrected);
}


// ============================================================
// TANGENT CORRECTION
// ============================================================

float3 CurveWorld_ApplyTangentFromData(
    float3 tangentWS,
    CurveWorldData data)
{
    float3 corrected;


    corrected.x =
        tangentWS.x
        + data.xSlope * tangentWS.z;


    corrected.y =
        tangentWS.y
        + data.ySlope * tangentWS.z;


    corrected.z =
        tangentWS.z;


    return normalize(corrected);
}


#endif