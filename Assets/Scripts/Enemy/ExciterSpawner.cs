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

    private float nextSpawnTime;
    private bool isSpawning = false;
    private GameObject warningVisualObj;
    private Renderer warningRenderer;

    private void Start()
    {
        FindPlayer();
        ScheduleNextSpawn();
    }

    private void Update()
    {
        if (playerTransform == null) { FindPlayer(); return; }

        // Vạch đỏ bám theo làn đường (X) và tiến phía trước mặt Player
        if (isSpawning && warningVisualObj != null)
        {
            float pX = playerTransform.position.x;
            float pY = playerTransform.position.y + 0.2f;
            float centerZ = playerTransform.position.z + 10f;
            
            warningVisualObj.transform.position = new Vector3(pX, pY, centerZ);
        }

        if (Time.time >= nextSpawnTime && !isSpawning)
        {
            StartCoroutine(SpawnExciterSequence());
        }
    }

    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;
    }

    private void ScheduleNextSpawn()
    {
        nextSpawnTime = Time.time + Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    private IEnumerator SpawnExciterSequence()
    {
        isSpawning = true;

        // 1. TẠO VẠCH ĐỎ CẢNH BÁO (Thu nhỏ bề ngang còn 1.2m - bằng đúng bề ngang xe)
        if (playerTransform != null)
        {
            warningVisualObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            warningVisualObj.name = "WarningRedLine";
            Destroy(warningVisualObj.GetComponent<Collider>());

            // Kích thước: Rộng 1.2m (bằng xe), dày 0.08m, dài 60m
            warningVisualObj.transform.localScale = new Vector3(1.2f, 0.08f, 60f);
            
            warningRenderer = warningVisualObj.GetComponent<Renderer>();
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Unlit/Color");
            Material redMat = new Material(unlitShader);
            redMat.color = Color.red;
            if (redMat.HasProperty("_BaseColor")) redMat.SetColor("_BaseColor", Color.red);
            warningRenderer.material = redMat;
        }

        // 2. ĐẾM NGƯỢC VÀ CHỚP TẮT KHI XE SẮP LAO TỚI
        float timer = 0f;
        while (timer < warningDuration)
        {
            timer += Time.deltaTime;

            // 0.8 giây cuối chớp tắt liên tục
            if (timer > warningDuration - 0.8f && warningRenderer != null)
            {
                bool isVisible = Mathf.FloorToInt(Time.time * 10f) % 2 == 0;
                warningRenderer.enabled = isVisible;
            }

            yield return null;
        }

        if (warningVisualObj != null)
        {
            Destroy(warningVisualObj);
        }

        // 3. SPAWN EXCITER ĐÚNG VỊ TRÍ X CỦA PLAYER
        if (exciterPrefab != null && playerTransform != null)
        {
            float exactTargetX = playerTransform.position.x;
            float startZ = playerTransform.position.z - spawnDistanceBehind;
            float endZ = playerTransform.position.z + 100f;
            float spawnY = playerTransform.position.y + 0.8f;

            List<Vector3> exciterPath = new List<Vector3>();
            for (float z = startZ; z <= endZ; z += 5f)
            {
                exciterPath.Add(new Vector3(exactTargetX, spawnY, z));
            }

            GameObject newExciter = Instantiate(exciterPrefab, exciterPath[0], Quaternion.identity);
            ExciterController exciterScript = newExciter.GetComponent<ExciterController>();
            if (exciterScript != null) exciterScript.SetPath(exciterPath, playerTransform);
        }

        isSpawning = false;
        ScheduleNextSpawn();
    }
}