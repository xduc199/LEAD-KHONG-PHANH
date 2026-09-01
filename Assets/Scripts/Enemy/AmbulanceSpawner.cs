using System.Collections;
using UnityEngine;

public class AmbulanceSpawner : MonoBehaviour
{
    //=============================================================
    // REFERENCES
    //=============================================================

    [Header("References")]

    [SerializeField]
    private Transform playerTransform;

    [SerializeField]
    private GameObject ambulancePrefab;


    //=============================================================
    // LANE
    //=============================================================

    [Header("3 Lane Settings")]

    [SerializeField]
    private float leftLaneX = -5f;

    [SerializeField]
    private float centerLaneX = 0f;

    [SerializeField]
    private float rightLaneX = 5f;


    //=============================================================
    // SPAWN
    //=============================================================

    [Header("Spawn Settings")]

    [SerializeField]
    private float minSpawnInterval = 8f;

    [SerializeField]
    private float maxSpawnInterval = 13f;

    [Tooltip("Ambulance xuất hiện phía sau Player.")]
    [SerializeField]
    private float spawnDistanceBehind = 35f;

    [Tooltip("RedLine kéo dài phía trước.")]
    [SerializeField]
    private float warningDistanceAhead = 220f;

    [Tooltip("RedLine kéo dài phía sau.")]
    [SerializeField]
    private float warningDistanceBehind = 50f;


    //=============================================================
    // WARNING
    //=============================================================

    [Header("RedLine Warning")]

    [SerializeField]
    private float warningDuration = 2.5f;

    [SerializeField]
    private float blinkDuration = 0.8f;

    [SerializeField]
    private float blinkFrequency = 10f;


    //=============================================================
    // LINE
    //=============================================================

    [Header("RedLine Visual")]

    [Tooltip("Độ rộng RedLine.")]
    [SerializeField]
    private float warningLineWidth = 1.2f;

    [Tooltip("Nâng RedLine khỏi mặt đường.")]
    [SerializeField]
    private float warningLineHeight = 0.18f;

    [Tooltip("Tăng thêm độ cao để chắc chắn không bị road che.")]
    [SerializeField]
    private float extraLineHeight = 0.15f;

    [SerializeField]
    private Material warningLineMaterial;

    [Tooltip("Nếu bật, tự tạo material đỏ emissive.")]
    [SerializeField]
    private bool forceCreateRedMaterial = true;


    //=============================================================
    // AUDIO
    //=============================================================

    [Header("Warning Audio")]

    [SerializeField]
    private AudioClip warningAudioClip;

    [Range(0f, 1f)]
    [SerializeField]
    private float warningAudioVolume = 1f;

    [Tooltip("Cảnh báo dạng 2D để luôn nghe được.")]
    [SerializeField]
    private bool warningAudio2D = true;


    //=============================================================
    // INTERNAL
    //=============================================================

    private float nextSpawnTime;

    private bool isSpawning;

    private LineRenderer warningLine;

    private Material runtimeWarningMaterial;

    private int selectedLane;


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


