using UnityEngine;

[DisallowMultipleComponent]
public sealed class FarBackgroundParallax : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform targetCamera;

    [Header("Distance")]
    [SerializeField, Min(1f)]
    private float distance = 180f;

    [Header("Height")]
    [SerializeField]
    private float height = 20f;

    [Header("Parallax")]
    [SerializeField, Range(0f, 1f)]
    private float horizontalParallax = 0.08f;

    [Header("Camera Rotation")]
    [SerializeField]
    private bool followCameraYaw = true;

    private Vector3 initialCameraPosition;
    private Vector3 initialBackgroundPosition;

    private void Awake()
    {
        if (targetCamera == null)
        {
            Camera mainCamera = Camera.main;

            if (mainCamera != null)
            {
                targetCamera = mainCamera.transform;
            }
        }

        if (targetCamera == null)
        {
            enabled = false;
            return;
        }

        initialCameraPosition = targetCamera.position;

        initialBackgroundPosition = new Vector3(
            targetCamera.position.x,
            height,
            targetCamera.position.z + distance
        );

        transform.position = initialBackgroundPosition;
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            return;
        }

        Vector3 cameraPosition = targetCamera.position;

        float cameraDeltaX =
            cameraPosition.x - initialCameraPosition.x;

        float backgroundX =
            initialBackgroundPosition.x +
            cameraDeltaX * horizontalParallax;

        float backgroundZ =
            cameraPosition.z + distance;

        transform.position = new Vector3(
            backgroundX,
            height,
            backgroundZ
        );

        if (followCameraYaw)
        {
            Vector3 cameraEuler = targetCamera.eulerAngles;

            transform.rotation = Quaternion.Euler(
                0f,
                cameraEuler.y,
                0f
            );
        }
    }
}