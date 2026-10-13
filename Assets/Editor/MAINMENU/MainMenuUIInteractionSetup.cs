#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class MainMenuUIInteractionSetup
{
    [MenuItem("LEAD KHONG PHANH/UI/Fix Main Menu UI Interaction")]
    public static void FixMainMenuUIInteraction()
    {
        //=========================================================
        // FIND MAIN MENU CANVAS
        //=========================================================

        GameObject canvasObject =
            GameObject.Find("MainMenuCanvas");

        if (canvasObject == null)
        {
            Debug.LogError(
                "[MainMenu UI] Không tìm thấy MainMenuCanvas."
            );

            return;
        }

        Canvas canvas =
            canvasObject.GetComponent<Canvas>();

        if (canvas == null)
        {
            Debug.LogError(
                "[MainMenu UI] MainMenuCanvas không có Canvas."
            );

            return;
        }


        //=========================================================
        // ENSURE GRAPHIC RAYCASTER
        //=========================================================

        GraphicRaycaster raycaster =
            canvasObject.GetComponent<GraphicRaycaster>();

        if (raycaster == null)
        {
            raycaster =
                canvasObject.AddComponent<GraphicRaycaster>();
        }

        raycaster.enabled = true;


        //=========================================================
        // ENSURE EVENT SYSTEM
        //=========================================================

        EventSystem eventSystem =
            Object.FindFirstObjectByType<EventSystem>();

        if (eventSystem == null)
        {
            GameObject eventSystemObject =
                new GameObject(
                    "EventSystem",
                    typeof(EventSystem),
                    typeof(StandaloneInputModule)
                );

            eventSystem =
                eventSystemObject.GetComponent<EventSystem>();

            Debug.Log(
                "[MainMenu UI] Đã tự tạo EventSystem."
            );
        }
        else
        {
            Debug.Log(
                "[MainMenu UI] EventSystem đã tồn tại."
            );
        }


        //=========================================================
        // ENSURE INPUT MODULE
        //=========================================================

        StandaloneInputModule inputModule =
            eventSystem.GetComponent<StandaloneInputModule>();

        if (inputModule == null)
        {
            inputModule =
                eventSystem.gameObject.AddComponent<
                    StandaloneInputModule
                >();
        }


        //=========================================================
        // FIX MISSION PANEL
        //=========================================================

        Transform safeArea =
            canvasObject.transform.Find("SafeArea");

        if (safeArea != null)
        {
            Transform missionPanel =
                safeArea.Find("MissionPanel");

            if (missionPanel != null)
            {
                // Không để MissionPanel chặn Main Menu
                // ngay khi bắt đầu Scene.
                missionPanel.gameObject.SetActive(false);

                Image panelImage =
                    missionPanel.GetComponent<Image>();

                if (panelImage != null)
                {
                    panelImage.raycastTarget = false;
                }

                Debug.Log(
                    "[MainMenu UI] MissionPanel đã được đóng."
                );
            }
        }


        //=========================================================
        // CHECK ALL BUTTONS
        //=========================================================

        Button[] buttons =
            canvasObject.GetComponentsInChildren<
                Button
            >(true);

        int enabledButtons = 0;

        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            button.enabled = true;
            button.interactable = true;

            Image buttonImage =
                button.GetComponent<Image>();

            if (buttonImage != null)
            {
                buttonImage.raycastTarget = true;
            }

            enabledButtons++;

            Debug.Log(
                $"[MainMenu UI] Button OK: " +
                $"{GetPath(button.transform)}"
            );
        }


        //=========================================================
        // FIX CANVAS GROUPS
        //=========================================================

        CanvasGroup[] groups =
            canvasObject.GetComponentsInChildren<
                CanvasGroup
            >(true);

        foreach (CanvasGroup group in groups)
        {
            if (group == null)
                continue;

            // Nếu object đang active thì phải cho phép tương tác.
            if (group.gameObject.activeInHierarchy)
            {
                group.interactable = true;
                group.blocksRaycasts = true;
            }
        }


        //=========================================================
        // SAVE
        //=========================================================

        EditorUtility.SetDirty(
            canvasObject
        );

        EditorUtility.SetDirty(
            eventSystem
        );

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager
                .GetActiveScene()
        );


        Selection.activeGameObject =
            canvasObject;


        Debug.Log(
            "========================================\n" +
            "[MainMenu UI] FIX HOÀN TẤT\n" +
            "Canvas       : OK\n" +
            "Raycaster    : OK\n" +
            "EventSystem  : OK\n" +
            "Buttons      : " +
            enabledButtons +
            "\n" +
            "MissionPanel : CLOSED\n" +
            "========================================"
        );
    }


    //=============================================================
    // GET HIERARCHY PATH
    //=============================================================

    private static string GetPath(
        Transform target)
    {
        string path =
            target.name;

        Transform current =
            target.parent;

        while (current != null)
        {
            path =
                current.name +
                "/" +
                path;

            current =
                current.parent;
        }

        return path;
    }
}

#endif