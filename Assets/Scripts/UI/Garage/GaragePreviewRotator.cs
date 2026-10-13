using UnityEngine;

[DisallowMultipleComponent]
public sealed class GaragePreviewRotator : MonoBehaviour
{
    // =========================================================
    // ROTATION
    // =========================================================

    [Header("Rotation")]
    [SerializeField, Min(0f)]
    private float rotationSpeed = 180f;

    [SerializeField]
    private bool invertRotation = false;

    // =========================================================
    // AUTO ROTATION
    // =========================================================

    [Header("Auto Rotation")]
    [SerializeField]
    private bool autoRotate = true;

    [SerializeField, Min(0f)]
    private float autoRotationSpeed = 25f;

    [SerializeField]
    private bool autoRotateOnlyWhenNotDragging = true;

    // =========================================================
    // INPUT
    // =========================================================

    [Header("Input")]
    [SerializeField]
    private bool rotateWithMouse = true;

    // =========================================================
    // RUNTIME
    // =========================================================

    private bool isDragging;
    private float lastMouseX;

    // =========================================================
    // UNITY
    // =========================================================

    private void Update()
    {
        HandleMouseInput();

        if (autoRotate && (!autoRotateOnlyWhenNotDragging || !isDragging))
        {
            HandleAutoRotation();
        }
    }

    // =========================================================
    // MOUSE ROTATION
    // =========================================================

    private void HandleMouseInput()
    {
        if (!rotateWithMouse)
        {
            isDragging = false;
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            lastMouseX = Input.mousePosition.x;
            return;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            return;
        }

        if (!isDragging)
            return;

        float currentMouseX = Input.mousePosition.x;
        float deltaX = currentMouseX - lastMouseX;

        lastMouseX = currentMouseX;

        if (Mathf.Abs(deltaX) <= 0.001f)
            return;

        float direction = invertRotation ? -1f : 1f;

        float rotationDelta =
            -deltaX *
            rotationSpeed *
            direction *
            Time.unscaledDeltaTime;

        transform.Rotate(
            0f,
            rotationDelta,
            0f,
            Space.World
        );
    }

    // =========================================================
    // AUTO ROTATION
    // =========================================================

    private void HandleAutoRotation()
    {
        float direction = invertRotation ? -1f : 1f;

        float rotationDelta =
            autoRotationSpeed *
            direction *
            Time.unscaledDeltaTime;

        transform.Rotate(
            0f,
            rotationDelta,
            0f,
            Space.World
        );
    }

    // =========================================================
    // PUBLIC API
    // =========================================================

    public void SetRotationEnabled(bool enabled)
    {
        rotateWithMouse = enabled;
        isDragging = false;
    }

    public void SetAutoRotationEnabled(bool enabled)
    {
        autoRotate = enabled;
    }

    public void SetAutoRotationSpeed(float speed)
    {
        autoRotationSpeed = Mathf.Max(0f, speed);
    }

    public void SetMouseRotationEnabled(bool enabled)
    {
        rotateWithMouse = enabled;
        isDragging = false;
    }

    public bool IsAutoRotationEnabled()
    {
        return autoRotate;
    }

    public bool IsMouseRotationEnabled()
    {
        return rotateWithMouse;
    }

    public void ResetRotation()
    {
        transform.localRotation = Quaternion.identity;
        isDragging = false;
    }
}
