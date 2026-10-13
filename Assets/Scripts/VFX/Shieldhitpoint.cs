// =============================================================================
// LEAD KHÔNG PHANH - Hexagon Energy Shield
// ShieldHitPoint.cs
// Một "slot" gợn sóng va chạm. ShieldHexVFX luân phiên dùng các slot này để
// nhiều va chạm gần nhau vẫn hiển thị được, không ghi đè lẫn nhau.
// =============================================================================

using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShieldHitPoint : MonoBehaviour
{
    private ShieldHexDome dome;
    private Color hitColor = Color.white;
    private float duration = 0.6f;

    public float StartTime { get; private set; } = float.MinValue;
    public bool IsBusy { get; private set; }

    private Vector3 localPoint;

    private void Awake()
    {
        dome = GetComponentInParent<ShieldHexDome>();
        if (dome == null)
            dome = transform.parent != null ? transform.parent.GetComponentInChildren<ShieldHexDome>(true) : null;
    }

    public void Trigger(Vector3 localPointNormalized, Color color, float rippleDuration, float startTime)
    {
        localPoint = localPointNormalized;
        hitColor = color;
        duration = Mathf.Max(0.05f, rippleDuration);
        StartTime = startTime;
        IsBusy = true;

        if (dome != null)
            dome.SetHit(localPoint, 1f, StartTime);
    }

    public void Tick(float time)
    {
        if (!IsBusy)
            return;

        float t = (time - StartTime) / duration;

        if (t >= 1f)
        {
            IsBusy = false;
            if (dome != null)
                dome.SetHit(localPoint, 0f, StartTime);
        }
    }
}