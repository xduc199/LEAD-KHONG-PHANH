using UnityEngine;

public class LaunchCleanup : MonoBehaviour
{
    private Rigidbody targetRigidbody;

    private float destroyTime;

    private bool initialized;


    //=========================================================
    // INITIALIZE
    //=========================================================

    public void Initialize(
        Rigidbody rigidbodyTarget,
        float lifetime
    )
    {
        targetRigidbody =
            rigidbodyTarget;

        destroyTime =
            Time.time +
            Mathf.Max(
                0.1f,
                lifetime
            );

        initialized =
            true;
    }


    //=========================================================
    // UPDATE
    //=========================================================

    private void Update()
    {
        if (!initialized)
            return;


        if (Time.time >= destroyTime)
        {
            Destroy(
                gameObject
            );
        }
    }
}
