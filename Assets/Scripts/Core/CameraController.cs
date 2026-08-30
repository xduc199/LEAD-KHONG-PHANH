using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;

    [SerializeField]
    private Vector3 offset = new Vector3(0f, 3.5f, -6f);

    [SerializeField]
    private float smoothSpeed = 10f;

    [Header("Photon FOV Effect")]
    [SerializeField] private PhotonController photonController;

    [SerializeField] private Camera targetCamera;

    [SerializeField] private float photonFOV = 64f;

    [SerializeField] private float fovSmoothSpeed = 5f;

    private float normalFOV;

    private void Start()
    {
        // Giữ nguyên cách tìm Player
        if (target == null)
        {
            GameObject playerObj =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
            {
                target = playerObj.transform;
            }
        }

        // Lấy đúng Camera hiện tại
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        // Lấy PhotonController từ Player
        if (photonController == null && target != null)
        {
            photonController =
                target.GetComponent<PhotonController>();
        }

        // QUAN TRỌNG:
        // Lưu FOV hiện tại của Camera, không tự ép thành 60.
        if (targetCamera != null)
        {
            normalFOV = targetCamera.fieldOfView;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        //=====================================================
        // CAMERA FOLLOW - GIỮ NGUYÊN CODE CŨ
        //=====================================================

        Vector3 targetPosition =
            target.position + offset;

        targetPosition.x =
            Mathf.Lerp(
                transform.position.x,
                target.position.x * 0.3f,
                Time.deltaTime * smoothSpeed
            );

        transform.position =
            Vector3.Lerp(
                transform.position,
                targetPosition,
                Time.deltaTime * smoothSpeed
            );

        //=====================================================
        // PHOTON FOV
        //=====================================================

        UpdatePhotonFOV();
    }

    private void UpdatePhotonFOV()
    {
        if (targetCamera == null)
            return;

        bool photonActive =
            photonController != null &&
            photonController.IsPhotonActive;

        float desiredFOV =
            photonActive
                ? photonFOV
                : normalFOV;

        targetCamera.fieldOfView =
            Mathf.Lerp(
                targetCamera.fieldOfView,
                desiredFOV,
                Time.deltaTime * fovSmoothSpeed
            );
    }
}