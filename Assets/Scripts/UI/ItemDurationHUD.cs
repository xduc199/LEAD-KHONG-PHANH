using UnityEngine;

public class ItemDurationHUD : MonoBehaviour
{
    //=========================================================
    // PLAYER
    //=========================================================

    [Header("Player")]
    [SerializeField] private PlayerController player;


    //=========================================================
    // SHIELD
    //=========================================================

    [Header("Shield")]
    [SerializeField] private ShieldController shieldController;
    [SerializeField] private ItemDurationEntry shieldEntry;


    //=========================================================
    // GUM
    //=========================================================

    [Header("Gum")]
    [SerializeField] private ItemDurationEntry gumEntry;


    //=========================================================
    // INTERNAL
    //=========================================================

    private void Awake()
    {
        FindReferences();

        HideAllEntries();
    }


    private void Start()
    {
        FindReferences();
    }


    private void Update()
    {
        UpdateShield();
        UpdateGum();
    }


    //=========================================================
    // FIND REFERENCES
    //=========================================================

    private void FindReferences()
    {
        //-----------------------------------------------------
        // PLAYER
        //-----------------------------------------------------

        if (player == null)
        {
            player =
                FindFirstObjectByType<PlayerController>();
        }


        if (player == null)
        {
            return;
        }


        //-----------------------------------------------------
        // SHIELD
        //-----------------------------------------------------

        if (shieldController == null)
        {
            shieldController =
                player.GetComponent<ShieldController>();
        }


        if (shieldController == null)
        {
            shieldController =
                player.GetComponentInChildren<
                    ShieldController
                >(true);
        }


        if (shieldController == null)
        {
            shieldController =
                player.GetComponentInParent<
                    ShieldController
                >();
        }
    }


    //=========================================================
    // SHIELD
    //=========================================================

    private void UpdateShield()
    {
        if (
            shieldController == null ||
            shieldEntry == null
        )
        {
            return;
        }


        //-----------------------------------------------------
        // SHIELD INACTIVE
        //-----------------------------------------------------

        if (!shieldController.IsActive())
        {
            HideEntry(shieldEntry);

            return;
        }


        //-----------------------------------------------------
        // GET REAL REMAINING TIME
        //-----------------------------------------------------

        float remainingTime =
            shieldController.GetRemainingTime();


        //-----------------------------------------------------
        // TIMER FINISHED
        //-----------------------------------------------------

        if (remainingTime <= 0f)
        {
            HideEntry(shieldEntry);

            return;
        }


        //-----------------------------------------------------
        // SHOW
        //-----------------------------------------------------

        ShowEntry(shieldEntry);


        //-----------------------------------------------------
        // UPDATE BAR
        //-----------------------------------------------------

        shieldEntry.SetTime(
            remainingTime
        );
    }


    //=========================================================
    // GUM
    //=========================================================

    private void UpdateGum()
    {
        if (
            player == null ||
            gumEntry == null
        )
        {
            return;
        }


        //-----------------------------------------------------
        // GUM INACTIVE
        //-----------------------------------------------------

        if (!player.IsGumBoosted)
        {
            HideEntry(gumEntry);

            return;
        }


        //-----------------------------------------------------
        // GET REAL REMAINING TIME
        //-----------------------------------------------------

        float remainingTime =
            player.GumTimeRemaining;


        //-----------------------------------------------------
        // TIMER FINISHED
        //-----------------------------------------------------

        if (remainingTime <= 0f)
        {
            HideEntry(gumEntry);

            return;
        }


        //-----------------------------------------------------
        // SHOW
        //-----------------------------------------------------

        ShowEntry(gumEntry);


        //-----------------------------------------------------
        // UPDATE BAR
        //-----------------------------------------------------

        gumEntry.SetTime(
            remainingTime
        );
    }


    //=========================================================
    // SHOW
    //=========================================================

    private void ShowEntry(
        ItemDurationEntry entry
    )
    {
        if (entry == null)
        {
            return;
        }


        if (!entry.gameObject.activeSelf)
        {
            entry.gameObject.SetActive(true);
        }
    }


    //=========================================================
    // HIDE
    //=========================================================

    private void HideEntry(
        ItemDurationEntry entry
    )
    {
        if (entry == null)
        {
            return;
        }


        if (entry.gameObject.activeSelf)
        {
            entry.gameObject.SetActive(false);
        }
    }


    //=========================================================
    // HIDE ALL
    //=========================================================

    private void HideAllEntries()
    {
        HideEntry(shieldEntry);
        HideEntry(gumEntry);
    }
}