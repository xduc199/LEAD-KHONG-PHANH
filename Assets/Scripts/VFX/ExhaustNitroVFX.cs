// =============================================================================
// LEAD KHÔNG PHANH - Cinematic Nitro Exhaust
// NitroFlameLayer.cs
// Gắn vào các lưỡi lửa dạng MESH (FlameMain / FlameSecondary / FlameTongue_* /
// FlameCore). Mỗi layer tự flicker, sway, co giãn theo cường độ nitro -> tạo ra
// chuyển động "random nhưng có kiểm soát", không đối xứng, không phải hình nón.
// =============================================================================

using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MeshRenderer))]
public sealed class NitroFlameLayer : MonoBehaviour
{
    [Header("Layer type")]
    [Tooltip("True = lõi xanh/trắng siêu nóng, False = thân lửa cam/đỏ.")]
    public bool isCore;

    [Header("Shape")]
    [Min(0.01f)] public float lengthScale = 1f;
    [Min(0.01f)] public float widthScale = 1f;

    [Tooltip("Chiều dài tối thiểu khi nitro gần tắt (tránh biến mất đột ngột).")]
    [Range(0f, 1f)] public float minLength = 0.25f;

    [Header("Flicker")]
    [Min(0f)] public float flickerSpeed = 13f;
    [Range(0f, 1f)] public float flickerAmount = 0.22f;

    [Header("Sway")]
    [Min(0f)] public float swaySpeed = 5.5f;
    [Range(0f, 30f)] public float swayAngle = 7f;
    [Range(-180f, 180f)] public float spinSpeed;

    [Header("Seed")]
    public float seed;

    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock block;
    private Quaternion baseRotation;
    private bool cached;

    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int SeedId = Shader.PropertyToID("_Seed");
    private static readonly int FlickerId = Shader.PropertyToID("_Flicker");

    private void Awake()
    {
        Cache();
    }

    public void Cache()
    {
        if (cached)
            return;

        cached = true;

        meshRenderer = GetComponent<MeshRenderer>();
        block = new MaterialPropertyBlock();
        baseRotation = transform.localRotation;

        if (Mathf.Approximately(seed, 0f))
            seed = Mathf.Abs(name.GetHashCode() % 977) * 0.0131f;
    }

    public void Hide()
    {
        Cache();

        if (meshRenderer != null && meshRenderer.enabled)
            meshRenderer.enabled = false;
    }

    /// <param name="intensity">0..1 (đã nhân với Nitro Intensity).</param>
    /// <param name="time">Time.time hoặc unscaledTime.</param>
    /// <param name="randomness">0 = phẳng lặng, 1 = flicker đầy đủ.</param>
    public void Apply(float intensity, float time, float randomness)
    {
        Cache();

        if (meshRenderer == null)
            return;

        if (intensity <= 0.005f)
        {
            if (meshRenderer.enabled)
                meshRenderer.enabled = false;
            return;
        }

        if (!meshRenderer.enabled)
            meshRenderer.enabled = true;

        float n1 = Mathf.PerlinNoise(seed * 7.31f, time * flickerSpeed);
        float n2 = Mathf.PerlinNoise(time * swaySpeed + seed * 3.17f, seed * 11.73f);
        float n3 = Mathf.PerlinNoise(seed * 5.91f, time * swaySpeed * 0.77f);

        float flicker = 1f + (n1 - 0.5f) * 2f * flickerAmount * randomness;

        float length = lengthScale
                       * Mathf.Lerp(minLength, 1f, Mathf.Clamp01(intensity))
                       * flicker;

        float width = widthScale
                      * Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(intensity))
                      * (1f + (n2 - 0.5f) * 0.28f * randomness);

        transform.localScale = new Vector3(
            Mathf.Max(0.01f, width),
            Mathf.Max(0.01f, width),
            Mathf.Max(0.01f, length));

        float swayY = (n2 - 0.5f) * 2f * swayAngle * randomness;
        float swayX = (n3 - 0.5f) * 2f * swayAngle * 0.6f * randomness;

        transform.localRotation =
            baseRotation * Quaternion.Euler(swayX, swayY, spinSpeed * time);

        meshRenderer.GetPropertyBlock(block);
        block.SetFloat(IntensityId, Mathf.Clamp(intensity * flicker, 0f, 4f));
        block.SetFloat(SeedId, seed);
        block.SetFloat(FlickerId, n1);
        meshRenderer.SetPropertyBlock(block);
    }
}