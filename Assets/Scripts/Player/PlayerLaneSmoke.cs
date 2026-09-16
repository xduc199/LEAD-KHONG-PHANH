using UnityEngine;

public class PlayerLaneSmoke : MonoBehaviour
{
    //=========================================================
    // REFERENCES
    //=========================================================

    [Header("References")]
    [SerializeField] private PlayerController playerController;

    [SerializeField] private ParticleSystem leftSmoke;

    [SerializeField] private ParticleSystem rightSmoke;


    //=========================================================
    // DETECTION
    //=========================================================

    [Header("Lane Change Detection")]

    [Tooltip("Input nhỏ hơn giá trị này sẽ không tạo khói.")]
    [SerializeField] private float inputThreshold = 0.05f;

    [Tooltip("Tốc độ ngang dùng để tính cường độ khói tối đa.")]
    [SerializeField] private float fullSmokeHorizontalSpeed = 12f;


    //=========================================================
    // SMOKE
    //=========================================================

    [Header("Smoke")]

    [Tooltip("Emission tối đa của mỗi bánh.")]
    [SerializeField] private float maxEmissionRate = 18f;

    [Tooltip("Bánh phía ngoài khi drift sẽ có nhiều khói hơn.")]
    [SerializeField] private float outsideWheelMultiplier = 1.5f;

    [Tooltip("Độ mượt khi khói tăng/giảm.")]
    [SerializeField] private float smokeSmoothTime = 0.08f;


    //=========================================================
    // INTERNAL
    //=========================================================

    private ParticleSystem.EmissionModule leftEmission;

    private ParticleSystem.EmissionModule rightEmission;

    private float smokeAmount;

    private float smokeVelocity;


    //=========================================================
    // AWAKE
    //=========================================================

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


    //=========================================================
    // ON ENABLE
    //=========================================================

    private void OnEnable()
    {
        smokeAmount = 0f;
        smokeVelocity = 0f;

        CacheEmissionModules();

        StopSmokeImmediate();
    }


    //=========================================================
    // UPDATE
    //=========================================================

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


    //=========================================================
    // UPDATE SMOKE
    //=========================================================

    private void UpdateSmoke()
    {
        //=====================================================
        // ĐỌC CHÍNH INPUT MÀ PLAYERCONTROLLER ĐANG DÙNG
        //=====================================================

        float xInput =
            Input.GetAxisRaw(
                "Horizontal"
            );

        bool isChangingLane =
            Mathf.Abs(xInput) >
            inputThreshold;


        //=====================================================
        // TARGET SMOKE
        //=====================================================

        float targetSmokeAmount = 0f;

        if (isChangingLane)
        {
            float horizontalSpeed =
                Mathf.Max(
                    0f,
                    playerController.CurrentHorizontalSpeed
                );

            targetSmokeAmount =
                Mathf.InverseLerp(
                    0f,
                    Mathf.Max(
                        0.01f,
                        fullSmokeHorizontalSpeed
                    ),
                    horizontalSpeed
                );

            // Input luôn được tính là drift
            // nếu đang bấm trái/phải.
            targetSmokeAmount =
                Mathf.Clamp01(
                    targetSmokeAmount
                );

            // Không cho trường hợp tốc độ quá thấp
            // khiến khói gần như bằng 0.
            targetSmokeAmount =
                Mathf.Max(
                    targetSmokeAmount,
                    0.35f
                );
        }


        //=====================================================
        // SMOOTH
        //=====================================================

        smokeAmount =
            Mathf.SmoothDamp(
                smokeAmount,
                targetSmokeAmount,
                ref smokeVelocity,
                smokeSmoothTime
            );


        //=====================================================
        // STOP
        //=====================================================

        if (smokeAmount <= 0.01f)
        {
            SetEmission(
                leftEmission,
                0f
            );

            SetEmission(
                rightEmission,
                0f
            );

            StopSmokeParticles();

            return;
        }


        //=====================================================
        // PLAY
        //=====================================================

        EnsureSmokePlaying();


        //=====================================================
        // BASE EMISSION
        //=====================================================

        float baseRate =
            maxEmissionRate *
            smokeAmount;


        float leftRate =
            baseRate;

        float rightRate =
            baseRate;


        //=====================================================
        // NGOÀI CUA / NGOÀI HƯỚNG CHUYỂN LÀN
        //
        // Bấm trái:
        //   bánh phải = ngoài -> nhiều khói
        //
        // Bấm phải:
        //   bánh trái = ngoài -> nhiều khói
        //=====================================================

        if (xInput < 0f)
        {
            rightRate *=
                outsideWheelMultiplier;
        }
        else if (xInput > 0f)
        {
            leftRate *=
                outsideWheelMultiplier;
        }


        //=====================================================
        // APPLY
        //=====================================================

        SetEmission(
            leftEmission,
            leftRate
        );

        SetEmission(
            rightEmission,
            rightRate
        );
    }


    //=========================================================
    // CACHE
    //=========================================================

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


    //=========================================================
    // PLAY
    //=========================================================

    private void EnsureSmokePlaying()
    {
        if (
            leftSmoke != null &&
            !leftSmoke.isPlaying
        )
        {
            leftSmoke.Play();
        }

        if (
            rightSmoke != null &&
            !rightSmoke.isPlaying
        )
        {
            rightSmoke.Play();
        }
    }


    //=========================================================
    // STOP PARTICLES
    //=========================================================

    private void StopSmokeParticles()
    {
        if (leftSmoke != null)
        {
            leftSmoke.Stop(
                true,
                ParticleSystemStopBehavior.StopEmitting
            );
        }

        if (rightSmoke != null)
        {
            rightSmoke.Stop(
                true,
                ParticleSystemStopBehavior.StopEmitting
            );
        }
    }


    //=========================================================
    // STOP IMMEDIATE
    //=========================================================

    private void StopSmokeImmediate()
    {
        if (leftSmoke != null)
        {
            leftSmoke.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }

        if (rightSmoke != null)
        {
            rightSmoke.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }

        SetEmission(
            leftEmission,
            0f
        );

        SetEmission(
            rightEmission,
            0f
        );
    }


    //=========================================================
    // SET EMISSION
    //=========================================================

    private void SetEmission(
        ParticleSystem.EmissionModule emission,
        float rate
    )
    {
        emission.rateOverTime =
            Mathf.Max(
                0f,
                rate
            );
    }


    //=========================================================
    // PUBLIC API
    //=========================================================

    public void StopSmoke()
    {
        StopSmokeImmediate();
    }
}