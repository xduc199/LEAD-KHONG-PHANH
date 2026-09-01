using System.Collections.Generic;
using UnityEngine;

public class TrafficCarBehavior : MonoBehaviour
{
    //=========================================================
    // LANE POSITIONS
    //=========================================================

    [Header("Lane Positions")]
    [SerializeField] private float leftLaneX = -5f;
    [SerializeField] private float centerLaneX = 0f;
    [SerializeField] private float rightLaneX = 5f;


    //=========================================================
    // LANE
    //=========================================================

    [Header("Lane")]
    [Range(0, 2)]
    [SerializeField] private int laneIndex = 1;


    //=========================================================
    // LANE CHANGE
    //=========================================================

    [Header("Lane Change")]

    [Range(0f, 1f)]
    [SerializeField] private float laneChangeProbability = 0.85f;

    [SerializeField] private float laneChangeDuration = 1.2f;

    [SerializeField] private float decisionDistance = 20f;

    [SerializeField] private float targetFrontSafety = 18f;

    [SerializeField] private float targetRearSafety = 18f;

    [SerializeField] private float lateralSafety = 2.5f;

    [SerializeField] private float laneChangeBlockingDistance = 9f;

    [SerializeField] private float laneChangeCooldown = 1.2f;


    //=========================================================
    // SMOOTH STEERING
    //=========================================================

    [Header("Smooth Steering")]

    [Tooltip("Góc đánh lái theo hướng chuyển làn.")]
    [SerializeField] private float maxTurnAngle = 10f;

    [Tooltip("Độ nghiêng thân xe khi chuyển làn.")]
    [SerializeField] private float maxLeanAngle = 7f;

    [Tooltip("Tốc độ phản hồi rotation.")]
    [SerializeField] private float rotationSpeed = 10f;

    [Tooltip("Tốc độ quay về rotation bình thường sau khi chuyển làn.")]
    [SerializeField] private float rotationResetSpeed = 8f;

    [Tooltip("Hệ số góc đánh lái theo loại xe.")]
    [SerializeField] private bool useVehicleTypeSteering = true;


    //=========================================================
    // FOLLOWING
    //=========================================================

    [Header("Following")]

    [SerializeField] private float softBrakeDistance = 14f;

    [SerializeField] private float strongBrakeDistance = 8f;

    [SerializeField] private float hardSafetyDistance = 4.5f;

    [SerializeField] private float brakingMultiplier = 0.90f;


    //=========================================================
    // PLAYER SAFETY
    //=========================================================

    [Header("Player Safety")]

    [SerializeField] private float playerSafetyDistance = 22f;

    [SerializeField] private float playerLaneTolerance = 2.5f;

    [SerializeField] private float playerFrontBlockDistance = 22f;

    [SerializeField] private float playerRearBlockDistance = 16f;


    //=========================================================
    // VEHICLE OCCUPANCY
    //=========================================================

    [Header("Vehicle Occupancy")]
    [SerializeField] private float laneOccupancyTolerance = 2.4f;


    //=========================================================
    // DEBUG
    //=========================================================

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    [SerializeField] private bool drawDebugGizmos = true;


    //=========================================================
    // INTERNAL
    //=========================================================

    private TrafficVehicle vehicle;

    private bool isChangingLane;

    private int targetLane;

    private float laneStartX;

    private float laneTargetX;

    private float laneChangeTimer;

    private float currentLaneChangeDuration;

    private float laneChangeCooldownTimer;

    private TrafficCarBehavior obstacleAhead;


    //=========================================================
    // ALL TRAFFIC
    //=========================================================

    private static readonly List<TrafficCarBehavior>
        allTraffic =
        new List<TrafficCarBehavior>();


    //=========================================================
    // PUBLIC
    //=========================================================

    public int LaneIndex
    {
        get { return laneIndex; }
    }

    public int TargetLane
    {
        get { return targetLane; }
    }

    public bool IsChangingLane
    {
        get { return isChangingLane; }
    }

