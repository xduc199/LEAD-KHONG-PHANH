using UnityEngine;

public class PhotonCameraEffect : MonoBehaviour
{
    [SerializeField] private PhotonController photonController;

    [Header("FOV")]
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float photonFOV = 72f;

    [SerializeField] private float transitionSpeed = 5f;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();

        if (photonController == null)
        {
            photonController =
                FindFirstObjectByType<PhotonController>();
        }

        if (cam != null)
        {
            normalFOV = cam.fieldOfView;
        }
    }

    private void Update()
    {
        if (cam == null ||
            photonController == null)
        {
            return;
        }

        float targetFOV =
            photonController.IsPhotonActive
                ? photonFOV
                : normalFOV;

        cam.fieldOfView =
            Mathf.Lerp(
                cam.fieldOfView,
                targetFOV,
                transitionSpeed *
                Time.deltaTime
            );
    }
}