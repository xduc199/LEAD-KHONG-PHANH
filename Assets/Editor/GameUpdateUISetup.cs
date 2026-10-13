#if UNITY_EDITOR

using LEADKhongPhanh.Update;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GameUpdateUISetup
{
    [MenuItem("LEAD KHONG PHANH/Update/Setup Update UI", priority = 1000)]
    private static void SetupUpdateUI()
    {
        UpdateManager manager = Object.FindFirstObjectByType<UpdateManager>(
            FindObjectsInactive.Include);

        if (manager == null)
        {
            Debug.LogError(
                "[GameUpdateUISetup] No UpdateManager exists in the open scene. " +
                "Add/configure the production UpdateManager first; no object was created.");
            return;
        }

        GameObject managerObject = manager.gameObject;
        GameUpdateUI[] existingUis = Object.FindObjectsByType<GameUpdateUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        GameUpdateUI managerUi = managerObject.GetComponent<GameUpdateUI>();
        if (managerUi == null)
        {
            managerUi = Undo.AddComponent<GameUpdateUI>(managerObject);
            Debug.Log("[GameUpdateUISetup] Added GameUpdateUI beside UpdateManager.",
                managerObject);
        }

        // Old setup versions put a second UI component on a standalone canvas.
        // Remove only that duplicate component; retain the Canvas and hierarchy.
        for (int i = 0; i < existingUis.Length; i++)
        {
            GameUpdateUI ui = existingUis[i];
            if (ui == null || ui == managerUi)
                continue;

            Transform legacyRoot = ui.transform.Find("UpdateRoot");
            if (legacyRoot != null && legacyRoot.gameObject.activeSelf)
            {
                Undo.RecordObject(legacyRoot.gameObject,
                    "Hide detached Update UI root");
                legacyRoot.gameObject.SetActive(false);
                EditorUtility.SetDirty(legacyRoot.gameObject);
            }

            Undo.DestroyObjectImmediate(ui);
        }

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(managerUi);
        AssetDatabase.SaveAssets();
        if (!EditorSceneManager.SaveScene(managerObject.scene))
        {
            Debug.LogError(
                "[GameUpdateUISetup] Could not save the scene containing UpdateManager.",
                managerObject);
            return;
        }

        Selection.activeGameObject = managerObject;
        EditorGUIUtility.PingObject(managerObject);
        Debug.Log("[GameUpdateUISetup] Update UI setup completed on " +
                  managerObject.name + ".", managerObject);
    }
}

#endif
