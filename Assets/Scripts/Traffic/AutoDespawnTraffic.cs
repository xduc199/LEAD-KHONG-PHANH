using UnityEngine;

public class AutoDespawnTraffic : MonoBehaviour
{
    [Header("Despawn Settings")]

    [Tooltip(
        "Khoảng cách phía sau Player mà traffic sẽ bị destroy."
    )]
    [SerializeField] private float despawnDistanceBehindPlayer = 30f;

    [Tooltip(
        "Nếu bật, traffic quá xa phía trước cũng sẽ bị destroy."
    )]
    [SerializeField] private bool despawnTooFarAhead = false;

    [SerializeField] private float despawnDistanceAhead = 120f;

    [Header("Player")]

    [Tooltip(
        "Tự tìm Player bằng Tag = Player."
    )]
    [SerializeField] private bool autoFindPlayer = true;

    private Transform playerTransform;

    //=========================================================
    // UNITY
    //=========================================================

    private void Start()
    {
        FindPlayer();
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();

            if (playerTransform == null)
                return;
        }

        float playerZ =
            playerTransform.position.z;

        float trafficZ =
            transform.position.z;

        //=====================================================
        // DESPAWN PHÍA SAU
        //=====================================================

        if (
            playerZ -
            trafficZ >
            despawnDistanceBehindPlayer
        )
        {
            Destroy(
                gameObject
            );

            return;
        }

        //=====================================================
        // DESPAWN PHÍA TRƯỚC
        //=====================================================

        if (despawnTooFarAhead)
        {
            if (
                trafficZ -
                playerZ >
                despawnDistanceAhead
            )
            {
                Destroy(
                    gameObject
                );
            }
        }
    }

    //=========================================================
    // FIND PLAYER
    //=========================================================

    private void FindPlayer()
    {
        if (!autoFindPlayer)
            return;

        GameObject player =
            GameObject.FindGameObjectWithTag(
                "Player"
            );

        if (player != null)
        {
            playerTransform =
                player.transform;
        }
    }

    //=========================================================
    // PUBLIC
    //=========================================================

    public void SetPlayerTransform(
        Transform player
    )
    {
        playerTransform =
            player;
    }

    public float GetDespawnDistance()
    {
        return despawnDistanceBehindPlayer;
    }
}