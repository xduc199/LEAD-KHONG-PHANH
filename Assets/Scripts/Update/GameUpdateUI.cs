using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LEADKhongPhanh.Update
{
    [DisallowMultipleComponent]
    public sealed class GameUpdateUI : MonoBehaviour
    {
        private const string GeneratedRootName = "UpdateNotification";
        private const float PanelWidth = 720f;
        private const float PanelHeight = 560f;

        private UpdateManager manager;
        private GameObject root;
        private Text titleText;
        private Text currentVersionText;
        private Text latestVersionText;
        private Text releaseNotesText;
        private Text statusText;
        private Text progressText;
        private Slider progressBar;
        private Button updateButton;
        private Button laterButton;
        private Button cancelButton;
        private readonly List<RaycastResult> raycastResults =
            new List<RaycastResult>(16);
        private bool visibleRaycastValidationComplete;

        private void Awake()
        {
            Debug.Log("[GameUpdateUI] Awake", this);
            manager = GetComponent<UpdateManager>();
            BuildUiIfMissing();
            if (root != null)
                root.SetActive(false);

            if (updateButton != null)
                updateButton.onClick.AddListener(OnUpdateClicked);
            if (laterButton != null)
                laterButton.onClick.AddListener(OnLaterClicked);
            if (cancelButton != null && manager != null)
                cancelButton.onClick.AddListener(manager.CancelDownload);

            Debug.Log("[GameUpdateUI] Initialize", this);
            ValidateRuntimeWiring();
        }

        private void OnEnable()
        {
            if (manager == null)
                return;

            manager.StateChanged += OnStateChanged;
            manager.ProgressChanged += OnProgressChanged;
            Refresh(manager);
        }

        private void OnDisable()
        {
            if (manager == null)
                return;

            manager.StateChanged -= OnStateChanged;
            manager.ProgressChanged -= OnProgressChanged;
        }

        private void OnDestroy()
        {
            if (updateButton != null)
                updateButton.onClick.RemoveListener(OnUpdateClicked);
            if (laterButton != null)
                laterButton.onClick.RemoveListener(OnLaterClicked);
            if (cancelButton != null && manager != null)
                cancelButton.onClick.RemoveListener(manager.CancelDownload);
        }

        private async void OnUpdateClicked()
        {
            Debug.Log("[GameUpdateUI] Update button clicked", this);
            if (manager == null)
            {
                Debug.LogError(
                    "[GameUpdateUI] UpdateManager is not assigned on this GameObject.",
                    this);
                return;
            }

            if (manager.State == UpdateState.ReadyToInstall)
            {
                Debug.Log("[GameUpdateUI] Apply/Install requested", this);
                await manager.InstallUpdateAsync();
            }
            else if (manager.State == UpdateState.Failed &&
                     manager.Manifest == null)
                await manager.CheckForUpdateAsync();
            else
                await manager.DownloadUpdateAsync();
        }

        private void OnLaterClicked()
        {
            Debug.Log("[GameUpdateUI] Later button clicked", this);
            if (manager == null)
                return;

            if (!manager.IsForcedUpdate)
                root.SetActive(false);
        }

        private void OnStateChanged(UpdateManager source)
        {
            Refresh(source);
        }

        private void OnProgressChanged(
            UpdateManager source,
            UpdateDownloadProgress progress)
        {
            if (source.State != UpdateState.Downloading)
                return;

            progressBar.value = progress.Progress01;
            progressText.text =
                FormatBytes(progress.DownloadedBytes) +
                " / " +
                FormatBytes(progress.TotalBytes) +
                "  (" +
                Mathf.RoundToInt(progress.Progress01 * 100f) +
                "%)";
        }

        private void Refresh(UpdateManager source)
        {
            if (root == null)
                return;

            switch (source.State)
            {
                case UpdateState.UpdateAvailable:
                    titleText.text = "CÓ PHIÊN BẢN MỚI";
                    currentVersionText.text =
                        "Phiên bản hiện tại: " + source.CurrentVersion;
                    latestVersionText.text =
                        "Phiên bản mới: " + source.Manifest.Version;
                    releaseNotesText.text =
                        source.Manifest.ReleaseNotes;
                    statusText.text = source.IsForcedUpdate
                        ? "Dung lượng tải: " +
                          FormatBytes(source.Manifest.FileSize) +
                          "\nBản cập nhật này là bắt buộc."
                        : "Dung lượng tải: " +
                          FormatBytes(source.Manifest.FileSize);
                    SetButtonLabel(updateButton, "CẬP NHẬT NGAY");
                    SetButtons(update: true, later: !source.IsForcedUpdate,
                        cancel: false);
                    progressBar.gameObject.SetActive(false);
                    progressText.gameObject.SetActive(false);
                    root.SetActive(true);
                    ValidateVisibleInteractionPath();
                    break;

                case UpdateState.Downloading:
                    titleText.text = "ĐANG TẢI...";
                    statusText.text = "Đang tải gói cập nhật.";
                    SetButtons(update: false, later: false, cancel: true);
                    progressBar.gameObject.SetActive(true);
                    progressText.gameObject.SetActive(true);
                    root.SetActive(true);
                    break;

                case UpdateState.Verifying:
                    titleText.text = "ĐANG KIỂM TRA GÓI CẬP NHẬT...";
                    statusText.text =
                        "Đang xác minh kích thước và SHA-256.";
                    SetButtons(update: false, later: false, cancel: true);
                    progressBar.gameObject.SetActive(false);
                    progressText.gameObject.SetActive(false);
                    root.SetActive(true);
                    break;

                case UpdateState.ReadyToInstall:
                    titleText.text =
                        "GÓI CẬP NHẬT ĐÃ ĐƯỢC XÁC MINH";
                    statusText.text =
                        "Gói cập nhật đã sẵn sàng. " +
                        "Trò chơi sẽ khởi động lại sau khi cài đặt.";
                    SetButtonLabel(updateButton, "CÀI ĐẶT & KHỞI ĐỘNG LẠI");
                    SetButtons(update: true, later: !source.IsForcedUpdate,
                        cancel: false);
                    progressBar.gameObject.SetActive(false);
                    progressText.gameObject.SetActive(false);
                    root.SetActive(true);
                    ValidateVisibleInteractionPath();
                    break;

                case UpdateState.Installing:
                    titleText.text = "ĐANG CHUẨN BỊ CÀI ĐẶT";
                    statusText.text =
                        "Đang chuyển quyền cài đặt cho trình cập nhật an toàn.";
                    SetButtons(update: false, later: false, cancel: false);
                    progressBar.gameObject.SetActive(false);
                    progressText.gameObject.SetActive(false);
                    root.SetActive(true);
                    break;

                case UpdateState.Failed:
                    if (source.DownloadError != UpdateDownloadError.None)
                    {
                        titleText.text =
                            "KHÔNG THỂ TẢI BẢN CẬP NHẬT";
                        statusText.text =
                            "Vui lòng kiểm tra kết nối và thử lại.";
                        SetButtons(
                            update: source.Manifest != null,
                            later: !source.IsForcedUpdate,
                            cancel: false);
                        progressBar.gameObject.SetActive(false);
                        progressText.gameObject.SetActive(false);
                        root.SetActive(true);
                    }
                    else if (source.ManifestError != UpdateValidationError.None)
                    {
                        titleText.text = "KHÔNG THỂ KIỂM TRA CẬP NHẬT";
                        statusText.text = string.IsNullOrWhiteSpace(
                            source.FailureDetails)
                            ? "Lỗi: " + source.ManifestError
                            : "Lỗi: " + source.FailureDetails;
                        SetButtonLabel(updateButton, "THỬ LẠI");
                        SetButtons(update: true, later: true, cancel: false);
                        progressBar.gameObject.SetActive(false);
                        progressText.gameObject.SetActive(false);
                        root.SetActive(true);
                    }
                    else
                    {
                        root.SetActive(false);
                    }

                    break;

                case UpdateState.Cancelled:
                    titleText.text = "ĐÃ HỦY TẢI";
                    statusText.text = "Bạn có thể thử tải lại.";
                    SetButtons(
                        update: source.Manifest != null,
                        later: !source.IsForcedUpdate,
                        cancel: false);
                    progressBar.gameObject.SetActive(false);
                    progressText.gameObject.SetActive(false);
                    root.SetActive(source.Manifest != null);
                    break;

                default:
                    root.SetActive(false);
                    break;
            }
        }

        private void SetButtons(bool update, bool later, bool cancel)
        {
            updateButton.gameObject.SetActive(update);
            laterButton.gameObject.SetActive(later);
            cancelButton.gameObject.SetActive(cancel);
        }

        private static void SetButtonLabel(Button button, string label)
        {
            Transform labelTransform = button.transform.Find("Label");
            Text buttonText = labelTransform != null
                ? labelTransform.GetComponent<Text>()
                : null;
            if (buttonText != null)
                buttonText.text = label;
        }

        private void BuildUiIfMissing()
        {
            Transform existing = transform.Find(GeneratedRootName);
            if (existing != null)
            {
                root = existing.gameObject;
                titleText = FindText(existing, "Title");
                currentVersionText = FindText(existing, "CurrentVersion");
                latestVersionText = FindText(existing, "LatestVersion");
                releaseNotesText = FindText(existing, "ReleaseNotes");
                statusText = FindText(existing, "Status");
                progressText = FindText(existing, "ProgressText");
                progressBar = existing.GetComponentInChildren<Slider>(true);
                updateButton = FindButton(existing, "UpdateNow");
                laterButton = FindButton(existing, "Later");
                cancelButton = FindButton(existing, "Cancel");

                if (HasCompleteUi())
                    return;

                Destroy(existing.gameObject);
            }

            root = CreateRect(GeneratedRootName, transform);
            Stretch(root.GetComponent<RectTransform>());
            AddImage(root, new Color(0f, 0f, 0f, 0.66f));

            GameObject panel = CreateRect("Panel", root.transform);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            AddImage(panel, new Color(0.06f, 0.08f, 0.11f, 0.98f));

            titleText = AddText(
                "Title", panel.transform, "CÓ PHIÊN BẢN MỚI",
                30, new Vector2(0f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -46f), new Vector2(PanelWidth - 64f, 54f),
                FontStyle.Bold, TextAnchor.MiddleCenter);
            currentVersionText = AddText(
                "CurrentVersion", panel.transform, string.Empty,
                20, new Vector2(0.08f, 1f), new Vector2(0.92f, 1f),
                new Vector2(0f, -112f), new Vector2(0f, 36f),
                FontStyle.Normal, TextAnchor.MiddleLeft);
            latestVersionText = AddText(
                "LatestVersion", panel.transform, string.Empty,
                20, new Vector2(0.08f, 1f), new Vector2(0.92f, 1f),
                new Vector2(0f, -152f), new Vector2(0f, 36f),
                FontStyle.Normal, TextAnchor.MiddleLeft);
            AddText(
                "ReleaseNotesHeader", panel.transform, "Ghi chú phát hành",
                18, new Vector2(0.08f, 1f), new Vector2(0.92f, 1f),
                new Vector2(0f, -202f), new Vector2(0f, 30f),
                FontStyle.Bold, TextAnchor.MiddleLeft);
            releaseNotesText = AddText(
                "ReleaseNotes", panel.transform, string.Empty,
                16, new Vector2(0.08f, 1f), new Vector2(0.92f, 1f),
                new Vector2(0f, -226f), new Vector2(0f, 110f),
                FontStyle.Normal, TextAnchor.UpperLeft);
            statusText = AddText(
                "Status", panel.transform, string.Empty,
                16, new Vector2(0.08f, 0f), new Vector2(0.92f, 0f),
                new Vector2(0f, 174f), new Vector2(0f, 40f),
                FontStyle.Normal, TextAnchor.MiddleCenter);

            GameObject sliderObject = CreateRect(
                "ProgressBar",
                panel.transform);
            RectTransform sliderRect =
                sliderObject.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.12f, 0f);
            sliderRect.anchorMax = new Vector2(0.88f, 0f);
            sliderRect.pivot = new Vector2(0.5f, 0f);
            sliderRect.anchoredPosition = new Vector2(0f, 118f);
            sliderRect.sizeDelta = new Vector2(0f, 18f);
            progressBar = sliderObject.AddComponent<Slider>();
            progressBar.minValue = 0f;
            progressBar.maxValue = 1f;
            progressBar.value = 0f;
            Image sliderBackground = AddImage(
                sliderObject,
                new Color(0.2f, 0.23f, 0.27f, 1f));
            progressBar.targetGraphic = sliderBackground;
            GameObject fill = CreateRect("Fill", sliderObject.transform);
            Stretch(fill.GetComponent<RectTransform>());
            Image fillImage = AddImage(
                fill,
                new Color(0.92f, 0.62f, 0.04f, 1f));
            progressBar.fillRect = fill.GetComponent<RectTransform>();
            progressBar.direction = Slider.Direction.LeftToRight;

            progressText = AddText(
                "ProgressText", panel.transform, "0%",
                16, new Vector2(0.12f, 0f), new Vector2(0.88f, 0f),
                new Vector2(0f, 88f), new Vector2(0f, 28f),
                FontStyle.Normal, TextAnchor.MiddleCenter);

            updateButton = AddButton(
                "UpdateNow", panel.transform, "CẬP NHẬT NGAY",
                new Vector2(-130f, 38f));
            laterButton = AddButton(
                "Later", panel.transform, "ĐỂ SAU",
                new Vector2(130f, 38f));
            cancelButton = AddButton(
                "Cancel", panel.transform, "HỦY TẢI",
                new Vector2(0f, 38f));

            root.SetActive(false);
        }

        private bool HasCompleteUi()
        {
            return titleText != null &&
                   currentVersionText != null &&
                   latestVersionText != null &&
                   releaseNotesText != null &&
                   statusText != null &&
                   progressText != null &&
                   progressBar != null &&
                   updateButton != null &&
                   laterButton != null &&
                   cancelButton != null;
        }

        private void ValidateRuntimeWiring()
        {
            UpdateManager[] managers = UnityEngine.Object.FindObjectsByType<
                UpdateManager>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            GameUpdateUI[] updateUis = UnityEngine.Object.FindObjectsByType<
                GameUpdateUI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            EventSystem[] eventSystems = UnityEngine.Object.FindObjectsByType<
                EventSystem>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            Debug.Log(
                "[GameUpdateUI] Runtime counts: UpdateManager=" +
                managers.Length + "; GameUpdateUI=" + updateUis.Length +
                "; EventSystem=" + eventSystems.Length +
                "; activeInHierarchy=" + gameObject.activeInHierarchy,
                this);
            Canvas uiCanvas = GetComponentInParent<Canvas>();
            Debug.Log(
                "[GameUpdateUI] GameObject = " +
                GetHierarchyPath(transform) +
                "; activeInHierarchy=" + gameObject.activeInHierarchy +
                "; canvas=" + (uiCanvas == null ? "<none>" : uiCanvas.name) +
                "; sortingOrder=" +
                (uiCanvas == null ? -1 : uiCanvas.sortingOrder) +
                "; Time.timeScale=" + Time.timeScale,
                this);

            if (managers.Length != 1)
            {
                Debug.LogError(
                    "[GameUpdateUI] Expected exactly one UpdateManager; found " +
                    managers.Length + ".",
                    this);
            }
            if (root == null)
            {
                Debug.LogError(
                    "[GameUpdateUI] Update dialog root could not be created or found.",
                    this);
            }
            else if (manager != null)
            {
                Debug.Log(
                    "[GameUpdateUI] UpdateManager active=" +
                    manager.isActiveAndEnabled +
                    "; activeInHierarchy=" +
                    manager.gameObject.activeInHierarchy,
                    manager);
            }
            if (updateUis.Length != 1)
            {
                Debug.LogError(
                    "[GameUpdateUI] Expected exactly one GameUpdateUI; found " +
                    updateUis.Length + ".",
                    this);
            }
            if (eventSystems.Length != 1)
            {
                Debug.LogError(
                    "[GameUpdateUI] Expected exactly one EventSystem; found " +
                    eventSystems.Length + ".",
                    this);
            }
            if (manager == null)
            {
                Debug.LogError(
                    "[GameUpdateUI] Required UpdateManager is missing from " +
                    "the GameUpdateUI GameObject.",
                    this);
            }

            LogButtonWiring("Update", updateButton, nameof(OnUpdateClicked));
            LogButtonWiring("Later", laterButton, nameof(OnLaterClicked));

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                Debug.Log(
                    "[GameUpdateUI] EventSystem.current is not initialized " +
                    "yet; the active module will be checked when the dialog appears.",
                    this);
            }
            else
            {
                Debug.Log(
                    "[GameUpdateUI] EventSystem.current = " +
                    GetHierarchyPath(eventSystem.transform) +
                    "; enabled=" + eventSystem.enabled +
                    "; currentInputModule=" +
                    (eventSystem.currentInputModule == null
                        ? "<none>"
                        : eventSystem.currentInputModule.GetType().FullName),
                    this);

                BaseInputModule[] modules =
                    eventSystem.GetComponents<BaseInputModule>();
                int enabledModuleCount = 0;
                for (int index = 0; index < modules.Length; index++)
                {
                    BaseInputModule module = modules[index];
                    if (module.enabled && module.gameObject.activeInHierarchy)
                        enabledModuleCount++;
                    Debug.Log(
                        "[GameUpdateUI] Input module[" + index + "] type=" +
                        module.GetType().FullName +
                        "; enabled=" + module.enabled +
                        "; activeInHierarchy=" +
                        module.gameObject.activeInHierarchy +
                        "; isCurrent=" +
                        (eventSystem.currentInputModule == module),
                        module);
                }
                if (modules.Length != 1 || enabledModuleCount != 1)
                {
                    Debug.LogError(
                        "[GameUpdateUI] EventSystem input configuration: " +
                        "modules=" + modules.Length +
                        "; enabledModules=" + enabledModuleCount +
                        "; expected exactly one enabled module.",
                        eventSystem);
                }
            }
        }

        private void LogButtonWiring(
            string label,
            Button button,
            string runtimeListenerMethod)
        {
            if (button == null)
            {
                Debug.LogError(
                    "[GameUpdateUI] " + label + " button reference is null.",
                    this);
                return;
            }

            Image image = button.GetComponent<Image>();
            Graphic targetGraphic = button.targetGraphic;
            Canvas canvas = button.GetComponentInParent<Canvas>();
            Debug.Log(
                "[GameUpdateUI] " + label + " button: " +
                GetHierarchyPath(button.transform) +
                "; enabled=" + button.enabled +
                "; interactable=" + button.interactable +
                "; activeInHierarchy=" + button.gameObject.activeInHierarchy +
                "; imageEnabled=" + (image != null && image.enabled) +
                "; imageRaycastTarget=" +
                (image != null && image.raycastTarget) +
                "; targetGraphic=" +
                (targetGraphic == null ? "<none>" : targetGraphic.GetType().Name) +
                "; targetRaycastTarget=" +
                (targetGraphic != null && targetGraphic.raycastTarget) +
                "; persistentListeners=" +
                button.onClick.GetPersistentEventCount() +
                "; runtimeListener=GameUpdateUI." + runtimeListenerMethod +
                " (bound=true)" +
                "; canvas=" + (canvas == null ? "<none>" : canvas.name) +
                "; sortingOrder=" + (canvas == null ? -1 : canvas.sortingOrder),
                button);

            if (!button.enabled)
            {
                Debug.LogError(
                    "[GameUpdateUI] " + label + " Button component is disabled.",
                    button);
            }
            if (!button.interactable)
            {
                Debug.LogError(
                    "[GameUpdateUI] " + label + " Button is not interactable.",
                    button);
            }
            if (image == null || !image.enabled || !image.raycastTarget)
            {
                Debug.LogError(
                    "[GameUpdateUI] " + label +
                    " button Image is missing, disabled, or not raycastable.",
                    button);
            }

            bool parentGroupsIgnored = false;
            bool foundCanvasGroup = false;
            for (Transform current = button.transform;
                 current != null;
                 current = current.parent)
            {
                CanvasGroup group = current.GetComponent<CanvasGroup>();
                if (group == null)
                    continue;
                foundCanvasGroup = true;

                bool groupBlocksInteraction =
                    !group.interactable || !group.blocksRaycasts;
                bool blocksInteraction =
                    !parentGroupsIgnored && groupBlocksInteraction;
                Debug.Log(
                    "[GameUpdateUI] CanvasGroup: " +
                    GetHierarchyPath(current) +
                    "; alpha=" + group.alpha +
                    "; interactable=" + group.interactable +
                    "; blocksRaycasts=" + group.blocksRaycasts +
                    "; ignoreParentGroups=" + group.ignoreParentGroups +
                    "; effectiveForButton=" + !parentGroupsIgnored +
                    (blocksInteraction ? " [BLOCKS INTERACTION]" : string.Empty),
                    group);

                if (!parentGroupsIgnored && group.ignoreParentGroups)
                    parentGroupsIgnored = true;
            }

            if (!foundCanvasGroup)
            {
                Debug.Log(
                    "[GameUpdateUI] No CanvasGroup exists in the " +
                    label + " button parent hierarchy.",
                    button);
            }
        }

        private void ValidateVisibleInteractionPath()
        {
            if (visibleRaycastValidationComplete ||
                !root.activeInHierarchy ||
                updateButton == null ||
                laterButton == null)
            {
                return;
            }

            visibleRaycastValidationComplete = true;
            Canvas.ForceUpdateCanvases();
            EventSystem eventSystem = EventSystem.current;
            Debug.Log(
                "[GameUpdateUI] EventSystem at dialog visibility: " +
                (eventSystem == null
                    ? "<none>"
                    : eventSystem.currentInputModule == null
                        ? "<no active input module>"
                        : eventSystem.currentInputModule.GetType().FullName),
                this);
            Debug.Log("[GameUpdateUI] Time.timeScale = " + Time.timeScale, this);
            if (eventSystem == null || eventSystem.currentInputModule == null)
            {
                Debug.LogError(
                    "[GameUpdateUI] EventSystem has no selected input module " +
                    "when the update dialog becomes visible.",
                    this);
            }
            ValidateButtonRaycast("Update", updateButton);
            ValidateButtonRaycast("Later", laterButton);
        }

        private void ValidateButtonRaycast(string label, Button button)
        {
            if (!button.gameObject.activeInHierarchy)
            {
                Debug.Log(
                    "[GameUpdateUI] " + label +
                    " button is intentionally inactive in the current UI state; " +
                    "center raycast skipped.",
                    button);
                return;
            }
            if (!button.enabled || !button.interactable)
            {
                Debug.LogError(
                    "[GameUpdateUI] " + label +
                    " button is disabled or non-interactable when the update " +
                    "dialog is visible.",
                    button);
            }

            EventSystem eventSystem = EventSystem.current;
            RectTransform buttonRect = button.transform as RectTransform;
            Canvas canvas = button.GetComponentInParent<Canvas>();
            if (eventSystem == null || buttonRect == null || canvas == null)
            {
                Debug.LogError(
                    "[GameUpdateUI] Cannot raycast " + label +
                    " button: missing EventSystem, RectTransform, or Canvas.",
                    button);
                return;
            }

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                canvas.worldCamera,
                buttonRect.TransformPoint(buttonRect.rect.center));
            var pointer = new PointerEventData(eventSystem)
            {
                position = screenPoint
            };
            raycastResults.Clear();
            eventSystem.RaycastAll(pointer, raycastResults);

            Debug.Log(
                "[GameUpdateUI] " + label + " center raycast at (" +
                screenPoint.x + ", " + screenPoint.y + ") hits=" +
                raycastResults.Count,
                button);

            int buttonHitIndex = -1;
            for (int index = 0; index < raycastResults.Count; index++)
            {
                GameObject hitObject = raycastResults[index].gameObject;
                if (hitObject == null)
                    continue;

                bool belongsToButton =
                    hitObject.transform == button.transform ||
                    hitObject.transform.IsChildOf(button.transform);
                if (belongsToButton && buttonHitIndex < 0)
                    buttonHitIndex = index;
            }

            for (int index = 0; index < raycastResults.Count; index++)
            {
                RaycastResult result = raycastResults[index];
                GameObject hitObject = result.gameObject;
                if (hitObject == null)
                    continue;

                bool belongsToButton =
                    hitObject.transform == button.transform ||
                    hitObject.transform.IsChildOf(button.transform);
                Graphic graphic = hitObject.GetComponent<Graphic>();
                Canvas hitCanvas = hitObject.GetComponentInParent<Canvas>();
                bool precedesButton = buttonHitIndex >= 0
                    ? index < buttonHitIndex
                    : index == 0;
                string marker = precedesButton
                    ? " [BLOCKER]"
                    : belongsToButton
                        ? " [BUTTON HIT]"
                        : string.Empty;
                Debug.Log(
                    "[GameUpdateUI] " + label + " raycast[" + index + "] " +
                    GetHierarchyPath(hitObject.transform) +
                    "; module=" +
                    (result.module == null
                        ? "<none>"
                        : result.module.GetType().FullName) +
                    "; sortingOrder=" + result.sortingOrder +
                    "; depth=" + result.depth +
                    "; canvas=" + (hitCanvas == null ? "<none>" : hitCanvas.name) +
                    "; raycastTarget=" +
                    (graphic != null && graphic.raycastTarget) + marker,
                    hitObject);
            }

            if (buttonHitIndex < 0)
            {
                Debug.LogError(
                    "[GameUpdateUI] " + label +
                    " button hierarchy was not hit by the EventSystem raycast.",
                    button);
            }
            else
            {
                Debug.Log(
                    "[GameUpdateUI] " + label + " pointer path: " +
                    (buttonHitIndex == 0
                        ? "first raycast hit is the button hierarchy; no prior blocker."
                        : buttonHitIndex +
                          " raycast result(s) precede the button hierarchy."),
                    button);
            }
        }

        private static string GetHierarchyPath(Transform target)
        {
            if (target == null)
                return "<none>";

            string path = target.name;
            while (target.parent != null)
            {
                target = target.parent;
                path = target.name + "/" + path;
            }
            return path;
        }

        private static GameObject CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Image AddImage(GameObject target, Color color)
        {
            Image image = target.GetComponent<Image>();
            if (image == null)
                image = target.AddComponent<Image>();

            image.color = color;
            image.raycastTarget = true;
            return image;
        }

        private static Text AddText(
            string name,
            Transform parent,
            string initialText,
            int fontSize,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            FontStyle fontStyle,
            TextAnchor alignment)
        {
            GameObject textObject = CreateRect(name, parent);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = initialText;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static Button AddButton(
            string name,
            Transform parent,
            string label,
            Vector2 position)
        {
            GameObject buttonObject = CreateRect(name, parent);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(230f, 54f);

            Image image = AddImage(
                buttonObject,
                new Color(0.92f, 0.62f, 0.04f, 1f));
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            Text buttonText = AddText(
                "Label", buttonObject.transform, label, 17,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                FontStyle.Bold, TextAnchor.MiddleCenter);
            buttonText.rectTransform.offsetMin = new Vector2(8f, 4f);
            buttonText.rectTransform.offsetMax = new Vector2(-8f, -4f);
            return button;
        }

        private static Text FindText(Transform parent, string name)
        {
            Transform child = parent.Find("Panel/" + name);
            return child != null ? child.GetComponent<Text>() : null;
        }

        private static Button FindButton(Transform parent, string name)
        {
            Transform child = parent.Find("Panel/" + name);
            return child != null ? child.GetComponent<Button>() : null;
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024L)
                return bytes + " B";
            if (bytes < 1024L * 1024L)
                return (bytes / 1024f).ToString("0.0") + " KB";
            if (bytes < 1024L * 1024L * 1024L)
                return (bytes / (1024f * 1024f)).ToString("0.0") + " MB";

            return (bytes / (1024f * 1024f * 1024f)).ToString("0.00") +
                   " GB";
        }
    }
}
