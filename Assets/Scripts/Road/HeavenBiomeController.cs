using UnityEngine;

public sealed class HeavenBiomeController : MonoBehaviour
{
    [Header("Heaven Root")]
    [SerializeField] private GameObject heavenEnvironmentRoot;

    [Header("Visual")]
    [SerializeField] private GameObject heavenCloudRoot;
    [SerializeField] private GameObject heavenPropRoot;
    [SerializeField] private GameObject heavenVfxRoot;

    [Header("Lighting")]
    [SerializeField] private Light heavenDirectionalLight;

    [Header("Sky")]
    [SerializeField] private Material heavenSkybox;

    [Header("Audio")]
    [SerializeField] private AudioSource heavenMusic;

    [Header("Transition")]
    [SerializeField] private float transitionSpeed = 2.5f;

    private bool isHeavenActive;
    private float transitionValue;

    private Color originalAmbientColor;
    private float originalAmbientIntensity;
    private Material originalSkybox;

    private void Awake()
    {
        originalAmbientColor =
            RenderSettings.ambientLight;

        originalAmbientIntensity =
            RenderSettings.ambientIntensity;

        originalSkybox =
            RenderSettings.skybox;

        SetVisualState(false, true);
    }

    private void Update()
    {
        float target =
            isHeavenActive
                ? 1f
                : 0f;

        transitionValue =
            Mathf.MoveTowards(
                transitionValue,
                target,
                transitionSpeed *
                Time.deltaTime
            );

        UpdateTransition();
    }

    public void EnterHeaven()
    {
        if (isHeavenActive)
        {
            return;
        }

        isHeavenActive = true;

        SetVisualState(
            true,
            false
        );
    }

    public void ExitHeaven()
    {
        if (!isHeavenActive)
        {
            return;
        }

        isHeavenActive = false;

        SetVisualState(
            false,
            false
        );
    }

    private void SetVisualState(
        bool active,
        bool instant
    )
    {
        if (heavenEnvironmentRoot != null)
        {
            heavenEnvironmentRoot.SetActive(
                active
            );
        }

        if (heavenCloudRoot != null)
        {
            heavenCloudRoot.SetActive(
                active
            );
        }

        if (heavenPropRoot != null)
        {
            heavenPropRoot.SetActive(
                active
            );
        }

        if (heavenVfxRoot != null)
        {
            heavenVfxRoot.SetActive(
                active
            );
        }

        if (heavenDirectionalLight != null)
        {
            heavenDirectionalLight.enabled =
                active;
        }

        if (heavenMusic != null)
        {
            if (active)
            {
                if (!heavenMusic.isPlaying)
                {
                    heavenMusic.Play();
                }
            }
            else
            {
                heavenMusic.Stop();
            }
        }

        if (instant)
        {
            transitionValue =
                active
                    ? 1f
                    : 0f;

            UpdateTransition();
        }
    }

    private void UpdateTransition()
    {
        if (heavenDirectionalLight != null)
        {
            heavenDirectionalLight.intensity =
                Mathf.Lerp(
                    0f,
                    1.15f,
                    transitionValue
                );
        }

        if (heavenMusic != null)
        {
            heavenMusic.volume =
                transitionValue;
        }

        if (
            heavenSkybox != null &&
            transitionValue > 0f
        )
        {
            RenderSettings.skybox =
                heavenSkybox;
        }

        RenderSettings.ambientIntensity =
            Mathf.Lerp(
                originalAmbientIntensity,
                1.2f,
                transitionValue
            );
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        RenderSettings.skybox =
            originalSkybox;

        RenderSettings.ambientLight =
            originalAmbientColor;

        RenderSettings.ambientIntensity =
            originalAmbientIntensity;
    }
}