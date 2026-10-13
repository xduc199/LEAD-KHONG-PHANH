using UnityEngine;

public sealed class CowChunkWalker : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.5f;

    [Tooltip("Khoảng cách bò được phép di chuyển tính từ vị trí ban đầu.")]
    [SerializeField] private float travelDistance = 8f;

    [Header("Direction")]
    [SerializeField] private bool moveAlongLocalZ = true;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [SerializeField] private string animationStateName = "Armature|ArmatureAction";

    private Vector3 startLocalPosition;
    private float direction = 1f;

    private int animationStateHash;

    private void Awake()
    {
        startLocalPosition = transform.localPosition;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        animationStateHash = Animator.StringToHash(animationStateName);
    }

    private void Start()
    {
        if (animator != null)
        {
            animator.Play(animationStateHash, 0, 0f);
        }
    }

    private void Update()
    {
        Move();
    }

    private void Move()
    {
        Vector3 position = transform.localPosition;

        float axis = moveAlongLocalZ
            ? position.z
            : position.x;

        float startAxis = moveAlongLocalZ
            ? startLocalPosition.z
            : startLocalPosition.x;

        float distance = axis - startAxis;

        if (direction > 0f && distance >= travelDistance)
        {
            direction = -1f;
            TurnAround();
        }
        else if (direction < 0f && distance <= -travelDistance)
        {
            direction = 1f;
            TurnAround();
        }

        float delta = moveSpeed * Time.deltaTime * direction;

        if (moveAlongLocalZ)
            position.z += delta;
        else
            position.x += delta;

        transform.localPosition = position;
    }

    private void TurnAround()
    {
        Vector3 rotation = transform.localEulerAngles;
        rotation.y += 180f;
        transform.localEulerAngles = rotation;
    }
}