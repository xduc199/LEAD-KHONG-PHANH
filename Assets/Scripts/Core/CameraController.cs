using UnityEngine;

public class CameraController : MonoBehaviour
{
    public enum CameraViewMode
    {
        ThirdPerson,
        FirstPerson
    }

    // =========================================================
    // CAMERA VIEW
    // =========================================================

    [Header("CAMERA VIEW")]
    [SerializeField]
    private CameraViewMode currentViewMode =
        CameraViewMode.ThirdPerson;

    [SerializeField]
    private Transform target;

    [SerializeField]
    private Camera targetCamera;


    // =========================================================
    // THIRD PERSON POSITION
    // =========================================================

    [Header("THIRD PERSON - LOCKED POSITION")]

    [SerializeField]
    private float thirdPersonX = 0f;

    [SerializeField]
    private float thirdPersonY = 3.8f;

    [SerializeField]
    private float thirdPersonZ = -6.5f;


    // =========================================================
    // THIRD PERSON ANGLE
    // =========================================================

    [Header("THIRD PERSON - CAMERA ANGLE")]

    [Tooltip("Camera nhìn lên/xuống. Âm = nhìn xuống, dương = nhìn lên.")]
    [SerializeField]
    private float thirdPersonPitch = 8f;

    [Tooltip("Camera xoay trái/phải.")]
    [SerializeField]
    private float thirdPersonYaw = 0f;

    [Tooltip("Camera nghiêng trái/phải.")]
    [SerializeField]
    private float thirdPersonRoll = 0f;

    [SerializeField]
    private float thirdPersonLookHeight = 1.2f;

    [SerializeField]
    private float thirdPersonRotationSmooth = 12f;


    // =========================================================
    // FIRST PERSON POSITION
    // =========================================================

    [Header("FIRST PERSON - LOCKED POSITION")]

    [SerializeField]
    private float firstPersonX = 0f;

    [SerializeField]
    private float firstPersonY = 1.35f;

    [SerializeField]
    private float firstPersonZ = 0.45f;


    // =========================================================
    // FIRST PERSON ANGLE
    // =========================================================

    [Header("FIRST PERSON - CAMERA ANGLE")]

    [Tooltip("Camera nhìn lên/xuống.")]
    [SerializeField]
    private float firstPersonPitch = 0f;

    [Tooltip("Camera xoay trái/phải.")]
    [SerializeField]
    private float firstPersonYaw = 0f;

    [Tooltip("Camera nghiêng trái/phải.")]
    [SerializeField]
    private float firstPersonRoll = 0f;

    [SerializeField]
    private float firstPersonLookHeight = 1.35f;

    [SerializeField]
    private float firstPersonLookDistance = 20f;

    [SerializeField]
    private float firstPersonRotationSmooth = 18f;


    // =========================================================
    // ROTATION
    // =========================================================

    [Header("ROTATION")]

    [SerializeField]
    private bool smoothRotation = true;


    // =========================================================
    // PHOTON
    // =========================================================

    [Header("PHOTON")]

    [SerializeField]
    private PhotonController photonController;


    // =========================================================
    // FOV
    // =========================================================

    [Header("FOV")]

    [SerializeField]
    private float normalFOV = 60f;

    [SerializeField]
    private float firstPersonFOV = 72f;

    [SerializeField]
    private float photonFOV = 76f;

    [SerializeField]
    private float fovSmoothTime = 0.16f;

    private float fovVelocity;


    // =========================================================
    // PHOTON ROLL EFFECT
    // =========================================================

    [Header("PHOTON ROLL EFFECT")]

    [SerializeField]
    private float photonRollAngle = 0.8f;

    [SerializeField]
    private float photonRollSmoothTime = 0.18f;

    [SerializeField]
    private float photonRollFrequency = 2.2f;

    private float currentPhotonRoll;

    private float photonRollVelocity;


    // =========================================================
    // PHOTON MICRO SHAKE
    // =========================================================

    [Header("PHOTON MICRO SHAKE")]

    [SerializeField]
    private float microShakeAmount = 0.008f;

    [SerializeField]
    private float microShakeFrequency = 10f;

    [SerializeField]
    private float microShakeSmoothTime = 0.15f;

    private Vector3 currentMicroShake;

    private Vector3 microShakeVelocity;


    // =========================================================
    // INTERNAL
    // =========================================================

    private Vector3 baseCameraPosition;

    private Quaternion baseCameraRotation;

    private Vector3 finalCameraPosition;

