// =============================================================================
// LEAD KHÔNG PHANH - Cinematic Nitro Exhaust
// NitroVfxModule.cs
// Gắn vào TỪNG ParticleSystem con của PF_CinematicNitroExhaust.
// Mục đích: thay thế cách so sánh bằng TÊN GameObject ("08_Burst") vốn rất dễ
// sai khi đổi tên hierarchy. Mỗi module tự khai báo vai trò của nó.
// =============================================================================

using UnityEngine;

public enum NitroModuleRole
{
    OuterFlame = 0,   // thân lửa cam/vàng
    CoreFlame = 1,    // lõi xanh/trắng siêu nóng
    Glow = 2,         // quầng sáng ống bô
    Energy = 3,       // hot streaks / energy particles
    Burst = 4,        // burst khởi động (one-shot)
    Sparks = 5,       // tia lửa
    Smoke = 6,        // khói dư
    Heat = 7          // heat haze
}

[DisallowMultipleComponent]
[RequireComponent(typeof(ParticleSystem))]
public sealed class NitroVfxModule : MonoBehaviour
{
    [Header("Role")]
    public NitroModuleRole role = NitroModuleRole.OuterFlame;

    [Tooltip("Hệ số nhân riêng cho module này (0 = tắt hẳn).")]
    [Min(0f)]
    public float intensityScale = 1f;

    [Tooltip("Module chỉ bắn 1 lần khi bật nitro (dùng cho BurstSystem).")]
    public bool oneShot;

    [Tooltip("Vẫn tiếp tục phát một lúc sau khi tắt nitro (khói, tàn lửa).")]
    public bool lingersAfterStop;

    [Header("Runtime scaling")]
    [Tooltip("Ngưỡng cường độ tối thiểu để module bắt đầu emit.")]
    [Range(0f, 0.5f)]
    public float emitThreshold = 0.02f;

    [Tooltip("Scale cả startSize theo cường độ nitro.")]
    public bool scaleSize = true;

    [Range(0f, 1f)]
    public float sizeAtZero = 0.55f;

    private ParticleSystem system;
    private ParticleSystemRenderer psRenderer;

    private float baseRate;
    private float baseSizeMin;
    private float baseSizeMax;
    private bool baseSizeIsRange;

    private bool cached;
    private bool emitting;

    public ParticleSystem System
    {
        get
        {
            Cache();
            return system;
        }
    }

    private void Awake()
    {
        Cache();
    }

    public void Cache()
    {
        if (cached)
            return;

        cached = true;

        system = GetComponent<ParticleSystem>();
        if (system == null)
            return;

        psRenderer = GetComponent<ParticleSystemRenderer>();

        ParticleSystem.MainModule main = system.main;
        main.playOnAwake = false;

        ParticleSystem.MinMaxCurve rate = system.emission.rateOverTime;
        baseRate = Mathf.Max(rate.constant, rate.constantMax);

        ParticleSystem.MinMaxCurve size = main.startSize;
        baseSizeIsRange = size.mode == ParticleSystemCurveMode.TwoConstants;
        baseSizeMin = baseSizeIsRange ? size.constantMin : size.constant;
        baseSizeMax = baseSizeIsRange ? size.constantMax : size.constant;
    }

    /// <summary>Đưa module về trạng thái "Nitro OFF" tuyệt đối.</summary>
    public void ResetState()
    {
        Cache();
        if (system == null)
            return;

        system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        system.Clear(false);
        emitting = false;
    }

    /// <summary>Bật/tắt renderer (dùng cho các toggle trong Inspector).</summary>
    public void SetVisible(bool visible)
    {
        Cache();
        if (psRenderer != null && psRenderer.enabled != visible)
            psRenderer.enabled = visible;
    }

    /// <summary>Chuyển hệ mô phỏng (Local = dính xe, World = để lại vệt phía sau).</summary>
    public void SetSimulationSpace(ParticleSystemSimulationSpace space)
    {
        Cache();
        if (system == null)
            return;

        ParticleSystem.MainModule main = system.main;
        if (main.simulationSpace != space)
            main.simulationSpace = space;
    }

    /// <summary>Cập nhật cường độ liên tục. Không bao giờ nhân dồn vì luôn dựa trên baseRate.</summary>
    public void ApplyIntensity(float intensity, bool enabledByUser)
    {
        Cache();
        if (system == null || oneShot)
            return;

        float k = enabledByUser ? Mathf.Max(0f, intensity * intensityScale) : 0f;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = baseRate * k;

        if (scaleSize && baseSizeMax > 0f)
        {
            float s = Mathf.Lerp(sizeAtZero, 1f, Mathf.Clamp01(intensity));
            ParticleSystem.MainModule main = system.main;

            main.startSize = baseSizeIsRange
                ? new ParticleSystem.MinMaxCurve(baseSizeMin * s, baseSizeMax * s)
                : new ParticleSystem.MinMaxCurve(baseSizeMax * s);
        }

        bool shouldEmit = k > emitThreshold;

        if (shouldEmit && !emitting)
        {
            system.Play(false);
            emitting = true;
        }
        else if (!shouldEmit && emitting)
        {
            system.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            emitting = false;
        }
    }

    /// <summary>Bắn burst 1 lần (InitialBurst / BurstRays / BurstSparks).</summary>
    public void PlayOneShot(float intensity)
    {
        Cache();
        if (system == null)
            return;

        if (scaleSize && baseSizeMax > 0f)
        {
            float s = Mathf.Lerp(sizeAtZero, 1f, Mathf.Clamp01(intensity));
            ParticleSystem.MainModule main = system.main;

            main.startSize = baseSizeIsRange
                ? new ParticleSystem.MinMaxCurve(baseSizeMin * s, baseSizeMax * s)
                : new ParticleSystem.MinMaxCurve(baseSizeMax * s);
        }

        system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        system.Clear(false);
        system.Play(false);
    }

    public void HardStop()
    {
        ResetState();
    }
}