    public int TravelDirection
    {
        get
        {
            if (vehicle == null)
                return 1;

            return vehicle.TravelDirection;
        }
    }


    //=========================================================
    // REGISTRY
    //=========================================================

    private void OnEnable()
    {
        if (!allTraffic.Contains(this))
        {
            allTraffic.Add(this);
        }
    }

    private void OnDisable()
    {
        allTraffic.Remove(this);
    }


    //=========================================================
    // AWAKE
    //=========================================================

    private void Awake()
    {
        vehicle =
            GetComponent<TrafficVehicle>();
    }


    //=========================================================
    // START
    //=========================================================

    private void Start()
    {
        laneIndex =
            Mathf.Clamp(
                laneIndex,
                0,
                2
            );

        targetLane =
            laneIndex;

        currentLaneChangeDuration =
            GetLaneChangeDuration();

        SnapToLane();

        transform.rotation =
            Quaternion.identity;
    }


    //=========================================================
    // UPDATE
    //=========================================================

    private void Update()
    {
        if (vehicle == null)
            return;

        if (laneChangeCooldownTimer > 0f)
        {
            laneChangeCooldownTimer -=
                Time.deltaTime;
        }

        if (isChangingLane)
        {
            UpdateLaneChange();
            return;
        }

        UpdateNormalRotation();

        UpdateTrafficLogic();
    }


    //=========================================================
    // SET LANE
    //=========================================================

    public void SetLaneIndex(
        int lane
    )
    {
        laneIndex =
            Mathf.Clamp(
                lane,
                0,
                2
            );

        targetLane =
            laneIndex;

        isChangingLane = false;

        SnapToLane();

        transform.rotation =
            Quaternion.identity;
    }


    //=========================================================
    // SNAP
    //=========================================================

    private void SnapToLane()
    {
        Vector3 position =
            transform.position;

        position.x =
            GetLaneX(laneIndex);

        transform.position =
            position;
    }


    //=========================================================
    // MAIN TRAFFIC LOGIC
    //=========================================================

    private void UpdateTrafficLogic()
    {
        TrafficCarBehavior ahead =
            FindClosestVehicleAhead(
                laneIndex
            );

        obstacleAhead =
            ahead;

        if (ahead == null)
        {
            RestoreSpeed();
            return;
        }

        float distance =
            GetForwardDistance(
                ahead
            );

        if (distance <= 0f)
        {
            RestoreSpeed();
            return;
        }

        TrafficVehicle aheadVehicle =
            ahead.GetComponent<TrafficVehicle>();

        if (aheadVehicle == null)
        {
            RestoreSpeed();
            return;
        }

        float mySpeed =
            vehicle.GetMoveSpeed();

        float aheadSpeed =
            aheadVehicle.GetMoveSpeed();

        bool playerBlocking =
            IsPlayerBlockingLaneChangeCorridor();


        //=====================================================
        // CỰC GẦN
        //=====================================================

        if (
            distance <=
            hardSafetyDistance
        )
        {
            FollowVehicle(
                aheadVehicle,
                distance
            );

            return;
        }


        //=====================================================
        // GẦN
        //=====================================================

        if (
            distance <=
            strongBrakeDistance
        )
        {
            FollowVehicle(
                aheadVehicle,
                distance
            );

            return;
        }


        //=====================================================
        // ĐANG TIẾN GẦN
        //=====================================================

        if (
            distance <=
            softBrakeDistance
        )
        {
            FollowVehicle(
                aheadVehicle,
                distance
            );

            if (playerBlocking)
                return;

            if (
                aheadSpeed <
                mySpeed -
                0.5f
            )
            {
                TryChangeLane();
            }

            return;
        }


        //=====================================================
        // VÙNG QUYẾT ĐỊNH
        //=====================================================

        if (
            distance <=
            decisionDistance
        )
        {
            if (playerBlocking)
            {
                FollowVehicle(
                    aheadVehicle,
                    distance
                );

                return;
            }

            if (
                aheadSpeed <
                mySpeed -
                0.75f
            )
            {
                if (TryChangeLane())
                    return;

                FollowVehicle(
                    aheadVehicle,
                    distance
                );

                return;
            }

            FollowVehicle(
                aheadVehicle,
                distance
            );

            return;
        }


        //=====================================================
        // XA
        //=====================================================

        RestoreSpeed();
    }


