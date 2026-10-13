using UnityEngine;

[DisallowMultipleComponent]
public sealed class RoadChunk : MonoBehaviour
{
    [Header("ROAD CHUNK")]
    [Tooltip("Chiều dài gameplay thực tế của toàn bộ chunk theo trục Z. Pivot của prefab nằm ở giữa chunk.")]
    [Min(1f)]
    [SerializeField] private float length = 180f;

    public float Length => length;

    /// <summary>
    /// Z bắt đầu thực tế của chunk.
    /// Pivot của prefab nằm ở giữa chunk.
    /// </summary>
    public float StartZ =>
        transform.position.z - (length * 0.5f);

    /// <summary>
    /// Z kết thúc thực tế của chunk.
    /// Pivot của prefab nằm ở giữa chunk.
    /// </summary>
    public float EndZ =>
        transform.position.z + (length * 0.5f);

    private void OnValidate()
    {
        if (length < 1f)
        {
            length = 1f;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 center = transform.position;

        Vector3 start =
            center - Vector3.forward * (length * 0.5f);

        Vector3 end =
            center + Vector3.forward * (length * 0.5f);

        Gizmos.DrawLine(start, end);

        Vector3 leftStart =
            start + Vector3.left * 2f;

        Vector3 rightStart =
            start + Vector3.right * 2f;

        Vector3 leftEnd =
            end + Vector3.left * 2f;

        Vector3 rightEnd =
            end + Vector3.right * 2f;

        Gizmos.DrawLine(leftStart, rightStart);
        Gizmos.DrawLine(leftEnd, rightEnd);
    }
#endif
}
