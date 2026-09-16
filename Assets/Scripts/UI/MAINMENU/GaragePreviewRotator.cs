using UnityEngine;

public sealed class GaragePreviewRotator : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField, Min(0f)]
    private float rotationSpeed = 180f;

    [SerializeField]
    private bool invertRotation = false;

    [Header("Input")]
    [SerializeField]
    private bool rotateWithMouse = true;

    private bool isDragging;
    private float lastMouseX;

    private void Update()
    {
        if (!rotateWithMouse)
            return;

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

        transform.Rotate(
            0f,
            -deltaX * rotationSpeed * direction * Time.unscaledDeltaTime,
            0f,
            Space.World
        );
    }

    public void SetRotationEnabled(bool enabled)
    {
        rotateWithMouse = enabled;
        isDragging = false;
    }

    public void ResetRotation()
    {
        transform.localRotation = Quaternion.identity;
        isDragging = false;
    }
}