    //=========================================================
    // TRY CHANGE LANE
    //=========================================================

    private bool TryChangeLane()
    {
        if (
            laneChangeCooldownTimer >
            0f
        )
        {
            return false;
        }

        int safeLane =
            FindBestAlternativeLane();

        if (safeLane < 0)
            return false;

        if (
            Random.value >
            laneChangeProbability
        )
        {
            return false;
        }

        StartLaneChange(
            safeLane
        );

        return true;
    }


    //=========================================================
    // FIND CLOSEST VEHICLE AHEAD
    //=========================================================

    private TrafficCarBehavior FindClosestVehicleAhead(
        int lane
    )
    {
        TrafficCarBehavior closest =
            null;

        float closestDistance =
            float.MaxValue;

        float laneX =
            GetLaneX(lane);

        for (
            int i = 0;
            i < allTraffic.Count;
            i++
        )
        {
            TrafficCarBehavior other =
                allTraffic[i];

            if (
                other == null ||
                other == this ||
                !other.isActiveAndEnabled
            )
            {
                continue;
            }

            if (
                other.TravelDirection !=
                TravelDirection
            )
            {
                continue;
            }

            float xDistance =
                Mathf.Abs(
                    other.transform.position.x -
                    laneX
                );

            bool physicallyInLane =
                xDistance <=
                laneOccupancyTolerance;

            bool logicalInLane =
                other.LaneIndex ==
                lane;

            bool enteringLane =
                other.IsChangingLane &&
                other.TargetLane ==
                lane;

            if (
                !physicallyInLane &&
                !logicalInLane &&
                !enteringLane
            )
            {
                continue;
            }

            float distance =
                GetForwardDistance(
                    other
                );

            if (distance <= 0f)
                continue;

            if (
                distance <
                closestDistance
            )
            {
                closestDistance =
                    distance;

                closest =
                    other;
            }
        }

        return closest;
    }


    //=========================================================
    // FORWARD DISTANCE
    //=========================================================

    private float GetForwardDistance(
        TrafficCarBehavior other
    )
    {
        if (other == null)
            return -1f;

        return GetForwardDistanceFromPosition(
            other.transform.position
        );
    }


    private float GetForwardDistanceFromPosition(
        Vector3 position
    )
    {
        float raw =
            position.z -
            transform.position.z;

        if (TravelDirection >= 0)
            return raw;

        return -raw;
    }


    //=========================================================
    // FIND BEST ALTERNATIVE LANE
    //=========================================================

    private int FindBestAlternativeLane()
    {
        int bestLane = -1;

        float bestScore =
            float.MinValue;

        for (
            int lane = 0;
            lane < 3;
            lane++
        )
        {
            if (
                lane ==
                laneIndex
            )
            {
                continue;
            }

            if (
                Mathf.Abs(
                    lane -
                    laneIndex
                ) != 1
            )
            {
                continue;
            }

            if (
                IsPlayerBlockingSpecificLane(
                    lane
                )
            )
            {
                continue;
            }

            if (
                IsPlayerBlockingSpecificLaneChange(
                    lane
                )
            )
            {
                continue;
            }

            if (
                !IsLaneSafe(
                    lane
                )
            )
            {
                continue;
            }

            float score =
                GetLaneClearanceScore(
                    lane
                );

            if (
                score >
                bestScore
            )
            {
                bestScore =
                    score;

                bestLane =
                    lane;
            }
        }

        return bestLane;
    }


    //=========================================================
    // LANE CLEARANCE SCORE
    //=========================================================

