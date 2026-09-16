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
    // STUCK RECOVERY
    //=========================================================

    [Header("Stuck Recovery")]

    [Tooltip(
        "Tốc độ tối đa được xem là xe đang bị kẹt."
    )]
    [SerializeField] private float stuckSpeedThreshold = 2.5f;

    [Tooltip(
        "Thời gian xe phải bị kẹt liên tục trước khi kích hoạt recovery."
    )]
    [SerializeField] private float stuckDuration = 2.5f;

    [Tooltip(
        "Khoảng an toàn phía trước khi recovery."
    )]
    [SerializeField] private float stuckFrontSafety = 13f;

    [Tooltip(
        "Khoảng an toàn phía sau khi recovery."
    )]
    [SerializeField] private float stuckRearSafety = 13f;

    [Tooltip(
        "Thời gian chờ trước khi thử recovery lại."
    )]
    [SerializeField] private float stuckRecoveryCooldown = 2f;

    [Tooltip(
        "Cho phép hệ thống tự xử lý khi xe bị kẹt."
    )]
    [SerializeField] private bool enableStuckRecovery = true;


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
    // STUCK INTERNAL
    //=========================================================

    private float stuckTimer;

    private float stuckRecoveryCooldownTimer;

    private bool isStuck;


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

        if (stuckRecoveryCooldownTimer > 0f)
        {
            stuckRecoveryCooldownTimer -=
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

        ResetStuckState();

        stuckRecoveryCooldownTimer = 0f;

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


        //=====================================================
        // KHÔNG CÓ XE PHÍA TRƯỚC
        //=====================================================

        if (ahead == null)
        {
            ResetStuckState();

            RestoreSpeed();

            return;
        }


        float distance =
            GetForwardDistance(
                ahead
            );


        if (distance <= 0f)
        {
            ResetStuckState();

            RestoreSpeed();

            return;
        }


        TrafficVehicle aheadVehicle =
            ahead.GetComponent<TrafficVehicle>();


        if (aheadVehicle == null)
        {
            ResetStuckState();

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
        // CẬP NHẬT STUCK
        //=====================================================

        UpdateStuckState(
            mySpeed,
            aheadSpeed,
            distance
        );


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

            if (
                TryStuckRecovery()
            )
            {
                return;
            }

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

            if (
                TryStuckRecovery()
            )
            {
                return;
            }

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
            {
                TryStuckRecovery();

                return;
            }


            if (
                aheadSpeed <
                mySpeed -
                0.5f
            )
            {
                if (
                    TryChangeLane()
                )
                {
                    return;
                }

                TryStuckRecovery();
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

                TryStuckRecovery();

                return;
            }


            if (
                aheadSpeed <
                mySpeed -
                0.75f
            )
            {
                if (
                    TryChangeLane()
                )
                {
                    return;
                }


                FollowVehicle(
                    aheadVehicle,
                    distance
                );

                TryStuckRecovery();

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

        ResetStuckState();

        RestoreSpeed();
    }


    //=========================================================
    // STUCK STATE
    //=========================================================

    private void UpdateStuckState(
        float mySpeed,
        float aheadSpeed,
        float distance
    )
    {
        if (!enableStuckRecovery)
        {
            ResetStuckState();

            return;
        }


        bool speedTooLow =
            mySpeed <=
            Mathf.Max(
                0f,
                stuckSpeedThreshold
            );


        bool vehicleTooClose =
            distance <=
            softBrakeDistance;


        bool blockedByVehicle =
            aheadSpeed <=
            mySpeed +
            0.5f;


        if (
            speedTooLow &&
            vehicleTooClose &&
            blockedByVehicle
        )
        {
            stuckTimer +=
                Time.deltaTime;


            if (
                stuckTimer >=
                Mathf.Max(
                    0.1f,
                    stuckDuration
                )
            )
            {
                if (!isStuck)
                {
                    isStuck = true;

                    if (debugLogs)
                    {
                        Debug.Log(
                            name +
                            " | STUCK DETECTED | " +
                            "Speed=" +
                            mySpeed.ToString("F2") +
                            " | Distance=" +
                            distance.ToString("F2")
                        );
                    }
                }
            }
        }
        else
        {
            ResetStuckState();
        }
    }


    //=========================================================
    // RESET STUCK
    //=========================================================

    private void ResetStuckState()
    {
        stuckTimer = 0f;

        isStuck = false;
    }


    //=========================================================
    // STUCK RECOVERY
    //=========================================================

    private bool TryStuckRecovery()
    {
        if (!enableStuckRecovery)
            return false;


        if (!isStuck)
            return false;


        if (isChangingLane)
            return false;


        if (
            laneChangeCooldownTimer >
            0f
        )
        {
            return false;
        }


        if (
            stuckRecoveryCooldownTimer >
            0f
        )
        {
            return false;
        }


        int recoveryLane =
            FindBestRecoveryLane();


        if (recoveryLane < 0)
        {
            stuckRecoveryCooldownTimer =
                Mathf.Max(
                    0.1f,
                    stuckRecoveryCooldown
                );

            return false;
        }


        StartLaneChange(
            recoveryLane
        );


        if (isChangingLane)
        {
            stuckRecoveryCooldownTimer =
                Mathf.Max(
                    0.1f,
                    stuckRecoveryCooldown
                );

            stuckTimer = 0f;

            isStuck = false;


            if (debugLogs)
            {
                Debug.Log(
                    name +
                    " | STUCK RECOVERY | " +
                    laneIndex +
                    " -> " +
                    recoveryLane
                );
            }


            return true;
        }


        return false;
    }


    //=========================================================
    // FIND RECOVERY LANE
    //=========================================================

    private int FindBestRecoveryLane()
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


            //=================================================
            // CHỈ ĐỔI 1 LANE
            //=================================================

            if (
                Mathf.Abs(
                    lane -
                    laneIndex
                ) != 1
            )
            {
                continue;
            }


            //=================================================
            // PLAYER BLOCK
            //=================================================

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


            //=================================================
            // TRAFFIC SAFETY
            //=================================================

            if (
                !IsRecoveryLaneSafe(
                    lane
                )
            )
            {
                continue;
            }


            float score =
                GetRecoveryLaneClearanceScore(
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
    // RECOVERY LANE SCORE
    //=========================================================

    private float GetRecoveryLaneClearanceScore(
        int lane
    )
    {
        float laneX =
            GetLaneX(lane);


        float frontClearance =
            stuckFrontSafety;

        float rearClearance =
            stuckRearSafety;


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
    // RECOVERY LANE SAFETY
    //=========================================================

    private bool IsRecoveryLaneSafe(
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
            GetLaneX(
                candidateLane
            );


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


            //=================================================
            // XE PHÍA TRƯỚC
            //=================================================

            if (
                forwardDistance >= 0f &&
                forwardDistance <
                stuckFrontSafety
            )
            {
                return false;
            }


            //=================================================
            // XE PHÍA SAU
            //=================================================

            if (
                forwardDistance < 0f &&
                Mathf.Abs(
                    forwardDistance
                ) <
                stuckRearSafety
            )
            {
                return false;
            }


            //=================================================
            // XE ĐANG ĐỔI LANE
            //=================================================

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


        //=====================================================
        // CORRIDOR AN TOÀN
        //=====================================================

        if (
            !IsRecoveryLaneChangeCorridorClear(
                candidateLane
            )
        )
        {
            return false;
        }


        return true;
    }


    //=========================================================
    // RECOVERY LANE CHANGE CORRIDOR
    //=========================================================

    private bool IsRecoveryLaneChangeCorridorClear(
        int candidateLane
    )
    {
        float startX =
            transform.position.x;

        float targetX =
            GetLaneX(
                candidateLane
            );


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


        float recoveryCorridorDistance =
            Mathf.Max(
                stuckFrontSafety,
                stuckRearSafety
            );


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
                otherX < minX ||
                otherX > maxX
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
                recoveryCorridorDistance
            )
            {
                return false;
            }
        }


        return true;
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


        return isChangingLane;
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


            ResetStuckState();

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