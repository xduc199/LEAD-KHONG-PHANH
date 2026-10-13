using System;
using UnityEngine;

public sealed class DailyMissionRewardBridge : MonoBehaviour
{
    public static DailyMissionRewardBridge Instance { get; private set; }

    private bool subscribed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        StartCoroutine(BindNextFrame());
    }

    private System.Collections.IEnumerator BindNextFrame()
    {
        yield return null;
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();

        if (Instance == this)
            Instance = null;
    }

    private void Subscribe()
    {
        if (subscribed)
            return;

        MissionManager missionManager =
            MissionManager.Instance;

        if (missionManager == null)
            return;

        missionManager.OnAllDailyMissionsClaimed +=
            HandleAllDailyMissionsClaimed;

        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed)
            return;

        MissionManager missionManager =
            MissionManager.Instance;

        if (missionManager != null)
        {
            missionManager.OnAllDailyMissionsClaimed -=
                HandleAllDailyMissionsClaimed;
        }

        subscribed = false;
    }

    private void HandleAllDailyMissionsClaimed()
    {
        LuckyWheelManager wheel =
            LuckyWheelManager.Instance;

        if (wheel == null)
        {
            Debug.LogError(
                "[DailyMissionRewardBridge] LuckyWheelManager không tồn tại.");
            return;
        }

        string dateKey =
            DateTime.Now.ToString("yyyyMMdd");

        if (!wheel.TryMarkDailyTicketGranted(dateKey))
        {
            Debug.Log(
                "[DailyMissionRewardBridge] Daily Wheel ticket đã được cấp cho ngày " +
                dateKey + ".");
            return;
        }

        wheel.AddDailyTicket(1);

        Debug.Log(
            "[DailyMissionRewardBridge] Hoàn thành toàn bộ Daily Mission → +1 Daily Wheel Ticket.");
    }
}