    private float GetLaneClearanceScore(
        int lane
    )
    {
        float laneX =
            GetLaneX(lane);

        float frontClearance =
            targetFrontSafety;

        float rearClearance =
            targetRearSafety;

        for (
            int i = 0;
            i < allTraffic.Count;
            i++
        )
        {
            TrafficCarBehavior other =
                allTraffic[i];

            if (
                other == null ||
                other == this ||
                !other.isActiveAndEnabled
            )
            {
                continue;
            }

            if (
                other.TravelDirection !=
                TravelDirection
            )
            {
                continue;
            }

            float xDistance =
                Mathf.Abs(
                    other.transform.position.x -
                    laneX
                );

            if (
                xDistance >
                laneOccupancyTolerance
            )
            {
                continue;
            }

            float forwardDistance =
                GetForwardDistance(
                    other
                );

            if (
                forwardDistance >= 0f
            )
            {
                frontClearance =
                    Mathf.Min(
                        frontClearance,
                        forwardDistance
                    );
            }
            else
            {
                rearClearance =
                    Mathf.Min(
                        rearClearance,
                        Mathf.Abs(
                            forwardDistance
                        )
                    );
            }
        }

        return
            frontClearance +
            rearClearance;
    }


    //=========================================================
    // LANE SAFETY
    //=========================================================

    private bool IsLaneSafe(
        int candidateLane
    )
    {
        if (
            candidateLane < 0 ||
            candidateLane > 2
        )
        {
            return false;
        }

        if (
            candidateLane ==
            laneIndex
        )
        {
            return false;
        }

        float targetX =
            GetLaneX(candidateLane);

        for (
            int i = 0;
            i < allTraffic.Count;
            i++
        )
        {
            TrafficCarBehavior other =
                allTraffic[i];

            if (
                other == null ||
                other == this ||
                !other.isActiveAndEnabled
            )
            {
                continue;
            }

            if (
                other.TravelDirection !=
                TravelDirection
            )
            {
                continue;
            }

            float xDistance =
                Mathf.Abs(
                    other.transform.position.x -
                    targetX
                );

            bool occupiesLane =
                xDistance <=
                laneOccupancyTolerance;

            bool enteringLane =
                other.IsChangingLane &&
                other.TargetLane ==
                candidateLane;

            if (
                !occupiesLane &&
                !enteringLane
            )
            {
                continue;
            }

            float forwardDistance =
                GetForwardDistance(
                    other
                );

            if (
                forwardDistance >= 0f &&
                forwardDistance <
                targetFrontSafety
            )
            {
                return false;
            }

            if (
                forwardDistance < 0f &&
                Mathf.Abs(
                    forwardDistance
                ) <
                targetRearSafety
            )
            {
                return false;
            }

            if (
                other.IsChangingLane &&
                Mathf.Abs(
                    forwardDistance
                ) <
                laneChangeBlockingDistance
            )
            {
                return false;
            }
        }

        return IsLaneChangeCorridorClear(
            candidateLane
        );
    }


    //=========================================================
    // LANE CHANGE CORRIDOR
    //=========================================================

    private bool IsLaneChangeCorridorClear(
        int candidateLane
    )
    {
        float startX =
            transform.position.x;

        float targetX =
            GetLaneX(candidateLane);

        float minX =
            Mathf.Min(
                startX,
                targetX
            ) -
            lateralSafety;

        float maxX =
            Mathf.Max(
                startX,
                targetX
            ) +
            lateralSafety;

        for (
            int i = 0;
            i < allTraffic.Count;
            i++
        )
        {
            TrafficCarBehavior other =
                allTraffic[i];

            if (
                other == null ||
                other == this ||
                !other.isActiveAndEnabled
            )
            {
                continue;
            }

            if (
                other.TravelDirection !=
                TravelDirection
            )
            {
                continue;
            }

            float otherX =
                other.transform.position.x;

            if (
                otherX <
                minX ||
                otherX >
                maxX
            )
            {
                continue;
            }

            float zDistance =
                Mathf.Abs(
                    other.transform.position.z -
                    transform.position.z
                );

            if (
                zDistance <
                Mathf.Max(
                    targetFrontSafety,
                    targetRearSafety
                )
            )
            {
                return false;
            }
        }

        return true;
    }


    //=========================================================
    // START LANE CHANGE
    //=========================================================

