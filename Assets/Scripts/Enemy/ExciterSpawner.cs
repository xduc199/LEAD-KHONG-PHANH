using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExciterSpawner : MonoBehaviour
{
    //=============================================================
    // REFERENCES
    //=============================================================

    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private GameObject exciterPrefab;


    //=============================================================
    // SPAWN
    //=============================================================

    [Header("Spawn Settings")]

    [SerializeField]
    private float minSpawnInterval = 6f;

    [SerializeField]
    private float maxSpawnInterval = 10f;

    [Tooltip("Thời gian cảnh báo trước khi Exciter xuất hiện.")]
    [SerializeField]
    private float warningDuration = 2.5f;

    [Tooltip("Thời gian cuối RedLine bắt đầu nhấp nháy.")]
    [SerializeField]
    private float blinkDuration = 0.8f;

    [Tooltip("Exciter xuất hiện phía sau Player.")]
    [SerializeField]
    private float spawnDistanceBehind = 20f;

    [Tooltip("RedLine kéo dài về phía trước Player.")]
    [SerializeField]
    private float pathDistanceAhead = 100f;


    //=============================================================
    // MULTIPLE EXCITER
    //=============================================================

    [Header("Multiple Exciter")]

    [Tooltip("Số Exciter tối đa trong một lần. 1 hoặc 2.")]
    [SerializeField]
    private int maxExciterPerWave = 2;

    [Tooltip("Khoảng cách tối thiểu giữa 2 RedLine.")]
    [SerializeField]
    private float redLineSeparation = 3.5f;


    //=============================================================
    // RANDOM REDLINE MOVEMENT
    //=============================================================

    [Header("Independent RedLine Movement")]

    [Tooltip("RedLine có thể lệch tối đa bao nhiêu so với X ban đầu.")]
    [SerializeField]
    private float randomMoveRadius = 3.5f;

    [Tooltip("Khoảng thời gian RedLine đổi mục tiêu X.")]
    [SerializeField]
    private float minRandomMoveInterval = 0.35f;

    [SerializeField]
    private float maxRandomMoveInterval = 0.8f;

    [Tooltip("Tốc độ RedLine di chuyển trái phải.")]
    [SerializeField]
    private float redLineMoveSmoothTime = 0.18f;

    [Tooltip("Khoảng cách RedLine tối đa so với Player khi bắt đầu.")]
    [SerializeField]
    private float playerAreaLimit = 5f;


    //=============================================================
    // PATH
    //=============================================================

    [Header("Curved Red Line")]

    [Tooltip("Số điểm tạo nên đường cong.")]
    [SerializeField]
    private int pathPointCount = 32;

    [Tooltip("Độ rộng tối đa đường cong sang trái/phải.")]
    [SerializeField]
    private float curveAmplitude = 4.0f;

    [Tooltip("Độ cong tổng thể.")]
    [SerializeField]
    private float curveFrequency = 1.15f;

    [Tooltip("Độ cong phụ ngẫu nhiên.")]
    [SerializeField]
    private float secondaryCurveAmplitude = 1.2f;

    [Tooltip("Độ cong phụ.")]
    [SerializeField]
    private float secondaryCurveFrequency = 2.1f;

    [Tooltip("Giới hạn khoảng cách RedLine lệch khỏi tâm.")]
    [SerializeField]
    private float maxCurveOffset = 6f;


    //=============================================================
    // REDLINE HEIGHT
    //=============================================================

    [Header("Redline Height")]

    [Tooltip("Độ cao RedLine so với Player.")]
    [SerializeField]
    private float redLineHeightOffset = 1.2f;


    //=============================================================
    // BLINK
    //=============================================================

    [Header("Redline Blink")]

    [Tooltip("Tần số nhấp nháy RedLine.")]
    [SerializeField]
    private float blinkFrequency = 10f;


    //=============================================================
    // REDLINE VISUAL
    //=============================================================

    [Header("Redline Visual")]

    [Tooltip("Độ rộng RedLine.")]
    [SerializeField]
    private float warningLineWidth = 0.45f;

    [SerializeField]
    private Material warningLineMaterial;


    //=============================================================
    // AUDIO
    //=============================================================

    [Header("Warning Audio")]

    [SerializeField]
    private AudioClip warningAudioClip;

    [Range(0f, 1f)]
    [SerializeField]
    private float warningAudioVolume = 1f;


    //=============================================================
    // EXCITER STAGGER
    //=============================================================

    [Header("Exciter Spawn Stagger")]

    [Tooltip("Nếu có 2 Exciter, Exciter thứ 2 sẽ xuất hiện trễ ít nhất từng này.")]
    [SerializeField]
    private float minExciterSpawnDelay = 0.25f;

    [Tooltip("Nếu có 2 Exciter, Exciter thứ 2 sẽ xuất hiện trễ tối đa từng này.")]
    [SerializeField]
    private float maxExciterSpawnDelay = 0.7f;


    //=============================================================
    // INTERNAL
    //=============================================================

    private float nextSpawnTime;
    private bool isSpawning;

    private float curveSeed;


    //=============================================================
    // WARNING LINE DATA
    //=============================================================

    private readonly List<LineRenderer> warningLines =
        new List<LineRenderer>();

    private readonly List<Vector3> lockedWarningCenters =
        new List<Vector3>();

    private readonly List<List<Vector3>> lockedPaths =
        new List<List<Vector3>>();

    private readonly List<float> trackedWarningX =
        new List<float>();

    private readonly List<float> warningXVelocities =
        new List<float>();

    private readonly List<float> randomTargetX =
        new List<float>();

    private readonly List<float> nextRandomMoveTime =
        new List<float>();

    private readonly List<float> lineMoveRadius =
        new List<float>();

    private readonly List<bool> warningLocks =
        new List<bool>();


    //=============================================================
    // START
    //=============================================================

    private void Start()
    {
        FindPlayer();

        ScheduleNextSpawn();
    }


    //=============================================================
    // UPDATE
    //=============================================================

    private void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            return;
        }

        if (isSpawning)
        {
            UpdateWarningLines();
        }

        if (
            Time.time >= nextSpawnTime &&
            !isSpawning
        )
        {
            StartCoroutine(
                SpawnExciterSequence()
            );
        }
    }


    //=============================================================
    // FIND PLAYER
    //=============================================================

    private void FindPlayer()
    {
        if (playerTransform != null)
            return;

        GameObject playerObj =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            playerTransform =
                playerObj.transform;
        }
    }


    //=============================================================
    // SCHEDULE
    //=============================================================

    private void ScheduleNextSpawn()
    {
        nextSpawnTime =
            Time.time +
            Random.Range(
                minSpawnInterval,
                maxSpawnInterval
            );
    }


    //=============================================================
    // MAIN SPAWN SEQUENCE
    //=============================================================

    private IEnumerator SpawnExciterSequence()
    {
        if (playerTransform == null)
            yield break;

        isSpawning = true;

        ClearWarningData();

        //=========================================================
        // RANDOM 1 - 2 EXCITER
        //=========================================================

        int count =
            Random.Range(
                1,
                Mathf.Clamp(
                    maxExciterPerWave,
                    1,
                    2
                ) + 1
            );


        //=========================================================
        // RANDOM SEED
        //=========================================================

        curveSeed =
            Random.Range(
                0f,
                10000f
            );


        //=========================================================
        // CREATE REDLINES
        //=========================================================

        for (
            int i = 0;
            i < count;
            i++
        )
        {
            CreateIndependentRedLine(
                i,
                count
            );
        }


        //=========================================================
        // WARNING SOUND
        //=========================================================

        PlayWarningSound();


        //=========================================================
        // COUNTDOWN
        //=========================================================

        float timer = 0f;

        while (
            timer <
            warningDuration
        )
        {
            timer +=
                Time.deltaTime;

            float remainingTime =
                warningDuration -
                timer;


            //=====================================================
            // UPDATE INDEPENDENT REDLINES
            //=====================================================

            UpdateIndependentRedLines();


            //=====================================================
            // LOCK PATH
            //=====================================================

            if (
                remainingTime <=
                blinkDuration
            )
            {
                for (
                    int i = 0;
                    i < warningLines.Count;
                    i++
                )
                {
                    if (warningLocks[i])
                        continue;

                    LockRedLine(i);
                }
            }


            //=====================================================
            // BLINK
            //=====================================================

            UpdateBlink();


            //=====================================================
            // PREVIEW
            //=====================================================

            UpdatePreviewLines();


            yield return null;
        }


        //=========================================================
        // FORCE LOCK
        //=========================================================

        for (
            int i = 0;
            i < warningLines.Count;
            i++
        )
        {
            if (!warningLocks[i])
            {
                LockRedLine(i);
            }
        }


        //=========================================================
        // REMOVE REDLINES
        //=========================================================

        DestroyWarningLines();


        //=========================================================
        // SPAWN EXCITERS
        //=========================================================

        yield return StartCoroutine(
            SpawnExcitersWithDelay()
        );


        //=========================================================
        // RESET
        //=========================================================

        isSpawning = false;

        ClearWarningData();

        ScheduleNextSpawn();
    }


    //=============================================================
    // CREATE INDEPENDENT REDLINE
    //=============================================================

    private void CreateIndependentRedLine(
        int index,
        int count
    )
    {
        if (playerTransform == null)
            return;


        //=========================================================
        // PLAYER X CHỈ DÙNG LÀM TÂM BAN ĐẦU
        // KHÔNG FOLLOW PLAYER SAU ĐÓ
        //=========================================================

        float playerX =
            playerTransform.position.x;


        //=========================================================
        // CREATE INITIAL OFFSET
        //=========================================================

        float initialOffset = 0f;

        if (count == 2)
        {
            initialOffset =
                index == 0
                    ? -redLineSeparation * 0.5f
                    : redLineSeparation * 0.5f;
        }


        //=========================================================
        // RANDOM OFFSET RIÊNG
        //=========================================================

        float randomOffset =
            Random.Range(
                -randomMoveRadius,
                randomMoveRadius
            );


        float startX =
            playerX +
            initialOffset +
            randomOffset;


        //=========================================================
        // LIMIT
        //=========================================================

        startX =
            Mathf.Clamp(
                startX,
                playerX - playerAreaLimit,
                playerX + playerAreaLimit
            );


        //=========================================================
        // DATA
        //=========================================================

        trackedWarningX.Add(
            startX
        );

        warningXVelocities.Add(
            0f
        );

        randomTargetX.Add(
            startX
        );

        nextRandomMoveTime.Add(
            Time.time +
            Random.Range(
                0.05f,
                maxRandomMoveInterval
            )
        );

        lineMoveRadius.Add(
            Random.Range(
                randomMoveRadius * 0.7f,
                randomMoveRadius
            )
        );

        warningLocks.Add(
            false
        );

        lockedWarningCenters.Add(
            Vector3.zero
        );

        lockedPaths.Add(
            new List<Vector3>()
        );


        //=========================================================
        // CREATE VISUAL
        //=========================================================

        CreateWarningLine(
            index,
            startX
        );
    }


    //=============================================================
    // UPDATE INDEPENDENT REDLINES
    //=============================================================

    private void UpdateIndependentRedLines()
    {
        if (playerTransform == null)
            return;


        int count =
            warningLines.Count;


        for (
            int i = 0;
            i < count;
            i++
        )
        {
            if (
                warningLines[i] == null ||
                warningLocks[i]
            )
            {
                continue;
            }


            //=====================================================
            // ĐỔI MỤC TIÊU NGẪU NHIÊN
            //=====================================================

            if (
                Time.time >=
                nextRandomMoveTime[i]
            )
            {
                ChooseNewRandomTarget(i);

                nextRandomMoveTime[i] =
                    Time.time +
                    Random.Range(
                        minRandomMoveInterval,
                        maxRandomMoveInterval
                    );
            }


            //=====================================================
            // SMOOTH MOVE
            //=====================================================

            float velocity =
                warningXVelocities[i];


            trackedWarningX[i] =
                Mathf.SmoothDamp(
                    trackedWarningX[i],
                    randomTargetX[i],
                    ref velocity,
                    redLineMoveSmoothTime
                );


            warningXVelocities[i] =
                velocity;
        }
    }


    //=============================================================
    // CHOOSE RANDOM TARGET
    //=============================================================

    private void ChooseNewRandomTarget(
        int index
    )
    {
        if (playerTransform == null)
            return;

        if (
            index < 0 ||
            index >= randomTargetX.Count
        )
        {
            return;
        }


        float playerX =
            playerTransform.position.x;


        float radius =
            lineMoveRadius[index];


        //=========================================================
        // RANDOM LEFT / RIGHT
        //=========================================================

        float newTarget =
            playerX +
            Random.Range(
                -radius,
                radius
            );


        //=========================================================
        // LIMIT
        //=========================================================

        newTarget =
            Mathf.Clamp(
                newTarget,
                playerX - playerAreaLimit,
                playerX + playerAreaLimit
            );


        //=========================================================
        // GIỮ 2 LINE KHÔNG DÍNH NHAU
        //=========================================================

        if (warningLines.Count == 2)
        {
            if (index == 0)
            {
                float otherX =
                    randomTargetX[1];

                if (
                    Mathf.Abs(
                        newTarget - otherX
                    ) <
                    redLineSeparation * 0.55f
                )
                {
                    newTarget -=
                        redLineSeparation;
                }
            }
            else
            {
                float otherX =
                    randomTargetX[0];

                if (
                    Mathf.Abs(
                        newTarget - otherX
                    ) <
                    redLineSeparation * 0.55f
                )
                {
                    newTarget +=
                        redLineSeparation;
                }
            }


            newTarget =
                Mathf.Clamp(
                    newTarget,
                    playerX - playerAreaLimit,
                    playerX + playerAreaLimit
                );
        }


        randomTargetX[index] =
            newTarget;
    }


    //=============================================================
    // LOCK REDLINE
    //=============================================================

    private void LockRedLine(
        int index
    )
    {
        if (
            index < 0 ||
            index >= warningLines.Count
        )
        {
            return;
        }

        if (warningLocks[index])
            return;


        lockedWarningCenters[index] =
            new Vector3(
                trackedWarningX[index],
                playerTransform.position.y +
                redLineHeightOffset,
                0f
            );


        warningLocks[index] =
            true;


        GenerateLockedPath(index);


        if (
            warningLines[index] != null
        )
        {
            warningLines[index].enabled =
                true;
        }
    }


    //=============================================================
    // UPDATE WARNING LINES
    //=============================================================

    private void UpdateWarningLines()
    {
        UpdateIndependentRedLines();

        UpdatePreviewLines();
    }


    //=============================================================
    // UPDATE PREVIEW
    //=============================================================

    private void UpdatePreviewLines()
    {
        int count =
            warningLines.Count;


        for (
            int i = 0;
            i < count;
            i++
        )
        {
            if (
                warningLines[i] == null ||
                warningLocks[i]
            )
            {
                continue;
            }


            List<Vector3> previewPath =
                GeneratePreviewPath(
                    trackedWarningX[i],
                    i
                );


            ApplyPathToLine(
                warningLines[i],
                previewPath
            );
        }
    }


    //=============================================================
    // UPDATE BLINK
    //=============================================================

    private void UpdateBlink()
    {
        for (
            int i = 0;
            i < warningLines.Count;
            i++
        )
        {
            if (
                warningLines[i] == null ||
                !warningLocks[i]
            )
            {
                continue;
            }


            bool visible =
                Mathf.FloorToInt(
                    Time.time *
                    blinkFrequency
                ) % 2 == 0;


            warningLines[i].enabled =
                visible;
        }
    }


    //=============================================================
    // GENERATE PREVIEW PATH
    //=============================================================

    private List<Vector3> GeneratePreviewPath(
        float centerX,
        int lineIndex
    )
    {
        List<Vector3> points =
            new List<Vector3>();


        if (playerTransform == null)
            return points;


        int count =
            Mathf.Max(
                8,
                pathPointCount
            );


        float startZ =
            playerTransform.position.z -
            spawnDistanceBehind;


        float endZ =
            playerTransform.position.z +
            pathDistanceAhead;


        //=========================================================
        // SEED RIÊNG TỪNG LINE
        //=========================================================

        float seed =
            curveSeed +
            lineIndex * 137.37f;


        for (
            int i = 0;
            i < count;
            i++
        )
        {
            float t =
                i /
                (float)(count - 1);


            float z =
                Mathf.Lerp(
                    startZ,
                    endZ,
                    t
                );


            //=====================================================
            // MAIN CURVE
            //=====================================================

            float mainCurve =
                Mathf.Sin(
                    t *
                    Mathf.PI *
                    2f *
                    curveFrequency +
                    seed
                );


            //=====================================================
            // SECONDARY CURVE
            //=====================================================

            float secondaryCurve =
                Mathf.Sin(
                    t *
                    Mathf.PI *
                    2f *
                    secondaryCurveFrequency +
                    seed *
                    1.73f
                );


            float offset =
                mainCurve *
                curveAmplitude;


            offset +=
                secondaryCurve *
                secondaryCurveAmplitude;


            //=====================================================
            // EDGE WEIGHT
            //=====================================================

            float edgeWeight =
                Mathf.Sin(
                    t *
                    Mathf.PI
                );


            offset *=
                edgeWeight;


            //=====================================================
            // LIMIT
            //=====================================================

            offset =
                Mathf.Clamp(
                    offset,
                    -maxCurveOffset,
                    maxCurveOffset
                );


            //=====================================================
            // FINAL POSITION
            //=====================================================

            points.Add(
                new Vector3(
                    centerX + offset,

                    playerTransform.position.y +
                    redLineHeightOffset,

                    z
                )
            );
        }


        return points;
    }


    //=============================================================
    // GENERATE LOCKED PATH
    //=============================================================

    private void GenerateLockedPath(
        int index
    )
    {
        if (
            index < 0 ||
            index >= lockedPaths.Count
        )
        {
            return;
        }


        lockedPaths[index] =
            GeneratePreviewPath(
                trackedWarningX[index],
                index
            );


        if (
            index < warningLines.Count &&
            warningLines[index] != null
        )
        {
            ApplyPathToLine(
                warningLines[index],
                lockedPaths[index]
            );
        }
    }


    //=============================================================
    // APPLY PATH
    //=============================================================

    private void ApplyPathToLine(
        LineRenderer line,
        List<Vector3> points
    )
    {
        if (
            line == null ||
            points == null ||
            points.Count == 0
        )
        {
            return;
        }


        line.positionCount =
            points.Count;


        for (
            int i = 0;
            i < points.Count;
            i++
        )
        {
            line.SetPosition(
                i,
                points[i]
            );
        }
    }


    //=============================================================
    // CREATE WARNING LINE
    //=============================================================

    private void CreateWarningLine(
        int index,
        float centerX
    )
    {
        if (playerTransform == null)
            return;


        GameObject lineObj =
            new GameObject(
                "WarningRedLine_" +
                index
            );


        LineRenderer line =
            lineObj.AddComponent<LineRenderer>();


        warningLines.Add(
            line
        );


        //=========================================================
        // RENDER SETTINGS
        //=========================================================

        line.useWorldSpace =
            true;


        line.startWidth =
            warningLineWidth;


        line.endWidth =
            warningLineWidth;


        line.numCornerVertices =
            8;


        line.numCapVertices =
            8;


        line.alignment =
            LineAlignment.View;


        line.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;


        line.receiveShadows =
            false;


        //=========================================================
        // MATERIAL
        //=========================================================

        if (
            warningLineMaterial != null
        )
        {
            line.material =
                warningLineMaterial;
        }
        else
        {
            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Unlit"
                );


            if (shader == null)
            {
                shader =
                    Shader.Find(
                        "Unlit/Color"
                    );
            }


            if (shader != null)
            {
                Material mat =
                    new Material(shader);


                mat.color =
                    Color.red;


                if (
                    mat.HasProperty(
                        "_BaseColor"
                    )
                )
                {
                    mat.SetColor(
                        "_BaseColor",
                        Color.red
                    );
                }


                line.material =
                    mat;
            }
        }


        //=========================================================
        // INITIAL PATH
        //=========================================================

        List<Vector3> initialPath =
            GeneratePreviewPath(
                centerX,
                index
            );


        ApplyPathToLine(
            line,
            initialPath
        );
    }


    //=============================================================
    // SPAWN EXCITERS WITH DELAY
    //=============================================================

    private IEnumerator SpawnExcitersWithDelay()
    {
        if (
            exciterPrefab == null ||
            playerTransform == null
        )
        {
            yield break;
        }


        int count =
            lockedPaths.Count;


        for (
            int i = 0;
            i < count;
            i++
        )
        {
            List<Vector3> path =
                lockedPaths[i];


            if (
                path == null ||
                path.Count < 2
            )
            {
                continue;
            }


            //=====================================================
            // SPAWN POSITION
            //=====================================================

            Vector3 spawnPosition =
                path[0];


            //=====================================================
            // SPAWN
            //=====================================================

            GameObject newExciter =
                Instantiate(
                    exciterPrefab,
                    spawnPosition,
                    Quaternion.identity
                );


            //=====================================================
            // CONTROLLER
            //=====================================================

            ExciterController controller =
                newExciter.GetComponent<ExciterController>();


            if (controller != null)
            {
                controller.SetPath(
                    new List<Vector3>(
                        path
                    ),
                    playerTransform
                );
            }
            else
            {
                Debug.LogWarning(
                    "[ExciterSpawner] Exciter prefab không có ExciterController."
                );
            }


            //=====================================================
            // STAGGER
            //=====================================================

            if (i < count - 1)
            {
                float delay =
                    Random.Range(
                        minExciterSpawnDelay,
                        maxExciterSpawnDelay
                    );


                yield return new WaitForSeconds(
                    delay
                );
            }
        }
    }


    //=============================================================
    // WARNING SOUND
    //=============================================================

    private void PlayWarningSound()
    {
        if (warningAudioClip == null)
            return;


        GameObject audioObj =
            new GameObject(
                "ExciterWarningAudio"
            );


        AudioSource source =
            audioObj.AddComponent<AudioSource>();


        source.clip =
            warningAudioClip;


        source.volume =
            warningAudioVolume;


        source.spatialBlend =
            0f;


        source.dopplerLevel =
            0f;


        source.pitch =
            1f;


        source.playOnAwake =
            false;


        source.loop =
            false;


        source.Play();


        Destroy(
            audioObj,
            warningAudioClip.length +
            0.1f
        );
    }


    //=============================================================
    // DESTROY WARNING LINES
    //=============================================================

    private void DestroyWarningLines()
    {
        for (
            int i = 0;
            i < warningLines.Count;
            i++
        )
        {
            if (warningLines[i] != null)
            {
                Destroy(
                    warningLines[i].gameObject
                );
            }
        }


        warningLines.Clear();
    }


    //=============================================================
    // CLEAR WARNING DATA
    //=============================================================

    private void ClearWarningData()
    {
        trackedWarningX.Clear();

        warningXVelocities.Clear();

        randomTargetX.Clear();

        nextRandomMoveTime.Clear();

        lineMoveRadius.Clear();

        warningLocks.Clear();

        lockedWarningCenters.Clear();

        lockedPaths.Clear();
    }


    //=============================================================
    // ON DESTROY
    //=============================================================

    private void OnDestroy()
    {
        DestroyWarningLines();
    }
}