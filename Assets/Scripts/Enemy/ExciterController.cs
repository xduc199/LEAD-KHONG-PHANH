
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExciterController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float baseSpeed = 65f;

    [Tooltip("Exciter luôn nhanh hơn Player tối thiểu từng này.")]
    [SerializeField] private float speedBonusOverPlayer = 20f;

    [SerializeField] private float rotationSpeed = 15f;
    [SerializeField] private float modelYRotationOffset = 0f;

    [Header("Slight Weave")]
    [Tooltip("Biên độ đánh võng sang trái/phải.")]
    [SerializeField] private float weaveAmplitude = 0.3f;

    [Tooltip("Tần số đánh võng. Cao hơn = đánh võng nhanh hơn.")]
    [SerializeField] private float weaveFrequency = 0.09f;

    [Tooltip("Thời gian ban đầu trước khi đánh võng bắt đầu.")]
    [SerializeField] private float weaveStartDelay = 0.15f;

    [Header("Effects")]
    [SerializeField] private GameObject explosionEffectPrefab;

    [Header("Exciter Audio")]
    [SerializeField] private AudioClip exciterEngineClip;

    [Range(0f, 1f)]
    [SerializeField] private float exciterEngineVolume = 0.65f;

    [SerializeField] private float engineMinDistance = 5f;
    [SerializeField] private float engineMaxDistance = 45f;

    [SerializeField] private bool engineLoop = true;

    //=============================================================
    // VARIABLES
    //=============================================================
    private List<Vector3> pathPoints = new List<Vector3>();

    private int currentPointIndex = 0;

    private bool isKnockedBack = false;

    private Transform playerTransform;

    private Rigidbody rb;

    private Vector3 lastMoveDir = Vector3.forward;

    private float spawnTime;

    private Vector3 lastPlayerPos;

    private float currentPlayerSpeed = 0f;

    // Đường trung tâm được lấy từ Redline
    private float lockedCenterX;

    // Dùng cho đánh võng
    private float weaveTime;

    private AudioSource engineAudioSource;

    //=============================================================
    // AWAKE
    //=============================================================
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.isKinematic = true;
        rb.useGravity = false;

        spawnTime = Time.time;

        //=========================================================
        // TẠO AUDIO SOURCE
        //=========================================================
        SetupEngineAudio();
    }

    //=============================================================
    // SET PATH
    //=============================================================
    public void SetPath(
        List<Vector3> points,
        Transform player
    )
    {
        pathPoints = points;
        playerTransform = player;

        if (playerTransform != null)
        {
            lastPlayerPos = playerTransform.position;
        }

        currentPointIndex = 0;

        weaveTime = 0f;

        //=========================================================
        // LẤY X CỦA ĐƯỜNG ĐỎ ĐÃ KHÓA
        //=========================================================
        if (pathPoints != null && pathPoints.Count > 0)
        {
            lockedCenterX = pathPoints[0].x;
        }
        else
        {
            lockedCenterX = transform.position.x;
        }
    }

    //=============================================================
    // UPDATE
    //=============================================================
    private void Update()
    {
        if (isKnockedBack)
            return;

        //=========================================================
        // TÍNH TỐC ĐỘ THỰC TẾ CỦA PLAYER
        //=========================================================
        if (playerTransform != null)
        {
            float distMoved =
                Vector3.Distance(
                    playerTransform.position,
                    lastPlayerPos
                );

            if (Time.deltaTime > 0)
            {
                currentPlayerSpeed =
                    distMoved / Time.deltaTime;
            }

            lastPlayerPos =
                playerTransform.position;

            //=====================================================
            // XÓA XE KHI ĐI QUÁ XA KHỎI PLAYER
            //=====================================================
            if (
                transform.position.z -
                playerTransform.position.z > 90f
            )
            {
                Destroy(gameObject);
                return;
            }
        }

        //=========================================================
        // CHECK COLLISION
        //=========================================================
        CheckDistanceCollisions();

        //=========================================================
        // TÍNH TỐC ĐỘ
        //=========================================================
        float currentExciterSpeed =
            Mathf.Max(
                baseSpeed,
                currentPlayerSpeed +
                speedBonusOverPlayer
            );

        //=========================================================
        // DI CHUYỂN THEO PATH
        //=========================================================
        if (
            pathPoints != null &&
            currentPointIndex < pathPoints.Count
        )
        {
            Vector3 baseTargetPos =
                pathPoints[currentPointIndex];

            baseTargetPos.y = transform.position.y;

            //=====================================================
            // ĐÁNH VÕNG NHẸ
            //=====================================================
            weaveTime += Time.deltaTime;

            float weaveOffset = 0f;

            if (weaveTime >= weaveStartDelay)
            {
                float distanceAlongPath =
                    transform.position.z -
                    pathPoints[0].z;

                weaveOffset =
                    Mathf.Sin(
                        distanceAlongPath *
                        weaveFrequency
                    ) *
                    weaveAmplitude;
            }

            Vector3 targetPos =
                new Vector3(
                    lockedCenterX + weaveOffset,
                    baseTargetPos.y,
                    baseTargetPos.z
                );

            //=====================================================
            // DI CHUYỂN
            //=====================================================
            transform.position =
                Vector3.MoveTowards(
                    transform.position,
                    targetPos,
                    currentExciterSpeed *
                    Time.deltaTime
                );

            //=====================================================
            // HƯỚNG XE
            //=====================================================
            Vector3 moveDir =
                targetPos -
                transform.position;

            moveDir.y = 0f;

            if (moveDir.sqrMagnitude > 0.001f)
            {
                lastMoveDir =
                    moveDir.normalized;

                Quaternion targetRotation =
                    Quaternion.LookRotation(
                        lastMoveDir
                    ) *
                    Quaternion.Euler(
                        0f,
                        modelYRotationOffset,
                        0f
                    );

                transform.rotation =
                    Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        rotationSpeed *
                        Time.deltaTime
                    );
            }

            //=====================================================
            // ĐẾN ĐIỂM TIẾP THEO
            //=====================================================
            if (
                Vector3.Distance(
                    transform.position,
                    targetPos
                ) < 0.6f
            )
            {
                currentPointIndex++;
            }
        }
        else
        {
            //=====================================================
            // HẾT PATH -> ĐI THẲNG
            //=====================================================
            transform.position +=
                lastMoveDir *
                currentExciterSpeed *
                Time.deltaTime;
        }
    }

    //=============================================================
    // ENGINE AUDIO
    //=============================================================
   private void SetupEngineAudio()
{
    if (exciterEngineClip == null)
        return;

    engineAudioSource = GetComponent<AudioSource>();

    if (engineAudioSource == null)
    {
        engineAudioSource = gameObject.AddComponent<AudioSource>();
    }

    engineAudioSource.clip = exciterEngineClip;

    engineAudioSource.volume = exciterEngineVolume;

    //==============================
    // AUDIO 2D
    //==============================
    engineAudioSource.spatialBlend = 0f;

    // Tắt Doppler hoàn toàn
    engineAudioSource.dopplerLevel = 0f;

    // Pitch cố định
    engineAudioSource.pitch = 1f;

    engineAudioSource.playOnAwake = false;
    engineAudioSource.loop = true;

    // Không cần khoảng cách / rolloff cho 2D
    engineAudioSource.minDistance = 1f;
    engineAudioSource.maxDistance = 500f;

    engineAudioSource.Stop();
    engineAudioSource.Play();
}

    //=============================================================
    // DISTANCE COLLISION
    //=============================================================
    private void CheckDistanceCollisions()
    {
        if (Time.time - spawnTime < 0.2f)
            return;

        //=========================================================
        // 1. PLAYER
        //=========================================================
        if (playerTransform != null)
        {
            float distToPlayer =
                Vector3.Distance(
                    transform.position,
                    playerTransform.position
                );

            if (distToPlayer < 2.2f)
            {
                TriggerPlayerHit(
                    playerTransform.gameObject
                );

                return;
            }
        }

        //=========================================================
        // 2. OBSTACLES
        //=========================================================
        GameObject[] obstacles =
            GameObject.FindGameObjectsWithTag(
                "Obstacle"
            );

        foreach (
            GameObject obs
            in obstacles
        )
        {
            if (
                obs == null ||
                obs == gameObject
            )
            {
                continue;
            }

            float distToObstacle =
                Vector3.Distance(
                    transform.position,
                    obs.transform.position
                );

            if (distToObstacle < 2.0f)
            {
                TriggerObstacleExplosion(
                    obs
                );

                break;
            }
        }
    }

    //=============================================================
    // EXPLOSION EFFECT
    //=============================================================
    private void SpawnExplosionEffect(
        Vector3 pos
    )
    {
        // Kiểm tra nếu tính năng nổ đang BẬT
        if (PlayerController.EnableExplosionStatic)
        {
            GameObject prefabToUse =
                explosionEffectPrefab != null
                    ? explosionEffectPrefab
                    : PlayerController.ExplosionEffectPrefabStatic;

            if (prefabToUse != null)
            {
                Instantiate(
                    prefabToUse,
                    pos,
                    Quaternion.identity
                );
            }
        }
    }

    //=============================================================
    // OBSTACLE HIT
    //=============================================================
    private void TriggerObstacleExplosion(
        GameObject obstacleObj = null
    )
    {
        if (isKnockedBack)
            return;

        Vector3 spawnPos =
            obstacleObj != null
                ? (
                    transform.position +
                    obstacleObj.transform.position
                  ) * 0.5f
                : transform.position;

        SpawnExplosionEffect(
            spawnPos
        );

        // Húc xe -> bay cao + văng lùi
        ApplyKnockback(
            new Vector3(
                0f,
                12f,
                -18f
            )
        );
    }

    //=============================================================
    // PLAYER HIT
    //=============================================================
    private void TriggerPlayerHit(
        GameObject playerObj
    )
    {
        if (isKnockedBack)
            return;

        Vector3 spawnPos =
            (
                transform.position +
                playerObj.transform.position
            ) * 0.5f;

        SpawnExplosionEffect(
            spawnPos
        );

        ApplyKnockback(
            new Vector3(
                0f,
                6f,
                -10f
            )
        );

        PlayerController player =
            playerObj.GetComponentInParent<PlayerController>();

        if (player != null)
        {
            player.ApplyKnockback(
                new Vector3(
                    0f,
                    4f,
                    6f
                )
            );
        }
    }

    //=============================================================
    // KNOCKBACK
    //=============================================================
    public void ApplyKnockback(
        Vector3 force
    )
    {
        isKnockedBack = true;

        // TẮT ENGINE AUDIO KHI XE BAY
        if (engineAudioSource != null)
        {
            engineAudioSource.Stop();
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            rb.linearDamping = 1.5f;
            rb.angularDamping = 2f;

            rb.AddForce(
                force,
                ForceMode.Impulse
            );

            rb.AddTorque(
                new Vector3(
                    15f,
                    5f,
                    20f
                ),
                ForceMode.Impulse
            );
        }

        Destroy(
            gameObject,
            3f
        );
    }
}
