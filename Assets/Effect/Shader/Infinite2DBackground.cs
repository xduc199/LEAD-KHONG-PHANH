using UnityEngine;

namespace LeadKhongPhanh.Environment
{
    [DisallowMultipleComponent]
    public sealed class Infinite2DBackground : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera targetCamera;

        [Header("Background")]
        [SerializeField, Min(1f)] private float distanceFromCamera = 500f;
        [SerializeField, Min(1f)] private float extraCoverage = 1.05f;

        [Header("Follow")]
        [SerializeField] private bool followPosition = true;
        [SerializeField] private bool followRotation = true;

        private Transform _cameraTransform;
        private float _aspect;
        private float _halfVerticalFovRadians;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null)
            {
                enabled = false;
                Debug.LogError(
                    "[Infinite2DBackground] Không tìm thấy Camera. " +
                    "Hãy gán Target Camera trong Inspector.",
                    this
                );
                return;
            }

            _cameraTransform = targetCamera.transform;

            CacheCameraData();
            UpdateBackgroundTransform();
        }

        private void LateUpdate()
        {
            if (_cameraTransform == null)
            {
                return;
            }

            CacheCameraData();
            UpdateBackgroundTransform();
        }

        private void CacheCameraData()
        {
            _aspect = Mathf.Max(0.01f, targetCamera.aspect);

            _halfVerticalFovRadians =
                targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
        }

        private void UpdateBackgroundTransform()
        {
            if (followPosition)
            {
                transform.position =
                    _cameraTransform.position +
                    _cameraTransform.forward * distanceFromCamera;
            }

            if (followRotation)
            {
                // Quad phải quay mặt về phía Camera.
                transform.rotation = Quaternion.LookRotation(
                    -_cameraTransform.forward,
                    _cameraTransform.up
                );
            }

            float height =
                2f *
                distanceFromCamera *
                Mathf.Tan(_halfVerticalFovRadians);

            float width = height * _aspect;

            width *= extraCoverage;
            height *= extraCoverage;

            transform.localScale = new Vector3(
                width,
                height,
                1f
            );
        }
    }
}