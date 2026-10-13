using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LuckyWheelUIController : MonoBehaviour
{
    // =====================================================================
    // DAILY WHEEL
    // =====================================================================

    [Header("Daily Wheel")]
    [SerializeField] private GameObject dailyPanel;
    [SerializeField] private TMP_Text dailyTicketText;
    [SerializeField] private TMP_Text dailyRewardText;
    [SerializeField] private Button dailySpinButton;
    [SerializeField] private Button dailyCloseButton;


    // =====================================================================
    // VIP WHEEL
    // =====================================================================

    [Header("VIP Wheel")]
    [SerializeField] private GameObject vipPanel;
    [SerializeField] private TMP_Text vipGemText;
    [SerializeField] private TMP_Text vipRewardText;
    [SerializeField] private Button vipSpinButton;
    [SerializeField] private Button vipCloseButton;


    // =====================================================================
    // WHEEL VISUAL
    // =====================================================================

    [Header("Wheel Visual")]
    [SerializeField] private RectTransform dailyWheel;
    [SerializeField] private RectTransform vipWheel;

    [SerializeField] private RectTransform dailyPointer;
    [SerializeField] private RectTransform vipPointer;

    [SerializeField] private TMP_Text dailyCenterText;
    [SerializeField] private TMP_Text vipCenterText;


    // =====================================================================
    // ANIMATION
    // =====================================================================

    [Header("Wheel Animation")]
    [SerializeField, Min(1f)]
    private float totalTurns = 6f;

    [SerializeField, Min(0.5f)]
    private float spinDuration = 3.2f;

    [SerializeField, Min(0f)]
    private float settleDuration = 0.25f;

    [SerializeField]
    private bool useUnscaledTime = true;


    // =====================================================================
    // POINTER
    // =====================================================================

    [Header("Pointer")]
    [SerializeField]
    private bool enablePointerTick = false;

    [SerializeField, Min(0.1f)]
    private float pointerTickAngle = 7.5f;

    [SerializeField, Min(0.1f)]
    private float pointerTickThreshold = 30f;


    // =====================================================================
    // OPTIONAL
    // =====================================================================

    [Header("Optional")]
    [SerializeField] private TMP_Text globalMessageText;


    // =====================================================================
    // INTERNAL STATE
    // =====================================================================

    private LuckyWheelManager wheelManager;
    private GameManager gameManager;

    private Coroutine dailySpinRoutine;
    private Coroutine vipSpinRoutine;

    private bool dailySpinning;
    private bool vipSpinning;


    // =====================================================================
    // UNITY LIFECYCLE
    // =====================================================================

    private void Awake()
    {
        AutoFindReferences();
        BindButtons();
    }

    private void OnEnable()
    {
        ResolveManagers();
        Subscribe();
        RefreshAll();
    }

    private void OnDisable()
    {
        StopSpinAnimations();
        Unsubscribe();
    }

    private void OnDestroy()
    {
        StopSpinAnimations();
        UnbindButtons();
    }


    // =====================================================================
    // MANAGER
    // =====================================================================

    private void ResolveManagers()
    {
        if (wheelManager == null)
            wheelManager = LuckyWheelManager.Instance;

        if (gameManager == null)
            gameManager = GameManager.Instance;
    }

    private void Subscribe()
    {
        if (wheelManager == null)
            return;

        wheelManager.OnTicketsChanged -= HandleTicketsChanged;
        wheelManager.OnTicketsChanged += HandleTicketsChanged;

        wheelManager.OnRewardGranted -= HandleRewardGranted;
        wheelManager.OnRewardGranted += HandleRewardGranted;
    }

    private void Unsubscribe()
    {
        if (wheelManager == null)
            return;

        wheelManager.OnTicketsChanged -= HandleTicketsChanged;
        wheelManager.OnRewardGranted -= HandleRewardGranted;
    }


    // =====================================================================
    // BUTTON BINDING
    // =====================================================================

    private void BindButtons()
    {
        if (dailySpinButton != null)
        {
            dailySpinButton.onClick.RemoveListener(SpinDaily);
            dailySpinButton.onClick.AddListener(SpinDaily);
        }

        if (vipSpinButton != null)
        {
            vipSpinButton.onClick.RemoveListener(SpinVip);
            vipSpinButton.onClick.AddListener(SpinVip);
        }

        if (dailyCloseButton != null)
        {
            dailyCloseButton.onClick.RemoveListener(CloseDaily);
            dailyCloseButton.onClick.AddListener(CloseDaily);
        }

        if (vipCloseButton != null)
        {
            vipCloseButton.onClick.RemoveListener(CloseVip);
            vipCloseButton.onClick.AddListener(CloseVip);
        }
    }

    private void UnbindButtons()
    {
        if (dailySpinButton != null)
            dailySpinButton.onClick.RemoveListener(SpinDaily);

        if (vipSpinButton != null)
            vipSpinButton.onClick.RemoveListener(SpinVip);

        if (dailyCloseButton != null)
            dailyCloseButton.onClick.RemoveListener(CloseDaily);

        if (vipCloseButton != null)
            vipCloseButton.onClick.RemoveListener(CloseVip);
    }


    // =====================================================================
    // OPEN / CLOSE
    // =====================================================================

    public void OpenDaily()
    {
        ResolveManagers();

        if (dailyPanel != null)
            dailyPanel.SetActive(true);

        RefreshDailyState();
    }

    public void OpenVip()
    {
        ResolveManagers();

        if (vipPanel != null)
            vipPanel.SetActive(true);

        RefreshVipState();
    }

    public void CloseDaily()
    {
        if (dailySpinning)
            return;

        if (dailyPanel != null)
            dailyPanel.SetActive(false);
    }

    public void CloseVip()
    {
        if (vipSpinning)
            return;

        if (vipPanel != null)
            vipPanel.SetActive(false);
    }


    // =====================================================================
    // DAILY SPIN
    // =====================================================================

    public void SpinDaily()
    {
        if (dailySpinning)
            return;

        ResolveManagers();

        if (wheelManager == null)
        {
            SetMessage(
                "Lucky Wheel chưa được khởi tạo.");

            return;
        }

        if (!wheelManager.CanSpinDaily())
        {
            SetMessage(
                "Bạn không có lượt Daily Wheel.");

            RefreshDailyState();

            return;
        }

        LuckyWheelReward reward;

        bool success =
            wheelManager.TrySpinDaily(
                out reward);

        if (!success || reward == null)
        {
            RefreshDailyState();

            SetMessage(
                "Không thể quay Daily Wheel.");

            return;
        }

        dailySpinRoutine =
            StartCoroutine(
                AnimateDailySpin(reward));
    }


    // =====================================================================
    // VIP SPIN
    // =====================================================================

    public void SpinVip()
    {
        if (vipSpinning)
            return;

        ResolveManagers();

        if (wheelManager == null)
        {
            SetMessage(
                "Lucky Wheel chưa được khởi tạo.");

            return;
        }

        if (!wheelManager.CanSpinVip())
        {
            SetMessage(
                "Không đủ 500 Gem để quay VIP Wheel.");

            RefreshVipState();

            return;
        }

        LuckyWheelReward reward;

        bool success =
            wheelManager.TrySpinVip(
                out reward);

        if (!success || reward == null)
        {
            RefreshVipState();

            SetMessage(
                "Không thể quay VIP Wheel.");

            return;
        }

        vipSpinRoutine =
            StartCoroutine(
                AnimateVipSpin(reward));
    }


    // =====================================================================
    // DAILY ANIMATION
    // =====================================================================

    private IEnumerator AnimateDailySpin(
        LuckyWheelReward reward)
    {
        dailySpinning = true;

        SetSpinButtonsInteractable(false);

        if (dailyWheel == null)
        {
            FinishDailySpin(reward);
            yield break;
        }

        int rewardIndex =
            FindRewardIndex(
                LuckyWheelType.Daily,
                reward);

        float sectorAngle =
            360f / 12f;

        float currentRotation =
            NormalizeAngle(
                dailyWheel.localEulerAngles.z);

        float targetRotation =
            CalculateTargetRotation(
                currentRotation,
                rewardIndex,
                sectorAngle);

        yield return AnimateWheel(
            dailyWheel,
            dailyPointer,
            currentRotation,
            targetRotation);

        FinishDailySpin(reward);
    }


    // =====================================================================
    // VIP ANIMATION
    // =====================================================================

    private IEnumerator AnimateVipSpin(
        LuckyWheelReward reward)
    {
        vipSpinning = true;

        SetSpinButtonsInteractable(false);

        if (vipWheel == null)
        {
            FinishVipSpin(reward);
            yield break;
        }

        int rewardIndex =
            FindRewardIndex(
                LuckyWheelType.VIP,
                reward);

        float sectorAngle =
            360f / 15f;

        float currentRotation =
            NormalizeAngle(
                vipWheel.localEulerAngles.z);

        float targetRotation =
            CalculateTargetRotation(
                currentRotation,
                rewardIndex,
                sectorAngle);

        yield return AnimateWheel(
            vipWheel,
            vipPointer,
            currentRotation,
            targetRotation);

        FinishVipSpin(reward);
    }


    // =====================================================================
    // GENERIC WHEEL ANIMATION
    // =====================================================================

    private IEnumerator AnimateWheel(
        RectTransform wheel,
        RectTransform pointer,
        float currentRotation,
        float targetRotation)
    {
        float elapsed = 0f;
        float previousRotation = currentRotation;

        while (elapsed < spinDuration)
        {
            float deltaTime =
                useUnscaledTime
                    ? Time.unscaledDeltaTime
                    : Time.deltaTime;

            elapsed += deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / spinDuration);

            float eased =
                EaseOutCubic(t);

            float rotation =
                Mathf.LerpUnclamped(
                    currentRotation,
                    targetRotation,
                    eased);

            wheel.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    rotation);

            if (
                enablePointerTick &&
                pointer != null
            )
            {
                UpdatePointerTick(
                    pointer,
                    previousRotation,
                    rotation);
            }

            previousRotation = rotation;

            yield return null;
        }

        wheel.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                targetRotation);

        if (settleDuration > 0f)
        {
            yield return AnimateSettle(
                wheel,
                targetRotation);
        }
    }


    // =====================================================================
    // SETTLE
    // =====================================================================

    private IEnumerator AnimateSettle(
        RectTransform wheel,
        float targetRotation)
    {
        float elapsed = 0f;

        float startRotation =
            targetRotation;

        float overshootRotation =
            targetRotation + 2f;

        while (elapsed < settleDuration)
        {
            float deltaTime =
                useUnscaledTime
                    ? Time.unscaledDeltaTime
                    : Time.deltaTime;

            elapsed += deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / settleDuration);

            float eased =
                EaseOutBack(t);

            float rotation =
                Mathf.LerpUnclamped(
                    startRotation,
                    overshootRotation,
                    eased);

            wheel.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    rotation);

            yield return null;
        }

        wheel.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                targetRotation);
    }


    // =====================================================================
    // TARGET CALCULATION
    // =====================================================================

    private float CalculateTargetRotation(
        float currentRotation,
        int rewardIndex,
        float sectorAngle)
    {
        // Sector 0 được giả định nằm chính giữa tại vị trí TOP.

        float targetSectorRotation =
            -(rewardIndex * sectorAngle);

        float delta =
            Mathf.DeltaAngle(
                currentRotation,
                targetSectorRotation);

        return
            currentRotation +
            (totalTurns * 360f) +
            delta;
    }


    // =====================================================================
    // FIND REWARD INDEX
    // =====================================================================

    private int FindRewardIndex(
        LuckyWheelType type,
        LuckyWheelReward reward)
    {
        if (
            wheelManager == null ||
            reward == null
        )
        {
            return 0;
        }

        IReadOnlyList<LuckyWheelReward> rewards =
            type == LuckyWheelType.Daily
                ? wheelManager.DailyRewards
                : wheelManager.VipRewards;

        int count =
            rewards.Count;

        for (int i = 0; i < count; i++)
        {
            LuckyWheelReward candidate =
                rewards[i];

            if (ReferenceEquals(
                    candidate,
                    reward))
            {
                return i;
            }

            if (
                candidate != null &&
                candidate.id == reward.id
            )
            {
                return i;
            }
        }

        return 0;
    }


    // =====================================================================
    // FINISH DAILY
    // =====================================================================

    private void FinishDailySpin(
        LuckyWheelReward reward)
    {
        dailySpinning = false;
        dailySpinRoutine = null;

        SetRewardText(
            dailyRewardText,
            reward);

        if (dailyCenterText != null)
        {
            dailyCenterText.text =
                reward.displayName;
        }

        SetMessage(
            "Chúc mừng! Bạn đã nhận được " +
            reward.displayName +
            ".");

        SetSpinButtonsInteractable(true);

        RefreshDailyState();
    }


    // =====================================================================
    // FINISH VIP
    // =====================================================================

    private void FinishVipSpin(
        LuckyWheelReward reward)
    {
        vipSpinning = false;
        vipSpinRoutine = null;

        SetRewardText(
            vipRewardText,
            reward);

        if (vipCenterText != null)
        {
            vipCenterText.text =
                reward.displayName;
        }

        SetMessage(
            "Chúc mừng! Bạn đã nhận được " +
            reward.displayName +
            ".");

        SetSpinButtonsInteractable(true);

        RefreshVipState();
    }


    // =====================================================================
    // BUTTON STATE
    // =====================================================================

    private void SetSpinButtonsInteractable(
        bool value)
    {
        if (dailySpinButton != null)
        {
            dailySpinButton.interactable =
                value &&
                !dailySpinning;
        }

        if (vipSpinButton != null)
        {
            vipSpinButton.interactable =
                value &&
                !vipSpinning;
        }
    }


    // =====================================================================
    // EVENTS
    // =====================================================================

    private void HandleTicketsChanged(
        LuckyWheelType type,
        int value)
    {
        if (type == LuckyWheelType.Daily)
        {
            RefreshDailyState();
        }
    }

    private void HandleRewardGranted(
        LuckyWheelType type,
        LuckyWheelReward reward)
    {
        if (reward == null)
            return;

        if (type == LuckyWheelType.Daily)
        {
            RefreshDailyState();
        }
        else
        {
            RefreshVipState();
        }
    }


    // =====================================================================
    // REFRESH
    // =====================================================================

    public void RefreshNow()
    {
        RefreshAll();
    }

    private void RefreshAll()
    {
        RefreshDailyState();
        RefreshVipState();
    }


    // =====================================================================
    // REFRESH DAILY
    // =====================================================================

    private void RefreshDailyState()
    {
        if (wheelManager == null)
            ResolveManagers();

        if (wheelManager == null)
            return;

        int tickets =
            wheelManager.GetDailyTickets();

        if (dailyTicketText != null)
        {
            dailyTicketText.text =
                "Lượt quay: " +
                tickets;
        }

        if (dailySpinButton != null)
        {
            dailySpinButton.interactable =
                tickets > 0 &&
                !dailySpinning;
        }
    }


    // =====================================================================
    // REFRESH VIP
    // =====================================================================

    private void RefreshVipState()
    {
        if (wheelManager == null)
            ResolveManagers();

        if (gameManager == null)
            gameManager =
                GameManager.Instance;

        if (
            wheelManager == null ||
            gameManager == null
        )
        {
            return;
        }

        int gem =
            gameManager.GetGem();

        bool canAffordVipSpin =
            gem >=
            LuckyWheelManager.VipSpinCostGem;

        if (vipGemText != null)
        {
            vipGemText.text =
                "💎 " +
                gem +
                " Gem";
        }

        if (vipSpinButton != null)
        {
            vipSpinButton.interactable =
                canAffordVipSpin &&
                wheelManager.CanSpinVip() &&
                !vipSpinning;
        }
    }


    // =====================================================================
    // REWARD TEXT
    // =====================================================================

    private static void SetRewardText(
        TMP_Text target,
        LuckyWheelReward reward)
    {
        if (
            target == null ||
            reward == null
        )
        {
            return;
        }

        target.text =
            "🎁 " +
            reward.displayName;
    }


    // =====================================================================
    // MESSAGE
    // =====================================================================

    private void SetMessage(
        string message)
    {
        if (globalMessageText != null)
        {
            globalMessageText.text =
                message;
        }
    }


    // =====================================================================
    // STOP
    // =====================================================================

    private void StopSpinAnimations()
    {
        if (dailySpinRoutine != null)
        {
            StopCoroutine(
                dailySpinRoutine);

            dailySpinRoutine = null;
        }

        if (vipSpinRoutine != null)
        {
            StopCoroutine(
                vipSpinRoutine);

            vipSpinRoutine = null;
        }

        dailySpinning = false;
        vipSpinning = false;
    }


    // =====================================================================
    // POINTER
    // =====================================================================

    private void UpdatePointerTick(
        RectTransform pointer,
        float previousRotation,
        float currentRotation)
    {
        float previousSector =
            Mathf.Floor(
                previousRotation /
                pointerTickThreshold);

        float currentSector =
            Mathf.Floor(
                currentRotation /
                pointerTickThreshold);

        if (previousSector == currentSector)
            return;


        // Presentation-only.
        // Không allocation.
        pointer.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                pointerTickAngle);

        pointer.localRotation =
            Quaternion.identity;
    }


    // =====================================================================
    // AUTO FIND REFERENCES
    // =====================================================================

    private void AutoFindReferences()
    {
        if (dailyPanel == null)
        {
            dailyPanel =
                FindChildObject(
                    "DailyWheelPanel");
        }

        if (vipPanel == null)
        {
            vipPanel =
                FindChildObject(
                    "VIPWheelPanel");
        }

        if (dailySpinButton == null)
        {
            dailySpinButton =
                FindChildButton(
                    "DailySpinButton");
        }

        if (vipSpinButton == null)
        {
            vipSpinButton =
                FindChildButton(
                    "VIPSpinButton");
        }

        if (dailyCloseButton == null)
        {
            dailyCloseButton =
                FindChildButton(
                    "DailyCloseButton");
        }

        if (vipCloseButton == null)
        {
            vipCloseButton =
                FindChildButton(
                    "VIPCloseButton");
        }

        if (dailyTicketText == null)
        {
            dailyTicketText =
                FindChildTMP(
                    "DailyTicketText");
        }

        if (vipGemText == null)
        {
            vipGemText =
                FindChildTMP(
                    "VIPGemText");
        }

        if (dailyRewardText == null)
        {
            dailyRewardText =
                FindChildTMP(
                    "DailyRewardText");
        }

        if (vipRewardText == null)
        {
            vipRewardText =
                FindChildTMP(
                    "VIPRewardText");
        }

        if (globalMessageText == null)
        {
            globalMessageText =
                FindChildTMP(
                    "LuckyWheelMessageText");
        }

        if (dailyWheel == null)
        {
            dailyWheel =
                FindChildRectTransform(
                    "DailyWheel");
        }

        if (vipWheel == null)
        {
            vipWheel =
                FindChildRectTransform(
                    "VIPWheel");
        }

        if (dailyPointer == null)
        {
            dailyPointer =
                FindChildRectTransform(
                    "DailyPointer");
        }

        if (vipPointer == null)
        {
            vipPointer =
                FindChildRectTransform(
                    "VIPPointer");
        }

        if (dailyCenterText == null)
        {
            dailyCenterText =
                FindChildTMP(
                    "DailyCenterText");
        }

        if (vipCenterText == null)
        {
            vipCenterText =
                FindChildTMP(
                    "VIPCenterText");
        }
    }


    // =====================================================================
    // FIND OBJECT
    // =====================================================================

    private GameObject FindChildObject(
        string objectName)
    {
        Transform[] transforms =
            GetComponentsInChildren<Transform>(
                true);

        for (
            int i = 0;
            i < transforms.Length;
            i++)
        {
            Transform candidate =
                transforms[i];

            if (candidate.name == objectName)
            {
                return candidate.gameObject;
            }
        }

        return null;
    }


    // =====================================================================
    // FIND BUTTON
    // =====================================================================

    private Button FindChildButton(
        string objectName)
    {
        GameObject obj =
            FindChildObject(
                objectName);

        return obj != null
            ? obj.GetComponent<Button>()
            : null;
    }


    // =====================================================================
    // FIND TMP
    // =====================================================================

    private TMP_Text FindChildTMP(
        string objectName)
    {
        GameObject obj =
            FindChildObject(
                objectName);

        return obj != null
            ? obj.GetComponent<TMP_Text>()
            : null;
    }


    // =====================================================================
    // FIND RECT TRANSFORM
    // =====================================================================

    private RectTransform FindChildRectTransform(
        string objectName)
    {
        GameObject obj =
            FindChildObject(
                objectName);

        return obj != null
            ? obj.GetComponent<RectTransform>()
            : null;
    }


    // =====================================================================
    // MATH - NORMALIZE ANGLE
    // =====================================================================

    private static float NormalizeAngle(
        float angle)
    {
        angle %= 360f;

        if (angle < 0f)
            angle += 360f;

        return angle;
    }


    // =====================================================================
    // EASE OUT CUBIC
    // =====================================================================

    private static float EaseOutCubic(
        float t)
    {
        float inverse =
            1f - t;

        return
            1f -
            inverse *
            inverse *
            inverse;
    }


    // =====================================================================
    // EASE OUT BACK
    // =====================================================================

    private static float EaseOutBack(
        float t)
    {
        const float c1 =
            1.70158f;

        const float c3 =
            c1 + 1f;

        float x =
            t - 1f;

        return
            1f +
            c3 * x * x * x +
            c1 * x * x;
    }

    // =====================================================================
