using UnityEngine;

public class GumController : MonoBehaviour
{
    //=========================================================
    // GUM SETTINGS
    //=========================================================

    [Header("Gum Settings")]

    [Tooltip("Âm thanh khi Player nhặt Gum.")]
    [SerializeField] private AudioClip pickupSound;

    [Tooltip("Âm lượng pickup.")]
    [SerializeField, Range(0f, 1f)]
    private float pickupVolume = 0.9f;

    [Tooltip("Có tự hủy Gum sau khi Player nhặt không.")]
    [SerializeField]
    private bool destroyOnCollect = true;


    //=========================================================
    // DEBUG
    //=========================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugLogs = false;


    //=========================================================
    // RUNTIME
    //=========================================================

    private bool collected;


    //=========================================================
    // COLLECT
    //=========================================================

    public void Collect()
    {
        if (collected)
            return;

        collected = true;


        //=====================================================
        // PLAY AUDIO
        //=====================================================

        PlayPickupSound();


        //=====================================================
        // FIND PLAYER
        //=====================================================

        PlayerController player =
            FindPlayerController();


        if (player != null)
        {
            player.ActivateGumBoost();
        }
        else
        {
            if (debugLogs)
            {
                Debug.LogWarning(
                    "[GumController] Không tìm thấy PlayerController."
                );
            }
        }


        //=====================================================
        // DESTROY
        //=====================================================

        if (destroyOnCollect)
        {
            Destroy(gameObject);
        }


        if (debugLogs)
        {
            Debug.Log(
                "[GumController] GUM COLLECTED."
            );
        }
    }


    //=========================================================
    // FIND PLAYER
    //=========================================================

    private PlayerController FindPlayerController()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            PlayerController player =
                playerObject.GetComponent<PlayerController>();

            if (player != null)
                return player;
        }


        return FindFirstObjectByType<PlayerController>();
    }


    //=========================================================
    // AUDIO
    //=========================================================

    private void PlayPickupSound()
    {
        if (pickupSound == null)
            return;


        GameObject audioObject =
            new GameObject("GumPickupAudio");

        AudioSource source =
            audioObject.AddComponent<AudioSource>();

        source.clip =
            pickupSound;

        source.volume =
            pickupVolume;

        source.spatialBlend =
            0f;

        source.panStereo =
            0f;

        source.dopplerLevel =
            0f;

        source.playOnAwake =
            false;

        source.loop =
            false;


        source.Play();


        Destroy(
            audioObject,
            pickupSound.length + 0.1f
        );
    }


    //=========================================================
    // RESET
    //=========================================================

    public void ResetGum()
    {
        collected = false;
    }


    //=========================================================
    // PUBLIC API
    //=========================================================

    public bool IsCollected()
    {
        return collected;
    }


    //=========================================================
    // VALIDATE
    //=========================================================

    private void OnValidate()
    {
        pickupVolume =
            Mathf.Clamp01(
                pickupVolume
            );
    }
}