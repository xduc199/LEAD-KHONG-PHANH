// =============================================================================
// LEAD KHÔNG PHANH - Cinematic Nitro Exhaust
// NitroLightFlicker.cs
// Gắn vào ExhaustLight / CoreLight. Nhấp nháy theo Perlin noise để ánh sáng
// ống bô "thở" cùng ngọn lửa thay vì là một Point Light tĩnh.
// =============================================================================

using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Light))]
public sealed class NitroLightFlicker : MonoBehaviour
{
    [Min(0f)] public float baseIntensity = 6f;
    [Min(0f)] public float baseRange = 6f;

    [Range(0f, 1f)] public float flickerAmount = 0.35f;
    [Min(0f)] public float flickerSpeed = 17f;

    [Tooltip("Range cũng co giãn theo cường độ nitro.")]
    public bool scaleRange = true;

    [Range(0f, 1f)] public float rangeAtZero = 0.4f;

    public float seed;

    private Light lightRef;
    private bool cached;

    private void Awake()
    {
        Cache();
    }

    public void Cache()
    {
        if (cached)
            return;

        cached = true;

        lightRef = GetComponent<Light>();

        if (Mathf.Approximately(seed, 0f))
            seed = Mathf.Abs(name.GetHashCode() % 883) * 0.0173f;

        if (lightRef != null)
        {
            if (baseIntensity <= 0f) baseIntensity = lightRef.intensity;
            if (baseRange <= 0f) baseRange = lightRef.range;
        }
    }

    public void Hide()
    {
        Cache();

        if (lightRef != null && lightRef.enabled)
            lightRef.enabled = false;
    }

    public void Apply(float intensity, float time, float randomness)
    {
        Cache();

        if (lightRef == null)
            return;

        if (intensity <= 0.01f)
        {
            if (lightRef.enabled)
                lightRef.enabled = false;
            return;
        }

        if (!lightRef.enabled)
            lightRef.enabled = true;

        float n = Mathf.PerlinNoise(seed, time * flickerSpeed);
        float flicker = 1f + (n - 0.5f) * 2f * flickerAmount * randomness;

        lightRef.intensity = baseIntensity * intensity * flicker;

        if (scaleRange)
            lightRef.range = baseRange * Mathf.Lerp(rangeAtZero, 1f, Mathf.Clamp01(intensity));
    }
}