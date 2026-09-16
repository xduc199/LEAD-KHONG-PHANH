using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionUIController : MonoBehaviour
{
    //==============================================================
    // REFERENCES
    //==============================================================

    [Header("Mission UI")]
    [SerializeField] private Transform missionList;

    [Header("Mission Card")]
    [Tooltip("Có thể để trống. Script sẽ tự tìm MissionCardUI có UI con để làm template.")]
    [SerializeField] private MissionCardUI missionCardPrefab;

    [Header("Daily Reset")]
    [SerializeField] private TMP_Text dailyResetText;


    //==============================================================
    // RUNTIME
    //==============================================================

    private readonly List<MissionCardUI> spawnedCards =
        new List<MissionCardUI>();

    private MissionManager subscribedManager;

    private bool isSubscribed;

    private Coroutine bindCoroutine;

    // Card thật đang có UI đầy đủ trong MissionList.
    // Card này được giữ lại làm template.
    private MissionCardUI templateCard;


    //==============================================================
    // UNITY
    //==============================================================

    private void Awake()
    {
        AutoFindReferences();
    }


    private void OnEnable()
    {
        AutoFindReferences();

        StartBinding();

        RefreshAll();
    }


    private void Start()
    {
        AutoFindReferences();

        StartBinding();

        RefreshAll();
    }


    private void OnDisable()
    {
        StopBinding();

        UnsubscribeFromMissionManager();
    }


    private void OnDestroy()
    {
        StopBinding();

        UnsubscribeFromMissionManager();
    }


    //==============================================================
    // BINDING
    //==============================================================

    private void StartBinding()
    {
        StopBinding();

        bindCoroutine =
            StartCoroutine(
                BindMissionManagerRoutine()
            );
    }


    private void StopBinding()
    {
        if (bindCoroutine != null)
        {
            StopCoroutine(bindCoroutine);

            bindCoroutine = null;
        }
    }


    private IEnumerator BindMissionManagerRoutine()
    {
        TryBindMissionManager();

        RefreshAll();


        yield return null;


        TryBindMissionManager();

        RefreshAll();


        for (int i = 0; i < 10; i++)
        {
            if (isSubscribed)
            {
                break;
            }

            yield return null;

            TryBindMissionManager();

            RefreshAll();
        }


        bindCoroutine = null;
    }


    private void TryBindMissionManager()
    {
        MissionManager manager =
            MissionManager.Instance;


        if (manager == null)
        {
            return;
        }


        if (subscribedManager == manager &&
            isSubscribed)
        {
            return;
        }


        UnsubscribeFromMissionManager();


        subscribedManager =
            manager;


        subscribedManager.OnMissionProgressChanged +=
            HandleMissionProgressChanged;

        subscribedManager.OnMissionCompleted +=
            HandleMissionCompleted;

        subscribedManager.OnMissionClaimed +=
            HandleMissionClaimed;

        subscribedManager.OnDailyMissionsGenerated +=
            HandleDailyMissionsGenerated;


        isSubscribed = true;


        Debug.Log(
            "[MissionUIController] Đã kết nối MissionManager.",
            this
        );
    }


    private void UnsubscribeFromMissionManager()
    {
        if (subscribedManager != null)
        {
            subscribedManager.OnMissionProgressChanged -=
                HandleMissionProgressChanged;

            subscribedManager.OnMissionCompleted -=
                HandleMissionCompleted;

            subscribedManager.OnMissionClaimed -=
                HandleMissionClaimed;

            subscribedManager.OnDailyMissionsGenerated -=
                HandleDailyMissionsGenerated;
        }


        subscribedManager = null;

        isSubscribed = false;
    }


    //==============================================================
    // AUTO FIND REFERENCES
    //==============================================================

    private void AutoFindReferences()
    {
        if (missionList == null)
        {
            Transform target =
                FindDeepChild(
                    transform,
                    "MissionList"
                );

            if (target != null)
            {
                missionList =
                    target;
            }
        }


        if (dailyResetText == null)
        {
            Transform target =
                FindDeepChild(
                    transform,
                    "DailyResetText"
                );

            if (target != null)
            {
                dailyResetText =
                    target.GetComponent<TMP_Text>();
            }
        }


        FindMissionCardTemplate();
    }


    private Transform FindDeepChild(
        Transform parent,
        string childName)
    {
        if (parent == null)
        {
            return null;
        }


        for (int i = 0;
             i < parent.childCount;
             i++)
        {
            Transform child =
                parent.GetChild(i);


            if (child.name == childName)
            {
                return child;
            }


            Transform result =
                FindDeepChild(
                    child,
                    childName
                );


            if (result != null)
            {
                return result;
            }
        }


        return null;
    }


    //==============================================================
    // FIND TEMPLATE CARD
    //==============================================================

    private void FindMissionCardTemplate()
    {
        if (missionList == null)
        {
            return;
        }


        // Nếu Inspector đã có card prefab/template hợp lệ,
        // ưu tiên dùng nó.
        if (missionCardPrefab != null &&
            HasUsableMissionCardUI(
                missionCardPrefab
            ))
        {
            templateCard =
                missionCardPrefab;

            return;
        }


        // Tìm tất cả MissionCardUI trong MissionList.
        MissionCardUI[] cards =
            missionList.GetComponentsInChildren<MissionCardUI>(
                true
            );


        MissionCardUI firstUsableCard = null;


        for (int i = 0;
             i < cards.Length;
             i++)
        {
            MissionCardUI card =
                cards[i];


            if (card == null)
            {
                continue;
            }


            // Bỏ qua những clone rỗng.
            if (!HasUsableMissionCardUI(card))
            {
                continue;
            }


            // Nếu đây là card đang nằm trong spawnedCards
            // thì vẫn có thể dùng làm template.
            firstUsableCard =
                card;

            break;
        }


        if (firstUsableCard != null)
        {
            templateCard =
                firstUsableCard;

            missionCardPrefab =
                firstUsableCard;


            Debug.Log(
                $"[MissionUIController] " +
                $"Đã tìm được MissionCard làm TEMPLATE → " +
                $"{firstUsableCard.gameObject.name}",
                firstUsableCard
            );
        }
    }


    private bool HasUsableMissionCardUI(
        MissionCardUI card)
    {
        if (card == null)
        {
            return false;
        }


        Transform root =
            card.transform;


        if (root == null)
        {
            return false;
        }


        // Kiểm tra xem card có UI con thực sự hay không.
        TMP_Text[] texts =
            root.GetComponentsInChildren<TMP_Text>(
                true
            );


        Button[] buttons =
            root.GetComponentsInChildren<Button>(
                true
            );


        Slider[] sliders =
            root.GetComponentsInChildren<Slider>(
                true
            );


        Image[] images =
            root.GetComponentsInChildren<Image>(
                true
            );


        // Một MissionCard hoàn chỉnh tối thiểu phải có
        // Text + Button.
        bool hasText =
            texts != null &&
            texts.Length > 0;


        bool hasButton =
            buttons != null &&
            buttons.Length > 0;


        bool hasVisual =
            (sliders != null &&
             sliders.Length > 0)
            ||
            (images != null &&
             images.Length > 0);


        return
            hasText &&
            hasButton &&
            hasVisual;
    }


    //==============================================================
    // REFRESH ALL
    //==============================================================

    public void RefreshAll()
    {
        AutoFindReferences();


        MissionManager manager =
            MissionManager.Instance;


        if (manager == null)
        {
            return;
        }


        List<MissionManager.Mission> missionData =
            manager.GetMissions();


        if (missionData == null)
        {
            return;
        }


        Debug.Log(
            $"[MissionUIController] RefreshAll → " +
            $"Mission Count = {missionData.Count}",
            this
        );


        EnsureCardCount(
            missionData.Count
        );


        //==========================================================
        // SET DATA
        //==========================================================

        int count =
            Mathf.Min(
                spawnedCards.Count,
                missionData.Count
            );


        for (int i = 0;
             i < count;
             i++)
        {
            MissionCardUI card =
                spawnedCards[i];

            MissionManager.Mission mission =
                missionData[i];


            if (card == null ||
                mission == null)
            {
                continue;
            }


            card.gameObject.SetActive(true);


            SetupCard(
                card,
                mission
            );
        }


        //==========================================================
        // HIDE EXTRA CARDS
        //==========================================================

        for (int i = count;
             i < spawnedCards.Count;
             i++)
        {
            if (spawnedCards[i] != null)
            {
                spawnedCards[i]
                    .gameObject
                    .SetActive(false);
            }
        }


        UpdateDailyResetText();
    }


    //==============================================================
    // ENSURE CARD COUNT
    //==============================================================

    private void EnsureCardCount(
        int requiredCount)
    {
        if (requiredCount <= 0)
        {
            return;
        }


        if (missionList == null)
        {
            Debug.LogError(
                "[MissionUIController] " +
                "Không tìm thấy MissionList.",
                this
            );

            return;
        }


        CacheExistingCards();


        Debug.Log(
            $"[MissionUIController] " +
            $"EnsureCardCount → " +
            $"Existing={spawnedCards.Count} | " +
            $"Required={requiredCount}",
            this
        );


        if (spawnedCards.Count >= requiredCount)
        {
            return;
        }


        //==========================================================
        // TÌM TEMPLATE LẦN CUỐI
        //==========================================================

        FindMissionCardTemplate();


        if (templateCard == null)
        {
            Debug.LogError(
                "[MissionUIController] " +
                "Không tìm thấy MissionCard có UI đầy đủ " +
                "để làm template.",
                this
            );

            return;
        }


        //==========================================================
        // CLONE TỪ TEMPLATE
        //==========================================================

        while (
            spawnedCards.Count <
            requiredCount)
        {
            MissionCardUI newCard =
                Instantiate(
                    templateCard,
                    missionList,
                    false
                );


            if (newCard == null)
            {
                Debug.LogError(
                    "[MissionUIController] " +
                    "Instantiate MissionCardUI thất bại.",
                    this
                );

                break;
            }


            newCard.gameObject.name =
                $"MissionCard_{spawnedCards.Count + 1}";


            newCard.gameObject.SetActive(true);


            RectTransform rect =
                newCard.GetComponent<RectTransform>();


            if (rect != null)
            {
                rect.localScale =
                    Vector3.one;

                rect.localRotation =
                    Quaternion.identity;


                LayoutElement layout =
                    newCard.GetComponent<LayoutElement>();


                if (layout == null)
                {
                    layout =
                        newCard.gameObject
                            .AddComponent<LayoutElement>();
                }


                if (layout.preferredHeight <= 0f)
                {
                    layout.preferredHeight =
                        rect.rect.height > 0f
                            ? rect.rect.height
                            : 150f;
                }


                layout.flexibleWidth =
                    1f;
            }


            spawnedCards.Add(
                newCard
            );


            Debug.Log(
                $"[MissionUIController] " +
                $"Đã tạo Card từ TEMPLATE → " +
                $"{newCard.gameObject.name} | " +
                $"Index={spawnedCards.Count - 1}",
                newCard
            );
        }


        SortCards();
    }


    //==============================================================
    // CACHE EXISTING CARDS
    //==============================================================

    private void CacheExistingCards()
    {
        if (missionList == null)
        {
            return;
        }


        MissionCardUI[] cards =
            missionList.GetComponentsInChildren<MissionCardUI>(
                true
            );


        for (int i = 0;
             i < cards.Length;
             i++)
        {
            MissionCardUI card =
                cards[i];


            if (card == null)
            {
                continue;
            }


            if (!spawnedCards.Contains(card))
            {
                spawnedCards.Add(
                    card
                );
            }
        }


        SortCards();
    }


    private void SortCards()
    {
        spawnedCards.Sort(
            CompareCardHierarchyOrder
        );
    }


    private int CompareCardHierarchyOrder(
        MissionCardUI a,
        MissionCardUI b)
    {
        if (a == null && b == null)
        {
            return 0;
        }


        if (a == null)
        {
            return 1;
        }


        if (b == null)
        {
            return -1;
        }


        return
            a.transform.GetSiblingIndex()
            .CompareTo(
                b.transform.GetSiblingIndex()
            );
    }


    //==============================================================
    // SETUP CARD
    //==============================================================

    private void SetupCard(
        MissionCardUI card,
        MissionManager.Mission mission)
    {
        if (card == null ||
            mission == null)
        {
            return;
        }


        Debug.Log(
            $"[MissionUIController] " +
            $"Bắt đầu SetupCard → " +
            $"{mission.id}",
            card
        );


        card.Setup(
            mission.id
        );


        card.SetData(
            mission.title,
            mission.description,
            mission.CurrentProgress,
            mission.target,
            mission.expReward,
            mission.IsCompleted,
            mission.claimed
        );


        RectTransform rect =
            card.GetComponent<RectTransform>();


        if (rect != null)
        {
            Debug.Log(
                $"[MissionUIController] " +
                $"CARD SETUP → " +
                $"ID={mission.id} | " +
                $"Name={card.gameObject.name} | " +
                $"Active={card.gameObject.activeSelf} | " +
                $"Hierarchy={card.gameObject.activeInHierarchy} | " +
                $"Pos={rect.anchoredPosition} | " +
                $"Size={rect.rect.size} | " +
                $"Scale={rect.localScale}",
                card
            );
        }


        card.DebugReferences();
    }


    //==============================================================
    // FIND CARD
    //==============================================================

    private MissionCardUI FindCard(
        string missionId)
    {
        if (string.IsNullOrEmpty(missionId))
        {
            return null;
        }


        for (int i = 0;
             i < spawnedCards.Count;
             i++)
        {
            MissionCardUI card =
                spawnedCards[i];


            if (card == null)
            {
                continue;
            }


            if (card.MissionId ==
                missionId)
            {
                return card;
            }
        }


        return null;
    }


    //==============================================================
    // EVENTS
    //==============================================================

    private void HandleMissionProgressChanged(
        MissionManager.Mission mission)
    {
        RefreshMission(
            mission
        );
    }


    private void HandleMissionCompleted(
        MissionManager.Mission mission)
    {
        RefreshMission(
            mission
        );
    }


    private void HandleMissionClaimed(
        MissionManager.Mission mission)
    {
        RefreshMission(
            mission
        );
    }


    private void HandleDailyMissionsGenerated()
    {
        RefreshAll();
    }


    //==============================================================
    // REFRESH SINGLE MISSION
    //==============================================================

    private void RefreshMission(
        MissionManager.Mission mission)
    {
        if (mission == null)
        {
            return;
        }


        MissionCardUI card =
            FindCard(
                mission.id
            );


        if (card == null)
        {
            RefreshAll();

            return;
        }


        card.gameObject.SetActive(true);


        SetupCard(
            card,
            mission
        );
    }


    //==============================================================
    // DAILY RESET
    //==============================================================

    private void UpdateDailyResetText()
    {
        if (dailyResetText == null)
        {
            return;
        }


        System.DateTime tomorrow =
            System.DateTime.Today.AddDays(1);


        System.TimeSpan remaining =
            tomorrow -
            System.DateTime.Now;


        int totalMinutes =
            Mathf.Max(
                0,
                Mathf.FloorToInt(
                    (float)remaining.TotalMinutes
                )
            );


        int hours =
            totalMinutes / 60;


        int minutes =
            totalMinutes % 60;


        dailyResetText.text =
            $"Nhiệm vụ mới sau {hours:00}h {minutes:00}m";
    }
}