    private void StartLaneChange(
        int newLane
    )
    {
        if (
            isChangingLane ||
            newLane == laneIndex
        )
        {
            return;
        }

        if (
            laneChangeCooldownTimer >
            0f
        )
        {
            return;
        }

        if (
            !IsLaneSafe(newLane)
        )
        {
            return;
        }

        targetLane =
            Mathf.Clamp(
                newLane,
                0,
                2
            );

        laneStartX =
            transform.position.x;

        laneTargetX =
            GetLaneX(targetLane);

        laneChangeTimer =
            0f;

        currentLaneChangeDuration =
            GetLaneChangeDuration();

        isChangingLane =
            true;

        if (debugLogs)
        {
            Debug.Log(
                name +
                " | LANE CHANGE | " +
                laneIndex +
                " -> " +
                targetLane
            );
        }
    }


    //=========================================================
    // UPDATE LANE CHANGE
    //=========================================================

    private void UpdateLaneChange()
    {
        if (!isChangingLane)
            return;

        laneChangeTimer +=
            Time.deltaTime;

        float progress =
            Mathf.Clamp01(
                laneChangeTimer /
                Mathf.Max(
                    0.1f,
                    currentLaneChangeDuration
                )
            );


        //=====================================================
        // SMOOTH POSITION
        //=====================================================

        float smooth =
            Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

        Vector3 position =
            transform.position;

        position.x =
            Mathf.Lerp(
                laneStartX,
                laneTargetX,
                smooth
            );

        transform.position =
            position;


        //=====================================================
        // STEERING DIRECTION
        //=====================================================

        float direction =
            Mathf.Sign(
                laneTargetX -
                laneStartX
            );


        //=====================================================
        // STEERING CURVE
        //=====================================================

        float steeringCurve =
            Mathf.Sin(
                progress *
                Mathf.PI
            );


        //=====================================================
        // VEHICLE TYPE MULTIPLIER
        //=====================================================

        float steeringMultiplier =
            GetVehicleSteeringMultiplier();


        //=====================================================
        // Y ROTATION
        //=====================================================

        float targetY =
            direction *
            maxTurnAngle *
            steeringCurve *
            steeringMultiplier;


        //=====================================================
        // Z LEAN
        //=====================================================

        float targetZ =
            -direction *
            maxLeanAngle *
            steeringCurve *
            steeringMultiplier;


        Quaternion targetRotation =
            Quaternion.Euler(
                0f,
                targetY,
                targetZ
            );


        transform.rotation =
            Quaternion.Lerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime *
                rotationSpeed
            );


        //=====================================================
        // FINISH
        //=====================================================

