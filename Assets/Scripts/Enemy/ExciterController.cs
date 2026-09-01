    using System.Collections.Generic;
    using UnityEngine;

    public class ExciterController : MonoBehaviour
    {
        //=============================================================
        // MOVEMENT
        //=============================================================

        [Header("Movement Settings")]

        [Header("Height Settings")]

        [Tooltip("Độ cao Y của Exciter so với Redline.")]
        [SerializeField]
        private float vehicleHeightOffset = 0.5f;

        [Tooltip("Tốc độ tối thiểu thực tế của Exciter.")]
        [SerializeField]
        private float baseSpeed = 65f;

        [Tooltip("Exciter chạy nhanh hơn Player theo tỉ lệ.")]
        [SerializeField]
        private float playerSpeedMultiplier = 2f;

        [Tooltip("Khoảng tốc độ Exciter luôn cố gắng nhanh hơn Player.")]
        [SerializeField]
        private float speedBonusOverPlayer = 18f;

        [Tooltip("Tốc độ tối đa của Exciter.")]
        [SerializeField]
        private float maxExciterSpeed = 180f;

        [Tooltip("Độ mượt khi tốc độ thay đổi.")]
        [SerializeField]
        private float speedSmoothTime = 0.15f;

        [Tooltip("Khoảng cách Player bỏ xa Exciter để kích hoạt catch-up.")]
        [SerializeField]
        private float catchUpDistance = 35f;

        [Tooltip("Tốc độ cộng thêm khi cần catch-up.")]
        [SerializeField]
        private float catchUpSpeedBonus = 15f;

        [Tooltip("Không cho tốc độ Exciter tụt dưới giá trị này sau khi đã đạt tốc độ.")]
        [SerializeField]
        private bool maintainMinimumRunningSpeed = true;

        [Tooltip("Nếu bật, Exciter sẽ giữ tốc độ đã đạt thay vì giảm theo Player.")]
        [SerializeField]
        private bool maintainCurrentSpeed = true;

        [Tooltip("Tốc độ phản ứng khi tăng tốc.")]
        [SerializeField]
        private float accelerationSmoothTime = 0.08f;

        [Tooltip("Độ mượt khi xoay.")]
        [SerializeField]
        private float rotationSpeed = 10f;

        [Tooltip("Xoay model nếu model bị ngược.")]
        [SerializeField]
        private float modelYRotationOffset = 0f;


        //=============================================================
        // LIFETIME
        //=============================================================

        [Header("Lifetime")]

        [Tooltip("Exciter tự hủy sau số giây này.")]
        [SerializeField]
        private float lifetime = 15f;


        //=============================================================
        // WEAVE
        //=============================================================

        [Header("Continuous Weave")]

        [Tooltip("Biên độ đánh võng.")]
        [SerializeField]
        private float weaveAmplitude = 0.8f;

        [Tooltip("Tần số đánh võng theo quãng đường.")]
        [SerializeField]
        private float weaveFrequency = 0.08f;

        [Tooltip("Thời gian trước khi bắt đầu đánh võng.")]
        [SerializeField]
        private float weaveStartDelay = 0.15f;

        [Tooltip("Tốc độ thay đổi lateral movement.")]
        [SerializeField]
        private float weaveSmoothness = 10f;

        [Tooltip("Biên độ dao động thứ hai.")]
        [SerializeField]
        private float secondaryWeaveAmplitude = 0.25f;

        [Tooltip("Tần số dao động thứ hai.")]
        [SerializeField]
        private float secondaryWeaveFrequency = 0.47f;

        [Tooltip("Có tiếp tục lạng lách sau khi hết Red Line không.")]
        [SerializeField]
        private bool continueWeaveAfterPathEnd = true;


        //=============================================================
        // INFINITE PATH
        //=============================================================

        [Header("Infinite Redline Path")]

        [Tooltip("Bật để tự động nối dài Redline khi Exciter gần tới cuối đường.")]
        [SerializeField]
        private bool enableInfinitePath = true;

        [Tooltip("Số điểm Redline được tự động thêm mỗi lần gần cuối đường.")]
        [SerializeField]
        private int pathExtensionPointCount = 12;

        [Tooltip("Khoảng cách tối thiểu giữa các điểm Redline nối thêm.")]
        [SerializeField]
        private float pathExtensionSpacing = 10f;

        [Tooltip("Khi còn lại số segment này thì bắt đầu nối thêm Redline.")]
        [SerializeField]
        private int pathExtensionTriggerSegments = 3;

        [Tooltip("Giới hạn số điểm tối đa trong path.")]
        [SerializeField]
        private int maximumPathPoints = 500;


        //=============================================================
        // WEAVE ROTATION / LEAN
        //=============================================================

        [Header("Weave Rotation")]

        [Tooltip("Độ nghiêng thân xe khi lạng lách.")]
        [SerializeField]
        private float maxLeanAngle = 12f;

        [Tooltip("Độ mượt khi nghiêng xe.")]
        [SerializeField]
        private float leanSmoothSpeed = 8f;

        [Tooltip("Độ xoay theo hướng chuyển động.")]
        [SerializeField]
        private float steeringRotationSpeed = 12f;


        //=============================================================
        // EFFECTS
        //=============================================================

        [Header("Effects")]

        [SerializeField]
        private GameObject explosionEffectPrefab;


        //=============================================================
        // AUDIO
        //=============================================================

        [Header("Exciter Audio")]

        [SerializeField]
        private AudioClip exciterEngineClip;

        [Range(0f, 1f)]
        [SerializeField]
        private float exciterEngineVolume = 0.65f;

        [SerializeField]
        private float engineMinDistance = 5f;

        [SerializeField]
        private float engineMaxDistance = 45f;

        [SerializeField]
        private bool engineLoop = true;


        //=============================================================
        // PATH
        //=============================================================

        private readonly List<Vector3> pathPoints =
            new List<Vector3>();

        private Transform playerTransform;

        private int currentSegment;

        private float pathProgress;

        private float totalPathLength;


        //=============================================================
        // STATE
        //=============================================================

        private bool isKnockedBack;

        private Rigidbody rb;

        private Vector3 lastMoveDir =
            Vector3.forward;

        private float spawnTime;

        private float weaveTime;

        private float weaveSeed;

        private float totalTravelDistance;

        private bool pathFinished;


        //=============================================================
        // PLAYER SPEED
        //=============================================================

        private Vector3 lastPlayerPosition;

        private float playerSpeed;

        private float currentExciterSpeed;

        private float currentExciterSpeedVelocity;

        private float highestStableSpeed;


        //=============================================================
        // WEAVE STATE
        //=============================================================

        private float currentWeaveOffset;

        private float currentLeanAngle;


        //=============================================================
        // AUDIO
        //=============================================================

        private AudioSource engineAudioSource;


        //=============================================================
        // AWAKE
        //=============================================================

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();

            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }

            rb.isKinematic = true;
            rb.useGravity = false;

            rb.interpolation =
                RigidbodyInterpolation.Interpolate;

            spawnTime = Time.time;

            weaveSeed =
                Random.Range(
                    0f,
                    1000f
                );

            currentExciterSpeed =
                baseSpeed;

            highestStableSpeed =
                baseSpeed;

            currentWeaveOffset = 0f;
            currentLeanAngle = 0f;

            pathFinished = false;

            SetupEngineAudio();
        }


        //=============================================================
        // SET PATH
        //=============================================================

        public void SetPath(
            List<Vector3> points,
            Transform player
        )
        {
            playerTransform = player;

            pathPoints.Clear();

            pathFinished = false;

            if (
                points == null ||
                points.Count < 2
            )
            {
                pathFinished = true;
                return;
            }

            //=========================================================
            // COPY PATH
            //=========================================================

            for (
                int i = 0;
                i < points.Count;
                i++
            )
            {
                pathPoints.Add(
                    points[i]
                );
            }

            //=========================================================
            // PATH DATA
            //=========================================================

            CalculatePathLength();

            currentSegment = 0;

            pathProgress = 0f;

            weaveTime = 0f;

            totalTravelDistance = 0f;

            //=========================================================
            // INITIAL DIRECTION
            //=========================================================

            Vector3 direction =
                pathPoints[1] -
                pathPoints[0];

            direction.y = 0f;

            if (
                direction.sqrMagnitude >
                0.001f
            )
            {
                lastMoveDir =
                    direction.normalized;
            }

            //=========================================================
            // PLAYER SPEED
            //=========================================================

            if (playerTransform != null)
            {
                lastPlayerPosition =
                    playerTransform.position;

                playerSpeed = 0f;
            }

            //=========================================================
            // RESET
            //=========================================================

            spawnTime =
                Time.time;

            currentExciterSpeed =
                baseSpeed;

            highestStableSpeed =
                baseSpeed;

            currentExciterSpeedVelocity = 0f;

            currentWeaveOffset = 0f;

            currentLeanAngle = 0f;

            pathFinished = false;

            isKnockedBack = false;

            //=========================================================
            // ĐẢM BẢO AUDIO CHẠY LẠI KHI SET PATH
            //=========================================================

            RestartEngineAudio();
        }


        //=============================================================
        // CALCULATE PATH LENGTH
        //=============================================================

        private void CalculatePathLength()
        {
            totalPathLength = 0f;

            for (
                int i = 0;
                i < pathPoints.Count - 1;
                i++
            )
            {
                totalPathLength +=
                    Vector3.Distance(
                        pathPoints[i],
                        pathPoints[i + 1]
                    );
            }
        }


        //=============================================================
        // UPDATE
        //=============================================================

        private void Update()
        {
            if (isKnockedBack)
                return;

            //=========================================================
            // LIFETIME
            //=========================================================

            if (
                Time.time -
                spawnTime >=
                lifetime
            )
            {
                StopAllEngineAudio();

                Destroy(
                    gameObject
                );

                return;
            }

            //=========================================================
            // PLAYER SPEED
            //=========================================================

            UpdatePlayerSpeed();

            //=========================================================
            // COLLISION
            //=========================================================

            CheckDistanceCollisions();

            if (isKnockedBack)
                return;

            //=========================================================
            // TARGET SPEED
            //=========================================================

            float targetSpeed =
                CalculateTargetSpeed();

            //=========================================================
            // SPEED CONTROL
            //=========================================================

            float smoothTime =
                targetSpeed >
                currentExciterSpeed
                    ? accelerationSmoothTime
                    : speedSmoothTime;

            currentExciterSpeed =
                Mathf.SmoothDamp(
                    currentExciterSpeed,
                    targetSpeed,
                    ref currentExciterSpeedVelocity,
                    smoothTime
                );

            //=========================================================
            // HARD SPEED FLOOR
            //=========================================================

            if (maintainMinimumRunningSpeed)
            {
                currentExciterSpeed =
                    Mathf.Max(
                        currentExciterSpeed,
                        baseSpeed
                    );
            }

            //=========================================================
            // KEEP HIGHEST STABLE SPEED
            //=========================================================

            if (
                maintainCurrentSpeed &&
                currentExciterSpeed >
                highestStableSpeed
            )
            {
                highestStableSpeed =
                    currentExciterSpeed;
            }

            if (
                maintainCurrentSpeed &&
                highestStableSpeed >
                currentExciterSpeed
            )
            {
                float minimumMaintainedSpeed =
                    Mathf.Max(
                        baseSpeed,
                        highestStableSpeed * 0.92f
                    );

                currentExciterSpeed =
                    Mathf.Max(
                        currentExciterSpeed,
                        minimumMaintainedSpeed
                    );
            }

            //=========================================================
            // MOVE
            //=========================================================

            MoveAlongPath(
                currentExciterSpeed
            );
        }


        //=============================================================
        // PLAYER SPEED
        //=============================================================

        private void UpdatePlayerSpeed()
        {
            if (playerTransform == null)
                return;

            Vector3 currentPosition =
                playerTransform.position;

            Vector3 delta =
                currentPosition -
                lastPlayerPosition;

            float forwardDistance =
                Mathf.Abs(delta.z);

            float measuredSpeed =
                forwardDistance /
                Mathf.Max(
                    Time.deltaTime,
                    0.0001f
                );

            playerSpeed =
                Mathf.Lerp(
                    playerSpeed,
                    measuredSpeed,
                    10f *
                    Time.deltaTime
                );

            lastPlayerPosition =
                currentPosition;
        }


        //=============================================================
        // TARGET SPEED
        //=============================================================

        private float CalculateTargetSpeed()
        {
            float targetSpeed =
                playerSpeed *
                playerSpeedMultiplier;

            targetSpeed +=
                speedBonusOverPlayer;

            targetSpeed =
                Mathf.Max(
                    targetSpeed,
                    baseSpeed
                );

            if (playerTransform != null)
            {
                float zDifference =
                    playerTransform.position.z -
                    transform.position.z;

                if (
                    zDifference >
                    catchUpDistance
                )
                {
                    targetSpeed +=
                        catchUpSpeedBonus;
                }
            }

            targetSpeed =
                Mathf.Min(
                    targetSpeed,
                    maxExciterSpeed
                );

            if (maintainCurrentSpeed)
            {
                targetSpeed =
                    Mathf.Max(
                        targetSpeed,
                        highestStableSpeed
                    );
            }

            return Mathf.Min(
                targetSpeed,
                maxExciterSpeed
            );
        }


        //=============================================================
        // MOVE ALONG PATH
        //=============================================================

        private void MoveAlongPath(
            float speed
        )
        {
            float moveDistance =
                speed *
                Time.deltaTime;

            totalTravelDistance +=
                moveDistance;

            weaveTime +=
                Time.deltaTime;

            //=========================================================
            // INFINITE PATH EXTENSION
            //=========================================================

            if (
                enableInfinitePath &&
                pathPoints != null &&
                pathPoints.Count >= 2
            )
            {
                EnsureInfinitePath();
            }

            //=========================================================
            // PATH STILL ACTIVE
            //=========================================================

            if (
                !pathFinished &&
                pathPoints != null &&
                pathPoints.Count >= 2
            )
            {
                AdvancePath(
                    ref moveDistance
                );

                if (
                    !enableInfinitePath &&
                    currentSegment >=
                    pathPoints.Count - 1
                )
                {
                    pathFinished = true;
                }
            }

            //=========================================================
            // PATH POSITION
            //=========================================================

            if (
                !pathFinished &&
                pathPoints.Count >= 2 &&
                currentSegment <
                pathPoints.Count - 1
            )
            {
                MoveOnActivePath(
                    moveDistance
                );

                return;
            }

            //=========================================================
            // PATH FINISHED
            //=========================================================

            if (continueWeaveAfterPathEnd)
            {
                MoveInfiniteWeave(
                    speed
                );
            }
            else
            {
                Vector3 previousPosition =
                    transform.position;

                transform.position +=
                    lastMoveDir *
                    speed *
                    Time.deltaTime;

                RotateToMovement(
                    previousPosition
                );
            }
        }


        //=============================================================
        // ENSURE INFINITE PATH
        //=============================================================

        private void EnsureInfinitePath()
        {
            if (!enableInfinitePath)
                return;

            if (pathPoints == null)
                return;

            if (pathPoints.Count < 2)
                return;

            if (
                maximumPathPoints > 0 &&
                pathPoints.Count >=
                maximumPathPoints
            )
            {
                return;
            }

            int remainingSegments =
                pathPoints.Count -
                1 -
                currentSegment;

            if (
                remainingSegments >
                Mathf.Max(
                    1,
                    pathExtensionTriggerSegments
                )
            )
            {
                return;
            }

            ExtendPathForward();
        }


        //=============================================================
        // EXTEND PATH FORWARD
        //=============================================================

        private void ExtendPathForward()
        {
            if (
                pathPoints == null ||
                pathPoints.Count < 2
            )
            {
                return;
            }

            int pointsToAdd =
                Mathf.Max(
                    1,
                    pathExtensionPointCount
                );

            if (maximumPathPoints > 0)
            {
                int availableSlots =
                    maximumPathPoints -
                    pathPoints.Count;

                pointsToAdd =
                    Mathf.Min(
                        pointsToAdd,
                        availableSlots
                    );
            }

            if (pointsToAdd <= 0)
                return;

            Vector3 lastPoint =
                pathPoints[
                    pathPoints.Count - 1
                ];

            Vector3 previousPoint =
                pathPoints[
                    pathPoints.Count - 2
                ];

            Vector3 direction =
                lastPoint -
                previousPoint;

            direction.y = 0f;

            if (
                direction.sqrMagnitude <=
                0.0001f
            )
            {
                direction =
                    lastMoveDir;
            }

            if (
                direction.sqrMagnitude <=
                0.0001f
            )
            {
                direction =
                    Vector3.forward;
            }

            direction.Normalize();

            float spacing =
                pathExtensionSpacing;

            if (spacing <= 0.001f)
            {
                spacing =
                    Vector3.Distance(
                        previousPoint,
                        lastPoint
                    );
            }

            if (spacing <= 0.001f)
            {
                spacing = 10f;
            }

            for (
                int i = 0;
                i < pointsToAdd;
                i++
            )
            {
                lastPoint +=
                    direction *
                    spacing;

                pathPoints.Add(
                    lastPoint
                );
            }

            CalculatePathLength();
        }


        //=============================================================
        // ADVANCE PATH
        //=============================================================

        private void AdvancePath(
            ref float moveDistance
        )
        {
            while (
                moveDistance > 0f &&
                currentSegment <
                pathPoints.Count - 1
            )
            {
                Vector3 start =
                    pathPoints[
                        currentSegment
                    ];

                Vector3 end =
                    pathPoints[
                        currentSegment + 1
                    ];

                float segmentLength =
                    Vector3.Distance(
                        start,
                        end
                    );

                if (
                    segmentLength <=
                    0.001f
                )
                {
                    currentSegment++;

                    pathProgress = 0f;

                    continue;
                }

                float remainingSegment =
                    segmentLength -
                    pathProgress;

                if (
                    moveDistance >=
                    remainingSegment
                )
                {
                    moveDistance -=
                        remainingSegment;

                    currentSegment++;

                    pathProgress = 0f;

                    //=================================================
                    // INFINITE PATH
                    //=================================================

                    if (
                        enableInfinitePath &&
                        currentSegment >=
                        pathPoints.Count - 1
                    )
                    {
                        ExtendPathForward();
                    }
                }
                else
                {
                    pathProgress +=
                        moveDistance;

                    moveDistance = 0f;
                }
            }
        }


        //=============================================================
        // ACTIVE PATH
        //=============================================================

        private void MoveOnActivePath(
            float remainingMoveDistance
        )
        {
            if (
                currentSegment < 0 ||
                currentSegment >=
                pathPoints.Count - 1
            )
            {
                return;
            }

            Vector3 segmentStart =
                pathPoints[
                    currentSegment
                ];

            Vector3 segmentEnd =
                pathPoints[
                    currentSegment + 1
                ];

            float segmentLength =
                Vector3.Distance(
                    segmentStart,
                    segmentEnd
                );

            float t =
                pathProgress /
                Mathf.Max(
                    0.001f,
                    segmentLength
                );

            Vector3 pathPosition =
                Vector3.Lerp(
                    segmentStart,
                    segmentEnd,
                    t
                );

            //=========================================================
            // PATH DIRECTION
            //=========================================================

            Vector3 pathDirection =
                segmentEnd -
                segmentStart;

            pathDirection.y = 0f;

            if (
                pathDirection.sqrMagnitude >
                0.001f
            )
            {
                pathDirection.Normalize();

                lastMoveDir =
                    pathDirection;
            }

            //=========================================================
            // WEAVE
            //=========================================================

            float weaveOffset =
                CalculateWeaveOffset(
                    GetCurrentPathDistance()
                );

            Vector3 sideways =
                Vector3.Cross(
                    Vector3.up,
                    lastMoveDir
                ).normalized;

            Vector3 finalPosition =
        pathPosition +
        sideways *
        weaveOffset;

    //=========================================================
    // VEHICLE HEIGHT
    //=========================================================

    finalPosition.y =
        pathPosition.y +
        vehicleHeightOffset;

    //=========================================================
    // MOVE
    //=========================================================

    Vector3 previousPosition =
        transform.position;

    transform.position =
        finalPosition;

            //=========================================================
            // ROTATION
            //=========================================================

            RotateToMovement(
                previousPosition
            );
        }


        //=============================================================
        // INFINITE WEAVE
        //=============================================================

        private void MoveInfiniteWeave(
            float speed
        )
        {
            Vector3 previousPosition =
                transform.position;

            float weaveOffset =
                CalculateWeaveOffset(
                    totalTravelDistance
                );

            Vector3 sideways =
                Vector3.Cross(
                    Vector3.up,
                    lastMoveDir
                ).normalized;

            Vector3 forwardMovement =
                lastMoveDir *
                speed *
                Time.deltaTime;

            float targetWeave =
                weaveOffset;

            float previousWeave =
                currentWeaveOffset;

            currentWeaveOffset =
                Mathf.Lerp(
                    currentWeaveOffset,
                    targetWeave,
                    weaveSmoothness *
                    Time.deltaTime
                );

            Vector3 lateralMovement =
                sideways *
                (
                    currentWeaveOffset -
                    previousWeave
                );

            transform.position +=
                forwardMovement +
                lateralMovement;

            transform.position =
                new Vector3(
                    transform.position.x,
                    previousPosition.y,
                    transform.position.z
                );

            RotateToMovement(
                previousPosition
            );
        }


        //=============================================================
        // WEAVE OFFSET
        //=============================================================

        private float CalculateWeaveOffset(
            float distance
        )
        {
            if (
                weaveTime <
                weaveStartDelay
            )
            {
                return 0f;
            }

            float wave =
                Mathf.Sin(
                    distance *
                    weaveFrequency +
                    weaveSeed
                );

            float wave2 =
                Mathf.Sin(
                    distance *
                    weaveFrequency *
                    secondaryWeaveFrequency +
                    weaveSeed *
                    0.73f
                );

            float result =
                (
                    wave *
                    0.72f
                ) +
                (
                    wave2 *
                    0.28f
                );

            return result *
                weaveAmplitude;
        }


        //=============================================================
        // PREVIOUS WEAVE OFFSET
        //=============================================================

        private float GetCurrentWeaveOffsetPreviousFrame()
        {
            return currentWeaveOffset;
        }


        //=============================================================
        // ROTATION
        //=============================================================

        private void RotateToMovement(
            Vector3 previousPosition
        )
        {
            Vector3 moveDirection =
                transform.position -
                previousPosition;

            moveDirection.y = 0f;

            if (
                moveDirection.sqrMagnitude >
                0.00001f
            )
            {
                moveDirection.Normalize();

                lastMoveDir =
                    Vector3.Slerp(
                        lastMoveDir,
                        moveDirection,
                        steeringRotationSpeed *
                        Time.deltaTime
                    ).normalized;
            }

            if (
                lastMoveDir.sqrMagnitude <
                0.0001f
            )
            {
                return;
            }

            Quaternion baseRotation =
                Quaternion.LookRotation(
                    lastMoveDir,
                    Vector3.up
                );

            baseRotation *=
                Quaternion.Euler(
                    0f,
                    modelYRotationOffset,
                    0f
                );

            Vector3 localDirection =
                transform.InverseTransformDirection(
                    lastMoveDir
                );

            float targetLean =
                Mathf.Clamp(
                    -localDirection.x *
                    maxLeanAngle,
                    -maxLeanAngle,
                    maxLeanAngle
                );

            currentLeanAngle =
                Mathf.Lerp(
                    currentLeanAngle,
                    targetLean,
                    leanSmoothSpeed *
                    Time.deltaTime
                );

            Quaternion leanRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    currentLeanAngle
                );

            Quaternion targetRotation =
                baseRotation *
                leanRotation;

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed *
                    Time.deltaTime
                );
        }


        //=============================================================
        // CURRENT PATH DISTANCE
        //=============================================================

        private float GetCurrentPathDistance()
        {
            float distance = 0f;

            for (
                int i = 0;
                i < currentSegment;
                i++
            )
            {
                distance +=
                    Vector3.Distance(
                        pathPoints[i],
                        pathPoints[i + 1]
                    );
            }

            distance +=
                pathProgress;

            return distance;
        }


        //=============================================================
        // ENGINE AUDIO
        //=============================================================

        private void SetupEngineAudio()
        {
            if (exciterEngineClip == null)
                return;

            //=========================================================
            // ƯU TIÊN AUDIO SOURCE Ở ROOT
            //=========================================================

            engineAudioSource =
                GetComponent<AudioSource>();

            //=========================================================
            // NẾU KHÔNG CÓ -> TÌM TRONG CHILD
            //=========================================================

            if (engineAudioSource == null)
            {
                engineAudioSource =
                    GetComponentInChildren<AudioSource>(
                        true
                    );
            }

            //=========================================================
            // NẾU VẪN KHÔNG CÓ -> TẠO MỚI
            //=========================================================

            if (engineAudioSource == null)
            {
                engineAudioSource =
                    gameObject.AddComponent<AudioSource>();
            }

            engineAudioSource.clip =
                exciterEngineClip;

            engineAudioSource.volume =
                exciterEngineVolume;

            engineAudioSource.spatialBlend =
                0f;

            engineAudioSource.dopplerLevel =
                0f;

            engineAudioSource.pitch =
                1f;

            engineAudioSource.playOnAwake =
                false;

            engineAudioSource.loop =
                engineLoop;

            engineAudioSource.minDistance =
                engineMinDistance;

            engineAudioSource.maxDistance =
                engineMaxDistance;

            engineAudioSource.Stop();

            if (engineLoop)
            {
                engineAudioSource.Play();
            }
        }


        //=============================================================
        // RESTART ENGINE AUDIO
        //=============================================================

        private void RestartEngineAudio()
        {
            if (exciterEngineClip == null)
                return;

            if (engineAudioSource == null)
            {
                SetupEngineAudio();
                return;
            }

            if (!engineAudioSource.isPlaying)
            {
                engineAudioSource.Play();
            }
        }


        //=============================================================
        // STOP ALL ENGINE AUDIO
        //=============================================================

        private void StopAllEngineAudio()
        {
            //=========================================================
            // STOP ROOT + CHILD AUDIO SOURCE
            //=========================================================

            AudioSource[] audioSources =
                GetComponentsInChildren<AudioSource>(
                    true
                );

            if (
                audioSources != null &&
                audioSources.Length > 0
            )
            {
                for (
                    int i = 0;
                    i < audioSources.Length;
                    i++
                )
                {
                    if (audioSources[i] != null)
                    {
                        audioSources[i].Stop();

                        audioSources[i].enabled =
                            false;
                    }
                }
            }

            //=========================================================
            // SAFETY
            //=========================================================

            if (engineAudioSource != null)
            {
                engineAudioSource.Stop();

                engineAudioSource.enabled =
                    false;
            }
        }


        //=============================================================
        // COLLISION
        //=============================================================

        private void CheckDistanceCollisions()
        {
            if (
                Time.time -
                spawnTime <
                0.2f
            )
            {
                return;
            }

            //=========================================================
            // PLAYER
            //=========================================================

            if (playerTransform != null)
            {
                float distance =
                    Vector3.Distance(
                        transform.position,
                        playerTransform.position
                    );

                if (
                    distance <
                    2.2f
                )
                {
                    TriggerPlayerHit(
                        playerTransform.gameObject
                    );

                    return;
                }
            }

            //=========================================================
            // OBSTACLES
            //=========================================================

            GameObject[] obstacles =
                GameObject.FindGameObjectsWithTag(
                    "Obstacle"
                );

            foreach (
                GameObject obstacle
                in obstacles
            )
            {
                if (
                    obstacle == null ||
                    obstacle == gameObject
                )
                {
                    continue;
                }

                float distance =
                    Vector3.Distance(
                        transform.position,
                        obstacle.transform.position
                    );

                if (
                    distance <
                    2.0f
                )
                {
                    TriggerObstacleExplosion(
                        obstacle
                    );

                    break;
                }
            }
        }


        //=============================================================
        // EXPLOSION
        //=============================================================

        private void SpawnExplosionEffect(
            Vector3 position
        )
        {
            if (
                PlayerController
                    .EnableExplosionStatic
            )
            {
                GameObject prefabToUse =
                    explosionEffectPrefab != null
                        ? explosionEffectPrefab
                        : PlayerController
                            .ExplosionEffectPrefabStatic;

                if (prefabToUse != null)
                {
                    Instantiate(
                        prefabToUse,
                        position,
                        Quaternion.identity
                    );
                }
            }
        }


        //=============================================================
        // OBSTACLE HIT
        //=============================================================

        private void TriggerObstacleExplosion(
            GameObject obstacleObj
        )
        {
            if (isKnockedBack)
                return;

            Vector3 spawnPos =
                obstacleObj != null
                    ? (
                        transform.position +
                        obstacleObj.transform.position
                    ) * 0.5f
                    : transform.position;

            SpawnExplosionEffect(
                spawnPos
            );

            ApplyKnockback(
                new Vector3(
                    0f,
                    22f,
                    -18f
                )
            );
        }



        //=============================================================
        // PLAYER HIT
        //=============================================================

        private void TriggerPlayerHit(
    
    GameObject playerObj
    )
    {
        if (isKnockedBack)
            return;

        if (playerObj == null)
            return;

        //=========================================================
        // FIND PLAYER
        //=========================================================

        PlayerController player =
            playerObj.GetComponentInParent<PlayerController>();

        if (player == null)
            return;

        //=========================================================
        // FIND SHIELD
        //=========================================================

        ShieldController shield =
            player.GetComponent<ShieldController>();

        if (shield == null)
        {
            shield =
                player.GetComponentInChildren<ShieldController>(true);
        }

        if (shield == null)
        {
            shield =
                player.GetComponentInParent<ShieldController>();
        }

        //=========================================================
        // SHIELD ACTIVE
        //
        // QUAN TRỌNG:
        // KHÔNG gọi shield.ConsumeShield() ở đây.
        //
        // PlayerController.ApplyKnockback() đã có hệ thống Shield
        // riêng và sẽ tự ConsumeShield().
        //
        // Nếu Exciter Consume trước -> PlayerController kiểm tra
        // lần 2 -> Shield đã hết -> Player có thể bị chết.
        //=========================================================

        if (
            shield != null &&
            shield.IsActive()
        )
        {
            Vector3 shieldImpactPosition =
                (
                    transform.position +
                    playerObj.transform.position
                ) * 0.5f;

            //=====================================================
            // EXPLOSION
            //=====================================================

            SpawnExplosionEffect(
                shieldImpactPosition
            );

            //=====================================================
            // EXCITER BAY LÊN + BAY NGƯỢC
            //
            // Exciter vẫn chết / bị hất như yêu cầu.
            // Audio cũng sẽ được Stop() trong ApplyKnockback().
            //=====================================================

            ApplyKnockback(
                new Vector3(
                    0f,
                    22f,
                    -16f
                )
            );

            //=====================================================
            // PLAYER
            //
            // KHÔNG tự xử lý Shield ở Exciter.
            //
            // PlayerController sẽ:
            //
            // Shield còn:
            //     ConsumeShield()
            //     return
            //
            // Shield hết:
            //     Player mới bị chết.
            //=====================================================

            player.ApplyKnockback(
                new Vector3(
                    0f,
                    13f,
                    -1.5f
                )
            );

            return;
        }

        //=========================================================
        // NORMAL PLAYER HIT
        //=========================================================

        Vector3 spawnPos =
            (
                transform.position +
                playerObj.transform.position
            ) * 0.5f;

        SpawnExplosionEffect(
            spawnPos
        );

        //=========================================================
        // EXCITER BAY
        //=========================================================

        ApplyKnockback(
            new Vector3(
                0f,
                22f,
                -16f
            )
        );

        //=========================================================
        // PLAYER BỊ HẤT / CHẾT
        //=========================================================

        player.ApplyKnockback(
            new Vector3(
                0f,
                4f,
                6f
            )
        );
    }



        //=============================================================
        // KNOCKBACK
        //=============================================================

        public void ApplyKnockback(
            Vector3 force
        )
        {
            if (isKnockedBack)
                return;

            isKnockedBack = true;

            //=========================================================
            // DỪNG TOÀN BỘ TIẾNG MÁY NGAY LẬP TỨC
            //=========================================================

            StopAllEngineAudio();

            //=========================================================
            // NGẮT MOVEMENT AI
            //=========================================================

            currentExciterSpeed = 0f;

            currentExciterSpeedVelocity = 0f;

            //=========================================================
            // PHYSICS
            //=========================================================

            if (rb != null)
            {
                rb.isKinematic = false;

                rb.useGravity = true;

                // Giảm damping để xe bay xa và cao hơn.
                rb.linearDamping = 0.35f;

                rb.angularDamping = 0.8f;

                //=====================================================
                // FORCE
                //=====================================================

                rb.AddForce(
                    force,
                    ForceMode.Impulse
                );

                //=====================================================
                // ROTATION
                //=====================================================

                rb.AddTorque(
                    new Vector3(
                        22f,
                        8f,
                        32f
                    ),
                    ForceMode.Impulse
                );
            }

            //=========================================================
            // DESTROY SAU KHI BAY
            //=========================================================

            Destroy(
                gameObject,
                3.5f
            );
        }


        //=============================================================
        // ON DESTROY
        //=============================================================

        private void OnDestroy()
        {
            //=========================================================
            // SAFETY:
            // Nếu Exciter bị Destroy bởi script khác,
            // tiếng máy cũng phải dừng.
            //=========================================================

            StopAllEngineAudio();
        }
    }