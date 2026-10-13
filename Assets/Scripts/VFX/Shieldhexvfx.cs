// =============================================================================
// LEAD KHÔNG PHANH - Hexagon Energy Shield
// ShieldHexVFX.cs
//
// Điều khiển runtime cho khiên năng lượng dạng "từ trường lục giác" màu xanh
// dương nhạt bọc quanh xe. Không phụ thuộc theme Nitro, dùng độc lập.
//
// Cách dùng:
//   shield.Activate();          // bật khiên (có hiệu ứng bung ra)
//   shield.Deactivate();        // tắt khiên (có hiệu ứng co lại)
//   shield.Hit(worldPoint);     // gợn sóng lục giác tại điểm va chạm
//   shield.SetCharge(0.6f);     // 0..1 -> khiên mờ dần khi sắp hết năng lượng
// =============================================================================

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShieldHexVFX : MonoBehaviour
{
    [Header("Effect Settings")]
    [SerializeField] private bool enableHexGrid = true;
    [SerializeField] private bool enableFresnelDome = true;
    [SerializeField] private bool enableEdgeGlow = true;
    [SerializeField] private bool enableParticles = true;
    [SerializeField] private bool enableLight = true;

    [Header("Shield")]
    [SerializeField, Range(0f, 2f)] private float shieldIntensity = 1f;
    [SerializeField, Min(0.05f)] private float fadeSpeed = 5f;
    [SerializeField, Min(0.05f)] private float scalePunchSpeed = 7f;

    [Tooltip("Bán kính hình cầu/hình trứng bao quanh xe.")]
    [SerializeField, Min(0.1f)] private float radius = 1.4f;

    [Header("Hex Field")]
    [SerializeField, Min(1f)] private float hexScale = 9f;
    [SerializeField, Min(0f)] private float hexScrollSpeed = 0.35f;
    [SerializeField, Range(0f, 1f)] private float hexLineWidth = 0.14f;
    [SerializeField] private Color shieldColor = new Color(0.45f, 0.78f, 1f, 1f);
    [SerializeField] private Color hitColor = new Color(0.85f, 0.95f, 1f, 1f);

    [Header("Charge (0..1)")]
    [SerializeField, Range(0f, 1f)] private float charge = 1f;
    [SerializeField, Range(0f, 1f)] private float lowChargeThreshold = 0.25f;
    [SerializeField, Min(0f)] private float lowChargeFlickerSpeed = 9f;

    [Header("Hit Ripple")]
    [SerializeField, Min(0f)] private float hitRippleDuration = 0.6f;
    [SerializeField, Min(0f)] private float hitCooldown = 0.08f;
    [SerializeField, Min(1)] private int maxSimultaneousHits = 4;

    [Header("Misc")]
    [SerializeField] private bool playOnAwake;
    [SerializeField] private bool useUnscaledTime;

    // ------------------------------------------------------------------ Refs

    private ShieldHexDome dome;
    private ParticleSystem hexParticles;
    private ParticleSystem edgeSparkles;
    private Light shieldLight;
    private ShieldHitPoint[] hitSlots;

    private float targetIntensity;
    private float currentIntensity;
    private float currentScale;
    private float targetScale = 1f;
    private bool active;
    private bool initialized;
    private float lastHitTime = -999f;

    // ------------------------------------------------------------------ API

    public bool IsActive => active;
    public float CurrentIntensity => currentIntensity;

    public float Charge
    {
        get => charge;
        set => charge = Mathf.Clamp01(value);
    }

    public void SetCharge(float value01)
    {
        Charge = value01;
    }

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        if (playOnAwake) Activate();
        else DeactivateImmediate();
    }

    private void OnDisable()
    {
        DeactivateImmediate();
    }

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;

        dome = GetComponentInChildren<ShieldHexDome>(true);
        hexParticles = FindChildParticles("HexSparkles");
        edgeSparkles = FindChildParticles("EdgeSparkles");
        shieldLight = GetComponentInChildren<Light>(true);
        hitSlots = GetComponentsInChildren<ShieldHitPoint>(true);

        if (dome != null)
        {
            dome.SetColors(shieldColor, hitColor);
            dome.SetHexParams(hexScale, hexLineWidth, hexScrollSpeed);
            dome.SetRadius(radius);
        }

        StopAllVisualsHard();
    }

    private ParticleSystem FindChildParticles(string containsName)
    {
        ParticleSystem[] all = GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].gameObject.name.Contains(containsName))
                return all[i];
        }
        return null;
    }

    private void Update()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float time = useUnscaledTime ? Time.unscaledTime : Time.time;

        currentIntensity = Mathf.MoveTowards(currentIntensity, targetIntensity,
            Mathf.Max(0.05f, fadeSpeed) * dt);

        currentScale = Mathf.MoveTowards(currentScale, targetScale,
            Mathf.Max(0.05f, scalePunchSpeed) * dt);

        if (currentIntensity <= 0.0001f && Mathf.Approximately(targetIntensity, 0f) && !active)
            return;

        Apply(time);
    }

    private void Apply(float time)
    {
        float flicker = 1f;

        if (charge < lowChargeThreshold && charge > 0f)
        {
            float n = Mathf.PerlinNoise(time * lowChargeFlickerSpeed, 0.37f);
            float t = Mathf.InverseLerp(lowChargeThreshold, 0f, charge);
            flicker = Mathf.Lerp(1f, 0.35f + n * 0.5f, t);
        }
        else if (charge <= 0f)
        {
            flicker = 0f;
        }

        float alpha = Mathf.Clamp(currentIntensity * shieldIntensity * flicker, 0f, 2f);

        if (dome != null)
        {
            dome.SetVisible(enableFresnelDome || enableHexGrid || enableEdgeGlow);
            dome.SetIntensity(alpha, enableHexGrid, enableFresnelDome, enableEdgeGlow);
            dome.SetScale(currentScale);
            dome.Tick(time);
        }

        if (hexParticles != null)
            SetParticleActive(hexParticles, enableParticles && alpha > 0.02f, alpha);

        if (edgeSparkles != null)
            SetParticleActive(edgeSparkles, enableParticles && alpha > 0.02f, alpha);

        if (shieldLight != null)
        {
            bool on = enableLight && alpha > 0.02f;
            if (shieldLight.enabled != on) shieldLight.enabled = on;
            if (on) shieldLight.intensity = Mathf.Lerp(0.3f, 3.2f, alpha);
        }

        if (hitSlots != null)
        {
            for (int i = 0; i < hitSlots.Length; i++)
                hitSlots[i]?.Tick(time);
        }
    }

    private void SetParticleActive(ParticleSystem ps, bool on, float alpha)
    {
        ParticleSystemRenderer psr = ps.GetComponent<ParticleSystemRenderer>();
        if (psr != null) psr.enabled = on;

        if (on && !ps.isPlaying) ps.Play(false);
        else if (!on && ps.isPlaying) ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);

        ParticleSystem.EmissionModule emission = ps.emission;
        ParticleSystem.MinMaxCurve rate = emission.rateOverTime;
        float baseRate = Mathf.Max(rate.constant, rate.constantMax, 1f);
        emission.rateOverTime = on ? baseRate : 0f;
    }

    // ------------------------------------------------------------------ Ops

    public void Activate()
    {
        Initialize();
        active = true;
        targetIntensity = 1f;
        targetScale = 1f;
        currentScale = 0.7f;
    }

    public void Deactivate()
    {
        Initialize();
        active = false;
        targetIntensity = 0f;
    }

    public void DeactivateImmediate()
    {
        Initialize();
        active = false;
        targetIntensity = 0f;
        currentIntensity = 0f;
        currentScale = 1f;
        targetScale = 1f;

        StopAllVisualsHard();
    }

    private void StopAllVisualsHard()
    {
        if (dome != null)
        {
            dome.SetIntensity(0f, false, false, false);
            dome.SetVisible(false);
        }

        if (hexParticles != null)
        {
            hexParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystemRenderer r = hexParticles.GetComponent<ParticleSystemRenderer>();
            if (r != null) r.enabled = false;
        }

        if (edgeSparkles != null)
        {
            edgeSparkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystemRenderer r = edgeSparkles.GetComponent<ParticleSystemRenderer>();
            if (r != null) r.enabled = false;
        }

        if (shieldLight != null)
            shieldLight.enabled = false;
    }

    /// <summary>Gợn sóng lục giác tại điểm va chạm (world space).</summary>
    public void Hit(Vector3 worldPoint)
    {
        Initialize();

        float time = useUnscaledTime ? Time.unscaledTime : Time.time;
        if (time - lastHitTime < hitCooldown)
            return;

        lastHitTime = time;

        if (hitSlots == null || hitSlots.Length == 0)
        {
            PunchScale();
            return;
        }

        ShieldHitPoint slot = null;
        float oldest = float.MinValue;

        for (int i = 0; i < hitSlots.Length; i++)
        {
            if (hitSlots[i] == null) continue;

            if (!hitSlots[i].IsBusy)
            {
                slot = hitSlots[i];
                break;
            }

            if (hitSlots[i].StartTime > oldest)
            {
                oldest = hitSlots[i].StartTime;
            }
        }

        if (slot == null)
            slot = hitSlots[Random.Range(0, Mathf.Min(hitSlots.Length, maxSimultaneousHits))];

        Vector3 local = transform.InverseTransformPoint(worldPoint).normalized;
        slot.Trigger(local, hitColor, hitRippleDuration, time);

        PunchScale();
    }

    private void PunchScale()
    {
        currentScale = Mathf.Max(currentScale, 1.06f);
        targetScale = 1f;
    }
}