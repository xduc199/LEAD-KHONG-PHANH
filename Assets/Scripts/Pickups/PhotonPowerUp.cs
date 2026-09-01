using UnityEngine;

public class PhotonPowerUp : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 100f;

    private void Update()
    {
        transform.Rotate(
            0f,
            rotateSpeed * Time.deltaTime,
            0f,
            Space.World
        );
    }
}