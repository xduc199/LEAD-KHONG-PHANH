#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

public static class CleanupMissingScripts
{
    [MenuItem("LEAD KHONG PHANH/Cleanup Missing Scripts In Open Scene")]
    private static void Cleanup()
    {
        int removed = 0;

        GameObject[] roots =
            UnityEngine.SceneManagement.SceneManager
                .GetActiveScene()
                .GetRootGameObjects();

        foreach (GameObject root in roots)
        {
            removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
        }

        Debug.Log(
            $"[CleanupMissingScripts] Đã xóa {removed} Missing Script."
        );
    }
}

#endif