        if (progress >= 1f)
        {
            transform.position =
                new Vector3(
                    laneTargetX,
                    transform.position.y,
                    transform.position.z
                );

            laneIndex =
                targetLane;

            isChangingLane =
                false;

            laneChangeCooldownTimer =
                laneChangeCooldown;

            RestoreSpeed();

            if (debugLogs)
            {
                Debug.Log(
                    name +
                    " | LANE CHANGE COMPLETE | " +
                    LaneName(laneIndex)
                );
            }
        }
    }


    //=========================================================
    // NORMAL ROTATION
    //=========================================================

    private void UpdateNormalRotation()
    {
        Quaternion targetRotation =
            Quaternion.identity;

        transform.rotation =
            Quaternion.Lerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime *
                rotationResetSpeed
            );
    }


    //=========================================================
    // VEHICLE STEERING MULTIPLIER
    //=========================================================

    private float GetVehicleSteeringMultiplier()
    {
        if (!useVehicleTypeSteering)
            return 1f;

        string vehicleName =
            gameObject.name.ToLower();

        //=====================================================
        // BUS
        //=====================================================

        if (
            vehicleName.Contains("bus") ||
            vehicleName.Contains("coach")
        )
        {
            return 0.65f;
        }


        //=====================================================
        // BA GÁC
        //=====================================================

        if (
            vehicleName.Contains("bagac") ||
            vehicleName.Contains("ba_gac") ||
            vehicleName.Contains("ba gac") ||
            vehicleName.Contains("threewheel")
        )
        {
            return 0.80f;
        }


        //=====================================================
        // MOTOR
        //=====================================================

        if (
            vehicleName.Contains("motor") ||
            vehicleName.Contains("motorcycle") ||
            vehicleName.Contains("bike") ||
            vehicleName.Contains("scooter")
        )
        {
            return 1.15f;
        }


        //=====================================================
        // CAR
        //=====================================================

        if (
            vehicleName.Contains("car") ||
            vehicleName.Contains("vehicle") ||
            vehicleName.Contains("sedan") ||
            vehicleName.Contains("suv")
        )
        {
            return 1f;
        }

        return 1f;
    }


    //=========================================================
    // FOLLOW VEHICLE
    //=========================================================

    private void FollowVehicle(
        TrafficVehicle otherVehicle,
        float distance
    )
    {
        if (otherVehicle == null)
            return;

        float otherSpeed =
            otherVehicle.GetMoveSpeed();

        float targetSpeed;

        if (
            distance <=
            hardSafetyDistance
        )
        {
            targetSpeed =
                otherSpeed *
                0.20f;
        }
        else if (
            distance <=
            strongBrakeDistance
        )
        {
            targetSpeed =
                otherSpeed *
                0.45f;
        }
        else if (
            distance <=
            softBrakeDistance
        )
        {
            targetSpeed =
                otherSpeed *
                0.70f;
        }
        else
        {
            targetSpeed =
                otherSpeed *
                brakingMultiplier;
        }

        targetSpeed =
            Mathf.Min(
                targetSpeed,
                otherSpeed
            );

        targetSpeed =
            Mathf.Max(
                0.25f,
                targetSpeed
            );

        vehicle.SetTemporarySpeed(
            targetSpeed
        );
    }


    //=========================================================
    // RESTORE SPEED
    //=========================================================

    private void RestoreSpeed()
    {
        if (vehicle == null)
            return;

        vehicle.RestoreBaseSpeed();
    }


    //=========================================================
    // PLAYER
    //=========================================================

    private GameObject FindPlayer()
    {
        return GameObject.FindGameObjectWithTag(
            "Player"
        );
    }


    private bool IsPlayerBlockingLaneChangeCorridor()
    {
        GameObject player =
            FindPlayer();

        if (player == null)
            return false;

        Vector3 playerPosition =
            player.transform.position;

        float zDistance =
            GetForwardDistanceFromPosition(
                playerPosition
            );

        if (
            Mathf.Abs(zDistance) >
            playerSafetyDistance
        )
        {
            return false;
        }

        float currentLaneX =
            GetLaneX(
                laneIndex
            );

        if (
            Mathf.Abs(
                playerPosition.x -
                currentLaneX
            ) <
            playerLaneTolerance
        )
        {
            return true;
        }

        if (isChangingLane)
        {
            return IsPlayerInsideLaneCorridor(
                playerPosition,
                laneIndex,
                targetLane
            );
        }

        if (laneIndex > 0)
        {
            if (
                IsPlayerInsideLaneCorridor(
                    playerPosition,
                    laneIndex,
                    laneIndex - 1
                )
            )
            {
                return true;
            }
        }

        if (laneIndex < 2)
        {
            if (
                IsPlayerInsideLaneCorridor(
                    playerPosition,
                    laneIndex,
                    laneIndex + 1
                )
            )
            {
                return true;
            }
        }

        return false;
    }


    //=========================================================
    // PLAYER SPECIFIC LANE
    //=========================================================

    private bool IsPlayerBlockingSpecificLane(
        int lane
    )
    {
        GameObject player =
            FindPlayer();

        if (player == null)
            return false;

        float zDistance =
            GetForwardDistanceFromPosition(
                player.transform.position
            );

        if (
            Mathf.Abs(zDistance) >
            playerSafetyDistance
        )
        {
            return false;
        }

        float laneX =
            GetLaneX(lane);

        return
            Mathf.Abs(
                player.transform.position.x -
                laneX
            ) <
            playerLaneTolerance;
    }


    //=========================================================
    // PLAYER CORRIDOR
    //=========================================================

    private bool IsPlayerBlockingSpecificLaneChange(
        int targetLane
    )
    {
        GameObject player =
            FindPlayer();

        if (player == null)
            return false;

        Vector3 playerPosition =
            player.transform.position;

        float zDistance =
            GetForwardDistanceFromPosition(
                playerPosition
            );

        if (
            zDistance >
            playerFrontBlockDistance
        )
        {
            return false;
        }

        if (
            zDistance <
            -playerRearBlockDistance
        )
        {
            return false;
        }

        return IsPlayerInsideLaneCorridor(
            playerPosition,
            laneIndex,
            targetLane
        );
    }


    //=========================================================
    // PLAYER INSIDE CORRIDOR
    //=========================================================

    private bool IsPlayerInsideLaneCorridor(
        Vector3 playerPosition,
        int fromLane,
        int toLane
    )
    {
        float fromX =
            GetLaneX(fromLane);

        float toX =
            GetLaneX(toLane);

        float minX =
            Mathf.Min(
                fromX,
                toX
            ) -
            playerLaneTolerance;

        float maxX =
            Mathf.Max(
                fromX,
                toX
            ) +
            playerLaneTolerance;

        return
            playerPosition.x >= minX &&
            playerPosition.x <= maxX;
    }


    //=========================================================
    // LANE CHANGE DURATION
    //=========================================================

    private float GetLaneChangeDuration()
    {
        string vehicleName =
            gameObject.name.ToLower();

        if (
            vehicleName.Contains("bus") ||
            vehicleName.Contains("coach")
        )
        {
            return 2.4f;
        }

        if (
            vehicleName.Contains("bagac") ||
            vehicleName.Contains("ba_gac") ||
            vehicleName.Contains("ba gac") ||
            vehicleName.Contains("threewheel")
        )
        {
            return 1.9f;
        }

        if (
            vehicleName.Contains("motor") ||
            vehicleName.Contains("motorcycle") ||
            vehicleName.Contains("bike") ||
            vehicleName.Contains("scooter")
        )
        {
            return 0.85f;
        }

        if (
            vehicleName.Contains("car") ||
            vehicleName.Contains("vehicle") ||
            vehicleName.Contains("sedan") ||
            vehicleName.Contains("suv")
        )
        {
            return 1.4f;
        }

        return laneChangeDuration;
    }


    //=========================================================
    // LANE X
    //=========================================================

    private float GetLaneX(
        int lane
    )
    {
        switch (lane)
        {
            case 0:
                return leftLaneX;

            case 1:
                return centerLaneX;

            case 2:
                return rightLaneX;

            default:
                return centerLaneX;
        }
    }


    //=========================================================
    // OLD API
    //=========================================================

    public void TriggerPanicLaneChange(
        Vector3 playerPosition
    )
    {
        // Không sử dụng.
    }


    //=========================================================
    // LANE NAME
    //=========================================================

    private string LaneName(
        int lane
    )
    {
        switch (lane)
        {
            case 0:
                return "LEFT";

            case 1:
                return "CENTER";

            case 2:
                return "RIGHT";

            default:
                return "UNKNOWN";
        }
    }


    //=========================================================
    // DEBUG
    //=========================================================

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos)
            return;

        float[] lanes =
        {
            leftLaneX,
            centerLaneX,
            rightLaneX
        };

        for (
            int i = 0;
            i < lanes.Length;
            i++
        )
        {
            Gizmos.DrawWireSphere(
                new Vector3(
                    lanes[i],
                    transform.position.y,
                    transform.position.z
                ),
                0.35f
            );
        }

        float currentX =
            GetLaneX(laneIndex);

        Gizmos.DrawLine(
            new Vector3(
                currentX,
                transform.position.y,
                transform.position.z
            ),
            new Vector3(
                currentX,
                transform.position.y,
                transform.position.z +
                decisionDistance
            )
        );
    }
}