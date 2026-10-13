using UnityEngine;

[DisallowMultipleComponent]
public sealed class AlleyBiomeSpawner : MonoBehaviour
{
    // ============================================================
    // SPAWN POINT
    // ============================================================

    [System.Serializable]
    public struct SpawnPointElement
    {
        public Transform point;
        public Vector3 direction;
    }

    // ============================================================
    // PLAYER
    // ============================================================

    [Header("Player")]
    [SerializeField]
    private Transform playerTransform;

    [SerializeField]
    private string playerTag = "Player";

    [SerializeField, Min(0.1f)]
    private float triggerDistance = 30f;

    // ============================================================
    // EVENT
    // ============================================================

    [Header("Ambush Event")]
    [SerializeField, Range(0f, 1f)]
    private float eventChance = 0.5f;

    [SerializeField, Min(1)]
    private int minVehicles = 1;

    [SerializeField, Min(1)]
    private int maxVehicles = 3;

    // ============================================================
    // VEHICLE
    // ============================================================

    [Header("Ambush Vehicle")]
    [SerializeField]
    private GameObject ambushVehiclePrefab;

    [Header("Per-Vehicle Speed")]
    [Tooltip("Tốc độ thấp nhất của Alley. Mỗi xe sẽ nhận tốc độ riêng trong khoảng Min-Max.")]
    [SerializeField, Min(0f)]
    private float minAmbushVehicleSpeed = 18f;

    [Tooltip("Tốc độ cao nhất của Alley. Không nên đặt quá cao nếu khoảng spawn với Player ngắn.")]
    [SerializeField, Min(0f)]
    private float maxAmbushVehicleSpeed = 24f;

    // ============================================================
    // SPAWN POINTS
    // ============================================================

    [Header("Spawn Points")]
    [SerializeField]
    private SpawnPointElement[] spawnPoints;

    [SerializeField]
    private bool autoDirectionToRoad = true;

    [SerializeField]
    private float roadCenterX = 0f;

    // ============================================================
    // AUDIO
    // ============================================================

    [Header("Ambush Audio")]
    [SerializeField]
    private AudioSource ambushAudioSource;

    [SerializeField]
    private AudioClip[] ambushRevSounds;

    [SerializeField]
    private AudioClip[] ambushLaunchSounds;

    [SerializeField, Range(0f, 1f)]
    private float revVolume = 1f;

    [SerializeField, Range(0f, 1f)]
    private float launchVolume = 1f;

    [SerializeField, Min(0f)]
    private float launchDelay = 0.12f;

    // ============================================================
    // DEBUG
    // ============================================================

    [Header("Debug")]
    [SerializeField]
    private bool debugLogs = false;

    [SerializeField]
    private bool showGizmos = true;

    // ============================================================
    // RUNTIME STATE
    // ============================================================

    private bool armed;
    private bool eventTriggered;

    private bool launchPending;
    private float launchExecuteTime;

    private int triggerPointIndex = -1;

    private int[] candidatePointIndices;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        candidatePointIndices =
            spawnPoints != null
                ? new int[spawnPoints.Length]
                : System.Array.Empty<int>();

