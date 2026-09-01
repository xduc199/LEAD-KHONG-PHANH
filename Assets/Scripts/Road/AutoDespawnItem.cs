using UnityEngine;

public class AutoDespawnBehindPlayer : MonoBehaviour
{
    [Header("Despawn Settings")]
    [SerializeField] private float despawnBehindDistance = 35f;

    private Transform player;

    //=============================================================
    // START
    //=============================================================

    private void Start()
    {
        FindPlayer();
    }

    //=============================================================
    // UPDATE
    //=============================================================

    private void Update()
    {
        if (player == null)
        {
            FindPlayer();
            return;
        }

        if (transform.position.z <
            player.position.z - despawnBehindDistance)
        {
            Destroy(gameObject);
        }
    }

    //=============================================================
    // FIND PLAYER
    //=============================================================

    private void FindPlayer()
    {
        GameObject obj =
            GameObject.FindGameObjectWithTag("Player");

        if (obj != null)
        {
            player = obj.transform;
        }
    }

    //=============================================================
    // VALIDATE
    //=============================================================

    private void OnValidate()
    {
        if (despawnBehindDistance < 5f)
            despawnBehindDistance = 5f;
    }
}