    private Quaternion finalCameraRotation;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera =
                GetComponent<Camera>();
        }
    }


    private void Start()
    {
        FindTarget();

        FindPhotonController();

        if (targetCamera == null)
        {
            targetCamera =
                GetComponent<Camera>();
        }

        if (targetCamera != null)
        {
            targetCamera.fieldOfView =
                normalFOV;
        }

        if (target != null)
        {
            CalculateBaseCameraPosition();

            CalculateBaseCameraRotation();

            transform.position =
                baseCameraPosition;

            transform.rotation =
                baseCameraRotation;
        }
    }


    private void LateUpdate()
    {
        if (target == null)
        {
            FindTarget();

            if (target == null)
                return;
        }

        FindPhotonController();


        // =====================================================
        // 1. BASE POSITION
        // =====================================================

        CalculateBaseCameraPosition();


        // =====================================================
        // 2. BASE ROTATION + USER ANGLE
        // =====================================================

        CalculateBaseCameraRotation();


        // =====================================================
        // 3. PHOTON EFFECT
        // =====================================================

        UpdatePhotonEffects();


        // =====================================================
        // 4. FOV
        // =====================================================

        UpdateFOV();


        // =====================================================
        // 5. APPLY
        // =====================================================

        ApplyCamera();
    }


    // =========================================================
    // FIND TARGET
    // =========================================================

    private void FindTarget()
    {
        GameObject player =
            GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            target =
                player.transform;
        }
    }


    // =========================================================
    // FIND PHOTON
    // =========================================================

    private void FindPhotonController()
    {
        if (photonController != null)
            return;

        if (target != null)
        {
            photonController =
                target.GetComponent<PhotonController>();
        }

        if (photonController == null)
        {
            photonController =
                FindFirstObjectByType<PhotonController>();
        }
    }


    // =========================================================
    // BASE POSITION
    // =========================================================

    private void CalculateBaseCameraPosition()
    {
        if (target == null)
            return;


        if (currentViewMode ==
            CameraViewMode.ThirdPerson)
        {
            baseCameraPosition =
                target.position
                + target.right *
                thirdPersonX
                + Vector3.up *
                thirdPersonY
                + target.forward *
                thirdPersonZ;

            return;
        }


        // =====================================================
        // FIRST PERSON
        // =====================================================

        baseCameraPosition =
            target.position
            + target.right *
            firstPersonX
            + Vector3.up *
            firstPersonY
            + target.forward *
            firstPersonZ;
    }


    // =========================================================
    // BASE ROTATION
    // =========================================================

    private void CalculateBaseCameraRotation()
    {
        if (target == null)
            return;


        // =====================================================
        // THIRD PERSON
        // =====================================================

        if (currentViewMode ==
            CameraViewMode.ThirdPerson)
        {
            Vector3 forward =
                target.forward;

            // Không lấy pitch/roll của xe.
            forward.y = 0f;

            if (forward.sqrMagnitude <
                0.0001f)
            {
                forward =
                    Vector3.forward;
            }

            forward.Normalize();


            // =================================================
            // USER YAW
            // =================================================

            Quaternion yawRotation =
                Quaternion.AngleAxis(
                    thirdPersonYaw,
                    Vector3.up
                );


            forward =
                yawRotation *
                forward;


            // =================================================
            // LOOK TARGET
            // =================================================

            Vector3 lookTarget =
                target.position
                + Vector3.up *
                thirdPersonLookHeight;


            Vector3 direction =
                lookTarget -
                baseCameraPosition;


            if (direction.sqrMagnitude <
                0.0001f)
            {
                direction =
                    forward;
            }


            Quaternion lookRotation =
                Quaternion.LookRotation(
                    direction.normalized,
                    Vector3.up
                );


            // =================================================
            // PITCH
            // =================================================

            lookRotation =
                lookRotation *
                Quaternion.Euler(
                    thirdPersonPitch,
                    0f,
                    thirdPersonRoll
                );


            ApplyRotationSmooth(
                lookRotation,
                thirdPersonRotationSmooth
            );

            return;
        }


        // =====================================================
        // FIRST PERSON
        // =====================================================

        Vector3 firstForward =
            target.forward;

        firstForward.y = 0f;

        if (firstForward.sqrMagnitude <
            0.0001f)
        {
            firstForward =
                Vector3.forward;
        }

        firstForward.Normalize();


        // =====================================================
        // YAW
        // =====================================================

        Quaternion firstYawRotation =
            Quaternion.AngleAxis(
                firstPersonYaw,
                Vector3.up
            );


        firstForward =
            firstYawRotation *
            firstForward;


        // =====================================================
        // LOOK TARGET
        // =====================================================

        Vector3 firstLookTarget =
            baseCameraPosition
            + firstForward *
            firstPersonLookDistance;


        firstLookTarget.y =
            target.position.y
            + firstPersonLookHeight;


        Vector3 firstDirection =
            firstLookTarget -
            baseCameraPosition;


        if (firstDirection.sqrMagnitude <
            0.0001f)
        {
            firstDirection =
                firstForward;
        }


        Quaternion firstRotation =
            Quaternion.LookRotation(
                firstDirection.normalized,
                Vector3.up
            );


        // =====================================================
        // PITCH + ROLL
        // =====================================================

        firstRotation =
            firstRotation *
            Quaternion.Euler(
                firstPersonPitch,
                0f,
                firstPersonRoll
            );


        ApplyRotationSmooth(
            firstRotation,
            firstPersonRotationSmooth
        );
    }


    // =========================================================
    // ROTATION SMOOTH
    // =========================================================

    private void ApplyRotationSmooth(
        Quaternion wantedRotation,
        float smooth
    )
    {
        if (!smoothRotation)
        {
            baseCameraRotation =
                wantedRotation;

            return;
        }


        float t =
            1f -
            Mathf.Exp(
                -smooth *
                Time.deltaTime
            );


        baseCameraRotation =
            Quaternion.Slerp(
                transform.rotation,
                wantedRotation,
                t
            );
    }


    // =========================================================
    // PHOTON EFFECTS
    // =========================================================

    private void UpdatePhotonEffects()
    {
        bool photonActive =
            IsPhotonActive();


        // =====================================================
        // PHOTON ROLL
        // =====================================================

        float wantedRoll = 0f;


        if (photonActive)
        {
            wantedRoll =
                Mathf.Sin(
                    Time.time *
                    photonRollFrequency
                )
                *
                photonRollAngle;
        }


        currentPhotonRoll =
            Mathf.SmoothDamp(
                currentPhotonRoll,
                wantedRoll,
                ref photonRollVelocity,
                photonRollSmoothTime
            );


        // =====================================================
        // MICRO SHAKE
        // =====================================================

        Vector3 wantedShake =
            Vector3.zero;


        if (photonActive)
        {
            float time =
                Time.time *
                microShakeFrequency;


            wantedShake =
                new Vector3(
                    Mathf.Sin(
                        time * 1.13f
                    ),
                    Mathf.Cos(
                        time * 1.37f
                    ),
                    0f
                )
                *
                microShakeAmount;
        }


        currentMicroShake =
            Vector3.SmoothDamp(
                currentMicroShake,
                wantedShake,
                ref microShakeVelocity,
                microShakeSmoothTime
            );
    }


    // =========================================================
    // FOV
    // =========================================================

    private void UpdateFOV()
    {
        if (targetCamera == null)
            return;


        float wantedFOV;


        if (IsPhotonActive())
        {
            wantedFOV =
                photonFOV;
        }
        else if (
            currentViewMode ==
            CameraViewMode.FirstPerson)
        {
            wantedFOV =
                firstPersonFOV;
        }
        else
        {
            wantedFOV =
                normalFOV;
        }


        targetCamera.fieldOfView =
            Mathf.SmoothDamp(
                targetCamera.fieldOfView,
                wantedFOV,
                ref fovVelocity,
                fovSmoothTime
            );
    }


    // =========================================================
    // APPLY CAMERA
    // =========================================================

    private void ApplyCamera()
    {
        // =====================================================
        // POSITION
        // =====================================================

        finalCameraPosition =
            baseCameraPosition;


        // =====================================================
        // PHOTON MICRO SHAKE
        // =====================================================

        if (IsPhotonActive())
        {
            finalCameraPosition +=
                baseCameraRotation *
                new Vector3(
                    currentMicroShake.x,
                    currentMicroShake.y,
                    0f
                );
        }


        transform.position =
            finalCameraPosition;


        // =====================================================
        // ROTATION
        // =====================================================

        finalCameraRotation =
            baseCameraRotation;


        // Photon chỉ thêm roll.
        finalCameraRotation *=
            Quaternion.Euler(
                0f,
                0f,
                currentPhotonRoll
            );


        transform.rotation =
            finalCameraRotation;
    }


    // =========================================================
    // PHOTON STATE
    // =========================================================

    private bool IsPhotonActive()
    {
        if (photonController == null)
            return false;

        return photonController.IsPhotonActive;
    }


    // =========================================================
    // CAMERA SWITCH
    // =========================================================

    public void ToggleCameraView()
    {
        if (currentViewMode ==
            CameraViewMode.ThirdPerson)
        {
            SetCameraView(
                CameraViewMode.FirstPerson
            );
        }
        else
        {
            SetCameraView(
                CameraViewMode.ThirdPerson
            );
        }
    }


    public void SetCameraView(
        CameraViewMode newViewMode
    )
    {
        if (currentViewMode ==
            newViewMode)
        {
            return;
        }


        currentViewMode =
            newViewMode;


        if (target != null)
        {
            CalculateBaseCameraPosition();

            CalculateBaseCameraRotation();


            transform.position =
                baseCameraPosition;


            transform.rotation =
                baseCameraRotation;
        }
    }


    // =========================================================
    // PUBLIC METHODS
    // =========================================================

    public void SetThirdPerson()
    {
        SetCameraView(
            CameraViewMode.ThirdPerson
        );
    }


    public void SetFirstPerson()
    {
        SetCameraView(
            CameraViewMode.FirstPerson
        );
    }


    public bool IsThirdPerson()
    {
        return currentViewMode ==
            CameraViewMode.ThirdPerson;
    }


    public bool IsFirstPerson()
    {
        return currentViewMode ==
            CameraViewMode.FirstPerson;
    }


    public CameraViewMode
        GetCurrentCameraView()
    {
        return currentViewMode;
    }


    // =========================================================
    // Z CONTROL
    // =========================================================

    public float GetCurrentCameraZ()
    {
        if (currentViewMode ==
            CameraViewMode.ThirdPerson)
        {
            return thirdPersonZ;
        }

        return firstPersonZ;
    }


    public void SetThirdPersonZ(
        float z
    )
    {
        thirdPersonZ = z;
    }


    public void SetFirstPersonZ(
        float z
    )
    {
        firstPersonZ = z;
    }


    // =========================================================
    // DEBUG GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (target == null)
            return;


        Vector3 point;


        if (currentViewMode ==
            CameraViewMode.ThirdPerson)
        {
            point =
                target.position
                + target.right *
                thirdPersonX
                + Vector3.up *
                thirdPersonY
                + target.forward *
                thirdPersonZ;
        }
        else
        {
            point =
                target.position
                + target.right *
                firstPersonX
                + Vector3.up *
                firstPersonY
                + target.forward *
                firstPersonZ;
        }


        Gizmos.DrawWireSphere(
            point,
            0.15f
        );


        Gizmos.DrawLine(
            target.position,
            point
        );
    }


    // =========================================================
    // VALIDATION
    // =========================================================

    private void OnValidate()
    {
        // POSITION

        thirdPersonY =
            Mathf.Max(
                0f,
                thirdPersonY
            );


        firstPersonY =
            Mathf.Max(
                0f,
                firstPersonY
            );


        thirdPersonZ =
            Mathf.Min(
                thirdPersonZ,
                -0.5f
            );


        firstPersonZ =
            Mathf.Clamp(
                firstPersonZ,
                -1f,
                2f
            );


        // ANGLE

        thirdPersonPitch =
            Mathf.Clamp(
                thirdPersonPitch,
                -80f,
                80f
            );


        thirdPersonYaw =
            Mathf.Clamp(
                thirdPersonYaw,
                -180f,
                180f
            );


        thirdPersonRoll =
            Mathf.Clamp(
                thirdPersonRoll,
                -45f,
                45f
            );


        firstPersonPitch =
            Mathf.Clamp(
                firstPersonPitch,
                -80f,
                80f
            );


        firstPersonYaw =
            Mathf.Clamp(
                firstPersonYaw,
                -180f,
                180f
            );


        firstPersonRoll =
            Mathf.Clamp(
                firstPersonRoll,
                -45f,
                45f
            );


        // FOV

        normalFOV =
            Mathf.Clamp(
                normalFOV,
                30f,
                120f
            );


        firstPersonFOV =
            Mathf.Clamp(
                firstPersonFOV,
                30f,
                120f
            );


        photonFOV =
            Mathf.Clamp(
                photonFOV,
                30f,
                120f
            );


        // EFFECTS

        microShakeAmount =
            Mathf.Max(
                0f,
                microShakeAmount
            );


        photonRollAngle =
            Mathf.Max(
                0f,
                photonRollAngle
            );
    }
}