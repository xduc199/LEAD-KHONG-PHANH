using TMPro;
using UnityEngine;

public sealed class GemHUD : MonoBehaviour
{
    //==============================================================
    // UI
    //==============================================================

    [Header("UI")]
    [SerializeField]
    private TMP_Text gemText;


    //==============================================================
    // LIFECYCLE
    //==============================================================

    private void OnEnable()
    {
        GameManager gameManager = GameManager.Instance;

        if (gameManager == null)
        {
            Debug.LogWarning(
                "[GemHUD] GameManager chưa tồn tại khi OnEnable()."
            );

            return;
        }

        gameManager.OnGemChanged += HandleGemChanged;

        Refresh();
    }


    private void OnDisable()
    {
        GameManager gameManager = GameManager.Instance;

        if (gameManager != null)
        {
            gameManager.OnGemChanged -= HandleGemChanged;
        }
    }


    //==============================================================
    // GEM EVENT
    //==============================================================

    private void HandleGemChanged(int amount)
    {
        UpdateText(amount);
    }


    //==============================================================
    // REFRESH
    //==============================================================

    public void Refresh()
    {
        if (gemText == null)
        {
            Debug.LogWarning(
                "[GemHUD] gemText chưa được gán trong Inspector."
            );

            return;
        }

        GameManager gameManager = GameManager.Instance;

        if (gameManager == null)
        {
            UpdateText(0);
            return;
        }

        int currentGem = gameManager.GetGem();

        UpdateText(currentGem);
    }


    //==============================================================
    // UPDATE UI
    //==============================================================

    private void UpdateText(int amount)
    {
        if (gemText == null)
            return;

        if (amount < 0)
            amount = 0;

        //==========================================================
        // FORMAT GEM
        //==========================================================

        string formattedGem = amount.ToString("N0");

        //==========================================================
        // WRITE DIRECTLY TO TMP
        //==========================================================

        gemText.text = formattedGem;

        //==========================================================
        // FORCE TMP REFRESH
        //==========================================================

        gemText.ForceMeshUpdate();

        //==========================================================
        // DEBUG
        //==========================================================

        Debug.Log(
            $"[GemHUD] GEM = {amount} | TMP Text = '{gemText.text}'"
        );
    }
}