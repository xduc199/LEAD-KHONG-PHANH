using UnityEngine;

public sealed class PlayerLaneSmoke : MonoBehaviour
{
    // =====================================================================
    // REFERENCES
    // =====================================================================

    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private ParticleSystem leftSmoke;
    [SerializeField] private ParticleSystem rightSmoke;

    // =====================================================================
    // LANE CHANGE DETECTION
    // =====================================================================

    [Header("Lane Change Detection")]
    [SerializeField] private float inputThreshold = 0.05f;
    [SerializeField] private float releaseThreshold = 0.025f;

    // Chỉ dùng để tính cường độ smoke.
    [SerializeField] private float fullSmokeHorizontalSpeed = 12f;

    // =====================================================================
    // SMOKE
    // =====================================================================

    [Header("Smoke")]
    [SerializeField] private float maxEmissionRate = 18f;

    [SerializeField] private float outsideWheelMultiplier = 1.5f;

    // Không dùng để bật/tắt emitter.
    // Chỉ dùng để làm smoke tăng/giảm mượt.
    [SerializeField] private float smokeSmoothTime = 0.08f;

    [SerializeField] private float minimumSmokeAmount = 0.18f;

    // =====================================================================
    // INITIAL PUFF
    // =====================================================================

    [Header("Initial Puff")]
    [SerializeField] private bool enableInitialPuff = true;

    [SerializeField] private int initialPuffCount = 3;

    [SerializeField] private int outsideWheelExtraPuff = 2;

    // =====================================================================
    // INTERNAL STATE
    // =====================================================================

    private ParticleSystem.EmissionModule leftEmission;
    private ParticleSystem.EmissionModule rightEmission;

    private float smokeAmount;
    private float smokeVelocity;

    private bool isChangingLane;

    // -1 = LEFT
    //  0 = NEUTRAL
    // +1 = RIGHT
    private int laneDirection;

    private int previousDirection;

    // =====================================================================
    // UNITY
    // =====================================================================

    private void Awake()
    {
        if (playerController == null)
        {
            playerController =
                GetComponent<PlayerController>();
        }

        CacheEmissionModules();

        StopSmokeImmediate();
    }

    private void OnEnable()
    {
        smokeAmount = 0f;
        smokeVelocity = 0f;

        isChangingLane = false;

        laneDirection = 0;
        previousDirection = 0;

        CacheEmissionModules();

        StopSmokeImmediate();
    }

    private void OnDisable()
    {
        StopSmokeImmediate();
    }

    private void Update()
    {
        if (playerController == null)
        {
            return;
        }

        if (playerController.IsDead)
        {
            StopSmokeImmediate();
            return;
        }

        UpdateSmoke();
    }

    // =====================================================================
    // MAIN SMOKE LOGIC
    // =====================================================================

