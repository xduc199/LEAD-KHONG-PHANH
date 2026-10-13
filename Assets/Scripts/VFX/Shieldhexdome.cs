// =============================================================================
// LEAD KHÔNG PHANH - Hexagon Energy Shield
// ShieldHexDome.cs
// Gắn vào mesh hình cầu/trứng (dome) dùng M_ShieldHex.mat. Đẩy tham số sang
// shader qua MaterialPropertyBlock (không tạo material instance -> tiết kiệm
// draw call khi có nhiều xe/nhiều khiên).
// =============================================================================

using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MeshRenderer))]
public sealed class ShieldHexDome : MonoBehaviour
{
    private static readonly int ColorId = Shader.PropertyToID("_ShieldColor");
    private static readonly int HitColorId = Shader.PropertyToID("_HitColor");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int HexAlphaId = Shader.PropertyToID("_HexAlpha");
    private static readonly int FresnelAlphaId = Shader.PropertyToID("_FresnelAlpha");
    private static readonly int EdgeAlphaId = Shader.PropertyToID("_EdgeAlpha");
    private static readonly int HexScaleId = Shader.PropertyToID("_HexScale");
    private static readonly int HexLineWidthId = Shader.PropertyToID("_HexLineWidth");
    private static readonly int HexScrollId = Shader.PropertyToID("_HexScroll");
    private static readonly int HitPointId = Shader.PropertyToID("_HitPoint");
    private static readonly int HitTimeId = Shader.PropertyToID("_HitTime");
    private static readonly int HitStrengthId = Shader.PropertyToID("_HitStrength");
    private static readonly int TimeOverrideId = Shader.PropertyToID("_TimeOverride");

    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock block;
    private Vector3 baseScale = Vector3.one;
    private bool cached;

    private void Awake()
    {
        Cache();
    }

    private void Cache()
    {
        if (cached) return;
        cached = true;

        meshRenderer = GetComponent<MeshRenderer>();
        block = new MaterialPropertyBlock();
        baseScale = transform.localScale;

        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    public void SetVisible(bool visible)
    {
        Cache();
        if (meshRenderer.enabled != visible)
            meshRenderer.enabled = visible;
    }

    public void SetRadius(float radius)
    {
        Cache();
        baseScale = Vector3.one * radius * 2f;
        transform.localScale = baseScale;
    }

    public void SetScale(float punch01)
    {
        Cache();
        transform.localScale = baseScale * Mathf.Max(0.01f, punch01);
    }

    public void SetColors(Color shieldColor, Color hitColor)
    {
        Cache();
        meshRenderer.GetPropertyBlock(block);
        block.SetColor(ColorId, shieldColor);
        block.SetColor(HitColorId, hitColor);
        meshRenderer.SetPropertyBlock(block);
    }

    public void SetHexParams(float hexScale, float lineWidth, float scrollSpeed)
    {
        Cache();
        meshRenderer.GetPropertyBlock(block);
        block.SetFloat(HexScaleId, hexScale);
        block.SetFloat(HexLineWidthId, lineWidth);
        block.SetFloat(HexScrollId, scrollSpeed);
        meshRenderer.SetPropertyBlock(block);
    }

    public void SetIntensity(float alpha, bool hexOn, bool fresnelOn, bool edgeOn)
    {
        Cache();
        meshRenderer.GetPropertyBlock(block);
        block.SetFloat(IntensityId, alpha);
        block.SetFloat(HexAlphaId, hexOn ? 1f : 0f);
        block.SetFloat(FresnelAlphaId, fresnelOn ? 1f : 0f);
        block.SetFloat(EdgeAlphaId, edgeOn ? 1f : 0f);
        meshRenderer.SetPropertyBlock(block);
    }

    public void SetHit(Vector3 localPointNormalized, float strength, float startTime)
    {
        Cache();
        meshRenderer.GetPropertyBlock(block);
        block.SetVector(HitPointId, localPointNormalized);
        block.SetFloat(HitStrengthId, strength);
        block.SetFloat(HitTimeId, startTime);
        meshRenderer.SetPropertyBlock(block);
    }

    public void Tick(float time)
    {
        Cache();
        meshRenderer.GetPropertyBlock(block);
        block.SetFloat(TimeOverrideId, time);
        meshRenderer.SetPropertyBlock(block);
    }
}