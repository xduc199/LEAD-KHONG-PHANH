using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionCardUI : MonoBehaviour
{
    //=========================================================
    // UI REFERENCES
    //=========================================================

    private TMP_Text titleText;
    private TMP_Text descriptionText;
    private TMP_Text progressText;

    private Slider progressBar;
    private Image progressFill;

    private TMP_Text rewardText;

    private Button claimButton;
    private TMP_Text claimButtonText;

    private GameObject disabledOverlay;
    private GameObject completedBadge;

    private string missionId;

    public string MissionId => missionId;


    //=========================================================
    // UNITY
    //=========================================================

    private void Awake()
    {
        CacheReferences();
        FixRaycastBlocking();
    }


    //=========================================================
    // AUTO FIND UI
    //=========================================================

    private void CacheReferences()
    {
        titleText =
            FindTMPDeep("Title");

        descriptionText =
            FindTMPDeep("Description");

        progressText =
            FindTMPDeep("ProgressText");

        rewardText =
            FindTMPDeep("Reward/RewardValue");

        claimButton =
            FindButtonDeep("ClaimButton");

        if (claimButton != null)
        {
            claimButtonText =
                FindTMPDeepFrom(
                    claimButton.transform,
                    "Text"
                );
        }

        disabledOverlay =
            FindGameObjectDeep("DisabledOverlay");

        completedBadge =
            FindGameObjectDeep("CompletedBadge");


        //=====================================================
        // SLIDER
        //=====================================================

        progressBar =
            GetComponentInChildren<Slider>(true);


        //=====================================================
        // IMAGE FILL
        //=====================================================

        progressFill = null;

        if (progressBar == null)
        {
            Transform progressBarTransform =
                FindDeepChild(
                    transform,
                    "ProgressBar"
                );

            if (progressBarTransform != null)
            {
                Transform fillChild =
                    FindDeepChild(
                        progressBarTransform,
                        "Fill"
                    );

                if (fillChild != null)
                {
                    progressFill =
                        fillChild.GetComponent<Image>();
                }
            }
        }
    }


    //=========================================================
    // FIX RAYCAST BLOCKING
    //=========================================================

    private void FixRaycastBlocking()
    {
        //=====================================================
        // DISABLED OVERLAY
        //=====================================================

        if (disabledOverlay != null)
        {
            Image overlayImage =
                disabledOverlay.GetComponent<Image>();

            if (overlayImage != null)
            {
                overlayImage.raycastTarget = false;
            }

            CanvasGroup overlayGroup =
                disabledOverlay.GetComponent<CanvasGroup>();

            if (overlayGroup != null)
            {
                overlayGroup.blocksRaycasts = false;
                overlayGroup.interactable = false;
            }
        }


        //=====================================================
        // COMPLETED BADGE
        //=====================================================

        if (completedBadge != null)
        {
            Image badgeImage =
                completedBadge.GetComponent<Image>();

            if (badgeImage != null)
            {
                badgeImage.raycastTarget = false;
            }

            CanvasGroup badgeGroup =
                completedBadge.GetComponent<CanvasGroup>();

            if (badgeGroup != null)
            {
                badgeGroup.blocksRaycasts = false;
                badgeGroup.interactable = false;
            }

            TMP_Text badgeText =
                completedBadge.GetComponentInChildren<TMP_Text>(
                    true
                );

            if (badgeText != null)
            {
                badgeText.raycastTarget = false;
            }
        }


        //=====================================================
        // OTHER TEXT
        //=====================================================

        TMP_Text[] allTexts =
            GetComponentsInChildren<TMP_Text>(
                true
            );

        for (int i = 0;
             i < allTexts.Length;
             i++)
        {
            if (allTexts[i] != null)
            {
                allTexts[i].raycastTarget = false;
            }
        }


        //=====================================================
        // CLAIM BUTTON TEXT
        //=====================================================

        if (claimButtonText != null)
        {
            claimButtonText.raycastTarget = false;
        }
    }


    //=========================================================
    // FIND TMP
    //=========================================================

    private TMP_Text FindTMPDeep(string path)
    {
        Transform target =
            FindDeepPath(
                transform,
                path
            );

        if (target == null)
        {
            return null;
        }

        return target.GetComponent<TMP_Text>();
    }


    private TMP_Text FindTMPDeepFrom(
        Transform parent,
        string objectName)
    {
        if (parent == null)
        {
            return null;
        }

        Transform target =
            FindDeepChild(
                parent,
                objectName
            );

        if (target == null)
        {
            return null;
        }

        return target.GetComponent<TMP_Text>();
    }


    //=========================================================
    // FIND BUTTON
    //=========================================================

    private Button FindButtonDeep(
        string objectName)
    {
        Transform target =
            FindDeepChild(
                transform,
                objectName
            );

        if (target == null)
        {
            return null;
        }

        return target.GetComponent<Button>();
    }


    //=========================================================
    // FIND GAMEOBJECT
    //=========================================================

    private GameObject FindGameObjectDeep(
        string objectName)
    {
        Transform target =
            FindDeepChild(
                transform,
                objectName
            );

        return target != null
            ? target.gameObject
            : null;
    }


    //=========================================================
    // FIND DEEP CHILD
    //=========================================================

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


    //=========================================================
    // FIND DEEP PATH
    //=========================================================

    private Transform FindDeepPath(
        Transform parent,
        string path)
    {
        if (parent == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(path))
        {
            return parent;
        }

        string[] parts =
            path.Split('/');

        Transform current =
            parent;

        for (int i = 0;
             i < parts.Length;
             i++)
        {
            Transform next =
                current.Find(parts[i]);

            if (next == null)
            {
                next =
                    FindDeepChild(
                        current,
                        parts[i]
                    );
            }

            if (next == null)
            {
                return null;
            }

            current = next;
        }

        return current;
    }


    //=========================================================
    // SETUP
    //=========================================================

    public void Setup(string id)
    {
        missionId = id;

        CacheReferences();
        FixRaycastBlocking();

        if (claimButton != null)
        {
            claimButton.onClick.RemoveListener(
                OnClaimClicked
            );

            claimButton.onClick.AddListener(
                OnClaimClicked
            );
        }
        else
        {
            Debug.LogError(
                $"[MissionCardUI] Không tìm thấy ClaimButton " +
                $"trên card {gameObject.name}.",
                this
            );
        }
    }


    //=========================================================
    // DATA
    //=========================================================

    public void SetData(
        string title,
        string description,
        int currentProgress,
        int targetProgress,
        int expReward,
        bool completed,
        bool claimed)
    {
        CacheReferences();
        FixRaycastBlocking();


        //=====================================================
        // TEXT
        //=====================================================

        if (titleText != null)
        {
            titleText.text =
                title;
        }

        if (descriptionText != null)
        {
            descriptionText.text =
                description;
        }

        if (progressText != null)
        {
            progressText.text =
                $"{currentProgress} / {targetProgress}";
        }

        if (rewardText != null)
        {
            rewardText.text =
                $"+{expReward} EXP";
        }


        //=====================================================
        // PROGRESS
        //=====================================================

        UpdateProgress(
            currentProgress,
            targetProgress
        );


        //=====================================================
        // BUTTON
        //=====================================================

        UpdateClaimButton(
            completed,
            claimed
        );
    }


    //=========================================================
    // PROGRESS
    //=========================================================

    private void UpdateProgress(
        int current,
        int target)
    {
        float normalized =
            target > 0
                ? Mathf.Clamp01(
                    (float)current /
                    target
                )
                : 0f;


        //=====================================================
        // SLIDER
        //=====================================================

        if (progressBar != null)
        {
            progressBar.minValue =
                0f;

            progressBar.maxValue =
                1f;

            progressBar.value =
                normalized;
        }


        //=====================================================
        // IMAGE FILL
        //=====================================================

        if (progressFill != null)
        {
            RectTransform fillRect =
                progressFill.rectTransform;

            fillRect.anchorMin =
                new Vector2(
                    0f,
                    0f
                );

            fillRect.anchorMax =
                new Vector2(
                    normalized,
                    1f
                );

            fillRect.offsetMin =
                Vector2.zero;

            fillRect.offsetMax =
                Vector2.zero;
        }
    }


    //=========================================================
    // CLAIM
    //=========================================================

    private void OnClaimClicked()
    {
        Debug.Log(
            $"[MissionCardUI] CLICK NHẬN → " +
            $"MissionID={missionId} | " +
            $"Card={gameObject.name}",
            this
        );


        if (string.IsNullOrEmpty(missionId))
        {
            Debug.LogWarning(
                "[MissionCardUI] Mission ID chưa được thiết lập.",
                this
            );

            return;
        }


        if (MissionManager.Instance == null)
        {
            Debug.LogError(
                "[MissionCardUI] Không tìm thấy MissionManager.Instance.",
                this
            );

            return;
        }


        Debug.Log(
            $"[MissionCardUI] Gọi ClaimMission('{missionId}')",
            this
        );


        MissionManager.Instance.ClaimMission(
            missionId
        );
    }


    //=========================================================
    // BUTTON STATE
    //=========================================================

    public void UpdateClaimButton(
        bool completed,
        bool claimed)
    {
        CacheReferences();
        FixRaycastBlocking();


        if (claimButton != null)
        {
            claimButton.interactable =
                completed &&
                !claimed;
        }


        if (claimButtonText != null)
        {
            if (claimed)
            {
                claimButtonText.text =
                    "ĐÃ NHẬN";
            }
            else if (completed)
            {
                claimButtonText.text =
                    "NHẬN";
            }
            else
            {
                claimButtonText.text =
                    "CHƯA ĐẠT";
            }
        }


        if (disabledOverlay != null)
        {
            disabledOverlay.SetActive(
                !completed &&
                !claimed
            );

            Image overlayImage =
                disabledOverlay.GetComponent<Image>();

            if (overlayImage != null)
            {
                overlayImage.raycastTarget = false;
            }
        }


        if (completedBadge != null)
        {
            completedBadge.SetActive(
                completed &&
                !claimed
            );

            Image badgeImage =
                completedBadge.GetComponent<Image>();

            if (badgeImage != null)
            {
                badgeImage.raycastTarget = false;
            }
        }
    }


    //=========================================================
    // DEBUG
    //=========================================================

    public void DebugReferences()
    {
        Debug.Log(
            $"[MissionCardUI] DEBUG → " +
            $"Name={gameObject.name} | " +
            $"Active={gameObject.activeSelf} | " +
            $"Hierarchy={gameObject.activeInHierarchy} | " +
            $"MissionID={missionId} | " +
            $"Title={(titleText != null)} | " +
            $"Description={(descriptionText != null)} | " +
            $"ProgressText={(progressText != null)} | " +
            $"ProgressBar={(progressBar != null)} | " +
            $"ProgressFill={(progressFill != null)} | " +
            $"Reward={(rewardText != null)} | " +
            $"Button={(claimButton != null)} | " +
            $"ButtonText={(claimButtonText != null)} | " +
            $"ButtonInteractable={(claimButton != null && claimButton.interactable)}",
            this
        );
    }
}