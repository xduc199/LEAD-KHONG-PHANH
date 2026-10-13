using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-50)]
public sealed class GameManagerPlayerBinder : MonoBehaviour
{
    private const string GameplaySceneName = "GameplayScene";

    [Header("Player")]
    [SerializeField]
    private Transform playerTransform;

    private bool bound;
    private bool waitingForGameManager;

    private void Start()
    {
        TryBind();
    }

    private void Update()
    {
        // Không có allocation.
        // Chỉ polling khi chưa bind được.
        if (bound)
            return;

        if (!waitingForGameManager)
            return;

        TryBind();
    }

    private void OnDestroy()
    {
        UnbindPlayerFromGameManager();
    }

    private void TryBind()
    {
        if (bound)
            return;

        Scene activeScene = SceneManager.GetActiveScene();

        if (!activeScene.IsValid())
            return;

        if (activeScene.name != GameplaySceneName)
            return;

        GameManager gameManager = GameManager.Instance;

        if (gameManager == null)
        {
            waitingForGameManager = true;
            return;
        }

        Transform targetPlayer = playerTransform;

        if (targetPlayer == null)
            targetPlayer = transform;

        if (targetPlayer == null)
        {
            Debug.LogError(
                "[GameManagerPlayerBinder] Không tìm thấy Player Transform.",
                this);

            waitingForGameManager = false;
            return;
        }

        // Bắt đầu một runtime run mới.
        if (!gameManager.IsRunActive)
        {
            gameManager.BeginNewRun();
        }

        gameManager.SetPlayerTransform(targetPlayer);

        bound = true;
        waitingForGameManager = false;

        Debug.Log(
            "[GameManagerPlayerBinder] Player bound successfully.",
            this);
    }

    private void UnbindPlayerFromGameManager()
    {
        if (!bound)
            return;

        GameManager gameManager = GameManager.Instance;

        if (gameManager != null)
        {
            gameManager.ClearPlayerReference();
        }

        bound = false;
        waitingForGameManager = false;
    }
}