        if (
            !isSpawning &&
            Time.time >= nextSpawnTime
        )
        {
            StartCoroutine(
                SpawnAmbulanceSequence()
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
            GameObject.FindGameObjectWithTag(
                "Player"
            );


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
        float min =
            Mathf.Min(
                minSpawnInterval,
                maxSpawnInterval
            );


        float max =
            Mathf.Max(
                minSpawnInterval,
                maxSpawnInterval
            );


        nextSpawnTime =
            Time.time +
            Random.Range(
                min,
                max
            );
    }


    //=============================================================
    // MAIN SEQUENCE
    //=============================================================

    private IEnumerator SpawnAmbulanceSequence()
    {
        if (
            playerTransform == null ||
            ambulancePrefab == null
        )
        {
            isSpawning =
                false;

            ScheduleNextSpawn();

            yield break;
        }


        isSpawning =
            true;


        //=========================================================
        // RANDOM LANE
        //=========================================================

        selectedLane =
            Random.Range(
                0,
                3
            );


        float laneX =
            GetLaneX(
                selectedLane
            );


        //=========================================================
        // REDLINE
        //=========================================================

        CreateStraightWarningLine(
            laneX
        );


        //=========================================================
        // WARNING SOUND
        //=========================================================

        PlayWarningSound();


        //=========================================================
        // COUNTDOWN
        //=========================================================

        float timer =
            0f;


        while (
            timer <
            warningDuration
        )
        {
            timer +=
                Time.deltaTime;


            if (warningLine != null)
            {
                float remaining =
                    warningDuration -
                    timer;


                if (
                    remaining <=
                    blinkDuration
                )
                {
                    bool visible =
                        Mathf.FloorToInt(
                            Time.time *
                            blinkFrequency
                        ) % 2 == 0;


                    warningLine.enabled =
                        visible;
                }
                else
                {
                    warningLine.enabled =
                        true;
                }
            }


            yield return null;
        }


        //=========================================================
        // DESTROY REDLINE
        //=========================================================

        DestroyWarningLine();


        //=========================================================
        // SPAWN AMBULANCE
        //=========================================================

        SpawnAmbulance(
            selectedLane,
            laneX
        );


        //=========================================================
        // RESET
        //=========================================================

        isSpawning =
            false;


        ScheduleNextSpawn();
    }


    //=============================================================
    // GET LANE X
    //=============================================================

    private float GetLaneX(
        int lane
    )
    {
        switch (lane)
        {
            case 0:

                return leftLaneX;


            case 1:

                return centerLaneX;


            default:

                return rightLaneX;
        }
    }


    //=============================================================
    // CREATE REDLINE
    //=============================================================

    private void CreateStraightWarningLine(
        float laneX
    )
    {
        DestroyWarningLine();


        if (playerTransform == null)
            return;


        //=========================================================
        // CREATE OBJECT
        //=========================================================

        GameObject lineObject =
            new GameObject(
                "Ambulance_RedLine"
            );


        warningLine =
            lineObject.AddComponent<LineRenderer>();


        //=========================================================
        // BASIC
        //=========================================================

        warningLine.useWorldSpace =
            true;

        warningLine.positionCount =
            2;

        warningLine.startWidth =
            Mathf.Max(
                0.05f,
                warningLineWidth
            );

        warningLine.endWidth =
            Mathf.Max(
                0.05f,
                warningLineWidth
            );

        warningLine.numCornerVertices =
            8;

        warningLine.numCapVertices =
            8;

        warningLine.alignment =
            LineAlignment.View;

        warningLine.textureMode =
            LineTextureMode.Stretch;


        //=========================================================
        // RENDER
        //=========================================================

        warningLine.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;

        warningLine.receiveShadows =
            false;

        warningLine.lightProbeUsage =
            UnityEngine.Rendering.LightProbeUsage.Off;

        warningLine.reflectionProbeUsage =
            UnityEngine.Rendering.ReflectionProbeUsage.Off;

        warningLine.sortingOrder =
            500;


        //=========================================================
        // MATERIAL
        //=========================================================

        Material material =
            warningLineMaterial;


        if (
            forceCreateRedMaterial ||
            material == null
        )
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


            if (shader == null)
            {
                shader =
                    Shader.Find(
                        "Sprites/Default"
                    );
            }


            if (shader != null)
            {
                runtimeWarningMaterial =
                    new Material(shader);

                runtimeWarningMaterial.name =
                    "Ambulance_RedLine_Runtime";


                Color red =
                    new Color(
                        1f,
                        0.02f,
                        0.02f,
                        1f
                    );


                if (
                    runtimeWarningMaterial.HasProperty(
                        "_BaseColor"
                    )
                )
                {
                    runtimeWarningMaterial.SetColor(
                        "_BaseColor",
                        red
                    );
                }


                if (
                    runtimeWarningMaterial.HasProperty(
                        "_Color"
                    )
                )
                {
                    runtimeWarningMaterial.SetColor(
                        "_Color",
                        red
                    );
                }


                //=================================================
                // EMISSION
                //=================================================

                if (
                    runtimeWarningMaterial.HasProperty(
                        "_EmissionColor"
                    )
                )
                {
                    runtimeWarningMaterial.EnableKeyword(
                        "_EMISSION"
                    );


                    runtimeWarningMaterial.SetColor(
                        "_EmissionColor",
                        red * 3f
                    );
                }


                material =
                    runtimeWarningMaterial;
            }
        }


        if (material != null)
        {
            warningLine.material =
                material;
        }


        //=========================================================
        // POSITION
        //=========================================================

        float startZ =
            playerTransform.position.z -
            warningDistanceBehind;


        float endZ =
            playerTransform.position.z +
            warningDistanceAhead;


        //=========================================================
        // HEIGHT
        //=========================================================

        float y =
            playerTransform.position.y +
            warningLineHeight +
            extraLineHeight;


        //=========================================================
        // DRAW
        //=========================================================

        warningLine.SetPosition(
            0,
            new Vector3(
                laneX,
                y,
                startZ
            )
        );


        warningLine.SetPosition(
            1,
            new Vector3(
                laneX,
                y,
                endZ
            )
        );


        warningLine.enabled =
            true;
    }