    private void UpdateSmoke()
    {
        float xInput =
            Input.GetAxisRaw("Horizontal");

        float absoluteInput =
            Mathf.Abs(xInput);

        // =============================================================
        // INPUT STATE
        // =============================================================

        if (!isChangingLane)
        {
            // ---------------------------------------------------------
            // A / D bắt đầu được giữ.
            // ---------------------------------------------------------

            if (absoluteInput > inputThreshold)
            {
                BeginLaneChange(xInput);
            }
        }
        else
        {
            // ---------------------------------------------------------
            // Chỉ kết thúc khi thực sự nhả A/D.
            // ---------------------------------------------------------

            if (absoluteInput < releaseThreshold)
            {
                EndLaneChange();
            }
            else
            {
                UpdateLaneDirection(xInput);
            }
        }

        // =============================================================
        // A / D KHÔNG ĐƯỢC GIỮ
        // =============================================================

        if (!isChangingLane)
        {
            DisableContinuousEmission();
            return;
        }

        // =============================================================
        // ENSURE PARTICLE SYSTEM IS PLAYING
        //
        // QUAN TRỌNG:
        //
        // Khi đang chuyển làn:
        // - Play một lần nếu cần.
        // - Không Stop().
        // - Không Clear().
        //
        // Vì vậy emitter có thể chạy liên tục 5s, 10s,
        // hoặc bao lâu người chơi giữ A/D.
        // =============================================================

        EnsureSmokePlaying();

        // =============================================================
        // CALCULATE SMOKE INTENSITY
        //
        // CurrentHorizontalSpeed chỉ điều khiển mật độ.
        // Nó KHÔNG quyết định smoke có chạy hay không.
        // =============================================================

        float horizontalSpeed =
            Mathf.Max(
                0f,
                playerController.CurrentHorizontalSpeed);

        float speed01 =
            Mathf.InverseLerp(
                0f,
                Mathf.Max(
                    0.01f,
                    fullSmokeHorizontalSpeed),
                horizontalSpeed);

        speed01 =
            Mathf.Clamp01(
                speed01);

        float targetSmokeAmount =
            Mathf.Max(
                speed01,
                minimumSmokeAmount);

        smokeAmount =
            Mathf.SmoothDamp(
                smokeAmount,
                targetSmokeAmount,
                ref smokeVelocity,
                smokeSmoothTime);

        // =============================================================
        // SAFETY FLOOR
        //
        // Đang giữ A/D thì tuyệt đối không cho emission về 0.
        // =============================================================

        if (smokeAmount < minimumSmokeAmount)
        {
            smokeAmount =
                minimumSmokeAmount;
        }

        // =============================================================
        // CONTINUOUS EMISSION
        // =============================================================

        float baseRate =
            maxEmissionRate *
            smokeAmount;

        float leftRate =
            baseRate;

        float rightRate =
            baseRate;

        // =============================================================
        // OUTSIDE WHEEL
        // =============================================================

        if (laneDirection < 0)
        {
            // Đang chuyển LEFT.
            // Bánh RIGHT là bánh ngoài.

            rightRate *=
                outsideWheelMultiplier;
        }
        else if (laneDirection > 0)
        {
            // Đang chuyển RIGHT.
            // Bánh LEFT là bánh ngoài.

            leftRate *=
                outsideWheelMultiplier;
        }

        // =============================================================
        // WRITE EMISSION
        //
        // Đây là dòng quan trọng nhất.
        //
        // Mỗi frame khi A/D còn được giữ:
        //
        // rateOverTime > 0
        //
        // => ParticleSystem tiếp tục sinh particle.
        // =============================================================

        SetEmission(
            leftEmission,
            leftRate);

        SetEmission(
            rightEmission,
            rightRate);
    }

    // =====================================================================
    // BEGIN LANE CHANGE
    // =====================================================================

    private void BeginLaneChange(float xInput)
    {
        isChangingLane = true;

        laneDirection =
            GetDirectionFromInput(
                xInput);

        previousDirection =
            laneDirection;

        smokeAmount =
            Mathf.Max(
                smokeAmount,
                minimumSmokeAmount);

        smokeVelocity = 0f;

        // -------------------------------------------------------------
        // Start particle system FIRST.
        // -------------------------------------------------------------

        EnsureSmokePlaying();

        // -------------------------------------------------------------
        // Initial burst.
        // -------------------------------------------------------------

        if (enableInitialPuff)
        {
            EmitInitialPuff(
                laneDirection);
        }
    }

    // =====================================================================
    // END LANE CHANGE
    // =====================================================================

    private void EndLaneChange()
    {
        isChangingLane = false;

        laneDirection = 0;
        previousDirection = 0;

        // -------------------------------------------------------------
        // QUAN TRỌNG:
        //
        // KHÔNG Stop()
        // KHÔNG Clear()
        //
        // Chỉ ngừng sinh particle mới.
        // Các puff hiện tại vẫn tiếp tục sống và fade.
        // -------------------------------------------------------------

        DisableContinuousEmission();

        smokeVelocity = 0f;
    }

    // =====================================================================
    // DIRECTION UPDATE
    // =====================================================================

