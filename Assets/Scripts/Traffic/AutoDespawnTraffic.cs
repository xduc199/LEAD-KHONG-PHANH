using UnityEngine;

public class AutoDespawnTraffic : MonoBehaviour
{
    [Header("Despawn Settings")]
    [Tooltip("Khoảng cách phía sau Player (mét) để xe tự biến mất khi ra khỏi tầm nhìn Camera")]
    [SerializeField] private float despawnDistanceBehindPlayer = 15f;

    private Transform playerTransform;

    private void Start()
    {
        FindPlayer();
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            return;
        }

        // Khi người chơi vượt qua xe (hoặc xe ngược chiều chạy tuốt về phía sau Player > 15m)
        if (playerTransform.position.z - transform.position.z > despawnDistanceBehindPlayer)
        {
            Destroy(gameObject);
        }
    }

    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }
}