// REWARD LABELS
// =====================================================================

private void ApplyRewardLabels()
{
    if (wheelManager == null)
        ResolveManagers();

    if (wheelManager == null)
        return;

    SetWheelSectorLabels(
        dailyWheel,
        wheelManager.DailyRewards);

    SetWheelSectorLabels(
        vipWheel,
        wheelManager.VipRewards);
}


private static void SetWheelSectorLabels(
    RectTransform wheel,
    IReadOnlyList<LuckyWheelReward> rewards)
{
    if (wheel == null || rewards == null)
        return;

    int rewardCount = rewards.Count;

    int childCount =
        wheel.childCount;

    int count =
        rewardCount < childCount
            ? rewardCount
            : childCount;

    for (int i = 0; i < count; i++)
    {
        Transform sector =
            wheel.GetChild(i);

        if (sector == null)
            continue;

        Transform label =
            sector.Find("Label");

        if (label == null)
            continue;

        TMP_Text text =
            label.GetComponent<TMP_Text>();

        if (text == null)
            continue;

        LuckyWheelReward reward =
            rewards[i];

        if (reward == null)
        {
            text.text = string.Empty;
            continue;
        }

        text.text =
            FormatRewardLabel(reward);
    }
}


private static string FormatRewardLabel(
    LuckyWheelReward reward)
{
    if (reward == null)
        return string.Empty;

    switch (reward.rewardType)
    {
        case LuckyRewardType.Gold:
            return FormatAmount(
                reward.amount,
                "Gold");

        case LuckyRewardType.Exp:
            return FormatAmount(
                reward.amount,
                "EXP");

        case LuckyRewardType.Gem:
            return FormatAmount(
                reward.amount,
                "Gem");

        case LuckyRewardType.ShopItem:
            return reward.displayName;

        default:
            return reward.displayName;
    }
}


private static string FormatAmount(
    int amount,
    string suffix)
{
    return
        amount.ToString("N0") +
        " " +
        suffix;
}

public void OpenLuckyWheelMenu()
{
    GameObject menu =
        FindChildObject("LuckyWheelMenuPanel");

    if (menu != null)
        menu.SetActive(true);

    // Khi mở menu chính thì đóng 2 wheel.
    if (dailyPanel != null)
        dailyPanel.SetActive(false);

    if (vipPanel != null)
        vipPanel.SetActive(false);
}

public void CloseLuckyWheelMenu()
{
    if (dailySpinning || vipSpinning)
        return;

    GameObject menu =
        FindChildObject("LuckyWheelMenuPanel");

    if (menu != null)
        menu.SetActive(false);
}
}