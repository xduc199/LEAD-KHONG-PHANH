using UnityEngine;

public class GameManagerPlayerBinder : MonoBehaviour
{
    [Header("Bind Settings")]
    [SerializeField] private float retryInterval = 0.1f;

    private bool isBound;
    private float retryTimer;

    private void Start()
    {
        BeginNewRun();

        BindPlayerToGameManager();
    }

    private void Update()
    {
        if (isBound)
            return;

        retryTimer -= Time.unscaledDeltaTime;

        if (retryTimer > 0f)
            return;

        retryTimer = retryInterval;

        BindPlayerToGameManager();
    }

    //==============================================================
    // BEGIN NEW RUN
    //==============================================================

    private void BeginNewRun()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning(
                "[GameManagerPlayerBinder] " +
                "GameManager.Instance chưa tồn tại. " +
                "Không thể bắt đầu run mới."
            );

            return;
        }

        GameManager.Instance.BeginNewRun();

        Debug.Log(
            "[GameManagerPlayerBinder] " +
            "Đã bắt đầu Gameplay Run mới. " +
            "Run data đã được reset."
        );
    }

    //==============================================================
    // BIND PLAYER
    //==============================================================

    private void BindPlayerToGameManager()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetPlayerTransform(transform);

        isBound = true;

        Debug.Log(
            "[GameManagerPlayerBinder] " +
            "Đã gán Player '" +
            gameObject.name +
            "' vào GameManager."
        );
    }
}