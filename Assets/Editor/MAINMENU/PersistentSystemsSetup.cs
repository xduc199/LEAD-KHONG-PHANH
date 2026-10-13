#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PersistentSystemsSetup
{
    private const string RootName = "PersistentSystems";

    //==============================================================
    // MENU
    //==============================================================

    [MenuItem("LEAD KHONG PHANH/Core/Create Persistent Systems")]
    private static void CreatePersistentSystems()
    {
        //==========================================================
        // FIND EXISTING ROOT
        //==========================================================

        GameObject root = GameObject.Find(RootName);

        if (root == null)
        {
            root = new GameObject(RootName);

            Undo.RegisterCreatedObjectUndo(
                root,
                "Create Persistent Systems"
            );
        }


        //==========================================================
        // GAME MANAGER
        //==========================================================

        GameManager gameManager =
            root.GetComponent<GameManager>();

        if (gameManager == null)
        {
            gameManager =
                Undo.AddComponent<GameManager>(root);
        }


        //==========================================================
        // PLAYER PROGRESSION
        //==========================================================

        PlayerProgression progression =
            root.GetComponent<PlayerProgression>();

        if (progression == null)
        {
            progression =
                Undo.AddComponent<PlayerProgression>(root);
        }


        //==========================================================
        // MARK SCENE DIRTY
        //==========================================================

        EditorUtility.SetDirty(root);

        EditorSceneManager.MarkSceneDirty(
            root.scene
        );


        //==========================================================
        // SELECT
        //==========================================================

        Selection.activeGameObject = root;

        EditorGUIUtility.PingObject(root);


        //==========================================================
        // LOG
        //==========================================================

        Debug.Log(
            "[PersistentSystemsSetup] " +
            "PersistentSystems đã được tạo/cập nhật.\n" +
            "GameManager: " +
            (gameManager != null ? "OK" : "NULL") +
            "\nPlayerProgression: " +
            (progression != null ? "OK" : "NULL") +
            "\n\n" +
            "ROOT: " + root.name
        );
    }
}

#endif