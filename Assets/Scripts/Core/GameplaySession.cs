using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý lifecycle của một Gameplay Run.
/// 
/// Gameplay object:
/// - Player
/// - GameManager runtime reference
/// - Run counters
///
/// KHÔNG được sống tiếp như một gameplay session khi về MainMenu.
/// </summary>
public static class GameplaySession
{
    // =========================================================
    // STATE
    // =========================================================

    private static bool gameplayActive;

    public static bool IsGameplayActive
    {
        get { return gameplayActive; }
    }


    // =========================================================
    // BEGIN
    // =========================================================

    public static void BeginGameplay()
    {
        gameplayActive = true;

        Debug.Log(
            "[GameplaySession] GAMEPLAY STARTED."
        );
    }


    // =========================================================
    // END
    // =========================================================

    public static void EndGameplay()
    {
        if (!gameplayActive)
        {
            // Vẫn cleanup Player nếu còn sót object.
            DestroyGameplayPlayers();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResetRunData();
            }

            Debug.Log(
                "[GameplaySession] END requested while inactive. Cleanup forced."
            );

            return;
        }

        gameplayActive = false;

        // -----------------------------------------------------
        // 1. RESET GAME MANAGER RUN
        // -----------------------------------------------------

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetRunData();
        }


        // -----------------------------------------------------
        // 2. DESTROY PLAYER
        // -----------------------------------------------------

        DestroyGameplayPlayers();


        // -----------------------------------------------------
        // 3. RESET TIME SCALE
        // -----------------------------------------------------

        Time.timeScale = 1f;


        Debug.Log(
            "[GameplaySession] GAMEPLAY ENDED. " +
            "Run data cleared. Player destroyed."
        );
    }


    // =========================================================
    // PLAYER CLEANUP
    // =========================================================

    private static void DestroyGameplayPlayers()
    {
        PlayerController[] players =
            Object.FindObjectsByType<PlayerController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        int count = players.Length;

        for (int i = 0; i < count; i++)
        {
            PlayerController player = players[i];

            if (player == null)
            {
                continue;
            }

            Debug.Log(
                "[GameplaySession] Destroy Player → " +
                player.gameObject.name
            );

            Object.Destroy(
                player.gameObject
            );
        }
    }


    // =========================================================
    // SCENE SAFETY
    // =========================================================

    public static bool IsGameplayScene(
        Scene scene
    )
    {
        return scene.IsValid() &&
               scene.name == "GameplayScene";
    }
}