    //=============================================================
    // SPAWN AMBULANCE
    //=============================================================

    private void SpawnAmbulance(
        int lane,
        float laneX
    )
    {
        if (
            ambulancePrefab == null ||
            playerTransform == null
        )
        {
            return;
        }


        //=========================================================
        // POSITION
        //=========================================================

        Vector3 spawnPosition =
            new Vector3(
                laneX,
                playerTransform.position.y,
                playerTransform.position.z -
                spawnDistanceBehind
            );


        //=========================================================
        // ROTATION
        //=========================================================

        Quaternion rotation =
            Quaternion.LookRotation(
                Vector3.forward,
                Vector3.up
            );


        //=========================================================
        // SPAWN
        //=========================================================

        GameObject ambulance =
            Instantiate(
                ambulancePrefab,
                spawnPosition,
                rotation
            );


        if (ambulance == null)
            return;


        //=========================================================
        // CONTROLLER
        //=========================================================

        AmbulanceController controller =
            ambulance.GetComponent<AmbulanceController>();


        if (controller == null)
        {
            controller =
                ambulance.GetComponentInChildren<AmbulanceController>(
                    true
                );
        }


        if (controller != null)
        {
            controller.Initialize(
                lane,
                playerTransform
            );
        }
        else
        {
            Debug.LogError(
                "[AmbulanceSpawner] " +
                "Ambulance prefab không có AmbulanceController!"
            );


            Destroy(
                ambulance
            );
        }
    }


    //=============================================================
    // WARNING AUDIO
    //=============================================================

    private void PlayWarningSound()
    {
        if (
            warningAudioClip == null
        )
        {
            Debug.LogWarning(
                "[AmbulanceSpawner] " +
                "Chưa gán Warning Audio Clip."
            );

            return;
        }


        GameObject audioObject =
            new GameObject(
                "AmbulanceWarningAudio"
            );


        AudioSource source =
            audioObject.AddComponent<AudioSource>();


        source.clip =
            warningAudioClip;

        source.volume =
            Mathf.Clamp01(
                warningAudioVolume
            );

        source.playOnAwake =
            false;

        source.loop =
            false;

        source.dopplerLevel =
            0f;

        source.spatialBlend =
            warningAudio2D
                ? 0f
                : 1f;

        source.ignoreListenerPause =
            true;


        source.Play();


        Destroy(
            audioObject,
            warningAudioClip.length +
            0.2f
        );
    }


    //=============================================================
    // DESTROY REDLINE
    //=============================================================

    private void DestroyWarningLine()
    {
        if (
            warningLine != null
        )
        {
            GameObject lineObject =
                warningLine.gameObject;


            warningLine =
                null;


            Destroy(
                lineObject
            );
        }


        //=========================================================
        // DESTROY RUNTIME MATERIAL
        //=========================================================

        if (runtimeWarningMaterial != null)
        {
            Destroy(
                runtimeWarningMaterial
            );

            runtimeWarningMaterial =
                null;
        }
    }


    //=============================================================
    // ON DESTROY
    //=============================================================

    private void OnDestroy()
    {
        DestroyWarningLine();
    }
}