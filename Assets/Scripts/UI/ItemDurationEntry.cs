using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemDurationEntry : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private Slider durationSlider;

    [Header("Settings")]
    [SerializeField] private float maxDuration = 5f;

    public void SetItemName(string itemName)
    {
        if (itemNameText != null)
        {
            itemNameText.text = itemName;
        }
    }


    public void SetMaxDuration(float duration)
    {
        maxDuration =
            Mathf.Max(
                0.01f,
                duration
            );
    }


    public void SetTime(float remainingTime)
    {
        remainingTime =
            Mathf.Max(
                0f,
                remainingTime
            );


        float normalized =
            Mathf.Clamp01(
                remainingTime /
                maxDuration
            );


        if (durationSlider != null)
        {
            durationSlider.minValue = 0f;
            durationSlider.maxValue = 1f;
            durationSlider.value = normalized;
        }


        if (timeText != null)
        {
            timeText.text =
                remainingTime.ToString("0.0") +
                "s";
        }
    }
}