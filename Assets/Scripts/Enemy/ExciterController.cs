using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExciterController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float baseSpeed = 65f; // Tốc độ tối thiểu cơ bản
    [SerializeField] private float speedBonusOverPlayer = 20f; // Luôn nhanh hơn Player từng này tốc độ
    [SerializeField] private float rotationSpeed = 15f;
    [SerializeField] private float modelYRotationOffset = 0f;

    [Header("Effects")]
    [SerializeField] private GameObject explosionEffectPrefab;

    private List<Vector3> pathPoints = new List<Vector3>();
    private int currentPointIndex = 0;
    private bool isKnockedBack = false;
    private Transform playerTransform;
    private Rigidbody rb;
    private Vector3 lastMoveDir = Vector3.forward;
    private float spawnTime;
    
    private Vector3 lastPlayerPos;
    private float currentPlayerSpeed = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        
        rb.isKinematic = true;
        rb.useGravity = false;
        spawnTime = Time.time;
    }

    public void SetPath(List<Vector3> points, Transform player)
    {
        pathPoints = points;
        playerTransform = player;
        if (playerTransform != null) lastPlayerPos = playerTransform.position;
        currentPointIndex = 0;
    }

    private void Update()
    {
        if (isKnockedBack) return;

        // TÍNH TOÁN TỐC ĐỘ ĐỘNG CỦA PLAYER THEO THỜI GIAN THỰC
        if (playerTransform != null)
        {
            // Tính khoảng cách di chuyển của Player theo thời gian để ra tốc độ thực tế
            float distMoved = Vector3.Distance(playerTransform.position, lastPlayerPos);
            if (Time.deltaTime > 0) currentPlayerSpeed = distMoved / Time.deltaTime;
            lastPlayerPos = playerTransform.position;

            // Xóa xe nếu vượt quá xa player
            if (transform.position.z - playerTransform.position.z > 90f)
            {
                Destroy(gameObject);
                return;
            }
        }

        // Kiểm tra va chạm bằng khoảng cách
        CheckDistanceCollisions();

        // TỐC ĐỘ EXCITER LUÔN LUÔN NHANH HƠN PLAYER BẤT KỂ GAME CHƠI LÂU ĐẾN ĐÂU
        float currentExciterSpeed = Mathf.Max(baseSpeed, currentPlayerSpeed + speedBonusOverPlayer);

        if (pathPoints != null && currentPointIndex < pathPoints.Count)
        {
            Vector3 targetPos = pathPoints[currentPointIndex];
            targetPos.y = transform.position.y;

            transform.position = Vector3.MoveTowards(transform.position, targetPos, currentExciterSpeed * Time.deltaTime);

            Vector3 moveDir = (targetPos - transform.position).normalized;
            if (moveDir != Vector3.zero)
            {
                lastMoveDir = moveDir;
                Quaternion targetRotation = Quaternion.LookRotation(moveDir) * Quaternion.Euler(0f, modelYRotationOffset, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            if (Vector3.Distance(transform.position, targetPos) < 0.6f) currentPointIndex++;
        }
        else
        {
            transform.position += lastMoveDir * currentExciterSpeed * Time.deltaTime;
        }
    }

    private void CheckDistanceCollisions()
    {
        if (Time.time - spawnTime < 0.2f) return;

        // 1. Kiểm tra va chạm với Player
        if (playerTransform != null)
        {
            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distToPlayer < 2.2f)
            {
                TriggerPlayerHit(playerTransform.gameObject);
                return;
            }
        }

        // 2. Kiểm tra va chạm với xe ngược chiều / chướng ngại vật
        GameObject[] obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        foreach (GameObject obs in obstacles)
        {
            if (obs == null || obs == gameObject) continue;

            float distToObstacle = Vector3.Distance(transform.position, obs.transform.position);
            if (distToObstacle < 2.0f)
            {
                TriggerObstacleExplosion(obs);
                break;
            }
        }
    }

    private void SpawnExplosionEffect(Vector3 pos)
    {
        // Kiểm tra nếu tính năng nổ đang BẬT từ PlayerController
        if (PlayerController.EnableExplosionStatic)
        {
            GameObject prefabToUse = explosionEffectPrefab != null ? explosionEffectPrefab : PlayerController.ExplosionEffectPrefabStatic;
            if (prefabToUse != null)
            {
                Instantiate(prefabToUse, pos, Quaternion.identity);
            }
        }
    }

    private void TriggerObstacleExplosion(GameObject obstacleObj = null)
    {
        if (isKnockedBack) return;

        // Sinh vụ nổ tại vị trí điểm va chạm giữa Exciter và Chướng ngại vật
        Vector3 spawnPos = obstacleObj != null ? (transform.position + obstacleObj.transform.position) * 0.5f : transform.position;
        SpawnExplosionEffect(spawnPos);
        
        // Húc xe ngược chiều -> Bay cao và văng lùi ngược về phía sau hướng camera
        ApplyKnockback(new Vector3(0f, 12f, -18f));
    }

    private void TriggerPlayerHit(GameObject playerObj)
    {
        if (isKnockedBack) return;
        
        // Sinh vụ nổ tại điểm va chạm giữa Exciter và Player
        Vector3 spawnPos = (transform.position + playerObj.transform.position) * 0.5f;
        SpawnExplosionEffect(spawnPos);

        ApplyKnockback(new Vector3(0f, 6f, -10f));

        PlayerController player = playerObj.GetComponentInParent<PlayerController>();
        if (player != null) player.ApplyKnockback(new Vector3(0f, 4f, 6f));
    }

    public void ApplyKnockback(Vector3 force)
    {
        isKnockedBack = true;
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearDamping = 1.5f; 
            rb.angularDamping = 2f;
            
            rb.AddForce(force, ForceMode.Impulse);
            rb.AddTorque(new Vector3(15f, 5f, 20f), ForceMode.Impulse);
        }
        Destroy(gameObject, 3f);
    }
}