
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExciterSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private GameObject exciterPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float minSpawnInterval = 6f;
    [SerializeField] private float maxSpawnInterval = 10f;
    [SerializeField] private float warningDuration = 2.5f;
    [SerializeField] private float spawnDistanceBehind = 35f;

    [Header("Redline Tracking")]
    [Tooltip("Độ trễ khi redline bám theo X của Player.")]
    [SerializeField] private float targetFollowSmoothTime = 0.12f;

    [Tooltip("Thời gian cuối redline bắt đầu nhấp nháy.")]
    [SerializeField] private float blinkDuration = 0.8f;

    [Tooltip("Tần số nhấp nháy Redline.")]
    [SerializeField] private float blinkFrequency = 10f;

    [Header("Redline Visual")]
    [SerializeField] private float warningLineWidth = 1.2f;
    [SerializeField] private float warningLineHeight = 0.08f;
    [SerializeField] private float warningLineLength = 60f;

    [Header("Warning Audio")]
    [SerializeField] private AudioClip warningAudioClip;

    [Range(0f, 1f)]
    [SerializeField] private float warningAudioVolume = 1f;

    [SerializeField] private float warningAudioMinDistance = 5f;
    [SerializeField] private float warningAudioMaxDistance = 35f;

    private float nextSpawnTime;
    private bool isSpawning = false;

    private GameObject warningVisualObj;
    private Renderer warningRenderer;

    // X hiện tại của Redline sau khi có độ trễ
    private float trackedWarningX;

    // Velocity dùng cho SmoothDamp
    private float warningXVelocity;

    // X cuối cùng trước khi Redline bị khóa
    private float lockedWarningX;

    // Đã khóa Redline chưa?
    private bool isWarningLocked = false;

    private void Start()
    {
        FindPlayer();
        ScheduleNextSpawn();
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            return;
        }

        //=========================================================
        // REDLINE TRACK PLAYER
        //=========================================================
        if (isSpawning && warningVisualObj != null)
        {
            UpdateWarningLine();
        }

        //=========================================================
        // SPAWN
        //=========================================================
        if (Time.time >= nextSpawnTime && !isSpawning)
        {
            StartCoroutine(SpawnExciterSequence());
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

    private void ScheduleNextSpawn()
    {
        nextSpawnTime = Time.time + Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    //=============================================================
    // UPDATE REDLINE
    //=============================================================
    private void UpdateWarningLine()
    {
        if (playerTransform == null || warningVisualObj == null)
            return;

        float playerX = playerTransform.position.x;

        //=========================================================
        // GIAI ĐOẠN 1:
        // REDLINE BÁM THEO PLAYER NHƯNG CÓ ĐỘ TRỄ NHẸ
        //=========================================================
        if (!isWarningLocked)
        {
            trackedWarningX = Mathf.SmoothDamp(
                trackedWarningX,
                playerX,
                ref warningXVelocity,
                targetFollowSmoothTime
            );

            float pY = playerTransform.position.y + 0.2f;
            float centerZ = playerTransform.position.z + 10f;

            warningVisualObj.transform.position = new Vector3(
                trackedWarningX,
                pY,
                centerZ
            );
        }
        else
        {
            //=====================================================
            // GIAI ĐOẠN 2:
            // REDLINE ĐÃ KHÓA -> ĐỨNG YÊN
            //=====================================================
            float pY = playerTransform.position.y + 0.2f;
            float centerZ = playerTransform.position.z + 10f;

            warningVisualObj.transform.position = new Vector3(
                lockedWarningX,
                pY,
                centerZ
            );
        }
    }

    //=============================================================
    // MAIN SEQUENCE
    //=============================================================
    private IEnumerator SpawnExciterSequence()
    {
        isSpawning = true;
        isWarningLocked = false;

        //=========================================================
        // 1. TẠO REDLINE
        //=========================================================
        CreateWarningLine();

        // Bắt đầu tại vị trí Player hiện tại
        trackedWarningX = playerTransform != null
            ? playerTransform.position.x
            : 0f;

        warningXVelocity = 0f;

        //=========================================================
        // 2. PHÁT ÂM THANH CẢNH BÁO
        //=========================================================
        PlayWarningSound();

        //=========================================================
        // 3. ĐẾM NGƯỢC
        //=========================================================
        float timer = 0f;

        while (timer < warningDuration)
        {
            timer += Time.deltaTime;

            float remainingTime = warningDuration - timer;

            //=====================================================
            // 0.8 GIÂY CUỐI
            // REDLINE KHÓA + NHẤP NHÁY
            //=====================================================
            if (remainingTime <= blinkDuration)
            {
                // Chỉ khóa đúng một lần
                if (!isWarningLocked)
                {
                    lockedWarningX = trackedWarningX;
                    isWarningLocked = true;
                }

                // Nhấp nháy
                if (warningRenderer != null)
                {
                    bool isVisible =
                        Mathf.FloorToInt(Time.time * blinkFrequency) % 2 == 0;

                    warningRenderer.enabled = isVisible;
                }
            }
            else
            {
                if (warningRenderer != null)
                {
                    warningRenderer.enabled = true;
                }
            }

            yield return null;
        }

        //=========================================================
        // 4. HỦY REDLINE
        //=========================================================
        if (warningVisualObj != null)
        {
            Destroy(warningVisualObj);
            warningVisualObj = null;
            warningRenderer = null;
        }

        //=========================================================
        // 5. SPAWN EXCITER THEO REDLINE ĐÃ KHÓA
        //=========================================================
        if (exciterPrefab != null && playerTransform != null)
        {
            // X cuối cùng mà Redline đã khóa
            float exactTargetX = lockedWarningX;

            float startZ =
                playerTransform.position.z - spawnDistanceBehind;

            float endZ =
                playerTransform.position.z + 100f;

            float spawnY =
                playerTransform.position.y + 0.8f;

            List<Vector3> exciterPath = new List<Vector3>();

            //=====================================================
            // TẠO ĐƯỜNG THẲNG THEO REDLINE
            //=====================================================
            for (float z = startZ; z <= endZ; z += 5f)
            {
                exciterPath.Add(
                    new Vector3(
                        exactTargetX,
                        spawnY,
                        z
                    )
                );
            }

            //=====================================================
            // SPAWN
            //=====================================================
            GameObject newExciter = Instantiate(
                exciterPrefab,
                exciterPath[0],
                Quaternion.identity
            );

            ExciterController exciterScript =
                newExciter.GetComponent<ExciterController>();

            if (exciterScript != null)
            {
                exciterScript.SetPath(
                    exciterPath,
                    playerTransform
                );
            }
        }

        //=========================================================
        // 6. KẾT THÚC SEQUENCE
        //=========================================================
        isWarningLocked = false;
        isSpawning = false;

        ScheduleNextSpawn();
    }

    //=============================================================
    // CREATE REDLINE
    //=============================================================
    private void CreateWarningLine()
    {
        if (playerTransform == null)
            return;

        warningVisualObj = GameObject.CreatePrimitive(
            PrimitiveType.Cube
        );

        warningVisualObj.name = "WarningRedLine";

        // Xóa collider vì chỉ là visual
        Collider col = warningVisualObj.GetComponent<Collider>();

        if (col != null)
        {
            Destroy(col);
        }

        //=========================================================
        // SCALE
        //=========================================================
        warningVisualObj.transform.localScale = new Vector3(
            warningLineWidth,
            warningLineHeight,
            warningLineLength
        );

        //=========================================================
        // MATERIAL
        //=========================================================
        warningRenderer =
            warningVisualObj.GetComponent<Renderer>();

        Shader unlitShader =
            Shader.Find("Universal Render Pipeline/Unlit");

        if (unlitShader == null)
        {
            unlitShader = Shader.Find("Unlit/Color");
        }

        if (unlitShader != null)
        {
            Material redMat = new Material(unlitShader);

            redMat.color = Color.red;

            if (redMat.HasProperty("_BaseColor"))
            {
                redMat.SetColor("_BaseColor", Color.red);
            }

            warningRenderer.material = redMat;
        }

        //=========================================================
        // POSITION BAN ĐẦU
        //=========================================================
        warningVisualObj.transform.position = new Vector3(
            trackedWarningX,
            playerTransform.position.y + 0.2f,
            playerTransform.position.z + 10f
        );
    }

    //=============================================================
    // WARNING SOUND
    //=============================================================
    private void PlayWarningSound()
{
    if (warningAudioClip == null)
        return;

    GameObject audioObj =
        new GameObject("ExciterWarningAudio");

    AudioSource source =
        audioObj.AddComponent<AudioSource>();

    source.clip = warningAudioClip;
    source.volume = warningAudioVolume;

    //==============================
    // AUDIO 2D
    //==============================
    source.spatialBlend = 0f;

    // Tắt Doppler
    source.dopplerLevel = 0f;

    // Pitch cố định
    source.pitch = 1f;

    source.playOnAwake = false;
    source.loop = false;

    source.Play();

    Destroy(
        audioObj,
        warningAudioClip.length + 0.1f
    );
}
}