    private void UpdateLaneDirection(float xInput)
    {
        int currentDirection =
            GetDirectionFromInput(
                xInput);

        if (currentDirection == 0)
        {
            return;
        }

        // -------------------------------------------------------------
        // A -> D hoặc D -> A trong khi vẫn đang giữ input.
        // -------------------------------------------------------------

        if (currentDirection != previousDirection)
        {
            laneDirection =
                currentDirection;

            previousDirection =
                currentDirection;

            if (enableInitialPuff)
            {
                EmitInitialPuff(
                    currentDirection);
            }

            return;
        }

        laneDirection =
            currentDirection;
    }

    // =====================================================================
    // INITIAL PUFF
    // =====================================================================

    private void EmitInitialPuff(
        int direction)
    {
        int normalCount =
            Mathf.Max(
                0,
                initialPuffCount);

        int outsideExtra =
            Mathf.Max(
                0,
                outsideWheelExtraPuff);

        if (direction > 0)
        {
            // ---------------------------------------------------------
            // RIGHT
            // LEFT = OUTSIDE WHEEL
            // ---------------------------------------------------------

            if (leftSmoke != null)
            {
                leftSmoke.Emit(
                    normalCount +
                    outsideExtra);
            }

            if (rightSmoke != null)
            {
                rightSmoke.Emit(
                    normalCount);
            }
        }
        else if (direction < 0)
        {
            // ---------------------------------------------------------
            // LEFT
            // RIGHT = OUTSIDE WHEEL
            // ---------------------------------------------------------

            if (leftSmoke != null)
            {
                leftSmoke.Emit(
                    normalCount);
            }

            if (rightSmoke != null)
            {
                rightSmoke.Emit(
                    normalCount +
                    outsideExtra);
            }
        }
    }

    // =====================================================================
    // DIRECTION
    // =====================================================================

    private static int GetDirectionFromInput(
        float xInput)
    {
        if (xInput > 0f)
        {
            return 1;
        }

        if (xInput < 0f)
        {
            return -1;
        }

        return 0;
    }

    // =====================================================================
    // CACHE
    // =====================================================================

    private void CacheEmissionModules()
    {
        if (leftSmoke != null)
        {
            leftEmission =
                leftSmoke.emission;
        }

        if (rightSmoke != null)
        {
            rightEmission =
                rightSmoke.emission;
        }
    }

    // =====================================================================
    // PLAY
    // =====================================================================

    private void EnsureSmokePlaying()
    {
        if (leftSmoke != null &&
            !leftSmoke.isPlaying)
        {
            leftSmoke.Play();
        }

        if (rightSmoke != null &&
            !rightSmoke.isPlaying)
        {
            rightSmoke.Play();
        }
    }

    // =====================================================================
    // DISABLE CONTINUOUS EMISSION
    // =====================================================================

    private void DisableContinuousEmission()
    {
        SetEmission(
            leftEmission,
            0f);

        SetEmission(
            rightEmission,
            0f);
    }

    // =====================================================================
    // IMMEDIATE STOP
    //
    // Chỉ dùng khi:
    // - Death
    // - OnEnable reset
    // - OnDisable
    // - StopSmoke()
    // =====================================================================

    private void StopSmokeImmediate()
    {
        if (leftSmoke != null)
        {
            leftSmoke.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (rightSmoke != null)
        {
            rightSmoke.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        SetEmission(
            leftEmission,
            0f);

        SetEmission(
            rightEmission,
            0f);
    }

    // =====================================================================
    // EMISSION
    // =====================================================================

    private static void SetEmission(
        ParticleSystem.EmissionModule emission,
        float rate)
    {
        emission.rateOverTime =
            Mathf.Max(
                0f,
                rate);
    }

    // =====================================================================
    // PUBLIC API
    // =====================================================================

    public void StopSmoke()
    {
        StopSmokeImmediate();

        smokeAmount = 0f;
        smokeVelocity = 0f;

        isChangingLane = false;

        laneDirection = 0;
        previousDirection = 0;
    }
}