        Log(
            "READY | SpawnPoints = " +
            (spawnPoints != null
                ? spawnPoints.Length
                : 0));
    }

    private void Update()
    {
        // --------------------------------------------------------
        // DELAYED LAUNCH
        // --------------------------------------------------------

        if (launchPending)
        {
            if (Time.time >= launchExecuteTime)
            {
                launchPending = false;

                PlayRandomLaunchSound();

                SpawnAmbushVehicles();
            }

            return;
        }

        // --------------------------------------------------------
        // ALREADY TRIGGERED
        // --------------------------------------------------------

        if (!armed || eventTriggered)
            return;

        // --------------------------------------------------------
        // FIND PLAYER
        // --------------------------------------------------------

        if (playerTransform == null)
        {
            FindPlayer();

            if (playerTransform == null)
                return;

            Log(
                "PLAYER FOUND | " +
                playerTransform.name);
        }

        // --------------------------------------------------------
        // CHECK TRIGGER POINT
        // --------------------------------------------------------

        if (triggerPointIndex < 0)
        {
            triggerPointIndex =
                FindClosestTriggerPoint();

            if (triggerPointIndex < 0)
                return;

            Log(
                "WAITING | Trigger Point = " +
                triggerPointIndex);
        }

        Transform triggerPoint =
            spawnPoints[triggerPointIndex].point;

        if (triggerPoint == null)
            return;

        float distanceSqr =
            HorizontalDistanceSqr(
                playerTransform.position,
                triggerPoint.position);

        if (distanceSqr >
            triggerDistance *
            triggerDistance)
        {
            return;
        }

        TriggerAmbush();
    }

    // ============================================================
    // ARM
    // ============================================================

    public void TrySpawnAmbush()
    {
        armed = true;
        eventTriggered = false;
        launchPending = false;

        triggerPointIndex = -1;

        Log("ARMED");
    }

    // ============================================================
    // TRIGGER
    // ============================================================

    private void TriggerAmbush()
    {
        if (eventTriggered)
            return;

        eventTriggered = true;

        float roll =
            Random.value;

        Log(
            "TRIGGER | Roll = " +
            roll +
            " | Chance = " +
            eventChance);

        if (roll > eventChance)
        {
            Log("EVENT FAILED");
            return;
        }

        // --------------------------------------------------------
        // SURPRISE REV SOUND
        // --------------------------------------------------------

        PlayRandomRevSound();

        // --------------------------------------------------------
        // DELAY
        // --------------------------------------------------------

        launchPending = true;

        launchExecuteTime =
            Time.time +
            launchDelay;

        Log(
            "AMBUSH PREPARED | Launch Delay = " +
            launchDelay);
    }

    // ============================================================
    // SPAWN
    // ============================================================

    private void SpawnAmbushVehicles()
    {
        if (ambushVehiclePrefab == null)
        {
            Log(
                "SPAWN FAILED | Vehicle Prefab NULL");
            return;
        }

        if (spawnPoints == null ||
            spawnPoints.Length == 0)
        {
            Log(
                "SPAWN FAILED | No Spawn Points");
            return;
        }

        int candidateCount =
            BuildCandidatePointBuffer();

        if (candidateCount <= 0)
        {
            if (triggerPointIndex < 0)
            {
                Log(
                    "SPAWN FAILED | No valid point");
                return;
            }

            if (candidatePointIndices == null ||
                candidatePointIndices.Length == 0)
            {
                Log(
                    "SPAWN FAILED | Candidate buffer invalid");
                return;
            }

            candidatePointIndices[0] =
                triggerPointIndex;

            candidateCount = 1;
        }

        ShuffleCandidatePoints(
            candidateCount);

        int requestedCount =
            Random.Range(
                minVehicles,
                maxVehicles + 1);

        int spawnCount =
            Mathf.Min(
                requestedCount,
                candidateCount);

        Log(
            "SPAWN | Vehicles = " +
            spawnCount +
            " | Candidates = " +
            candidateCount);

        for (int i = 0;
             i < spawnCount;
             i++)
        {
            int pointIndex =
                candidatePointIndices[i];

            // Chia khoảng tốc độ thành các vùng để mỗi Alley có tốc độ khác nhau
            // nhưng vẫn nằm trong cùng một giới hạn có thể né.
            float speed = GetUniqueSpawnSpeed(i, spawnCount);
            SpawnVehicle(pointIndex, speed);
        }

        Log(
            "AMBUSH COMPLETE | Spawned = " +
            spawnCount);
    }

    // ============================================================
    // BUILD CANDIDATES
    // ============================================================

    private int BuildCandidatePointBuffer()
    {
        if (spawnPoints == null ||
            spawnPoints.Length == 0)
        {
            return 0;
        }

        if (candidatePointIndices == null ||
            candidatePointIndices.Length <
            spawnPoints.Length)
        {
            candidatePointIndices =
                new int[spawnPoints.Length];
        }

        int count = 0;

        for (int i = 0;
             i < spawnPoints.Length;
             i++)
        {
            if (spawnPoints[i].point == null)
                continue;

            candidatePointIndices[count] =
                i;

            count++;
        }

        return count;
    }

    // ============================================================
    // SHUFFLE
    // ============================================================

    private void ShuffleCandidatePoints(
        int count)
    {
        for (int i = count - 1;
             i > 0;
             i--)
        {
            int randomIndex =
                Random.Range(
                    0,
                    i + 1);

            int temp =
                candidatePointIndices[i];

            candidatePointIndices[i] =
                candidatePointIndices[randomIndex];

            candidatePointIndices[randomIndex] =
                temp;
        }
    }

    // ============================================================
    // SPAWN VEHICLE
    // ============================================================

    private void SpawnVehicle(
        int pointIndex,
        float vehicleSpeed)
    {
        if (pointIndex < 0 ||
            pointIndex >= spawnPoints.Length)
        {
            return;
        }

        SpawnPointElement element =
            spawnPoints[pointIndex];

        if (element.point == null)
            return;

        Vector3 position =
            element.point.position;

        Vector3 direction;

        if (autoDirectionToRoad)
        {
            direction =
                GetDirectionToRoad(
                    element.point);
        }
        else
        {
            direction =
                element.direction;

            direction.y = 0f;

            if (direction.sqrMagnitude <
                0.0001f)
            {
                direction =
                    element.point.forward;

                direction.y = 0f;
            }
        }

        if (direction.sqrMagnitude <
            0.0001f)
        {
            direction =
                Vector3.forward;
        }
        else
        {
            direction.Normalize();
        }

        Quaternion rotation =
            Quaternion.LookRotation(
                direction,
                Vector3.up);

        GameObject vehicle =
            Instantiate(
                ambushVehiclePrefab,
                position,
                rotation);

        if (vehicle == null)
            return;

        AlleyAmbushVehicle ambushVehicle =
            vehicle.GetComponent<AlleyAmbushVehicle>();

        if (ambushVehicle == null)
        {
            ambushVehicle =
                vehicle.GetComponentInChildren<
                    AlleyAmbushVehicle>();
        }

        if (ambushVehicle != null)
        {
            ambushVehicle.Initialize(
                direction,
                vehicleSpeed);
        }

        Log(
            "VEHICLE SPAWNED | Point = " +
            pointIndex +
            " | Speed = " +
            vehicleSpeed.ToString("F2") +
            " | Position = " +
            position);
    }

    private float GetUniqueSpawnSpeed(
        int spawnIndex,
        int spawnCount)
    {
        float minSpeed = Mathf.Max(0f, minAmbushVehicleSpeed);
        float maxSpeed = Mathf.Max(minSpeed, maxAmbushVehicleSpeed);

        if (spawnCount <= 1 || maxSpeed <= minSpeed)
            return minSpeed;

        // Mỗi Alley nhận một "speed band" riêng. Random.Value chỉ
        // jitter trong band đó nên các xe vẫn có tốc độ khác nhau
        // mà không vượt Min/Max.
        float bandSize = 1f / spawnCount;
        float t = (spawnIndex + Random.value) * bandSize;
        t = Mathf.Clamp01(t);

        return Mathf.Lerp(minSpeed, maxSpeed, t);
    }

    // ============================================================
    // DIRECTION
    // ============================================================

    private Vector3 GetDirectionToRoad(
        Transform point)
    {
        if (point == null)
            return Vector3.forward;

        Vector3 target =
            point.position;

        target.x =
            roadCenterX;

        Vector3 direction =
            target -
            point.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <
            0.0001f)
        {
            direction =
                point.forward;

            direction.y = 0f;
        }

        if (direction.sqrMagnitude <
            0.0001f)
        {
            direction =
                Vector3.forward;
        }

        return direction.normalized;
    }

    // ============================================================
    // TRIGGER POINT
    // ============================================================

    private int FindClosestTriggerPoint()
    {
        if (playerTransform == null ||
            spawnPoints == null)
        {
            return -1;
        }

        float bestDistance =
            float.MaxValue;

        int bestIndex = -1;

        for (int i = 0;
             i < spawnPoints.Length;
             i++)
        {
            Transform point =
                spawnPoints[i].point;

            if (point == null)
                continue;

            float distance =
                HorizontalDistanceSqr(
                    playerTransform.position,
                    point.position);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    // ============================================================
    // PLAYER
    // ============================================================

    private void FindPlayer()
    {
        if (playerTransform != null)
            return;

        GameObject player =
            GameObject.FindGameObjectWithTag(
                playerTag);

        if (player != null)
        {
            playerTransform =
                player.transform;
        }
    }

    // ============================================================
    // AUDIO
    // ============================================================

    private void PlayRandomRevSound()
    {
        if (ambushAudioSource == null)
            return;

        if (ambushRevSounds == null ||
            ambushRevSounds.Length == 0)
        {
            return;
        }

        int index =
            Random.Range(
                0,
                ambushRevSounds.Length);

        AudioClip clip =
            ambushRevSounds[index];

        if (clip == null)
            return;

        ambushAudioSource.PlayOneShot(
            clip,
            revVolume);

        Log(
            "AUDIO | REV");
    }

    private void PlayRandomLaunchSound()
    {
        if (ambushAudioSource == null)
            return;

        if (ambushLaunchSounds == null ||
            ambushLaunchSounds.Length == 0)
        {
            return;
        }

        int index =
            Random.Range(
                0,
                ambushLaunchSounds.Length);

        AudioClip clip =
            ambushLaunchSounds[index];

        if (clip == null)
            return;

        ambushAudioSource.PlayOneShot(
            clip,
            launchVolume);

        Log(
            "AUDIO | LAUNCH");
    }

    // ============================================================
    // MATH
    // ============================================================

    private static float HorizontalDistanceSqr(
        Vector3 a,
        Vector3 b)
    {
        float dx =
            a.x - b.x;

        float dz =
            a.z - b.z;

        return
            dx * dx +
            dz * dz;
    }

    // ============================================================
    // GIZMOS
    // ============================================================

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos)
            return;

        if (spawnPoints == null)
            return;

        for (int i = 0;
             i < spawnPoints.Length;
             i++)
        {
            SpawnPointElement element =
                spawnPoints[i];

            if (element.point == null)
                continue;

            Gizmos.DrawWireSphere(
                element.point.position,
                0.5f);

            Vector3 direction;

            if (autoDirectionToRoad)
            {
                direction =
                    GetDirectionToRoad(
                        element.point);
            }
            else
            {
                direction =
                    element.direction;

                direction.y = 0f;

                if (direction.sqrMagnitude <
                    0.0001f)
                {
                    direction =
                        element.point.forward;

                    direction.y = 0f;
                }
            }

            if (direction.sqrMagnitude <
                0.0001f)
                continue;

            direction.Normalize();

            Gizmos.DrawLine(
                element.point.position,
                element.point.position +
                direction * 3f);
        }

        if (playerTransform != null &&
            triggerDistance > 0f)
        {
            Gizmos.DrawWireSphere(
                playerTransform.position,
                triggerDistance);
        }
    }

    // ============================================================
    // DEBUG
    // ============================================================

    private void Log(string message)
    {
        if (!debugLogs)
            return;

        Debug.Log(
            "[AlleyBiomeSpawner] " +
            message,
            this);
    }
}